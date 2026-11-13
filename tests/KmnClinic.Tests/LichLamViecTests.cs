using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M3: lịch làm việc. Mỗi test tạo một ca riêng (mã CT + ngẫu nhiên) nên không đụng dữ liệu khác,
/// rồi xóa lịch, lịch hẹn, ca, bác sĩ tạm ở cuối.
/// </summary>
public class LichLamViecTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public LichLamViecTests(TestApp app) => _app = app;

    private async Task<HttpClient> Client(string loai, string ma)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai, ma });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return c;
    }
    private Task<HttpClient> Qtv() => Client("NV", "NV0001");

    private static Task<object?> Sql(string sql, params (string, object)[] ps) => TestApp.SqlAsync(sql, ps);

    private static async Task<string> DocLoi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static string Ngay(int sauSoNgay) => DateTime.Today.AddDays(sauSoNgay).ToString("yyyy-MM-dd");

    /// <summary>Tạo ca riêng cho test; trả mã ca. Dọn bằng <see cref="Don"/>.</summary>
    private static async Task<string> TaoCa()
    {
        var ma = "CT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@m, N'KT ca', '19:00', '20:00')", ("@m", ma));
        return ma;
    }

    private static async Task Don(string maCa, string? maBsTam = null)
    {
        await Sql("DELETE h FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ma_ca = @c", ("@c", maCa));
        await Sql("DELETE FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", maCa));
        await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", maCa));
        if (maBsTam != null) await Sql("DELETE FROM Nhan_vien WHERE ma_nv = @n", ("@n", maBsTam));
    }

    private static object Lich(string ngay, string maNv, string maPhong, string maCa) => new { ngayLamViec = ngay, maNv, maPhong, maCa };

    private static async Task<int> DemLich(string maCa)
        => (int)(await Sql("SELECT COUNT(*) FROM Lich_lam_viec WHERE ma_ca = @c", ("@c", maCa)))!;

    [Fact]
    public async Task Them_lich_va_chan_trung_phong_trung_bac_si()
    {
        var qtv = await Qtv();
        var ca = await TaoCa();
        var bsTam = "NVKT" + Random.Shared.Next(1000, 9999);
        try
        {
            await Sql(@"INSERT INTO Nhan_vien (ma_nv, ho_ten_nv, sdt_nv, chuc_vu, mat_khau_nv, ma_khoa)
                        VALUES (@n, N'KT Bác sĩ tạm', @s, N'Bác sĩ', 'x', 'K0001')", ("@n", bsTam), ("@s", "07" + Random.Shared.Next(10_000_000, 99_999_999)));
            var ngay = Ngay(200);
            var res = await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, "NV0003", "P0001", ca));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var l = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Matches("^LV\\d{4}$", l.GetProperty("maLichLamViec").GetString());
            Assert.Equal("Phòng 101", l.GetProperty("tenPhong").GetString());
            Assert.Equal("19:00", l.GetProperty("gioBatDau").GetString());
            Assert.False(l.GetProperty("daQua").GetBoolean());

            var trungPhong = await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, bsTam, "P0001", ca));
            Assert.Equal(HttpStatusCode.Conflict, trungPhong.StatusCode);
            Assert.Contains("RB12", await DocLoi(trungPhong));

            var trungBs = await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, "NV0003", "P0002", ca));
            Assert.Equal(HttpStatusCode.Conflict, trungBs.StatusCode);
            Assert.Contains("RB13", await DocLoi(trungBs));

            // Cùng phòng nhưng ngày khác thì được
            Assert.Equal(HttpStatusCode.OK, (await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(Ngay(201), bsTam, "P0001", ca))).StatusCode);
            Assert.Equal(2, await DemLich(ca));
        }
        finally { await Don(ca, bsTam); }
    }

    [Theory]
    [InlineData(-1, "NV0003", "P0001", "RB9")]
    [InlineData(0, "NV0003", "P0001", "ngày mai")]
    [InlineData(10, "NV0002", "P0001", "RB10")]
    [InlineData(10, "NV0003", "P0003", "RB11")]
    public async Task Vi_pham_RB9_RB10_RB11_tra_400(int sauSoNgay, string maNv, string maPhong, string rb)
    {
        var qtv = await Qtv();
        var ca = await TaoCa();
        try
        {
            var res = await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(Ngay(sauSoNgay), maNv, maPhong, ca));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains(rb, await DocLoi(res));
            Assert.Equal(0, await DemLich(ca));
        }
        finally { await Don(ca); }
    }

    [Fact]
    public async Task Xep_nhieu_ngay_tao_dung_so_lich_va_trung_thi_khong_tao_gi()
    {
        var qtv = await Qtv();
        var ca = await TaoCa();
        try
        {
            // 14 ngày bắt đầu từ một Thứ 2 cách xa hôm nay; chọn Thứ 2 và Thứ 4 → 4 lịch
            var dau = DateTime.Today.AddDays(210);
            while (dau.DayOfWeek != DayOfWeek.Monday) dau = dau.AddDays(1);
            var body = new { tuNgay = dau.ToString("yyyy-MM-dd"), denNgay = dau.AddDays(13).ToString("yyyy-MM-dd"), cacThu = new[] { 1, 3 },
                maNv = "NV0004", maPhong = "P0003", maCa = ca };
            var res = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", body);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal(4, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("soLichDaTao").GetInt32());

            // Lặp lại có Thứ 5: Thứ 2, Thứ 4 trùng nên toàn bộ bị từ chối, Thứ 5 không được tạo
            var lai = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", new
            {
                tuNgay = dau.ToString("yyyy-MM-dd"), denNgay = dau.AddDays(13).ToString("yyyy-MM-dd"), cacThu = new[] { 1, 3, 4 },
                maNv = "NV0004", maPhong = "P0003", maCa = ca
            });
            Assert.Equal(HttpStatusCode.Conflict, lai.StatusCode);
            Assert.StartsWith("Trùng lịch trong ca này", await DocLoi(lai));
            Assert.Equal(4, await DemLich(ca));
        }
        finally { await Don(ca); }
    }

    [Fact]
    public async Task Xep_nhieu_ngay_kiem_tra_khoang_va_thu()
    {
        var qtv = await Qtv();
        var dai = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", new
        { tuNgay = Ngay(5), denNgay = Ngay(100), cacThu = new[] { 1 }, maNv = "NV0004", maPhong = "P0003", maCa = "CA0001" });
        Assert.Equal(HttpStatusCode.BadRequest, dai.StatusCode);
        var thuSai = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", new
        { tuNgay = Ngay(5), denNgay = Ngay(10), cacThu = new[] { 9 }, maNv = "NV0004", maPhong = "P0003", maCa = "CA0001" });
        Assert.Equal(HttpStatusCode.BadRequest, thuSai.StatusCode);
        var homNay = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", new
        { tuNgay = Ngay(0), denNgay = Ngay(6), cacThu = new[] { 0, 1, 2, 3, 4, 5, 6 }, maNv = "NV0004", maPhong = "P0003", maCa = "CA0001" });
        Assert.Equal(HttpStatusCode.BadRequest, homNay.StatusCode);
        Assert.Equal("Chỉ xếp lịch làm việc từ ngày mai trở đi.", await DocLoi(homNay));
        var rong = await qtv.PostAsJsonAsync("/api/lich-lam-viec/hang-loat", new
        { tuNgay = Ngay(5), denNgay = Ngay(10), cacThu = Array.Empty<int>(), maNv = "NV0004", maPhong = "P0003", maCa = "CA0001" });
        Assert.Equal(HttpStatusCode.BadRequest, rong.StatusCode);
    }

    [Fact]
    public async Task Sua_xoa_bi_chan_khi_co_lich_hen_hoac_da_qua()
    {
        var qtv = await Qtv();
        var ca = await TaoCa();
        try
        {
            var a = await (await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(Ngay(220), "NV0003", "P0001", ca))).Content.ReadFromJsonAsync<JsonElement>();
            var b = await (await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(Ngay(221), "NV0003", "P0001", ca))).Content.ReadFromJsonAsync<JsonElement>();
            var c = await (await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(Ngay(222), "NV0003", "P0001", ca))).Content.ReadFromJsonAsync<JsonElement>();
            string maA = a.GetProperty("maLichLamViec").GetString()!, maB = b.GetProperty("maLichLamViec").GetString()!, maC = c.GetProperty("maLichLamViec").GetString()!;

            // A: chưa có gì → sửa sang phòng 102, ngày khác được
            var sua = await qtv.PutAsJsonAsync($"/api/lich-lam-viec/{maA}", Lich(Ngay(223), "NV0003", "P0002", ca));
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
            Assert.Equal("Phòng 102", (await sua.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tenPhong").GetString());
            // Sửa vào ngày đã qua → 400
            Assert.Equal(HttpStatusCode.BadRequest, (await qtv.PutAsJsonAsync($"/api/lich-lam-viec/{maA}", Lich(Ngay(-3), "NV0003", "P0002", ca))).StatusCode);
            // Sửa vào hôm nay → 400 (chỉ xếp từ ngày mai)
            Assert.Equal(HttpStatusCode.BadRequest, (await qtv.PutAsJsonAsync($"/api/lich-lam-viec/{maA}", Lich(Ngay(0), "NV0003", "P0002", ca))).StatusCode);

            // B: có lịch hẹn → không sửa, không xóa
            await Sql(@"INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trieu_chung_ban_dau, trang_thai, ma_kh, ma_lich_lam_viec, ma_nv)
                        VALUES (@h, GETDATE(), N'KT', N'Đã đặt', 'KH0001', @l, NULL)", ("@h", "LHK" + maB[2..]), ("@l", maB));
            var suaB = await qtv.PutAsJsonAsync($"/api/lich-lam-viec/{maB}", Lich(Ngay(221), "NV0003", "P0002", ca));
            Assert.Equal(HttpStatusCode.Conflict, suaB.StatusCode);
            Assert.Contains("1 lịch hẹn", await DocLoi(suaB));
            Assert.Equal(HttpStatusCode.Conflict, (await qtv.DeleteAsync($"/api/lich-lam-viec/{maB}")).StatusCode);
            var chiTietB = await qtv.GetFromJsonAsync<JsonElement>($"/api/lich-lam-viec/{maB}");
            Assert.Equal(1, chiTietB.GetProperty("soLichHenConHieuLuc").GetInt32());

            // C: chuyển về quá khứ bằng SQL (trigger chỉ chặn RB9 khi thêm) → không sửa, không xóa
            await Sql("UPDATE Lich_lam_viec SET ngay_lam_viec = DATEADD(day, -5, CAST(GETDATE() AS date)) WHERE ma_lich_lam_viec = @l", ("@l", maC));
            var xoaC = await qtv.DeleteAsync($"/api/lich-lam-viec/{maC}");
            Assert.Equal(HttpStatusCode.Conflict, xoaC.StatusCode);
            Assert.Contains("đã qua", await DocLoi(xoaC));

            // A: xóa được
            Assert.Equal(HttpStatusCode.OK, (await qtv.DeleteAsync($"/api/lich-lam-viec/{maA}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await qtv.GetAsync($"/api/lich-lam-viec/{maA}")).StatusCode);
        }
        finally { await Don(ca); }
    }

    [Fact]
    public async Task Bac_si_chi_thay_lich_cua_minh_le_tan_thay_tat_ca()
    {
        var qtv = await Qtv();
        var ca = await TaoCa();
        try
        {
            var ngay = Ngay(230);
            await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, "NV0003", "P0001", ca));
            var lichB = await (await qtv.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, "NV0004", "P0003", ca))).Content.ReadFromJsonAsync<JsonElement>();
            var url = $"/api/lich-lam-viec?tuNgay={ngay}&denNgay={ngay}&maCa={ca}";

            var bs = await Client("NV", "NV0003");
            var cuaBs = await bs.GetFromJsonAsync<JsonElement>(url + "&maNv=NV0004"); // cố xem lịch người khác vẫn chỉ ra lịch mình
            Assert.All(cuaBs.EnumerateArray(), x => Assert.Equal("NV0003", x.GetProperty("maNv").GetString()));
            Assert.Equal(1, cuaBs.GetArrayLength());
            Assert.Equal(HttpStatusCode.NotFound, (await bs.GetAsync($"/api/lich-lam-viec/{lichB.GetProperty("maLichLamViec").GetString()}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await bs.PostAsJsonAsync("/api/lich-lam-viec", Lich(ngay, "NV0003", "P0002", ca))).StatusCode);

            var lt = await Client("NV", "NV0002");
            Assert.Equal(2, (await lt.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());
            Assert.Equal(1, (await lt.GetFromJsonAsync<JsonElement>(url + "&maKhoa=K0002")).GetArrayLength());
            Assert.Equal(HttpStatusCode.Forbidden, (await lt.DeleteAsync($"/api/lich-lam-viec/{lichB.GetProperty("maLichLamViec").GetString()}")).StatusCode);
            var dsBs = await lt.GetStringAsync("/api/lich-lam-viec/bac-si");
            Assert.Contains("NV0003", dsBs);
            Assert.DoesNotContain("0900000003", dsBs); // không lộ số điện thoại
        }
        finally { await Don(ca); }
    }

    [Fact]
    public async Task Khach_hang_khong_xem_lich_noi_bo_va_khoang_qua_dai_bi_tu_choi()
    {
        var kh = await Client("KH", "KH0001");
        Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/lich-lam-viec")).StatusCode);
        var qtv = await Qtv();
        Assert.Equal(HttpStatusCode.OK, (await qtv.GetAsync("/api/lich-lam-viec")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await qtv.GetAsync($"/api/lich-lam-viec?tuNgay={Ngay(0)}&denNgay={Ngay(90)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await qtv.GetAsync($"/api/lich-lam-viec?tuNgay={Ngay(5)}&denNgay={Ngay(1)}")).StatusCode);
    }
}
