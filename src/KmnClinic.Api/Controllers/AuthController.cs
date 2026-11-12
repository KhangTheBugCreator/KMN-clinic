using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AuthRepository _repo;

    public AuthController(AuthRepository repo) => _repo = repo;

    /// <summary>Người đang được giả lập, đọc lại từ database theo mã trong token.</summary>
    [HttpGet("me")]
    public async Task<NguoiDungResponse> Me(CancellationToken ct)
    {
        var ma = User.MaNguoiDung();
        var nd = User.LaKhachHang() ? await _repo.TimKhachHangAsync(ma, ct) : await _repo.TimNhanVienAsync(ma, ct);
        return nd ?? throw AppException.Unauthorized("Người dùng này không còn trong hệ thống. Vui lòng chọn lại vai trò.");
    }
}
