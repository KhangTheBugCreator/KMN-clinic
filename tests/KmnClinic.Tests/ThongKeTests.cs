using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>Chạy riêng, không song song với test khác, để số liệu đếm được so khớp chính xác với truy vấn SQL kiểm tra tay.</summary>
[CollectionDefinition("ThongKe", DisableParallelization = true)]
public class ThongKeCollection { }

/// <summary>
/// M9: thống kê và báo cáo. Tạo 2 hóa đơn trên hồ sơ tạm (1 đã thu, 1 chờ thu) rồi so từng số liệu API với truy vấn SQL viết tay.
/// </summary>
[Collection("ThongKe")]
public class ThongKeTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public ThongKeTests(TestApp app) => _app = app;

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

    private static async Task<decimal> SoTien(string sql, params (string, object)[] ps) => Convert.ToDecimal(await Sql(sql, ps));
    private static async Task<int> Dem(string sql, params (string, object)[] ps) => Convert.ToInt32(await Sql(sql, ps));

    [Fact]
    public async Task So_lieu_khop_truy_van_sql()
    {
        var qtv = await NhanVien("NV0001");
        var tn = await NhanVien("NV0005");
        var ca = await TaoCaDaQua("NV0004", "P0003");
        var thuoc = await TaoThuoc(3000, 15); // tồn 15 ≤ ngưỡng 20 → sắp hết
        var (_, maKh) = await KhachTam();
        try
        {
            var hd1 = await TaoHoaDon(maKh, thuoc, 4, 120000);
            await TaoHoaDon(maKh, thuoc, 2, 80000); // chờ thu
            await Json(await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd1}/thanh-toan", null));

            var d = DateTime.Today.ToString("yyyy-MM-dd");
            var homNay = DateTime.Today; var mai = homNay.AddDays(1);
            var tq = await qtv.GetFromJsonAsync<JsonElement>($"/api/thong-ke/tong-quan?tuNgay={d}&denNgay={d}");
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Nhan_vien"), tq.GetProperty("soNhanVien").GetInt32());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Khach_hang"), tq.GetProperty("soKhachHang").GetInt32());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Ho_so_kham WHERE CONVERT(date, ngay_gio_kham) = CONVERT(date, GETDATE())"), tq.GetProperty("hoSoTrongKy").GetInt32());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Ho_so_kham WHERE CONVERT(date, ngay_gio_kham) = CONVERT(date, GETDATE()) AND ma_lich_hen IS NULL"), tq.GetProperty("vangLaiTrongKy").GetInt32());
            var doanhThuSql = await SoTien("SELECT ISNULL(SUM(tong_tien), 0) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Đã thanh toán' AND CONVERT(date, ngay_gio_thanh_toan) = CONVERT(date, GETDATE())");
            Assert.Equal(doanhThuSql, tq.GetProperty("doanhThu").GetDecimal());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Chưa thanh toán'"), tq.GetProperty("hoaDonChoThu").GetInt32());
            Assert.Equal(await SoTien("SELECT ISNULL(SUM(tong_tien), 0) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Chưa thanh toán'"), tq.GetProperty("tienChoThu").GetDecimal());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Thuoc WHERE so_luong_ton <= 20"), tq.GetProperty("thuocSapHet").GetInt32());
            Assert.True(tq.GetProperty("doanhThu").GetDecimal() >= 132000); // hóa đơn vừa thu: 120.000 + 4 × 3.000

            var dt = await qtv.GetFromJsonAsync<JsonElement>($"/api/thong-ke/doanh-thu?tuNgay={d}&denNgay={d}");
            Assert.Equal(doanhThuSql, dt[0].GetProperty("doanhThu").GetDecimal());

            var lk = (await qtv.GetFromJsonAsync<JsonElement>($"/api/thong-ke/luot-kham?tuNgay={d}&denNgay={d}"))[0];
            Assert.Equal(tq.GetProperty("vangLaiTrongKy").GetInt32(), lk.GetProperty("vangLai").GetInt32());
            Assert.Equal(tq.GetProperty("hoSoTrongKy").GetInt32(), lk.GetProperty("vangLai").GetInt32() + lk.GetProperty("coHen").GetInt32());

            // Thuốc kê nhiều: chỉ tính đơn đã thanh toán (4, không cộng 2 của hóa đơn chờ thu)
            var keNhieu = await qtv.GetFromJsonAsync<JsonElement>($"/api/thong-ke/thuoc-ke-nhieu?tuNgay={d}&denNgay={d}&top=50");
            Assert.Equal(4, keNhieu.EnumerateArray().Single(x => x.GetProperty("maThuoc").GetString() == thuoc).GetProperty("soLuong").GetInt32());

            // Báo cáo doanh thu: từng dòng công khám + thuốc = tổng; tổng các dòng = doanh thu
            var bc = await qtv.GetFromJsonAsync<JsonElement>($"/api/bao-cao/doanh-thu?tuNgay={d}&denNgay={d}");
            var dong = bc.GetProperty("dong").EnumerateArray().ToList();
            Assert.All(dong, x => Assert.Equal(x.GetProperty("tongTien").GetDecimal(), x.GetProperty("tienCongKham").GetDecimal() + x.GetProperty("tienThuoc").GetDecimal()));
            Assert.Equal(doanhThuSql, dong.Sum(x => x.GetProperty("tongTien").GetDecimal()));
            var cua = dong.Single(x => x.GetProperty("maHoaDon").GetString() == hd1);
            Assert.Equal(120000, cua.GetProperty("tienCongKham").GetDecimal());
            Assert.Equal(12000, cua.GetProperty("tienThuoc").GetDecimal());
            Assert.Equal("Thu ngân Mẫu", cua.GetProperty("tenThuNgan").GetString());

            var bcLk = await qtv.GetFromJsonAsync<JsonElement>($"/api/bao-cao/luot-kham?tuNgay={d}&denNgay={d}");
            Assert.Equal(tq.GetProperty("hoSoTrongKy").GetInt32(), bcLk.GetProperty("dong").EnumerateArray().Sum(x => x.GetProperty("tong").GetInt32()));

            var bcLh = await qtv.GetFromJsonAsync<JsonElement>($"/api/bao-cao/lich-hen?tuNgay={d}&denNgay={d}");
            Assert.Equal(tq.GetProperty("lichHenTrongKy").GetInt32(), bcLh.GetProperty("dong").GetArrayLength());

            var sd = await qtv.GetFromJsonAsync<JsonElement>($"/api/bao-cao/su-dung-thuoc?tuNgay={d}&denNgay={d}");
            var sdThuoc = sd.GetProperty("dong").EnumerateArray().Single(x => x.GetProperty("maThuoc").GetString() == thuoc);
            Assert.Equal(4, sdThuoc.GetProperty("tongSoLuong").GetInt32());
            Assert.Equal(12000, sdThuoc.GetProperty("thanhTien").GetDecimal());

            // Tồn kho: kho đã trừ 4 lúc thu (15 → 11), cảnh báo sắp hết
            var tk = await qtv.GetFromJsonAsync<JsonElement>("/api/bao-cao/ton-kho");
            var tkThuoc = tk.GetProperty("dong").EnumerateArray().Single(x => x.GetProperty("maThuoc").GetString() == thuoc);
            Assert.Equal(11, tkThuoc.GetProperty("soLuongTon").GetInt32());
            Assert.Equal("Sắp hết", tkThuoc.GetProperty("canhBao").GetString());
            Assert.Equal(await Dem("SELECT COUNT(*) FROM Thuoc"), tk.GetProperty("dong").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, tk.GetProperty("tuNgay").ValueKind);
        }
        finally { await Don(new[] { ca }, new[] { thuoc }, maKh); }
    }

    [Fact]
    public async Task Doanh_thu_theo_thang_va_ky_mac_dinh()
    {
        var qtv = await NhanVien("NV0001");
        var thang = await qtv.GetFromJsonAsync<JsonElement>("/api/thong-ke/doanh-thu?kieu=thang&tuNgay=2026-01-15&denNgay=2026-03-02");
        Assert.Equal(new[] { "2026-01", "2026-02", "2026-03" }, thang.EnumerateArray().Select(x => x.GetProperty("nhan").GetString()));
        var ngay = await qtv.GetFromJsonAsync<JsonElement>("/api/thong-ke/doanh-thu?tuNgay=2026-02-27&denNgay=2026-03-02");
        Assert.Equal(new[] { "2026-02-27", "2026-02-28", "2026-03-01", "2026-03-02" }, ngay.EnumerateArray().Select(x => x.GetProperty("nhan").GetString()));
        Assert.Equal(4, (await qtv.GetFromJsonAsync<JsonElement>("/api/thong-ke/luot-kham?tuNgay=2026-02-27&denNgay=2026-03-02")).GetArrayLength());

        // Bỏ trống kỳ: tháng hiện tại
        var tq = await qtv.GetFromJsonAsync<JsonElement>("/api/thong-ke/tong-quan");
        var dau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Assert.Equal(dau.ToString("yyyy-MM-dd"), tq.GetProperty("tuNgay").GetString());
        Assert.Equal(dau.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd"), tq.GetProperty("denNgay").GetString());
        var lh = await qtv.GetFromJsonAsync<JsonElement>("/api/thong-ke/lich-hen-theo-trang-thai");
        Assert.Equal(4, lh.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("/api/thong-ke/doanh-thu?kieu=nam")]
    [InlineData("/api/thong-ke/tong-quan?tuNgay=2026-11-10&denNgay=2026-11-01")]
    [InlineData("/api/thong-ke/luot-kham?tuNgay=2025-01-01&denNgay=2026-01-02")]
    [InlineData("/api/bao-cao/doanh-thu?tuNgay=2025-01-01&denNgay=2026-12-31")]
    public async Task Tham_so_sai_tra_400(string url)
        => Assert.Equal(HttpStatusCode.BadRequest, (await (await NhanVien("NV0001")).GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("NV0002")]
    [InlineData("NV0003")]
    [InlineData("NV0005")]
    public async Task Chi_quan_tri_vien_vao_duoc(string maNv)
    {
        var c = await NhanVien(maNv);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/thong-ke/tong-quan")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/bao-cao/doanh-thu")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.CreateClient().GetAsync("/api/bao-cao/ton-kho")).StatusCode);
    }
}
