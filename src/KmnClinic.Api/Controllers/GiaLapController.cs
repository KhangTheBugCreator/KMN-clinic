using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Giả lập vai trò thay cho đăng nhập (quyết định 12/11/2026): chọn một người có sẵn trong database
/// và nhận token mang vai trò + mã của người đó. Các API nghiệp vụ vẫn phân quyền theo token này.
/// </summary>
[ApiController]
[Route("api/gia-lap")]
[AllowAnonymous]
public class GiaLapController : ControllerBase
{
    private static readonly string[] ChucVuHopLe = { "Quản trị viên", "Lễ tân", "Bác sĩ", "Thu ngân" };

    private readonly AuthRepository _repo;
    private readonly JwtService _jwt;

    public GiaLapController(AuthRepository repo, JwtService jwt)
    {
        _repo = repo;
        _jwt = jwt;
    }

    /// <summary>Nhân viên theo chức vụ, để chọn người giả lập.</summary>
    [HttpGet("nhan-vien")]
    public async Task<List<NhanVienGiaLapItem>> NhanVien([FromQuery] string chucVu, CancellationToken ct)
    {
        if (!ChucVuHopLe.Contains(chucVu))
            throw AppException.BadRequest("Chức vụ không hợp lệ.");
        return await _repo.DanhSachNhanVienAsync(chucVu, ct);
    }

    /// <summary>Tìm khách hàng theo số điện thoại hoặc họ tên.</summary>
    [HttpGet("khach-hang")]
    public Task<List<KhachHangGiaLapItem>> KhachHang([FromQuery] string? q, CancellationToken ct)
        => _repo.TraCuuKhachHangAsync(q, ct);

    /// <summary>Tạo khách hàng mới (không mật khẩu) rồi vào vai trò khách hàng đó luôn.</summary>
    [HttpPost("khach-hang")]
    public async Task<PhienResponse> TaoKhachHang(TaoKhachHangRequest req, CancellationToken ct)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (req.NgaySinh > homNay || req.NgaySinh < homNay.AddYears(-130))
            throw AppException.BadRequest("Ngày sinh không hợp lệ.");
        var ma = await _repo.TaoKhachHangAsync(req, ct);
        var nd = await _repo.TimKhachHangAsync(ma, ct) ?? throw new InvalidOperationException("Không đọc lại được khách vừa tạo.");
        return TaoPhien(nd);
    }

    /// <summary>Vào vai trò của một nhân viên hoặc khách hàng có sẵn.</summary>
    [HttpPost("vao-vai-tro")]
    public async Task<PhienResponse> VaoVaiTro(VaoVaiTroRequest req, CancellationToken ct)
    {
        var nd = req.Loai == "NV" ? await _repo.TimNhanVienAsync(req.Ma, ct) : await _repo.TimKhachHangAsync(req.Ma, ct);
        if (nd is null) throw AppException.NotFound("Không tìm thấy người dùng đã chọn.");
        if (string.IsNullOrEmpty(nd.VaiTro)) throw AppException.BadRequest("Chức vụ của nhân viên này không hợp lệ.");
        return TaoPhien(nd);
    }

    private PhienResponse TaoPhien(NguoiDungResponse nd)
    {
        var (token, hetHan) = _jwt.TaoToken(new NguoiDungDangNhap(nd.Ma, nd.Loai, nd.HoTen, nd.VaiTro, nd.MaKhoa));
        return new PhienResponse(token, hetHan, nd);
    }
}
