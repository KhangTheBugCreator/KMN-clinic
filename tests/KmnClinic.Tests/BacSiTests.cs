using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M7: bác sĩ khám, kê đơn, hoàn tất tạo hóa đơn, lịch sử. Test tạo ca đã qua hôm nay (00:30–00:31, mã CT...) cho NV0004,
/// hồ sơ vãng lai thêm thẳng bằng SQL (mã LT...), thuốc tạm và khách tạm; xóa hết ở cuối.
/// Dùng ca đã qua để không ảnh hưởng test "bác sĩ đang trực" và test công khai.
/// </summary>
public class BacSiTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public BacSiTests(TestApp app) => _app = app;

    private static Task<object?> Sql(string sql, params (string, object)[] ps) => TestApp.SqlAsync(sql, ps);

    private HttpClient VoiToken(string token)
    {
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<HttpClient> NhanVien(string ma)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai = "NV", ma });
        return VoiToken((await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!);
    }

    private async Task<(HttpClient, string)> KhachTam()
    {
        var sdt = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
            new { hoTen = "KT Khách tạm", soDienThoai = sdt, ngaySinh = "1990-01-01", gioiTinh = "Nam" });
        var p = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (VoiToken(p.GetProperty("token").GetString()!), p.GetProperty("nguoiDung").GetProperty("ma").GetString()!);
    }

    /// <summary>Ca 00:30–00:31 hôm nay (không dùng ca bắt đầu 00:00 vì RB41 trừ 30 phút sẽ quay vòng sang 23:30) cho bác sĩ: đã kết thúc nên không ai đặt hay nhận khách được nữa.</summary>
    private static async Task<string> TaoCaDaQua(string maNv, string maPhong)
    {
        var ca = "CT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@m, N'KT ca', '00:30', '00:31')", ("@m", ca));
        await Sql("INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca) VALUES (@m, @n, @nv, @p, @c)",
            ("@m", "LT" + Random.Shared.Next(10_000_000, 99_999_999)), ("@n", DateTime.Today), ("@nv", maNv), ("@p", maPhong), ("@c", ca));
        return ca;
    }

    /// <summary>Hồ sơ vãng lai lúc 00:30:xx hôm nay (trong ca đã qua, thỏa RB41), lễ tân NV0002.</summary>
    private static async Task<string> TaoHoSo(string maKh, string maBs, int giay = 30)
    {
        var ma = "LT" + Random.Shared.Next(1_000_000, 9_999_999);
        await Sql(@"INSERT INTO Ho_so_kham (ma_ho_so, ngay_gio_kham, trieu_chung, chan_doan_benh, loi_dan, tien_cong_kham, ma_lich_hen, ma_kh, ma_nv_kham, ma_nv_tiep_nhan)
                    VALUES (@m, @g, N'Đau đầu, chóng mặt', NULL, NULL, 0, NULL, @k, @bs, 'NV0002')",
            ("@m", ma), ("@g", DateTime.Today.AddMinutes(30).AddSeconds(giay)), ("@k", maKh), ("@bs", maBs));
        return ma;
    }

    private static async Task<string> TaoThuoc(decimal gia, int ton)
    {
        var ma = "LT" + Random.Shared.Next(1_000_000, 9_999_999);
        await Sql(@"INSERT INTO Thuoc (ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton, mo_ta)
                    VALUES (@m, @t, N'KT', 250, N'Viên', @g, @n, NULL)",
            ("@m", ma), ("@t", "KT thuốc " + ma), ("@g", gia), ("@n", ton));
        return ma;
    }

    private static async Task Don(string[] cacCa, string[] cacThuoc, params string[] cacKhach)
    {
        foreach (var kh in cacKhach)
        {
            // Hóa đơn trước (đơn thuốc của hóa đơn đã thanh toán bị trigger khóa), rồi đơn thuốc, hồ sơ, khách
            await Sql("DELETE d FROM Hoa_don d JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so WHERE hs.ma_kh = @k", ("@k", kh));
            await Sql("DELETE ct FROM Chi_tiet_don_thuoc ct JOIN Ho_so_kham hs ON hs.ma_ho_so = ct.ma_ho_so WHERE hs.ma_kh = @k", ("@k", kh));
            await Sql("DELETE FROM Ho_so_kham WHERE ma_kh = @k", ("@k", kh));
            await Sql("DELETE FROM Khach_hang WHERE ma_kh = @k", ("@k", kh));
        }
        foreach (var t in cacThuoc) await Sql("DELETE FROM Thuoc WHERE ma_thuoc = @t", ("@t", t));
        foreach (var ca in cacCa)
        {
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", ca));
        }
    }

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> Loi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static object Kham(string? chanDoan, decimal tien = 150000, string trieuChung = "Đau đầu, chóng mặt 2 ngày")
        => new { trieuChung, chanDoanBenh = chanDoan, loiDan = "Nghỉ ngơi, uống nhiều nước", tienCongKham = tien };

    [Fact]
    public async Task Kham_ke_don_hoan_tat_tao_hoa_don_va_khoa_ho_so()
    {
        var bs = await NhanVien("NV0004");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(1500, 10);
        var (_, maKh) = await KhachTam();
        try
        {
            var hs = await TaoHoSo(maKh, "NV0004");
            var cho = await bs.GetFromJsonAsync<JsonElement>("/api/bac-si/hang-cho");
            Assert.Equal("Chờ khám", cho.EnumerateArray().Single(x => x.GetProperty("maHoSo").GetString() == hs).GetProperty("trangThai").GetString());

            // Lưu nháp: chẩn đoán được để trống
            var nhap = await Json(await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("  ")));
            Assert.Equal(JsonValueKind.Null, nhap.GetProperty("chanDoanBenh").ValueKind);
            Assert.Equal(150000, nhap.GetProperty("tienCongKham").GetDecimal());
            Assert.Equal("Chờ khám", nhap.GetProperty("trangThai").GetString());

            // RB27: vượt tồn kho
            var vuot = await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = thuoc, soLuong = 11, lieuDung = "Ngày 2 lần" });
            Assert.Equal(HttpStatusCode.BadRequest, vuot.StatusCode);
            Assert.Contains("RB27", await Loi(vuot));

            // Kê đúng: đơn giá lấy từ danh mục (RB28)
            var ke = await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = thuoc, soLuong = 4, lieuDung = "Ngày 2 lần, mỗi lần 1 viên" }));
            var dong = ke.GetProperty("donThuoc").EnumerateArray().Single();
            Assert.Equal(1500, dong.GetProperty("donGia").GetDecimal());
            Assert.Equal("Đang khám", ke.GetProperty("trangThai").GetString());

            Assert.Equal(HttpStatusCode.Conflict, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc",
                new { maThuoc = thuoc, soLuong = 1, lieuDung = "x" })).StatusCode); // trùng thuốc
            Assert.Equal(HttpStatusCode.NotFound, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc",
                new { maThuoc = "KHONGCO", soLuong = 1, lieuDung = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc",
                new { maThuoc = thuoc, soLuong = 0, lieuDung = "x" })).StatusCode);

            // Giá danh mục đổi sau khi kê: dòng đã kê giữ giá cũ khi sửa số lượng
            await Sql("UPDATE Thuoc SET don_gia_thuoc = 2000 WHERE ma_thuoc = @t", ("@t", thuoc));
            var sua = await Json(await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}", new { soLuong = 6, lieuDung = "Ngày 3 lần" }));
            dong = sua.GetProperty("donThuoc").EnumerateArray().Single();
            Assert.Equal(1500, dong.GetProperty("donGia").GetDecimal());
            Assert.Equal(9000, dong.GetProperty("thanhTien").GetDecimal());
            Assert.Equal("Ngày 3 lần", dong.GetProperty("lieuDung").GetString());

            // RB22: hoàn tất phải có chẩn đoán
            var thieuCd = await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham(""));
            Assert.Equal(HttpStatusCode.BadRequest, thieuCd.StatusCode);
            Assert.Contains("RB22", await Loi(thieuCd));

            // Tồn kho giảm sau lúc kê: không cho hoàn tất, báo rõ thuốc nào
            await Sql("UPDATE Thuoc SET so_luong_ton = 5 WHERE ma_thuoc = @t", ("@t", thuoc));
            var thieuTon = await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham("Rối loạn tiền đình"));
            Assert.Equal(HttpStatusCode.BadRequest, thieuTon.StatusCode);
            Assert.Contains("còn 5", await Loi(thieuTon));
            Assert.Equal(0, (int)(await Sql("SELECT COUNT(*) FROM Hoa_don WHERE ma_ho_so = @h", ("@h", hs)))!);
            await Sql("UPDATE Thuoc SET so_luong_ton = 10 WHERE ma_thuoc = @t", ("@t", thuoc));

            // Hoàn tất: hóa đơn Chưa thanh toán, chưa có thu ngân, tổng = công khám + thuốc (RB29)
            var xong = await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham("  Rối loạn tiền đình ", 120000)));
            Assert.Equal("Hoàn tất", xong.GetProperty("trangThai").GetString());
            Assert.True(xong.GetProperty("daKhoa").GetBoolean());
            Assert.Equal("Rối loạn tiền đình", xong.GetProperty("chanDoanBenh").GetString());
            var hd = xong.GetProperty("hoaDon");
            Assert.Equal("Chưa thanh toán", hd.GetProperty("trangThai").GetString());
            Assert.Equal(120000 + 6 * 1500, hd.GetProperty("tongTien").GetDecimal());
            Assert.StartsWith("HD", hd.GetProperty("maHoaDon").GetString());
            Assert.Equal(1, (int)(await Sql("SELECT COUNT(*) FROM Hoa_don WHERE ma_ho_so = @h AND ma_nv IS NULL AND ngay_gio_thanh_toan IS NULL", ("@h", hs)))!);
            Assert.Equal(10, (int)(await Sql("SELECT so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc)))!); // kho chỉ trừ khi thanh toán

            // Q4: khóa toàn bộ
            Assert.Equal(HttpStatusCode.Conflict, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("Khác"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham("Khác"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}", new { soLuong = 1, lieuDung = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await bs.DeleteAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = "TH0001", soLuong = 1, lieuDung = "x" })).StatusCode);

            cho = await bs.GetFromJsonAsync<JsonElement>("/api/bac-si/hang-cho");
            Assert.DoesNotContain(cho.EnumerateArray(), x => x.GetProperty("maHoSo").GetString() == hs);
            var ls = await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so?trangThai=Hoàn tất&q={hs}");
            Assert.Equal(1, ls.GetProperty("total").GetInt32());
            Assert.Equal(129000, ls.GetProperty("items")[0].GetProperty("tongTien").GetDecimal());
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Fact]
    public async Task Xoa_thuoc_va_sua_dong_khong_co()
    {
        var bs = await NhanVien("NV0004");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(3000, 50);
        var (_, maKh) = await KhachTam();
        try
        {
            var hs = await TaoHoSo(maKh, "NV0004");
            await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = thuoc, soLuong = 2, lieuDung = "Sáng 1 viên" }));
            var xoa = await Json(await bs.DeleteAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}"));
            Assert.Equal(0, xoa.GetProperty("donThuoc").GetArrayLength());
            Assert.Equal("Chờ khám", xoa.GetProperty("trangThai").GetString());
            Assert.Equal(HttpStatusCode.NotFound, (await bs.DeleteAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc/{thuoc}", new { soLuong = 1, lieuDung = "x" })).StatusCode);

            // Không thuốc vẫn hoàn tất được: tổng = tiền công
            var xong = await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham("Cảm cúm", 0)));
            Assert.Equal(0, xong.GetProperty("hoaDon").GetProperty("tongTien").GetDecimal());
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Fact]
    public async Task Chi_thay_va_sua_ho_so_cua_minh()
    {
        var bs3 = await NhanVien("NV0003");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var (_, maKh) = await KhachTam();
        try
        {
            var hs = await TaoHoSo(maKh, "NV0004");
            Assert.Equal(HttpStatusCode.NotFound, (await bs3.GetAsync($"/api/bac-si/ho-so/{hs}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs3.GetAsync($"/api/bac-si/ho-so/{hs}/lan-kham-truoc")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs3.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("X"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs3.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = "TH0001", soLuong = 1, lieuDung = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs3.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat", Kham("X"))).StatusCode);
            Assert.DoesNotContain((await bs3.GetFromJsonAsync<JsonElement>("/api/bac-si/hang-cho")).EnumerateArray(),
                x => x.GetProperty("maHoSo").GetString() == hs);
            Assert.Equal(0, (await bs3.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so?q={hs}")).GetProperty("total").GetInt32());
            Assert.Equal(0, (int)(await Sql("SELECT COUNT(*) FROM Hoa_don WHERE ma_ho_so = @h", ("@h", hs)))!);
        }
        finally { await Don(new[] { ca }, Array.Empty<string>(), maKh); }
    }

    [Theory]
    [InlineData("NV0001")] // quản trị viên
    [InlineData("NV0002")] // lễ tân
    [InlineData("NV0005")] // thu ngân
    public async Task Chi_bac_si_vao_duoc(string maNv)
    {
        var c = await NhanVien(maNv);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/bac-si/hang-cho")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/bac-si/ho-so/HS0001/hoan-tat", Kham("X"))).StatusCode);
    }

    [Fact]
    public async Task Khach_hang_va_an_danh_khong_vao_duoc()
    {
        var (kh, maKh) = await KhachTam();
        try
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/bac-si/tong-quan")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _app.CreateClient().GetAsync("/api/bac-si/tong-quan")).StatusCode);
        }
        finally { await Don(Array.Empty<string>(), Array.Empty<string>(), maKh); }
    }

    [Fact]
    public async Task Xem_lan_kham_truoc_cua_benh_nhan()
    {
        var bs = await NhanVien("NV0004");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var (_, maKh) = await KhachTam();
        try
        {
            var cu = await TaoHoSo(maKh, "NV0004", 10);
            var dangMo = await TaoHoSo(maKh, "NV0004", 20); // chưa hoàn tất: không hiện ở lịch sử
            var moi = await TaoHoSo(maKh, "NV0004", 40);
            await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{cu}/don-thuoc", new { maThuoc = "TH0001", soLuong = 2, lieuDung = "Khi sốt" }));
            await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{cu}/hoan-tat", Kham("Viêm họng")));

            var ds = await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so/{moi}/lan-kham-truoc");
            var lan = ds.EnumerateArray().Single();
            Assert.Equal(cu, lan.GetProperty("maHoSo").GetString());
            Assert.Equal("Viêm họng", lan.GetProperty("chanDoanBenh").GetString());
            Assert.Contains("Paracetamol", lan.GetProperty("thuoc")[0].GetString());
            Assert.DoesNotContain(ds.EnumerateArray(), x => x.GetProperty("maHoSo").GetString() == dangMo);

            // Hồ sơ cũ không thấy lần khám sau nó
            Assert.Equal(0, (await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so/{cu}/lan-kham-truoc")).GetArrayLength());
        }
        finally { await Don(new[] { ca }, Array.Empty<string>(), maKh); }
    }

    [Fact]
    public async Task Tong_quan_va_du_lieu_sai()
    {
        var bs = await NhanVien("NV0004");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var (_, maKh) = await KhachTam();
        try
        {
            var hs = await TaoHoSo(maKh, "NV0004");
            var tq = await bs.GetFromJsonAsync<JsonElement>("/api/bac-si/tong-quan");
            Assert.True(tq.GetProperty("choKham").GetInt32() >= 1);
            Assert.True(tq.GetProperty("tongHoSoHomNay").GetInt32() >= 1);
            // Ca 00:30–00:31 đã qua: không phải ca hiện tại, không nằm trong lịch sắp tới
            Assert.DoesNotContain(tq.GetProperty("lichSapToi").EnumerateArray(), l => l.GetProperty("tenCa").GetString() == "KT ca" && l.GetProperty("gioBatDau").GetString() == "00:30:00");

            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("X", -1))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("X", 1.5m))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham("X", 0, " "))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PutAsJsonAsync($"/api/bac-si/ho-so/{hs}", Kham(new string('a', 256)))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = "TH0001", soLuong = 1, lieuDung = "  " })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.GetAsync("/api/bac-si/ho-so?trangThai=abc")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await bs.GetAsync("/api/bac-si/ho-so?tuNgay=2026-11-10&denNgay=2026-11-01")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await bs.GetAsync("/api/bac-si/ho-so/KHONGCO")).StatusCode);
        }
        finally { await Don(new[] { ca }, Array.Empty<string>(), maKh); }
    }
}
