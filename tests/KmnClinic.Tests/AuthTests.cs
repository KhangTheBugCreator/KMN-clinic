using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KmnClinic.Tests;

/// <summary>M0: đăng nhập 2 loại tài khoản, 401/403, không lộ mật khẩu, lỗi trigger trả đúng câu.</summary>
public class AuthTests : IClassFixture<TestApp>
{
    private const string MatKhauMau = "matkhau123";
    private readonly TestApp _app;

    public AuthTests(TestApp app) => _app = app;

    private async Task<(HttpStatusCode Status, JsonElement Body)> DangNhap(string loai, string sdt, string mk)
    {
        var res = await _app.CreateClient().PostAsJsonAsync($"/api/auth/{loai}/dang-nhap", new { soDienThoai = sdt, matKhau = mk });
        return (res.StatusCode, await res.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<HttpClient> ClientDaDangNhap(string loai, string sdt)
    {
        var (status, body) = await DangNhap(loai, sdt, MatKhauMau);
        Assert.Equal(HttpStatusCode.OK, status);
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return c;
    }

    [Theory]
    [InlineData("0900000001", "QuanTriVien")]
    [InlineData("0900000002", "LeTan")]
    [InlineData("0900000003", "BacSi")]
    [InlineData("0900000005", "ThuNgan")]
    public async Task NhanVien_dang_nhap_dung_nhan_dung_vai_tro(string sdt, string vaiTro)
    {
        var (status, body) = await DangNhap("nhan-vien", sdt, MatKhauMau);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(vaiTro, body.GetProperty("nguoiDung").GetProperty("vaiTro").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task KhachHang_dang_nhap_dung()
    {
        var (status, body) = await DangNhap("khach-hang", "0911111111", MatKhauMau);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("KhachHang", body.GetProperty("nguoiDung").GetProperty("vaiTro").GetString());
    }

    [Theory]
    [InlineData("nhan-vien", "0900000001", "saimatkhau")]   // sai mật khẩu
    [InlineData("nhan-vien", "0999999999", MatKhauMau)]      // không có số điện thoại
    [InlineData("khach-hang", "0922222222", MatKhauMau)]     // khách vãng lai chưa có mật khẩu
    [InlineData("khach-hang", "0900000001", MatKhauMau)]     // nhân viên đăng nhập nhầm cửa khách hàng
    public async Task Dang_nhap_sai_tra_401_cung_mot_cau(string loai, string sdt, string mk)
    {
        var (status, body) = await DangNhap(loai, sdt, mk);
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal("Số điện thoại hoặc mật khẩu không đúng.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task So_dien_thoai_sai_dinh_dang_tra_400_tieng_viet()
    {
        var (status, body) = await DangNhap("nhan-vien", "12ab", "x");
        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Json_hong_khong_lo_chi_tiet_ky_thuat()
    {
        var res = await _app.CreateClient().PostAsync("/api/auth/nhan-vien/dang-nhap",
            new StringContent("{abc", System.Text.Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("Dữ liệu gửi lên không đúng định dạng.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Phan_hoi_khong_bao_gio_chua_mat_khau()
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/auth/nhan-vien/dang-nhap",
            new { soDienThoai = "0900000001", matKhau = MatKhauMau });
        var text = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain(MatKhauMau, text);
        Assert.DoesNotContain("matKhau", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mat_khau", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Khong_co_token_tra_401()
    {
        var res = await _app.CreateClient().GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Token_gia_tra_401()
    {
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.e30.sai");
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Sai_vai_tro_tra_403_dung_vai_tro_tra_200()
    {
        var bacSi = await ClientDaDangNhap("nhan-vien", "0900000003");
        var quanTri = await ClientDaDangNhap("nhan-vien", "0900000001");
        var khach = await ClientDaDangNhap("khach-hang", "0911111111");

        Assert.Equal(HttpStatusCode.Forbidden, (await bacSi.GetAsync("/api/kiem-thu/chi-quan-tri")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await quanTri.GetAsync("/api/kiem-thu/chi-quan-tri")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await khach.GetAsync("/api/kiem-thu/chi-nhan-vien")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bacSi.GetAsync("/api/kiem-thu/chi-nhan-vien")).StatusCode);

        var body = await (await bacSi.GetAsync("/api/kiem-thu/chi-quan-tri")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bạn không có quyền thực hiện thao tác này.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Me_tra_dung_nguoi_trong_token()
    {
        var khach = await ClientDaDangNhap("khach-hang", "0911111111");
        var me = await khach.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("KH0001", me.GetProperty("ma").GetString());
    }

    [Fact]
    public async Task Doi_mat_khau_kiem_tra_do_dai_va_mat_khau_cu()
    {
        var c = await ClientDaDangNhap("nhan-vien", "0900000002");
        var ngan = await c.PutAsJsonAsync("/api/auth/doi-mat-khau", new { matKhauCu = MatKhauMau, matKhauMoi = "1234567" });
        Assert.Equal(HttpStatusCode.BadRequest, ngan.StatusCode);
        var saiCu = await c.PutAsJsonAsync("/api/auth/doi-mat-khau", new { matKhauCu = "khongdung1", matKhauMoi = "matkhaumoi1" });
        Assert.Equal(HttpStatusCode.BadRequest, saiCu.StatusCode);

        // Đổi rồi đổi lại để không ảnh hưởng các bài kiểm thử khác.
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/auth/doi-mat-khau", new { matKhauCu = MatKhauMau, matKhauMoi = "matkhaumoi1" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DangNhap("nhan-vien", "0900000002", "matkhaumoi1")).Status);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/auth/doi-mat-khau", new { matKhauCu = "matkhaumoi1", matKhauMoi = MatKhauMau })).StatusCode);
    }

    [Fact]
    public async Task Loi_trigger_tra_400_voi_dung_cau_trong_database()
    {
        var res = await _app.CreateClient().PostAsync("/api/kiem-thu/lich-qua-khu", null);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("RB9: Không được tạo lịch làm việc cho ngày trong quá khứ.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Api_khong_ton_tai_tra_404_json()
    {
        var res = await _app.CreateClient().GetAsync("/api/khong-co");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Contains("application/json", res.Content.Headers.ContentType?.ToString());
    }
}
