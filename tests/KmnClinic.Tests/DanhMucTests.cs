using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M1: danh mục khoa, phòng, ca, thuốc. Dữ liệu tạo trong test có tên bắt đầu bằng "KT-" và được xóa ở cuối mỗi test.
/// </summary>
public class DanhMucTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public DanhMucTests(TestApp app) => _app = app;

    private async Task<HttpClient> Client(string maNv)
    {
        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai = "NV", ma = maNv });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return c;
    }

    private static async Task<string> DocLoi(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task Sql(string sql, params (string, object)[] ps)
    {
        await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string Ten(string goc) => "KT-" + goc + "-" + Guid.NewGuid().ToString("N")[..6];

    /* ---------- Khoa ---------- */

    [Fact]
    public async Task Khoa_them_sua_xoa_va_trung_ten()
    {
        var qtv = await Client("NV0001");
        var ten = Ten("Khoa");
        var tao = await qtv.PostAsJsonAsync("/api/khoa", new { tenKhoa = ten, moTa = "Khoa kiểm thử" });
        Assert.Equal(HttpStatusCode.OK, tao.StatusCode);
        var ma = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maKhoa").GetString()!;
        Assert.Matches("^K\\d{4}$", ma);
        try
        {
            var trung = await qtv.PostAsJsonAsync("/api/khoa", new { tenKhoa = ten });
            Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
            Assert.Equal("Tên khoa đã tồn tại.", await DocLoi(trung));

            var sua = await qtv.PutAsJsonAsync($"/api/khoa/{ma}", new { tenKhoa = ten + "x", moTa = (string?)null });
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);

            var tim = await qtv.GetFromJsonAsync<JsonElement>("/api/khoa?q=" + Uri.EscapeDataString(ten));
            Assert.Equal(1, tim.GetProperty("total").GetInt32());

            Assert.Equal(HttpStatusCode.OK, (await qtv.DeleteAsync($"/api/khoa/{ma}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await qtv.GetAsync($"/api/khoa/{ma}")).StatusCode);
        }
        finally { await Sql("DELETE FROM Khoa WHERE ma_khoa=@m", ("@m", ma)); }
    }

    [Fact]
    public async Task Khoa_dang_dung_khong_xoa_duoc()
    {
        var qtv = await Client("NV0001");
        var res = await qtv.DeleteAsync("/api/khoa/K0001");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.StartsWith("Không thể xóa", await DocLoi(res));
    }

    [Fact]
    public async Task Khoa_thieu_ten_tra_400_tieng_viet()
    {
        var qtv = await Client("NV0001");
        var res = await qtv.PostAsJsonAsync("/api/khoa", new { tenKhoa = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Matches("[^\\x00-\\x7F]", await DocLoi(res));
    }

    [Theory]
    [InlineData("NV0002")]
    [InlineData("NV0003")]
    [InlineData("NV0005")]
    public async Task Nhan_vien_khac_doc_duoc_khoa_nhung_khong_ghi(string maNv)
    {
        var c = await Client(maNv);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/khoa")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/khoa", new { tenKhoa = Ten("Khoa") })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.DeleteAsync("/api/khoa/K0003")).StatusCode);
    }

    [Fact]
    public async Task Phan_trang_gioi_han_page_size()
    {
        var qtv = await Client("NV0001");
        var kq = await qtv.GetFromJsonAsync<JsonElement>("/api/khoa?page=1&pageSize=1000");
        Assert.Equal(100, kq.GetProperty("pageSize").GetInt32());
        var trang2 = await qtv.GetFromJsonAsync<JsonElement>("/api/khoa?page=2&pageSize=2");
        Assert.Equal(2, trang2.GetProperty("page").GetInt32());
        Assert.True(trang2.GetProperty("items").GetArrayLength() <= 2);
    }

    [Fact]
    public async Task Tim_kiem_ky_tu_dac_biet_khong_loi()
    {
        var qtv = await Client("NV0001");
        var kq = await qtv.GetFromJsonAsync<JsonElement>("/api/khoa?q=" + Uri.EscapeDataString("%_['"));
        Assert.Equal(0, kq.GetProperty("total").GetInt32());
    }

    /* ---------- Phòng + Ca (RB2, RB3, RB11, không đổi giờ ca đã dùng) ---------- */

    [Fact]
    public async Task Phong_va_ca_da_co_lich_thi_khoa_doi_khoa_doi_gio_va_khong_xoa()
    {
        var qtv = await Client("NV0001");
        var tenPhong = Ten("P");
        string? maPhong = null, maCa = null;
        try
        {
            var p = await qtv.PostAsJsonAsync("/api/phong", new { tenPhong, maKhoa = "K0001" });
            Assert.Equal(HttpStatusCode.OK, p.StatusCode);
            maPhong = (await p.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maPhong").GetString()!;
            var c = await qtv.PostAsJsonAsync("/api/ca", new { tenCa = Ten("C"), gioBatDau = "18:00", gioKetThuc = "20:00" });
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
            var ca = await c.Content.ReadFromJsonAsync<JsonElement>();
            maCa = ca.GetProperty("maCa").GetString()!;
            Assert.Equal("18:00", ca.GetProperty("gioBatDau").GetString());
            var trung = await qtv.PostAsJsonAsync("/api/phong", new { tenPhong, maKhoa = "K0002" });
            Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);

            // Chưa có lịch: đổi khoa được
            Assert.Equal(HttpStatusCode.OK, (await qtv.PutAsJsonAsync($"/api/phong/{maPhong}", new { tenPhong, maKhoa = "K0002" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await qtv.PutAsJsonAsync($"/api/phong/{maPhong}", new { tenPhong, maKhoa = "K0001" })).StatusCode);

            await Sql(@"INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
                        VALUES ('LVKT99', CAST(DATEADD(day, 2, GETDATE()) AS date), 'NV0003', @p, @c)", ("@p", maPhong), ("@c", maCa));

            var doiKhoa = await qtv.PutAsJsonAsync($"/api/phong/{maPhong}", new { tenPhong, maKhoa = "K0002" });
            Assert.Equal(HttpStatusCode.Conflict, doiKhoa.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await qtv.DeleteAsync($"/api/phong/{maPhong}")).StatusCode);
            // Đổi tên phòng vẫn được
            Assert.Equal(HttpStatusCode.OK, (await qtv.PutAsJsonAsync($"/api/phong/{maPhong}", new { tenPhong = tenPhong + "b", maKhoa = "K0001" })).StatusCode);

            var ls = await qtv.GetFromJsonAsync<JsonElement>($"/api/phong/{maPhong}/lich-su-dung");
            Assert.Equal(1, ls.GetProperty("total").GetInt32());

            var doiGio = await qtv.PutAsJsonAsync($"/api/ca/{maCa}", new { tenCa = "KT doi", gioBatDau = "18:30", gioKetThuc = "20:00" });
            Assert.Equal(HttpStatusCode.Conflict, doiGio.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await qtv.PutAsJsonAsync($"/api/ca/{maCa}", new { tenCa = "KT doi", gioBatDau = "18:00", gioKetThuc = "20:00" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await qtv.DeleteAsync($"/api/ca/{maCa}")).StatusCode);

            // Lễ tân không xem được lịch sử dùng phòng
            var lt = await Client("NV0002");
            Assert.Equal(HttpStatusCode.Forbidden, (await lt.GetAsync($"/api/phong/{maPhong}/lich-su-dung")).StatusCode);
        }
        finally
        {
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec='LVKT99'");
            if (maPhong != null) await Sql("DELETE FROM Phong_lam_viec WHERE ma_phong=@m", ("@m", maPhong));
            if (maCa != null) await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca=@m", ("@m", maCa));
        }
    }

    [Theory]
    [InlineData("17:00", "16:00")]
    [InlineData("08:00", "08:00")]
    public async Task Ca_gio_bat_dau_phai_nho_hon_gio_ket_thuc(string bd, string kt)
    {
        var qtv = await Client("NV0001");
        var res = await qtv.PostAsJsonAsync("/api/ca", new { tenCa = "KT RB3", gioBatDau = bd, gioKetThuc = kt });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("Giờ bắt đầu ca phải nhỏ hơn giờ kết thúc.", await DocLoi(res));
    }

    [Fact]
    public async Task Phong_khoa_khong_ton_tai_tra_loi_tieng_viet()
    {
        var qtv = await Client("NV0001");
        var res = await qtv.PostAsJsonAsync("/api/phong", new { tenPhong = Ten("P"), maKhoa = "K9999" });
        Assert.True(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.NotFound);
        Assert.Matches("[^\\x00-\\x7F]", await DocLoi(res));
    }

    /* ---------- Thuốc (RB23, RB24, RB25, phân quyền) ---------- */

    [Fact]
    public async Task Thuoc_trung_ten_nsx_ham_luong_tra_409()
    {
        var qtv = await Client("NV0001");
        var res = await qtv.PostAsJsonAsync("/api/thuoc", new
        {
            tenThuoc = "Paracetamol", nsx = "Nhà sản xuất mẫu A", hamLuong = 500, donViTinh = "Viên", donGiaThuoc = 2000, soLuongTon = 10
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("Thuốc cùng tên, nhà sản xuất và hàm lượng đã tồn tại.", await DocLoi(res));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1000, -1)]
    public async Task Thuoc_don_gia_va_ton_kho_khong_hop_le_tra_400(int donGia, int ton)
    {
        var qtv = await Client("NV0001");
        var res = await qtv.PostAsJsonAsync("/api/thuoc", new
        {
            tenThuoc = Ten("T"), nsx = "KT", hamLuong = 1, donViTinh = "Viên", donGiaThuoc = donGia, soLuongTon = ton
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Matches("[^\\x00-\\x7F]", await DocLoi(res));
    }

    [Fact]
    public async Task Thuoc_them_sua_va_loc_sap_het()
    {
        var qtv = await Client("NV0001");
        var ten = Ten("T");
        var res = await qtv.PostAsJsonAsync("/api/thuoc", new
        {
            tenThuoc = ten, nsx = "KT", hamLuong = 2.5, donViTinh = "Viên", donGiaThuoc = 1200, soLuongTon = 5
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var t = await res.Content.ReadFromJsonAsync<JsonElement>();
        var ma = t.GetProperty("maThuoc").GetString()!;
        try
        {
            Assert.True(t.GetProperty("sapHet").GetBoolean());
            var sapHet = await qtv.GetFromJsonAsync<JsonElement>("/api/thuoc?sapHet=true&q=" + Uri.EscapeDataString(ten));
            Assert.Equal(1, sapHet.GetProperty("total").GetInt32());

            var sua = await qtv.PutAsJsonAsync($"/api/thuoc/{ma}", new
            {
                tenThuoc = ten, nsx = "KT", hamLuong = 2.5, donViTinh = "Viên", donGiaThuoc = 1500, soLuongTon = 500
            });
            Assert.Equal(HttpStatusCode.OK, sua.StatusCode);
            var sau = await sua.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(sau.GetProperty("sapHet").GetBoolean());
            Assert.Equal(1500, sau.GetProperty("donGiaThuoc").GetDecimal());

            // Không có API xóa thuốc (giữ lịch sử đơn thuốc)
            Assert.Contains((await qtv.DeleteAsync($"/api/thuoc/{ma}")).StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }
        finally { await Sql("DELETE FROM Thuoc WHERE ma_thuoc=@m", ("@m", ma)); }
    }

    [Theory]
    [InlineData("NV0003")]
    [InlineData("NV0005")]
    public async Task Bac_si_thu_ngan_xem_thuoc_nhung_khong_ghi(string maNv)
    {
        var c = await Client(maNv);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/thuoc")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/thuoc/sap-het")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync("/api/thuoc/TH0001", new
        {
            tenThuoc = "Paracetamol", nsx = "Nhà sản xuất mẫu A", hamLuong = 500, donViTinh = "Viên", donGiaThuoc = 1, soLuongTon = 0
        })).StatusCode);
    }

    [Fact]
    public async Task Le_tan_va_khach_khong_xem_duoc_thuoc()
    {
        var lt = await Client("NV0002");
        Assert.Equal(HttpStatusCode.Forbidden, (await lt.GetAsync("/api/thuoc")).StatusCode);

        var res = await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai = "KH", ma = "KH0001" });
        var kh = _app.CreateClient();
        kh.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/thuoc")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await kh.GetAsync("/api/khoa")).StatusCode);
    }
}
