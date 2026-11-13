using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M4: API website công khai. Không cần đăng nhập, không lộ thông tin nội bộ, chỉ trả ca còn đặt được (RB16).
/// Test tạo ca riêng (mã CT + ngẫu nhiên) và lịch tạm (mã LT...), xóa ở cuối.
/// </summary>
[Collection("ThongKe")] // chạy riêng: đếm khoa, bác sĩ trong lúc test danh mục, nhân sự chạy song song thêm bản ghi tạm sẽ lệch số
public class CongKhaiTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public CongKhaiTests(TestApp app) => _app = app;

    private static Task<object?> Sql(string sql, params (string, object)[] ps) => TestApp.SqlAsync(sql, ps);

    private static string Ngay(int sauSoNgay) => DateTime.Today.AddDays(sauSoNgay).ToString("yyyy-MM-dd");

    private static async Task<string> TaoCa(string tu, string den)
    {
        var ma = "CT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@m, N'KT ca', @t, @d)", ("@m", ma), ("@t", tu), ("@d", den));
        return ma;
    }

    /// <summary>Thêm thẳng lịch làm việc (bỏ qua quy tắc "từ ngày mai" của backend) để thử ca hôm nay.</summary>
    private static async Task<string> TaoLich(string ngay, string maCa, string maNv = "NV0003", string maPhong = "P0001")
    {
        var ma = "LT" + Random.Shared.Next(10_000_000, 99_999_999);
        await Sql("INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca) VALUES (@m, @n, @nv, @p, @c)",
            ("@m", ma), ("@n", ngay), ("@nv", maNv), ("@p", maPhong), ("@c", maCa));
        return ma;
    }

    private static async Task Don(params string[] cacCa)
    {
        foreach (var ca in cacCa)
        {
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", ca));
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", ca));
        }
    }

    private async Task<JsonElement> Get(string url)
    {
        var res = await _app.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [InlineData("/api/cong-khai/tong-quan")]
    [InlineData("/api/cong-khai/khoa")]
    [InlineData("/api/cong-khai/bac-si")]
    [InlineData("/api/cong-khai/bac-si/NV0003")]
    [InlineData("/api/cong-khai/lich-kham")]
    public async Task Khong_can_dang_nhap_va_khong_lo_thong_tin_noi_bo(string url)
    {
        var ca = await TaoCa("19:00", "20:00");
        try
        {
            await TaoLich(Ngay(1), ca);
            var res = await _app.CreateClient().GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var json = (await res.Content.ReadAsStringAsync()).ToLowerInvariant();
            foreach (var cam in new[] { "sdt", "dienthoai", "matkhau", "mat_khau", "ngaysinh", "solichhen", "chucvu", "pbkdf2" })
                Assert.DoesNotContain(cam, json);
        }
        finally { await Don(ca); }
    }

    [Fact]
    public async Task Chi_tra_ca_con_dat_duoc()
    {
        // Ca đã bắt đầu hôm nay (00:00) không được hiện; ca tối nay (23:58) và ca ngày mai được hiện.
        var caQua = await TaoCa("00:00", "00:01");
        var caToi = await TaoCa("23:58", "23:59");
        try
        {
            var lQua = await TaoLich(Ngay(0), caQua);
            var lToi = await TaoLich(Ngay(0), caToi);
            var lMai = await TaoLich(Ngay(1), caQua);

            var ma = (await Get("/api/cong-khai/lich-kham?maNv=NV0003&soNgay=2")).EnumerateArray()
                .Select(l => l.GetProperty("maLichLamViec").GetString()).ToList();
            Assert.DoesNotContain(lQua, ma);
            if (DateTime.Now.TimeOfDay < new TimeSpan(23, 58, 0)) Assert.Contains(lToi, ma);
            Assert.Contains(lMai, ma);

            var bs = await Get("/api/cong-khai/bac-si/NV0003");
            var lich = bs.GetProperty("lichSapToi").EnumerateArray().Select(l => l.GetProperty("maLichLamViec").GetString()).ToList();
            Assert.DoesNotContain(lQua, lich);
            Assert.Contains(lMai, lich);
            var mot = bs.GetProperty("lichSapToi").EnumerateArray().First(l => l.GetProperty("maLichLamViec").GetString() == lMai);
            Assert.Equal("Phòng 101", mot.GetProperty("tenPhong").GetString());
            Assert.Equal("00:00", mot.GetProperty("gioBatDau").GetString());

            // Lọc theo ngày hôm nay: bác sĩ chỉ có ca đã bắt đầu thì không được tính là "có lịch"
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec = @m", ("@m", lToi));
            var homNay = await Get($"/api/cong-khai/bac-si?ngay={Ngay(0)}&q=Bác sĩ Mẫu A");
            Assert.DoesNotContain(homNay.GetProperty("items").EnumerateArray(), b => b.GetProperty("maNv").GetString() == "NV0003");
        }
        finally { await Don(caQua, caToi); }
    }

    [Fact]
    public async Task Loc_bac_si_theo_khoa_ten_ngay()
    {
        var ca = await TaoCa("19:00", "20:00");
        try
        {
            var l = await TaoLich(Ngay(3), ca);

            var theoKhoa = await Get("/api/cong-khai/bac-si?maKhoa=K0002");
            Assert.All(theoKhoa.GetProperty("items").EnumerateArray(), b => Assert.Equal("K0002", b.GetProperty("maKhoa").GetString()));
            Assert.Contains(theoKhoa.GetProperty("items").EnumerateArray(), b => b.GetProperty("maNv").GetString() == "NV0004");

            // Chỉ bác sĩ: tìm "Mẫu" không ra quản trị viên, lễ tân, thu ngân
            var theoTen = await Get("/api/cong-khai/bac-si?q=Mẫu");
            Assert.All(theoTen.GetProperty("items").EnumerateArray(), b => Assert.StartsWith("Bác sĩ", b.GetProperty("hoTen").GetString()));

            var theoNgay = await Get($"/api/cong-khai/bac-si?ngay={Ngay(3)}");
            var nv3 = theoNgay.GetProperty("items").EnumerateArray().Single(b => b.GetProperty("maNv").GetString() == "NV0003");
            Assert.All(nv3.GetProperty("lichSapToi").EnumerateArray(), x => Assert.Equal(Ngay(3), x.GetProperty("ngay").GetString()));
            Assert.Contains(nv3.GetProperty("lichSapToi").EnumerateArray(), x => x.GetProperty("maLichLamViec").GetString() == l);

            // Bác sĩ có lịch xếp trước bác sĩ chưa có lịch
            var tatCa = (await Get("/api/cong-khai/bac-si")).GetProperty("items").EnumerateArray().ToList();
            var coLich = tatCa.Select(b => b.GetProperty("lichSapToi").GetArrayLength() > 0).ToList();
            Assert.Equal(coLich.OrderByDescending(x => x), coLich);

            var lichKhoa = await Get("/api/cong-khai/lich-kham?maKhoa=K0002&soNgay=7");
            Assert.All(lichKhoa.EnumerateArray(), x => Assert.Equal("K0002", x.GetProperty("maKhoa").GetString()));
        }
        finally { await Don(ca); }
    }

    [Theory]
    [InlineData("/api/cong-khai/lich-kham?soNgay=15")]
    [InlineData("/api/cong-khai/lich-kham?soNgay=0")]
    public async Task Tham_so_sai_tra_400(string url)
        => Assert.Equal(HttpStatusCode.BadRequest, (await _app.CreateClient().GetAsync(url)).StatusCode);

    [Fact]
    public async Task Ngay_loc_ngoai_14_ngay_toi_tra_400()
    {
        var c = _app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/cong-khai/bac-si?ngay={Ngay(-1)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/cong-khai/bac-si?ngay={Ngay(14)}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/cong-khai/bac-si?ngay={Ngay(13)}")).StatusCode);
    }

    [Fact]
    public async Task Chi_tiet_chi_danh_cho_bac_si()
    {
        var c = _app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/cong-khai/bac-si/NV0001")).StatusCode); // quản trị viên
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/cong-khai/bac-si/KHONGCO")).StatusCode);
    }

    [Fact]
    public async Task Tong_quan_va_khoa_dem_dung()
    {
        var t = await Get("/api/cong-khai/tong-quan");
        Assert.Equal((int)(await Sql("SELECT COUNT(*) FROM Khoa"))!, t.GetProperty("soKhoa").GetInt32());
        Assert.Equal((int)(await Sql("SELECT COUNT(*) FROM Nhan_vien WHERE chuc_vu = N'Bác sĩ'"))!, t.GetProperty("soBacSi").GetInt32());

        var k1 = (await Get("/api/cong-khai/khoa")).EnumerateArray().Single(k => k.GetProperty("maKhoa").GetString() == "K0001");
        Assert.Equal((int)(await Sql("SELECT COUNT(*) FROM Nhan_vien WHERE chuc_vu = N'Bác sĩ' AND ma_khoa = 'K0001'"))!, k1.GetProperty("soBacSi").GetInt32());
        Assert.Contains("Phòng 101", k1.GetProperty("cacPhong").EnumerateArray().Select(p => p.GetString()));
    }
}
