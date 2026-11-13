using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>
/// M10: một lượt khám đi hết các vai trò qua API thật: khách đặt lịch → lễ tân tiếp nhận → bác sĩ khám, kê đơn, hoàn tất
/// → thu ngân thu tiền, trừ kho → quản trị viên và bác sĩ xem lại (chỉ đọc). Dùng ca tối nay và thuốc tạm, xóa hết ở cuối.
/// </summary>
[Collection("ThongKe")] // chạy riêng, không song song: dùng chung bác sĩ NV0004 và phòng P0003 với các test khác
public class ToanLuongTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public ToanLuongTests(TestApp app) => _app = app;

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

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Dat_lich_den_thanh_toan_tru_kho_va_quan_tri_xem_lai()
    {
        var ca = "CT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var lich = "LT" + Random.Shared.Next(10_000_000, 99_999_999);
        var thuoc = "LT" + Random.Shared.Next(1_000_000, 9_999_999);
        string? maKh = null;
        await Sql("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@m, N'KT ca', '23:56', '23:57')", ("@m", ca));
        await Sql("INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca) VALUES (@m, @n, 'NV0004', 'P0003', @c)",
            ("@m", lich), ("@n", DateTime.Today), ("@c", ca));
        await Sql(@"INSERT INTO Thuoc (ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton, mo_ta)
                    VALUES (@m, @t, N'KT', 500, N'Viên', 5000, 10, NULL)", ("@m", thuoc), ("@t", "KT thuốc " + thuoc));
        try
        {
            // 1. Khách tự tạo tài khoản và đặt lịch; đặt trùng ca bị chặn (Q6)
            var sdt = "09" + Random.Shared.Next(10_000_000, 99_999_999);
            var dk = await Json(await _app.CreateClient().PostAsJsonAsync("/api/gia-lap/khach-hang",
                new { hoTen = "KT Toàn luồng", soDienThoai = sdt, ngaySinh = "1990-01-01", gioiTinh = "Nữ" }));
            maKh = dk.GetProperty("nguoiDung").GetProperty("ma").GetString()!;
            var kh = VoiToken(dk.GetProperty("token").GetString()!);
            var lh = (await Json(await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich, trieuChungBanDau = "Sốt, đau họng" })))
                .GetProperty("maLichHen").GetString()!;
            Assert.Equal(HttpStatusCode.Conflict, (await kh.PostAsJsonAsync("/api/lich-hen-cua-toi", new { maLichLamViec = lich })).StatusCode);

            // Bác sĩ thấy lịch hẹn vào ca của mình; bác sĩ khác không thấy
            var bs = await NhanVien("NV0004");
            var cuaBs = await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/lich-hen?q={lh}");
            Assert.Equal("Đã đặt", cuaBs.GetProperty("items").EnumerateArray().Single().GetProperty("trangThai").GetString());
            var bsKhac = await NhanVien("NV0003");
            Assert.Equal(0, (await bsKhac.GetFromJsonAsync<JsonElement>($"/api/bac-si/lich-hen?q={lh}&maNv=NV0004")).GetProperty("total").GetInt32());

            // 2. Lễ tân tiếp nhận (lịch Đã đặt: xác nhận + tạo hồ sơ cùng lúc, Q3)
            var lt = await NhanVien("NV0002");
            var hs = (await Json(await lt.PostAsJsonAsync($"/api/le-tan/lich-hen/{lh}/tiep-nhan", new { trieuChung = "Sốt, đau họng" })))
                .GetProperty("maHoSo").GetString()!;

            // 3. Bác sĩ khám, kê đơn, hoàn tất → hóa đơn = tiền công + thuốc (RB29)
            await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc", new { maThuoc = thuoc, soLuong = 4, lieuDung = "Ngày 2 lần, mỗi lần 2 viên" }));
            Assert.Equal(HttpStatusCode.NotFound, (await bsKhac.GetAsync($"/api/bac-si/ho-so/{hs}")).StatusCode);
            var xong = await Json(await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/hoan-tat",
                new { trieuChung = "Sốt, đau họng", chanDoanBenh = "Viêm họng cấp", loiDan = "Uống nhiều nước", tienCongKham = 150000 }));
            var hd = xong.GetProperty("hoaDon").GetProperty("maHoaDon").GetString()!;
            Assert.Equal(170000, xong.GetProperty("hoaDon").GetProperty("tongTien").GetDecimal());
            Assert.Equal(10, (int)(await Sql("SELECT so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc)))!); // chưa trừ kho trước khi thu

            // 4. Thu ngân thu tiền → trừ kho (RB33); thu lặp bị chặn
            var tn = await NhanVien("NV0005");
            var tt = await Json(await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null));
            Assert.Equal("Đã thanh toán", tt.GetProperty("trangThai").GetString());
            Assert.Equal("NV0005", tt.GetProperty("maNvThu").GetString());
            Assert.Equal(6, (int)(await Sql("SELECT so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc)))!);
            Assert.Equal(HttpStatusCode.Conflict, (await tn.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null)).StatusCode);
            Assert.Equal(6, (int)(await Sql("SELECT so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc)))!);

            // Sau khi thanh toán hồ sơ bị khóa
            Assert.Equal(HttpStatusCode.Conflict, (await bs.PostAsJsonAsync($"/api/bac-si/ho-so/{hs}/don-thuoc",
                new { maThuoc = thuoc, soLuong = 1, lieuDung = "x" })).StatusCode);

            // Bộ lọc lịch sử của bác sĩ có tác dụng thật (từng bị binder bỏ qua vì tham số tên q)
            Assert.Equal(1, (await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so?q={hs}")).GetProperty("total").GetInt32());
            Assert.Equal(0, (await bs.GetFromJsonAsync<JsonElement>("/api/bac-si/ho-so?q=KHONG-CO-HO-SO-NAY")).GetProperty("total").GetInt32());
            Assert.Equal(0, (await bs.GetFromJsonAsync<JsonElement>($"/api/bac-si/ho-so?q={hs}&trangThai=Chờ khám")).GetProperty("total").GetInt32());

            // 5. Quản trị viên xem lại cả chuỗi (chỉ đọc)
            var qt = await NhanVien("NV0001");
            var qLh = await Json(await qt.GetAsync($"/api/quan-tri/lich-hen/{lh}"));
            Assert.Equal("Đã khám", qLh.GetProperty("trangThai").GetString());
            Assert.Equal(hs, qLh.GetProperty("maHoSo").GetString());
            Assert.Equal("NV0002", qLh.GetProperty("maNvXacNhan").GetString());
            var qHs = await Json(await qt.GetAsync($"/api/quan-tri/ho-so/{hs}"));
            Assert.Equal("Đã thanh toán", qHs.GetProperty("trangThai").GetString());
            Assert.Equal("Viêm họng cấp", qHs.GetProperty("chanDoanBenh").GetString());
            Assert.Equal(20000, qHs.GetProperty("tienThuoc").GetDecimal());
            var dsHs = await qt.GetFromJsonAsync<JsonElement>($"/api/quan-tri/ho-so?q={hs}&maNv=NV0004&trangThai=Đã thanh toán");
            var dongHs = dsHs.GetProperty("items").EnumerateArray().Single();
            Assert.Equal("Bác sĩ Mẫu B", dongHs.GetProperty("tenBacSi").GetString());
            Assert.Equal(0, (await qt.GetFromJsonAsync<JsonElement>($"/api/quan-tri/ho-so?q={hs}&maNv=NV0003")).GetProperty("total").GetInt32());
            var qHd = await Json(await qt.GetAsync($"/api/quan-tri/hoa-don/{hd}"));
            Assert.Equal(170000, qHd.GetProperty("tongTien").GetDecimal());
            Assert.Equal("Thu ngân Mẫu", qHd.GetProperty("tenNvThu").GetString());
            Assert.Single((await qt.GetFromJsonAsync<JsonElement>($"/api/quan-tri/hoa-don?q={hd}&trangThai=Đã thanh toán")).GetProperty("items").EnumerateArray());

            // Quản trị viên không có API thu tiền hay khám thay
            Assert.Equal(HttpStatusCode.Forbidden, (await qt.PostAsync($"/api/thu-ngan/hoa-don/{hd}/thanh-toan", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await qt.GetAsync($"/api/bac-si/ho-so/{hs}")).StatusCode);

            // Khách thấy lịch hẹn đã khám
            var cuaKhach = await kh.GetFromJsonAsync<JsonElement>("/api/lich-hen-cua-toi");
            Assert.Equal("Đã khám", cuaKhach.EnumerateArray().Single().GetProperty("trangThai").GetString());
        }
        finally
        {
            if (maKh is not null)
            {
                await Sql("DELETE d FROM Hoa_don d JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so WHERE hs.ma_kh = @k", ("@k", maKh));
                await Sql("DELETE ct FROM Chi_tiet_don_thuoc ct JOIN Ho_so_kham hs ON hs.ma_ho_so = ct.ma_ho_so WHERE hs.ma_kh = @k", ("@k", maKh));
                await Sql("DELETE FROM Ho_so_kham WHERE ma_kh = @k", ("@k", maKh));
                await Sql("DELETE FROM Lich_hen WHERE ma_kh = @k", ("@k", maKh));
                await Sql("DELETE FROM Khach_hang WHERE ma_kh = @k", ("@k", maKh));
            }
            await Sql("DELETE FROM Thuoc WHERE ma_thuoc = @t", ("@t", thuoc));
            await Sql("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec = @l", ("@l", lich));
            await Sql("DELETE FROM Ca_lam_viec WHERE ma_ca = @c", ("@c", ca));
        }
    }
}
