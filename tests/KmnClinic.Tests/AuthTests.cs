using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>M0: giả lập vai trò, 401/403, không lộ mật khẩu, lỗi trigger trả đúng câu.</summary>
public class AuthTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;

    public AuthTests(TestApp app) => _app = app;

    private async Task<(HttpStatusCode Status, JsonElement Body)> VaoVaiTro(string loai, string ma)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai, ma });
        return (res.StatusCode, await res.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<HttpClient> ClientVaiTro(string loai, string ma)
    {
        var (status, body) = await VaoVaiTro(loai, ma);
        Assert.Equal(HttpStatusCode.OK, status);
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return c;
    }

    [Theory]
    [InlineData("NV", "NV0001", "QuanTriVien")]
    [InlineData("NV", "NV0002", "LeTan")]
    [InlineData("NV", "NV0003", "BacSi")]
    [InlineData("NV", "NV0005", "ThuNgan")]
    [InlineData("KH", "KH0001", "KhachHang")]
    [InlineData("KH", "KH0002", "KhachHang")]   // khách vãng lai (không mật khẩu) vẫn giả lập được
    public async Task Vao_vai_tro_nhan_dung_vai_tro_theo_database(string loai, string ma, string vaiTro)
    {
        var (status, body) = await VaoVaiTro(loai, ma);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(vaiTro, body.GetProperty("nguoiDung").GetProperty("vaiTro").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Theory]
    [InlineData("NV", "NV9999")]
    [InlineData("KH", "KH9999")]
    [InlineData("NV", "KH0001")]   // mã khách hàng nhưng chọn loại nhân viên
    public async Task Ma_khong_ton_tai_tra_404(string loai, string ma)
    {
        var (status, body) = await VaoVaiTro(loai, ma);
        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal("Không tìm thấy người dùng đã chọn.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Loai_sai_tra_400_tieng_viet()
    {
        var (status, body) = await VaoVaiTro("ADMIN", "NV0001");
        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("Loại người dùng chỉ nhận NV hoặc KH.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Danh_sach_nhan_vien_dung_chuc_vu_va_khong_lo_mat_khau()
    {
        var res = await _app.CreateClient().GetAsync("/api/gia-lap/nhan-vien?chucVu=" + Uri.EscapeDataString("Bác sĩ"));
        var text = await res.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var ds = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.True(ds.GetArrayLength() >= 2);
        Assert.All(ds.EnumerateArray(), nv => Assert.Equal("Bác sĩ", nv.GetProperty("chucVu").GetString()));
        Assert.DoesNotContain("matkhau", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mat_khau", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chuc_vu_khong_hop_le_tra_400()
    {
        var res = await _app.CreateClient().GetAsync("/api/gia-lap/nhan-vien?chucVu=Giam%20doc");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Tim_khach_theo_dau_so_dien_thoai()
    {
        var ds = await _app.CreateClient().GetFromJsonAsync<JsonElement>("/api/gia-lap/khach-hang?q=0911");
        Assert.Contains(ds.EnumerateArray(), kh => kh.GetProperty("ma").GetString() == "KH0001");
    }

    [Fact]
    public async Task Tao_khach_moi_khong_mat_khau_va_vao_vai_tro_luon()
    {
        const string sdt = "0988000111";
        try
        {
            var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
                new { hoTen = "Khách thử tự động", ngaySinh = "1999-05-20", gioiTinh = "Nữ", soDienThoai = sdt });
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var nd = body.GetProperty("nguoiDung");
            Assert.Equal("KhachHang", nd.GetProperty("vaiTro").GetString());
            Assert.StartsWith("KH", nd.GetProperty("ma").GetString());

            // Số điện thoại trùng: UNIQUE trong database trả câu tiếng Việt
            var trung = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
                new { hoTen = "Khách trùng", ngaySinh = "1999-05-20", gioiTinh = "Nam", soDienThoai = sdt });
            Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
            Assert.Equal("Số điện thoại khách hàng đã được sử dụng.",
                (await trung.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        }
        finally
        {
            await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("DELETE FROM Khach_hang WHERE sdt_kh = @sdt AND mat_khau_kh IS NULL", conn);
            cmd.Parameters.AddWithValue("@sdt", sdt);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Tao_khach_thieu_du_lieu_tra_400()
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
            new { hoTen = "", ngaySinh = "1999-05-20", gioiTinh = "Nam", soDienThoai = "0988000222" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("Vui lòng nhập họ tên.", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Json_hong_khong_lo_chi_tiet_ky_thuat()
    {
        var res = await _app.CreateClient().PostAsync("/api/gia-lap/vao-vai-tro",
            new StringContent("{abc", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("Dữ liệu gửi lên không đúng định dạng.",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
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
        var bacSi = await ClientVaiTro("NV", "NV0003");
        var quanTri = await ClientVaiTro("NV", "NV0001");
        var khach = await ClientVaiTro("KH", "KH0001");

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
        var bacSi = await ClientVaiTro("NV", "NV0003");
        var me = await bacSi.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("NV0003", me.GetProperty("ma").GetString());
        Assert.Equal("K0001", me.GetProperty("maKhoa").GetString());
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
