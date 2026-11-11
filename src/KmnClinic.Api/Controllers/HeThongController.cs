using KmnClinic.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

[ApiController]
[Route("api/he-thong")]
public class HeThongController : ControllerBase
{
    private readonly Db _db;

    public HeThongController(Db db) => _db = db;

    /// <summary>Kiểm tra API và kết nối database còn hoạt động. Không trả thông tin cấu hình.</summary>
    [HttpGet("trang-thai")]
    [AllowAnonymous]
    public async Task<IActionResult> TrangThai(CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd("SELECT 1");
        await cmd.ExecuteScalarAsync(ct);
        return Ok(new { api = "ok", database = "ok", thoiGianMayChu = DateTime.Now });
    }
}
