using KmnClinic.Api.Infrastructure;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Tests;

/// <summary>Đổi lỗi SQL thành câu tiếng Việt, và sinh mã không trùng khi chạy đồng thời.</summary>
public class SqlTests
{
    private static async Task<SqlException> BatLoi(string sql)
    {
        await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        try
        {
            await using var cmd = new SqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) { return ex; }
        finally { try { tx.Rollback(); } catch { } }
        throw new Xunit.Sdk.XunitException("Câu lệnh lẽ ra phải lỗi: " + sql);
    }

    [Fact]
    public async Task Trung_unique_tra_409_theo_ten_rang_buoc()
    {
        var ex = await BatLoi("INSERT INTO Khoa (ma_khoa, ten_khoa) VALUES ('KTEST1', N'Nội tổng quát')");
        Assert.Equal((409, "Tên khoa đã tồn tại."), SqlErrorTranslator.Translate(ex));
    }

    [Fact]
    public async Task Vi_pham_check_tra_400()
    {
        var ex = await BatLoi(@"INSERT INTO Khach_hang (ma_kh, ho_ten_kh, ngay_sinh_kh, gioi_tinh_kh, sdt_kh)
                                VALUES ('KTEST1', N'Thử', '2000-01-01', N'Khác', '0987654321')");
        Assert.Equal((400, "Giới tính chỉ nhận Nam hoặc Nữ."), SqlErrorTranslator.Translate(ex));
    }

    [Fact]
    public async Task Bac_si_khong_co_khoa_tra_400()
    {
        var ex = await BatLoi(@"INSERT INTO Nhan_vien (ma_nv, ho_ten_nv, sdt_nv, chuc_vu, mat_khau_nv)
                                VALUES ('NVTEST1', N'Thử', '0987654321', N'Bác sĩ', 'matkhau123')");
        Assert.Equal((400, "Bác sĩ phải thuộc một khoa; nhân viên khác không thuộc khoa nào."), SqlErrorTranslator.Translate(ex));
    }

    [Fact]
    public async Task Khoa_ngoai_khong_ton_tai_tra_409()
    {
        var ex = await BatLoi("INSERT INTO Phong_lam_viec (ma_phong, ten_phong, ma_khoa) VALUES ('PTEST1', N'Phòng thử', 'KHONGCO')");
        Assert.Equal(409, SqlErrorTranslator.Translate(ex)!.Value.Status);
    }

    [Fact]
    public async Task Xoa_du_lieu_dang_dung_tra_409()
    {
        var ex = await BatLoi("DELETE FROM Khoa WHERE ma_khoa = 'K0001'");
        Assert.Equal((409, "Không thể xóa vì dữ liệu này đang được sử dụng ở nơi khác."), SqlErrorTranslator.Translate(ex));
    }

    [Fact]
    public async Task Trigger_throw_giu_nguyen_cau_tieng_viet()
    {
        var ex = await BatLoi(@"INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
                                VALUES ('LVTEST1', DATEADD(day, 1, CAST(GETDATE() AS date)), 'NV0001', 'P0001', 'CA0001')");
        Assert.Equal((400, "RB10: Nhân viên được xếp lịch phải là bác sĩ."), SqlErrorTranslator.Translate(ex));
    }

    [Fact]
    public async Task Sinh_ma_ke_tiep_theo_so_lon_nhat()
    {
        await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.NhanVien);
        // Đọc số lớn nhất trong cùng transaction (bộ sinh mã đã khóa bảng), vì test khác có thể đang thêm nhân viên song song
        await using var cmd = new SqlCommand(
            "SELECT MAX(TRY_CAST(SUBSTRING(ma_nv, 3, 10) AS int)) FROM Nhan_vien WHERE ma_nv LIKE 'NV[0-9]%'", conn, tx);
        var lonNhat = (int)(await cmd.ExecuteScalarAsync())!;
        tx.Rollback();
        Assert.Equal("NV" + (lonNhat + 1).ToString("D4"), ma);
        Assert.True(lonNhat >= 5); // dữ liệu mẫu có NV0001..NV0005
    }

    [Fact]
    public async Task Sinh_ma_dong_thoi_khong_trung()
    {
        const int soLuong = 12;
        var ketQua = new System.Collections.Concurrent.ConcurrentBag<string>();
        try
        {
            await Task.WhenAll(Enumerable.Range(0, soLuong).Select(async i =>
            {
                await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
                await conn.OpenAsync();
                await using var tx = conn.BeginTransaction();
                var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.Ca);
                await Task.Delay(20); // giữ transaction mở để các yêu cầu chồng lên nhau
                await using var cmd = new SqlCommand(
                    "INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@ma, @ten, '08:00', '09:00')", conn, tx);
                cmd.Parameters.AddWithValue("@ma", ma);
                cmd.Parameters.AddWithValue("@ten", "Ca thử " + i);
                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
                ketQua.Add(ma);
            }));
            Assert.Equal(soLuong, ketQua.Distinct().Count());
        }
        finally
        {
            await using var conn = new SqlConnection(TestApp.ChuoiKetNoi);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("DELETE FROM Ca_lam_viec WHERE ten_ca LIKE N'Ca thử %'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
