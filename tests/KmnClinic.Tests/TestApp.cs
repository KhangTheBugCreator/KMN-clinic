using KmnClinic.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KmnClinic.Tests;

/// <summary>
/// Chạy API thật trong bộ nhớ, nối tới database PHONGKHAM dùng để kiểm thử.
/// Chuỗi kết nối lấy từ biến môi trường KMN_TEST_DB (không ghi vào mã nguồn).
/// Database cần có dữ liệu trong database/du-lieu-mau-phat-trien.sql.
/// </summary>
public class TestApp : WebApplicationFactory<Program>
{

    public static string ChuoiKetNoi =>
        Environment.GetEnvironmentVariable("KMN_TEST_DB")
        ?? throw new InvalidOperationException("Đặt biến môi trường KMN_TEST_DB là chuỗi kết nối tới database kiểm thử.");

    /// <summary>
    /// Chạy một câu SQL chuẩn bị hoặc dọn dữ liệu kiểm thử, trả giá trị đầu tiên. Các lớp test chạy song song nên
    /// lệnh dọn dẹp có thể bị SQL Server chọn làm nạn nhân deadlock (1205): thử lại tối đa 5 lần.
    /// </summary>
    public static async Task<object?> SqlAsync(string sql, params (string, object)[] ps)
    {
        for (var lan = 1; ; lan++)
        {
            try
            {
                await using var conn = new Microsoft.Data.SqlClient.SqlConnection(ChuoiKetNoi);
                await conn.OpenAsync();
                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
                return await cmd.ExecuteScalarAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number == 1205 && lan < 5)
            {
                await Task.Delay(50 * lan);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__PhongKham", ChuoiKetNoi);
        Environment.SetEnvironmentVariable("Jwt__Key", "khoa-chi-dung-cho-kiem-thu-0123456789abcdef");
        builder.UseEnvironment("Development");
        builder.ConfigureServices(s => s.AddControllers().AddApplicationPart(typeof(KiemThuController).Assembly));
    }
}

/// <summary>Các API chỉ có trong lúc kiểm thử, dùng để thử phân quyền và lỗi trigger.</summary>
[ApiController]
[Route("api/kiem-thu")]
public class KiemThuController : ControllerBase
{
    [HttpGet("chi-quan-tri")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public IActionResult ChiQuanTri() => Ok(new { ok = true });

    [HttpGet("chi-nhan-vien")]
    [Authorize(Roles = VaiTro.NhanVien)]
    public IActionResult ChiNhanVien() => Ok(new { ok = true });

    /// <summary>
    /// Thêm lịch làm việc vào ngày đã qua để trigger TG_LICH_LAM_VIEC_KiemTra chặn (RB9),
    /// trong transaction rồi rollback nên không để lại dữ liệu.
    /// </summary>
    [HttpPost("lich-qua-khu")]
    [AllowAnonymous]
    public async Task<IActionResult> LichQuaKhu([FromServices] Db db)
    {
        await db.InTransactionAsync<int>(async (conn, tx) =>
        {
            await using var cmd = conn.Cmd(@"
INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
VALUES ('LVTEST01', '2020-01-01', 'NV0003', 'P0001', 'CA0001')", tx);
            return await cmd.ExecuteNonQueryAsync();
        });
        return Ok();
    }
}
