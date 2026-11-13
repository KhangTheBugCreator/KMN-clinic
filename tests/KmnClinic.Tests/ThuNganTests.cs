using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M8: thu ngân xác nhận thanh toán. Hóa đơn được tạo qua API bác sĩ (M7) trên hồ sơ tạm (giống BacSiTests),
/// dùng thuốc tạm để việc trừ kho không đụng thuốc mẫu; xóa hết ở cuối.
/// </summary>
public class ThuNganTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public ThuNganTests(TestApp app) => _app = app;

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

    /// <summary>Hồ sơ tạm có 1 thuốc tạm (số lượng <paramref name="ke"/>), bác sĩ NV0004 hoàn tất → trả mã hóa đơn.</summary>
    private async Task<string> TaoHoaDon(string maKh, string thuoc, int ke, decimal tienCong = 100000)
    {
        var bs = await NhanVien("NV0004");
        var hs = await TaoHoSo(maKh, "NV0004");
        await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = thuoc, soLuong = ke, lieuDung = "Ngày 2 lần" }));
        var xong = await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat",
            new { trieuChung = "Ho", chanDoanBenh = "Viêm họng", loiDan = (string?)null, tienCongKham = tienCong }));
        return xong.GetProperty("hoaDon").GetProperty("maHoaDon").GetString()!;
    }

    private static async Task<int> Ton(string thuoc) => (int)(await Sql("SELECT so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc)))!;

    [Fact]
    public async Task Thanh_toan_ghi_nguoi_thu_va_tru_kho()
    {
        var tn = await NhanVien("NV0005");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(2500, 10);
        var (_, maKh) = await KhachTam();
        try
        {
            var hd = await TaoHoaDon(maKh, thuoc, 4);
            var cho = await tn.GetFromJsonAsync<JsonElement>($"/api/thu-ngan/hoa-don?trangThai=Chưa thanh toán&q={hd}");
            var dong = cho.GetProperty("items").EnumerateArray().Single();
            Assert.Equal(110000, dong.GetProperty("tongTien").GetDecimal());
            Assert.Equal(1, dong.GetProperty("soThuoc").GetInt32());

            var ct = await tn.GetFromJsonAsync<JsonElement>($"/api/thu-ngan/hoa-don/{hd}");
            Assert.Equal(100000, ct.GetProperty("tienCongKham").GetDecimal());
            Assert.Equal(10000, ct.GetProperty("tienThuoc").GetDecimal());
            Assert.Equal("Viêm họng", ct.GetProperty("chanDoanBenh").GetString());
            Assert.Equal(JsonValueKind.Null, ct.GetProperty("maNvThu").ValueKind);

            var truoc = DateTime.Now.AddSeconds(-2);
            var tt = await Json(await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null));
            Assert.Equal("Đã thanh toán", tt.GetProperty("trangThai").GetString());
            Assert.Equal("NV0005", tt.GetProperty("maNvThu").GetString());
            Assert.True(tt.GetProperty("ngayGioThanhToan").GetDateTime() >= truoc);
            Assert.Equal(110000, tt.GetProperty("tongTien").GetDecimal()); // tổng không đổi sau thanh toán
            Assert.Equal(6, await Ton(thuoc)); // RB33

            // Bấm lại: 409, kho không bị trừ lần nữa
            var lai = await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null);
            Assert.Equal(HttpStatusCode.Conflict, lai.StatusCode);
            Assert.Contains("đã được thanh toán", await Loi(lai));
            Assert.Equal(6, await Ton(thuoc));

            var cuaToi = await tn.GetFromJsonAsync<JsonElement>($"/api/thu-ngan/hoa-don?cuaToi=true&trangThai=Đã thanh toán&q={hd}");
            Assert.Equal(1, cuaToi.GetProperty("total").GetInt32());
            var homNay = DateTime.Today.ToString("yyyy-MM-dd");
            Assert.Equal(1, (await tn.GetFromJsonAsync<JsonElement>($"/api/thu-ngan/hoa-don?tuNgay={homNay}&denNgay={homNay}&q={hd}")).GetProperty("total").GetInt32());
            var tq = await tn.GetFromJsonAsync<JsonElement>("/api/thu-ngan/tong-quan");
            Assert.True(tq.GetProperty("daThuHomNay").GetInt32() >= 1);
            Assert.True(tq.GetProperty("doanhThuHomNay").GetDecimal() >= 110000);
            Assert.True(tq.GetProperty("theoGio")[DateTime.Now.Hour].GetInt32() >= 1 || DateTime.Now.Hour != tt.GetProperty("ngayGioThanhToan").GetDateTime().Hour);

            // Bác sĩ thấy hồ sơ chuyển sang Đã thanh toán
            var bs = await NhanVien("NV0004");
            var hs = tt.GetProperty("maHoSo").GetString();
            Assert.Equal("Đã thanh toán", (await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so/{hs}")).GetProperty("trangThai").GetString());
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Fact]
    public async Task Hai_lan_bam_cung_luc_chi_thu_mot_lan()
    {
        var tn = await NhanVien("NV0005");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(1000, 10);
        var (_, maKh) = await KhachTam();
        try
        {
            var hd = await TaoHoaDon(maKh, thuoc, 3);
            var kq = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null)));
            Assert.Equal(1, kq.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(3, kq.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.Equal(7, await Ton(thuoc));
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Fact]
    public async Task Thieu_ton_luc_thanh_toan_bao_ro_va_khong_doi_gi()
    {
        var tn = await NhanVien("NV0005");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(1000, 10);
        var (_, maKh) = await KhachTam();
        try
        {
            var hd = await TaoHoaDon(maKh, thuoc, 4);
            await Sql("UPDATE Thuoc SET so_luong_ton = 2 WHERE ma_thuoc = @t", ("@t", thuoc));
            var res = await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("cần 4, còn 2", await Loi(res));
            Assert.Equal(2, await Ton(thuoc));
            Assert.Equal("Chưa thanh toán", (await tn.GetFromJsonAsync<JsonElement>($"/api/thu-ngan/hoa-don/{hd}")).GetProperty("trangThai").GetString());
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Theory]
    [InlineData("NV0001")]
    [InlineData("NV0002")]
    [InlineData("NV0004")]
    public async Task Chi_thu_ngan_vao_duoc(string maNv)
    {
        var c = await NhanVien(maNv);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/thu-ngan/hoa-don")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync("/api/thu-ngan/hoa-don/HD0001/thanh-toan", null)).StatusCode);
    }

    [Fact]
    public async Task Khach_an_danh_va_du_lieu_sai()
    {
        var (kh, maKh) = await KhachTam();
        try
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/thu-ngan/tong-quan")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _app.CreateClient().GetAsync("/api/thu-ngan/tong-quan")).StatusCode);
            var tn = await NhanVien("NV0005");
            Assert.Equal(HttpStatusCode.NotFound, (await tn.GetAsync("/api/thu-ngan/hoa-don/KHONGCO")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await tn.PostAsync("/api/thu-ngan/hoa-don/KHONGCO/thanh-toan", null)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await tn.GetAsync("/api/thu-ngan/hoa-don?trangThai=abc")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await tn.GetAsync("/api/thu-ngan/hoa-don?tuNgay=2026-11-10&denNgay=2026-11-01")).StatusCode);
        }
        finally { await Don(Array.Empty<string>(), Array.Empty<string>(), maKh); }
    }
}
