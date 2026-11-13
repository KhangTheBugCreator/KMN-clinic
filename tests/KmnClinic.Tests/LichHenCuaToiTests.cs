using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M5: khách hàng đặt, xem, tự hủy lịch hẹn. Mỗi test tạo ca riêng (CT...), lịch làm việc tạm (LT...)
/// và khách tạm (qua API giả lập), xóa hết ở cuối.
/// </summary>
public class LichHenCuaToiTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public LichHenCuaToiTests(TestApp app) => _app = app;

    private static async Task<object?> Sql(string sql, params (string, object)[] ps)
    {
        await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return await cmd.ExecuteScalarAsync();
    }

    private static string Ngay(int sauSoNgay) => DateTime.Today.AddDays(sauSoNgay).ToString("yyyy-MM-dd");

    private HttpClient VoiToken(string token)
    {
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    /// <summary>Tạo khách tạm qua API giả lập; trả (client của khách, mã khách).</summary>
    private async Task<(HttpClient, string)> KhachTam()
    {
        var sdt = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
            new { hoTen = "KT Khách tạm", soDienThoai = sdt, ngaySinh = "1990-01-01", gioiTinh = "Nam", diaChi = "Vĩnh Long" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var p = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (VoiToken(p.GetProperty("token").GetString()!), p.GetProperty("nguoiDung").GetProperty("ma").GetString()!);
    }

    private async Task<HttpClient> NhanVien(string ma)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai = "NV", ma });
        return VoiToken((await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!);
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

    private static async Task Don(string[] cacCa, params string[] cacKhach)
    {
        foreach (var kh in cacKhach)
        {
            await Sql("DELETE FROM Lich_hen WHERE ma_kh = @k", ("@k", kh));
            await Sql("DELETE FROM Khach_hang WHERE ma_kh = @k", ("@k", kh));
        }
        foreach (var ca in cacCa)
        {
            await Sql("DELETE h FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", ca));
        }
    }

    private static async Task<string> Loi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    [Fact]
    public async Task Dat_lich_xem_va_tu_huy()
    {
        var ca = await TaoCa("19:00", "20:00");
        var (kh, maKh) = await KhachTam();
        try
        {
            var lich = await TaoLich(Ngay(2), ca);
            var res = await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich, trieuChungBanDau = "  Ho, sốt nhẹ  " });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var h = await res.Content.ReadFromJsonAsync<JsonElement>();
            var ma = h.GetProperty("maLichHen").GetString()!;
            Assert.Matches("^LH\\d{4}$", ma);
            Assert.Equal("Đã đặt", h.GetProperty("trangThai").GetString());
            Assert.Equal("Ho, sốt nhẹ", h.GetProperty("trieuChungBanDau").GetString());
            Assert.Equal("Phòng 101", h.GetProperty("tenPhong").GetString());
            Assert.True(h.GetProperty("coTheHuy").GetBoolean());

            // Trong database: đúng khách, chưa có lễ tân (RB14, CK_LICH_HEN_le_tan)
            Assert.Equal(maKh, await Sql("SELECT ma_kh FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", ma)));
            Assert.Equal(DBNull.Value, await Sql("SELECT ma_nv FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", ma)));

            var ds = await kh.GetFromJsonAsync<JsonElement>("/api/lich-hen-cua-toi");
            Assert.Single(ds.EnumerateArray());

            var huy = await kh.PostAsync($"/api/lich-hen-cua-toi/{ma}/huy", null);
            Assert.Equal(HttpStatusCode.OK, huy.StatusCode);
            var sau = await huy.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Đã hủy", sau.GetProperty("trangThai").GetString());
            Assert.False(sau.GetProperty("coTheHuy").GetBoolean());

            // Hủy lần nữa: báo đã hủy
            Assert.Equal(HttpStatusCode.Conflict, (await kh.PostAsync($"/api/lich-hen-cua-toi/{ma}/huy", null)).StatusCode);

            // Đã hủy rồi thì đặt lại cùng ca được (Q6 chỉ tính lịch còn hiệu lực)
            Assert.Equal(HttpStatusCode.OK, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich })).StatusCode);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Khong_dat_trung_ca_nhung_duoc_dat_bac_si_khac_cung_ca()
    {
        var ca = await TaoCa("19:00", "20:00");
        var (kh, maKh) = await KhachTam();
        try
        {
            var lichA = await TaoLich(Ngay(3), ca, "NV0003", "P0001");
            var lichB = await TaoLich(Ngay(3), ca, "NV0004", "P0003");
            Assert.Equal(HttpStatusCode.OK, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lichA })).StatusCode);

            var trung = await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lichA });
            Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
            Assert.Contains("đã có lịch hẹn", await Loi(trung));

            // Q6: hai bác sĩ cùng ca là hai lịch hẹn riêng
            Assert.Equal(HttpStatusCode.OK, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lichB })).StatusCode);

            // Lễ tân đã xác nhận vẫn tính là còn hiệu lực
            await Sql("UPDATE Lich_hen SET trang_thai = N'Đã xác nhận', ma_nv = 'NV0002' WHERE ma_kh = @k AND ma_lich_lam_viec = @l", ("@k", maKh), ("@l", lichA));
            Assert.Equal(HttpStatusCode.Conflict, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lichA })).StatusCode);
            Assert.Equal(2, (int)(await Sql("SELECT COUNT(*) FROM Lich_hen WHERE ma_kh = @k", ("@k", maKh)))!);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Hai_lan_bam_cung_luc_chi_tao_mot_lich_hen()
    {
        var ca = await TaoCa("19:00", "20:00");
        var (kh, maKh) = await KhachTam();
        try
        {
            var lich = await TaoLich(Ngay(4), ca);
            var kq = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich })));
            Assert.Single(kq, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Equal(1, (int)(await Sql("SELECT COUNT(*) FROM Lich_hen WHERE ma_kh = @k", ("@k", maKh)))!);
        }
        finally { await Don(new[] { ca }, maKh); }
    }

    [Fact]
    public async Task Nhieu_khach_dat_cung_luc_deu_thanh_cong()
    {
        var ca = await TaoCa("19:00", "20:00");
        var khach = new List<(HttpClient C, string Ma)>();
        try
        {
            for (int i = 0; i < 4; i++) khach.Add(await KhachTam());
            var lichA = await TaoLich(Ngay(5), ca, "NV0003", "P0001");
            var lichB = await TaoLich(Ngay(5), ca, "NV0004", "P0003");
            var kq = await Task.WhenAll(khach.Select((k, i) =>
                k.C.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = i % 2 == 0 ? lichA : lichB })));
            Assert.All(kq, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            var ma = await Task.WhenAll(kq.Select(async r => (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maLichHen").GetString()));
            Assert.Equal(4, ma.Distinct().Count());
        }
        finally { await Don(new[] { ca }, khach.Select(k => k.Ma).ToArray()); }
    }

    [Fact]
    public async Task Khong_dat_ca_da_bat_dau_va_lich_khong_ton_tai()
    {
        var caQua = await TaoCa("00:00", "00:01");
        var (kh, maKh) = await KhachTam();
        try
        {
            var lich = await TaoLich(Ngay(0), caQua);
            var res = await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("RB16", await Loi(res));

            Assert.Equal(HttpStatusCode.NotFound, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = "KHONGCO" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi",
                new { maLichLamViec = lich, trieuChungBanDau = new string('a', 256) })).StatusCode);
        }
        finally { await Don(new[] { caQua }, maKh); }
    }

    [Fact]
    public async Task Khong_tu_huy_khi_ca_da_bat_dau_hoac_da_kham()
    {
        var caQua = await TaoCa("00:00", "00:01");
        var caSau = await TaoCa("19:00", "20:00");
        var (kh, maKh) = await KhachTam();
        try
        {
            // Lịch hẹn đặt từ trước cho ca hôm nay 00:00 (thêm thẳng, ngay_gio_dat hôm qua)
            var lichQua = await TaoLich(Ngay(0), caQua);
            await Sql(@"INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trang_thai, ma_kh, ma_lich_lam_viec)
                        VALUES ('LT0000001', DATEADD(day, -1, GETDATE()), N'Đã đặt', @k, @l)", ("@k", maKh), ("@l", lichQua));
            var res = await kh.PostAsync("/api/lich-hen-cua-toi/LT0000001/huy", null);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("đã bắt đầu", await Loi(res));
            var ct = await kh.GetFromJsonAsync<JsonElement>("/api/lich-hen-cua-toi/LT0000001");
            Assert.False(ct.GetProperty("coTheHuy").GetBoolean());

            // Đã khám: không hủy
            var lichSau = await TaoLich(Ngay(1), caSau);
            var h = await (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lichSau })).Content.ReadFromJsonAsync<JsonElement>();
            var ma = h.GetProperty("maLichHen").GetString();
            await Sql("UPDATE Lich_hen SET trang_thai = N'Đã xác nhận', ma_nv = 'NV0002' WHERE ma_lich_hen = @m", ("@m", ma!));
            await Sql("UPDATE Lich_hen SET trang_thai = N'Đã khám' WHERE ma_lich_hen = @m", ("@m", ma!));
            Assert.Equal(HttpStatusCode.BadRequest, (await kh.PostAsync($"/api/lich-hen-cua-toi/{ma}/huy", null)).StatusCode);
        }
        finally { await Don(new[] { caQua, caSau }, maKh); }
    }

    [Fact]
    public async Task Khong_xem_hay_huy_lich_hen_cua_nguoi_khac()
    {
        var ca = await TaoCa("19:00", "20:00");
        var (khA, maA) = await KhachTam();
        var (khB, maB) = await KhachTam();
        try
        {
            var lich = await TaoLich(Ngay(2), ca);
            var h = await (await khA.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich })).Content.ReadFromJsonAsync<JsonElement>();
            var ma = h.GetProperty("maLichHen").GetString();

            Assert.Equal(HttpStatusCode.NotFound, (await khB.GetAsync($"/api/lich-hen-cua-toi/{ma}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await khB.PostAsync($"/api/lich-hen-cua-toi/{ma}/huy", null)).StatusCode);
            Assert.Empty((await khB.GetFromJsonAsync<JsonElement>("/api/lich-hen-cua-toi")).EnumerateArray());
            Assert.Equal("Đã đặt", await Sql("SELECT trang_thai FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", ma!)));

            // Gửi kèm maKh của người khác cũng bị bỏ qua: lịch hẹn vẫn thuộc khách B
            var res = await khB.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich, maKh = maA });
            var cuaB = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maLichHen").GetString();
            Assert.Equal(maB, await Sql("SELECT ma_kh FROM Lich_hen WHERE ma_lich_hen = @m", ("@m", cuaB!)));
        }
        finally { await Don(new[] { ca }, maA, maB); }
    }

    [Fact]
    public async Task Chi_khach_hang_moi_dung_duoc()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.CreateClient().GetAsync("/api/lich-hen-cua-toi")).StatusCode);
        var leTan = await NhanVien("NV0002");
        Assert.Equal(HttpStatusCode.Forbidden, (await leTan.GetAsync("/api/lich-hen-cua-toi")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await leTan.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = "LV0001" })).StatusCode);
    }
}
