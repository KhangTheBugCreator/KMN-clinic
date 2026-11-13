using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>M2: nhân viên và khách hàng. Dữ liệu tạo trong test được xóa ở cuối mỗi test.</summary>
public class NhanSuTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public NhanSuTests(TestApp app) => _app = app;

    private async Task<HttpClient> Client(string loai, string ma)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai, ma });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return c;
    }
    private Task<HttpClient> Qtv() => Client("NV", "NV0001");

    private static async Task<string> DocLoi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static Task<object?> Sql(string sql, params (string, object)[] ps) => TestApp.SqlAsync(sql, ps);

    /// <summary>Số điện thoại ngẫu nhiên đầu 08 (dữ liệu mẫu dùng 09).</summary>
    private static string Sdt() => "08" + Random.Shared.Next(0, 100_000_000).ToString("D8");

    private static object NhanVien(string sdt, string chucVu = "Lễ tân", string? maKhoa = null, string? anh = null, string? matKhau = "matkhau-thu-nghiem")
        => new { hoTen = "KT Nhân viên", soDienThoai = sdt, chucVu, maKhoa, duongDanAnh = anh, matKhau, ngaySinh = "1990-05-02" };

    private async Task<string> TaoNhanVien(HttpClient c, object body)
    {
        var res = await c.PostAsJsonAsync("/api/nhan-vien", body);
        Assert.True(res.StatusCode == HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNv").GetString()!;
    }

    private static Task XoaNv(string? ma) => ma is null ? Task.CompletedTask : Sql("DELETE FROM Nhan_vien WHERE ma_nv=@m", ("@m", ma));

    /* ---------------- Nhân viên ---------------- */

    [Fact]
    public async Task Them_bac_si_mat_khau_duoc_bam_va_khong_tra_ve()
    {
        var qtv = await Qtv();
        string? ma = null;
        try
        {
            var res = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt(), "Bác sĩ", "K0002", "https://example.com/anh.jpg"));
            var raw = await res.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.DoesNotContain("matKhau", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mat_khau", raw, StringComparison.OrdinalIgnoreCase);
            var nv = JsonDocument.Parse(raw).RootElement;
            ma = nv.GetProperty("maNv").GetString();
            Assert.Matches("^NV\\d{4}$", ma);
            Assert.Equal("Nhi", nv.GetProperty("tenKhoa").GetString());
            Assert.Equal("1990-05-02", nv.GetProperty("ngaySinh").GetString());

            var luu = (string)(await Sql("SELECT mat_khau_nv FROM Nhan_vien WHERE ma_nv=@m", ("@m", ma!)))!;
            Assert.StartsWith("PBKDF2-SHA256$", luu);
            Assert.True(KmnClinic.Api.Infrastructure.MatKhau.KiemTra("matkhau-thu-nghiem", luu));

            var ds = await qtv.GetStringAsync("/api/nhan-vien?pageSize=100");
            Assert.DoesNotContain("matKhau", ds, StringComparison.OrdinalIgnoreCase);
        }
        finally { await XoaNv(ma); }
    }

    [Fact]
    public async Task Nhan_vien_khong_phai_bac_si_thi_bo_khoa()
    {
        var qtv = await Qtv();
        string? ma = null;
        try
        {
            ma = await TaoNhanVien(qtv, NhanVien(Sdt(), "Thu ngân", "K0001"));
            Assert.IsType<DBNull>(await Sql("SELECT ma_khoa FROM Nhan_vien WHERE ma_nv=@m", ("@m", ma)));
        }
        finally { await XoaNv(ma); }
    }

    [Fact]
    public async Task Bac_si_thieu_khoa_hoac_khoa_sai_tra_400()
    {
        var qtv = await Qtv();
        var r1 = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt(), "Bác sĩ", null));
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal("Bác sĩ phải thuộc một khoa (RB6).", await DocLoi(r1));
        var r2 = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt(), "Bác sĩ", "K9999"));
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    public async Task Mat_khau_thieu_hoac_ngan_tra_400(string? matKhau)
    {
        var qtv = await Qtv();
        var res = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt(), matKhau: matKhau));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("8 ký tự", await DocLoi(res));
    }

    [Theory]
    [InlineData("0900000002", "Lễ tân", HttpStatusCode.Conflict)]   // RB4: trùng SĐT
    [InlineData("12345", "Lễ tân", HttpStatusCode.BadRequest)]
    [InlineData(null, "Giám đốc", HttpStatusCode.BadRequest)]       // RB5
    public async Task Sdt_va_chuc_vu_khong_hop_le(string? sdt, string chucVu, HttpStatusCode mongDoi)
    {
        var qtv = await Qtv();
        var res = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(sdt ?? Sdt(), chucVu));
        Assert.Equal(mongDoi, res.StatusCode);
        Assert.Matches("[^\\x00-\\x7F]", await DocLoi(res));
    }

    [Theory]
    [InlineData("http://example.com/a.jpg")]
    [InlineData("https://127.0.0.1/a.jpg")]
    [InlineData("https://localhost/a.jpg")]
    [InlineData("https://10.0.0.5/a.jpg")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://192.168.1.10/a.jpg")]
    [InlineData("https://[::1]/a.jpg")]
    [InlineData("https://may-noi-bo/a.jpg")]
    [InlineData("https://server.local/a.jpg")]
    [InlineData("https://user:pass@example.com/a.jpg")]
    [InlineData("https://example.com:8443/a.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("//example.com/a.jpg")]
    [InlineData("https://exa mple.com/a.jpg")]
    public async Task Anh_url_khong_hop_le_tra_400(string url)
    {
        var qtv = await Qtv();
        var res = await qtv.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt(), anh: url));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("ảnh", await DocLoi(res));
    }

    [Fact]
    public async Task Anh_url_https_cong_khai_va_bo_trong_deu_luu_duoc()
    {
        var qtv = await Qtv();
        string? ma = null;
        try
        {
            ma = await TaoNhanVien(qtv, NhanVien(Sdt(), anh: "  https://upload.wikimedia.org/a b.jpg ".Replace(" b", "_b")));
            var nv = await qtv.GetFromJsonAsync<JsonElement>($"/api/nhan-vien/{ma}");
            Assert.Equal("https://upload.wikimedia.org/a_b.jpg", nv.GetProperty("duongDanAnh").GetString());

            var sua = await qtv.PutAsJsonAsync($"/api/nhan-vien/{ma}", NhanVien(nv.GetProperty("soDienThoai").GetString()!, anh: ""));
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
            Assert.Equal(JsonValueKind.Null, (await sua.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duongDanAnh").ValueKind);
        }
        finally { await XoaNv(ma); }
    }

    [Fact]
    public async Task Nhan_vien_da_co_lich_thi_khong_doi_chuc_vu_khoa_va_khong_xoa()
    {
        var qtv = await Qtv();
        string? ma = null;
        var sdt = Sdt();
        try
        {
            ma = await TaoNhanVien(qtv, NhanVien(sdt, "Bác sĩ", "K0001"));
            await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES ('CAKT02', N'KT ca', '20:00', '21:00')");
            await Sql(@"INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
                        VALUES ('LVKT02', CAST(DATEADD(day, 3, GETDATE()) AS date), @nv, 'P0001', 'CAKT02')", ("@nv", ma));

            var doiCv = await qtv.PutAsJsonAsync($"/api/nhan-vien/{ma}", NhanVien(sdt, "Lễ tân"));
            Assert.Equal(HttpStatusCode.Conflict, doiCv.StatusCode);
            Assert.StartsWith("Không thể đổi chức vụ hoặc khoa", await DocLoi(doiCv));
            Assert.Equal(HttpStatusCode.Conflict, (await qtv.PutAsJsonAsync($"/api/nhan-vien/{ma}", NhanVien(sdt, "Bác sĩ", "K0002"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await qtv.DeleteAsync($"/api/nhan-vien/{ma}")).StatusCode);

            // Đổi tên, ảnh vẫn được
            Assert.Equal(HttpStatusCode.OK, (await qtv.PutAsJsonAsync($"/api/nhan-vien/{ma}", NhanVien(sdt, "Bác sĩ", "K0001", "https://example.com/moi.jpg"))).StatusCode);
            var nv = await qtv.GetFromJsonAsync<JsonElement>($"/api/nhan-vien/{ma}");
            Assert.Equal(1, nv.GetProperty("soLienKet").GetInt32());
        }
        finally
        {
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec='LVKT02'");
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca='CAKT02'");
            await XoaNv(ma);
        }
    }

    [Fact]
    public async Task Doi_chuc_vu_va_xoa_khi_chua_co_du_lieu()
    {
        var qtv = await Qtv();
        string? ma = null;
        var sdt = Sdt();
        try
        {
            ma = await TaoNhanVien(qtv, NhanVien(sdt, "Lễ tân"));
            var sua = await qtv.PutAsJsonAsync($"/api/nhan-vien/{ma}", NhanVien(sdt, "Bác sĩ", "K0003"));
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
            Assert.Equal("Tai Mũi Họng", (await sua.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tenKhoa").GetString());
            Assert.Equal(HttpStatusCode.OK, (await qtv.DeleteAsync($"/api/nhan-vien/{ma}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await qtv.GetAsync($"/api/nhan-vien/{ma}")).StatusCode);
            ma = null;
        }
        finally { await XoaNv(ma); }
    }

    [Fact]
    public async Task Quan_tri_vien_khong_tu_xoa_hay_tu_ha_chuc_vu()
    {
        var qtv = await Qtv();
        var xoa = await qtv.DeleteAsync("/api/nhan-vien/NV0001");
        Assert.Equal(HttpStatusCode.Conflict, xoa.StatusCode);
        Assert.Equal("Bạn không thể tự xóa chính mình.", await DocLoi(xoa));
        var ha = await qtv.PutAsJsonAsync("/api/nhan-vien/NV0001", NhanVien("0900000001", "Lễ tân"));
        Assert.Equal(HttpStatusCode.Conflict, ha.StatusCode);
        Assert.Equal("Quản trị viên", (string)(await Sql("SELECT chuc_vu FROM Nhan_vien WHERE ma_nv='NV0001'"))!);
    }

    [Fact]
    public async Task Loc_va_tim_nhan_vien()
    {
        var qtv = await Qtv();
        var bs = await qtv.GetFromJsonAsync<JsonElement>("/api/nhan-vien?chucVu=" + Uri.EscapeDataString("Bác sĩ"));
        Assert.All(bs.GetProperty("items").EnumerateArray(), x => Assert.Equal("Bác sĩ", x.GetProperty("chucVu").GetString()));
        var nhi = await qtv.GetFromJsonAsync<JsonElement>("/api/nhan-vien?maKhoa=K0002");
        Assert.Contains(nhi.GetProperty("items").EnumerateArray(), x => x.GetProperty("maNv").GetString() == "NV0004");
        var tim = await qtv.GetFromJsonAsync<JsonElement>("/api/nhan-vien?q=0900000003");
        Assert.Equal("NV0003", tim.GetProperty("items")[0].GetProperty("maNv").GetString());
    }

    [Theory]
    [InlineData("NV0002")]
    [InlineData("NV0003")]
    [InlineData("NV0005")]
    public async Task Chi_quan_tri_vien_quan_ly_nhan_vien(string maNv)
    {
        var c = await Client("NV", maNv);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/nhan-vien")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/nhan-vien", NhanVien(Sdt()))).StatusCode);
    }

    /* ---------------- Khách hàng ---------------- */

    private static object KhachHang(string sdt, string ngaySinh = "1995-03-04", string gioiTinh = "Nữ")
        => new { hoTen = "KT Khách", soDienThoai = sdt, ngaySinh, gioiTinh, diaChi = "Vĩnh Long" };

    private static Task XoaKh(string? ma) => ma is null ? Task.CompletedTask : Sql("DELETE FROM Khach_hang WHERE ma_kh=@m", ("@m", ma));

    [Fact]
    public async Task Le_tan_them_sua_khach_nhung_khong_xoa()
    {
        var lt = await Client("NV", "NV0002");
        string? ma = null;
        try
        {
            var sdt = Sdt();
            var res = await lt.PostAsJsonAsync("/api/khach-hang", KhachHang(sdt));
            var raw = await res.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.DoesNotContain("matKhau", raw, StringComparison.OrdinalIgnoreCase);
            ma = JsonDocument.Parse(raw).RootElement.GetProperty("maKh").GetString();
            Assert.IsType<DBNull>(await Sql("SELECT mat_khau_kh FROM Khach_hang WHERE ma_kh=@m", ("@m", ma!)));

            var sua = await lt.PutAsJsonAsync($"/api/khach-hang/{ma}", KhachHang(sdt, "1995-03-05", "Nam"));
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
            var kh = await sua.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Nam", kh.GetProperty("gioiTinh").GetString());
            Assert.Equal("1995-03-05", kh.GetProperty("ngaySinh").GetString());

            Assert.Equal(HttpStatusCode.Forbidden, (await lt.DeleteAsync($"/api/khach-hang/{ma}")).StatusCode);
            var qtv = await Qtv();
            Assert.Equal(HttpStatusCode.OK, (await qtv.DeleteAsync($"/api/khach-hang/{ma}")).StatusCode);
            ma = null;
        }
        finally { await XoaKh(ma); }
    }

    [Theory]
    [InlineData("0911111111", "1995-03-04", "Nữ", HttpStatusCode.Conflict)]   // RB7
    [InlineData(null, "2999-01-01", "Nữ", HttpStatusCode.BadRequest)]
    [InlineData(null, "1995-03-04", "Khác", HttpStatusCode.BadRequest)]      // RB8
    public async Task Khach_hang_khong_hop_le(string? sdt, string ngaySinh, string gioiTinh, HttpStatusCode mongDoi)
    {
        var qtv = await Qtv();
        var res = await qtv.PostAsJsonAsync("/api/khach-hang", KhachHang(sdt ?? Sdt(), ngaySinh, gioiTinh));
        Assert.Equal(mongDoi, res.StatusCode);
        Assert.Matches("[^\\x00-\\x7F]", await DocLoi(res));
    }

    [Fact]
    public async Task Khach_da_co_lich_hen_khong_xoa_duoc()
    {
        var qtv = await Qtv();
        string? ma = null;
        try
        {
            var res = await qtv.PostAsJsonAsync("/api/khach-hang", KhachHang(Sdt()));
            ma = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maKh").GetString();
            await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES ('CAKT03', N'KT ca 3', '20:00', '21:00')");
            await Sql(@"INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
                        VALUES ('LVKT03', CAST(DATEADD(day, 3, GETDATE()) AS date), 'NV0003', 'P0002', 'CAKT03')");
            await Sql(@"INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trieu_chung_ban_dau, trang_thai, ma_kh, ma_lich_lam_viec, ma_nv)
                        VALUES ('LHKT03', GETDATE(), N'Thử', N'Đã đặt', @kh, 'LVKT03', NULL)", ("@kh", ma!));

            var xoa = await qtv.DeleteAsync($"/api/khach-hang/{ma}");
            Assert.Equal(HttpStatusCode.Conflict, xoa.StatusCode);
            Assert.StartsWith("Không thể xóa: khách hàng đã có 1 lịch hẹn", await DocLoi(xoa));
        }
        finally
        {
            await Sql("DELETE FROM Lich_hen WHERE ma_lich_hen='LHKT03'");
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec='LVKT03'");
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca='CAKT03'");
            await XoaKh(ma);
        }
    }

    [Fact]
    public async Task Loc_khach_theo_gioi_tinh_va_tim_theo_sdt()
    {
        var lt = await Client("NV", "NV0002");
        var nam = await lt.GetFromJsonAsync<JsonElement>("/api/khach-hang?gioiTinh=Nam&pageSize=100");
        Assert.All(nam.GetProperty("items").EnumerateArray(), x => Assert.Equal("Nam", x.GetProperty("gioiTinh").GetString()));
        var tim = await lt.GetFromJsonAsync<JsonElement>("/api/khach-hang?q=0922222222");
        Assert.Equal("KH0002", tim.GetProperty("items")[0].GetProperty("maKh").GetString());
    }

    [Fact]
    public async Task Bac_si_thu_ngan_khach_khong_vao_quan_ly_khach_hang()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await (await Client("NV", "NV0003")).GetAsync("/api/khach-hang")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await Client("NV", "NV0005")).GetAsync("/api/khach-hang")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await Client("KH", "KH0001")).GetAsync("/api/khach-hang")).StatusCode);
    }
}
