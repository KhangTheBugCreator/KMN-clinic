using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M6: lễ tân xác nhận, hủy, tiếp nhận có hẹn và vãng lai. Test tạo ca riêng (CT...), lịch làm việc tạm (LT...),
/// khách tạm; xóa hồ sơ, lịch hẹn, lịch, ca, khách ở cuối.
/// </summary>
public class LeTanTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public LeTanTests(TestApp app) => _app = app;

    private static Task<object?> Sql(string sql, params (string, object)[] ps) => TestApp.SqlAsync(sql, ps);

    private static string Ngay(int sauSoNgay) => DateTime.Today.AddDays(sauSoNgay).ToString("yyyy-MM-dd");

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
    private Task<HttpClient> LeTan() => NhanVien("NV0002");

    private async Task<(HttpClient, string)> KhachTam()
    {
        var sdt = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
            new { hoTen = "KT Khách tạm", soDienThoai = sdt, ngaySinh = "1990-01-01", gioiTinh = "Nữ" });
        var p = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (VoiToken(p.GetProperty("token").GetString()!), p.GetProperty("nguoiDung").GetProperty("ma").GetString()!);
    }

    private static async Task<string> TaoCa(string tu, string den)
    {
        var ma = "CT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@m, N'KT ca', @t, @d)", ("@m", ma), ("@t", tu), ("@d", den));
        return ma;
    }

    private static async Task<string> TaoLich(string ngay, string maCa, string maNv = "NV0003", string maPhong = "P0001")
    {
        var ma = "LT" + Random.Shared.Next(10_000_000, 99_999_999);
        await Sql("INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca) VALUES (@m, @n, @nv, @p, @c)",
            ("@m", ma), ("@n", ngay), ("@nv", maNv), ("@p", maPhong), ("@c", maCa));
        return ma;
    }

    /// <summary>Lịch hẹn đặt từ hôm qua (bỏ qua RB16 của API khách) cho lịch làm việc bất kỳ.</summary>
    private static async Task<string> TaoLichHen(string maKh, string maLich, string? trieuChung = "Ho, sốt")
    {
        var ma = "LT" + Random.Shared.Next(1_000_000, 9_999_999);
        await Sql(@"INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trieu_chung_ban_dau, trang_thai, ma_kh, ma_lich_lam_viec)
                    VALUES (@m, DATEADD(day, -1, GETDATE()), @tc, N'Đã đặt', @k, @l)",
            ("@m", ma), ("@tc", (object?)trieuChung ?? DBNull.Value), ("@k", maKh), ("@l", maLich));
        return ma;
    }

    private static async Task Don(string[] cacCa, params string[] cacKhach)
    {
        foreach (var kh in cacKhach)
        {
            await Sql("DELETE FROM Ho_so_kham WHERE ma_kh = @k", ("@k", kh));
            await Sql("DELETE FROM Lich_hen WHERE ma_kh = @k", ("@k", kh));
            await Sql("DELETE FROM Khach_hang WHERE ma_kh = @k", ("@k", kh));
        }
        foreach (var ca in cacCa)
        {
            await Sql("DELETE hs FROM Ho_so_kham hs JOIN Lich_hen h ON h.ma_lich_hen = hs.ma_lich_hen JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ma_ca = @c", ("@c", ca));
            await Sql("DELETE h FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", ca));
        }
    }

    private static async Task<string> Loi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static object TrieuChung(string s = "Ho khan 3 ngày") => new { trieuChung = s };

    [Fact]
    public async Task Xac_nhan_roi_tiep_nhan_co_hen()
    {
        var lt = await LeTan();
        var ca = await TaoCa("23:58", "23:59"); // ca tối nay: chưa kết thúc, cùng ngày
        var (_, maKh) = await KhachTam();
        try
        {
            var lich = await TaoLich(Ngay(0), ca, "NV0004", "P0003"); // NV0003 để test công khai kiểm tra "không có ca hôm nay"
            var lh = await TaoLichHen(maKh, lich);

            var xn = await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/xac-nhan", null);
            Assert.Equal(HttpStatusCode.OK, xn.StatusCode);
            var h = await xn.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Đã xác nhận", h.GetProperty("trangThai").GetString());
            Assert.Equal("NV0002", h.GetProperty("maNvXacNhan").GetString()); // truy vết lễ tân xác nhận
            Assert.Equal(HttpStatusCode.Conflict, (await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/xac-nhan", null)).StatusCode);

            var tn = await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", TrieuChung("  Ho khan, sốt nhẹ  "));
            Assert.Equal(HttpStatusCode.OK, tn.StatusCode);
            var hs = await tn.Content.ReadFromJsonAsync<JsonElement>();
            var maHs = hs.GetProperty("maHoSo").GetString()!;
            Assert.Matches("^HS\\d{4}$", maHs);
            Assert.Equal(lh, hs.GetProperty("maLichHen").GetString());
            Assert.Equal("NV0004", hs.GetProperty("maNvKham").GetString());
            Assert.Equal("NV0002", hs.GetProperty("maNvTiepNhan").GetString());
            Assert.True(hs.GetProperty("choKham").GetBoolean());
            Assert.Equal("Ho khan, sốt nhẹ", hs.GetProperty("trieuChung").GetString());

            // Trigger chuyển lịch hẹn sang Đã khám; tiền công 0 chờ bác sĩ
            Assert.Equal("Đã khám", await Sql("SELECT trang_thai FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", lh)));
            Assert.Equal(0m, await Sql("SELECT tien_cong_kham FROM Ho_so_kham WHERE ma_ho_so = @m", ("@m", maHs)));
            var ct = await lt.GetFromJsonAsync<JsonElement>($"/api/le-tan/lich-hen/{lh}");
            Assert.Equal(maHs, ct.GetProperty("maHoSo").GetString());

            // Tiếp nhận lần hai / hủy lịch đã khám đều bị chặn
            Assert.Equal(HttpStatusCode.Conflict, (await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", TrieuChung())).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/huy", null)).StatusCode);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Lich_da_dat_xac_nhan_va_tiep_nhan_mot_lan_bam()
    {
        var lt = await LeTan();
        var ca = await TaoCa("23:58", "23:59");
        var (_, maKh) = await KhachTam();
        try
        {
            var lh = await TaoLichHen(maKh, await TaoLich(Ngay(0), ca, "NV0004", "P0003"));
            var tn = await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", TrieuChung());
            Assert.Equal(HttpStatusCode.OK, tn.StatusCode);
            Assert.Equal("Đã khám", await Sql("SELECT trang_thai FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", lh)));
            Assert.Equal("NV0002", await Sql("SELECT ma_nv FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", lh)));
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Chi_tiep_nhan_dung_ngay_hen_va_bat_buoc_trieu_chung()
    {
        var lt = await LeTan();
        var ca = await TaoCa("19:00", "20:00");
        var (_, maKh) = await KhachTam();
        try
        {
            var lh = await TaoLichHen(maKh, await TaoLich(Ngay(2), ca));
            var res = await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", TrieuChung());
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("đúng ngày hẹn", await Loi(res));
            Assert.Equal("Đã đặt", await Sql("SELECT trang_thai FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", lh))); // không xác nhận dở dang

            Assert.Equal(HttpStatusCode.BadRequest, (await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", TrieuChung("   "))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await lt.PostAsJsonAsync("/api/le-tan/lich-hen/KHONGCO/tiep-nhan", TrieuChung())).StatusCode);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Huy_lich_hen_va_khong_xac_nhan_khi_ca_da_ket_thuc()
    {
        var lt = await LeTan();
        var caQua = await TaoCa("00:00", "00:01");
        var caSau = await TaoCa("19:00", "20:00");
        var (_, maKh) = await KhachTam();
        try
        {
            var lhQua = await TaoLichHen(maKh, await TaoLich(Ngay(0), caQua));
            var res = await lt.PostAsync($"/api/le-tan/lich-hen/{lhQua}/xac-nhan", null);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("đã kết thúc", await Loi(res));

            var lh = await TaoLichHen(maKh, await TaoLich(Ngay(1), caSau));
            Assert.Equal(HttpStatusCode.OK, (await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/xac-nhan", null)).StatusCode);
            var huy = await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/huy", null);
            Assert.Equal(HttpStatusCode.OK, huy.StatusCode);
            var h = await huy.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Đã hủy", h.GetProperty("trangThai").GetString());
            Assert.Equal("NV0002", h.GetProperty("maNvXacNhan").GetString()); // giữ người đã xác nhận
            Assert.Equal(HttpStatusCode.Conflict, (await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/huy", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await lt.PostAsync($"/api/le-tan/lich-hen/{lh}/xac-nhan", null)).StatusCode);
        }
        finally { await Don(new[] { caQua, caSau }, maKh); }
    }

    [Fact]
    public async Task Tiep_nhan_vang_lai_chi_voi_bac_si_dang_truc()
    {
        var lt = await LeTan();
        var now = DateTime.Now.TimeOfDay;
        // Ca bao trùm giờ hiện tại (đang trực) và ca tối muộn chưa tới (ngoài 30 phút)
        var tu = now.Add(TimeSpan.FromMinutes(-10)); var den = now.Add(TimeSpan.FromMinutes(10));
        if (tu < TimeSpan.Zero || den >= TimeSpan.FromHours(24) || now > new TimeSpan(22, 0, 0)) return; // gần nửa đêm: bỏ qua
        var caTruc = await TaoCa(tu.ToString(@"hh\:mm\:ss"), den.ToString(@"hh\:mm\:ss"));
        var caMuon = await TaoCa("23:58", "23:59");
        var (_, maKh) = await KhachTam();
        try
        {
            await TaoLich(Ngay(0), caTruc, "NV0003", "P0001");
            await TaoLich(Ngay(0), caMuon, "NV0004", "P0003");

            var truc = await lt.GetFromJsonAsync<JsonElement>("/api/le-tan/bac-si-dang-truc");
            Assert.Contains(truc.EnumerateArray(), b => b.GetProperty("maNv").GetString() == "NV0003");
            Assert.DoesNotContain(truc.EnumerateArray(), b => b.GetProperty("maNv").GetString() == "NV0004");

            var res = await lt.PostAsJsonAsync("/api/le-tan/tiep-nhan-vang-lai", new { maKh, maNvKham = "NV0004", trieuChung = "Đau bụng" });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("RB41", await Loi(res));

            var ok = await lt.PostAsJsonAsync("/api/le-tan/tiep-nhan-vang-lai", new { maKh, maNvKham = "NV0003", trieuChung = "Đau bụng" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            var hs = await ok.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, hs.GetProperty("maLichHen").ValueKind);
            Assert.Equal("NV0002", hs.GetProperty("maNvTiepNhan").GetString());

            var dsHs = await lt.GetFromJsonAsync<JsonElement>("/api/le-tan/ho-so");
            Assert.Contains(dsHs.EnumerateArray(), x => x.GetProperty("maHoSo").GetString() == hs.GetProperty("maHoSo").GetString());
            var truc2 = await lt.GetFromJsonAsync<JsonElement>("/api/le-tan/bac-si-dang-truc");
            Assert.True(truc2.EnumerateArray().Single(b => b.GetProperty("maNv").GetString() == "NV0003").GetProperty("soChoKham").GetInt32() >= 1);

            Assert.Equal(HttpStatusCode.NotFound, (await lt.PostAsJsonAsync("/api/le-tan/tiep-nhan-vang-lai",
                new { maKh = "KHONGCO", maNvKham = "NV0003", trieuChung = "Đau bụng" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await lt.PostAsJsonAsync("/api/le-tan/tiep-nhan-vang-lai",
                new { maKh, maNvKham = "NV0003", trieuChung = "" })).StatusCode);
        }
        finally { await Don(new[] { caTruc, caMuon }, maKh); }
    }

    [Fact]
    public async Task Loc_lich_hen_va_tong_quan()
    {
        var lt = await LeTan();
        var ca = await TaoCa("19:00", "20:00");
        var (_, maKh) = await KhachTam();
        try
        {
            var lh = await TaoLichHen(maKh, await TaoLich(Ngay(3), ca));
            var kq = await lt.GetFromJsonAsync<JsonElement>($"/api/le-tan/lich-hen?ngay={Ngay(3)}&trangThai=Đã đặt&q={lh}");
            Assert.Equal(1, kq.GetProperty("total").GetInt32());
            var h = kq.GetProperty("items")[0];
            Assert.Equal("KT Khách tạm", h.GetProperty("tenKhach").GetString());
            Assert.Equal("Ho, sốt", h.GetProperty("trieuChungBanDau").GetString());

            Assert.Equal(0, (await lt.GetFromJsonAsync<JsonElement>($"/api/le-tan/lich-hen?ngay={Ngay(3)}&trangThai=Đã hủy&q={lh}")).GetProperty("total").GetInt32());
            Assert.Equal(0, (await lt.GetFromJsonAsync<JsonElement>($"/api/le-tan/lich-hen?ngay={Ngay(3)}&maNv=NV0004&q={lh}")).GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.BadRequest, (await lt.GetAsync("/api/le-tan/lich-hen?trangThai=abc")).StatusCode);

            var t = await lt.GetFromJsonAsync<JsonElement>("/api/le-tan/tong-quan");
            Assert.True(t.GetProperty("choXacNhan").GetInt32() >= 1);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Chi_le_tan_dung_duoc()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.CreateClient().GetAsync("/api/le-tan/lich-hen")).StatusCode);
        foreach (var ma in new[] { "NV0001", "NV0003", "NV0005" })
        {
            var c = await NhanVien(ma);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/le-tan/lich-hen")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/le-tan/tiep-nhan-vang-lai",
                new { maKh = "KH0001", maNvKham = "NV0003", trieuChung = "x" })).StatusCode);
        }
        var (kh, maKh) = await KhachTam();
        try { Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/le-tan/tong-quan")).StatusCode); }
        finally { await Don(Array.Empty<string>(), maKh); }
    }
}
