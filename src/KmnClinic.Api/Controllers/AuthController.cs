using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KmnClinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string SaiThongTin = "Số điện thoại hoặc mật khẩu không đúng.";

    private readonly AuthRepository _repo;
    private readonly IPasswordService _pw;
    private readonly JwtService _jwt;

    public AuthController(AuthRepository repo, IPasswordService pw, JwtService jwt)
    {
        _repo = repo;
        _pw = pw;
        _jwt = jwt;
    }

    /// <summary>Đăng nhập nhân viên bằng sdt_nv (RB35).</summary>
    [HttpPost("nhan-vien/dang-nhap")]
    [AllowAnonymous]
    [EnableRateLimiting("dang-nhap")]
    public async Task<PhienDangNhapResponse> DangNhapNhanVien(DangNhapRequest req, CancellationToken ct)
    {
        var tk = await _repo.TimNhanVienTheoSdtAsync(req.SoDienThoai, ct);
        if (tk is null || !_pw.KhopMatKhau(req.MatKhau, tk.MatKhauDangLuu))
            throw AppException.Unauthorized(SaiThongTin);
        if (string.IsNullOrEmpty(tk.NguoiDung.VaiTro))
            throw AppException.Forbidden("Chức vụ của tài khoản không hợp lệ.");
        return TaoPhien(tk.NguoiDung);
    }

    /// <summary>
    /// Đăng nhập khách hàng bằng sdt_kh. Khách vãng lai (mat_khau_kh NULL) chưa có tài khoản nên không đăng nhập được.
    /// </summary>
    [HttpPost("khach-hang/dang-nhap")]
    [AllowAnonymous]
    [EnableRateLimiting("dang-nhap")]
    public async Task<PhienDangNhapResponse> DangNhapKhachHang(DangNhapRequest req, CancellationToken ct)
    {
        var tk = await _repo.TimKhachHangTheoSdtAsync(req.SoDienThoai, ct);
        if (tk is null || !_pw.KhopMatKhau(req.MatKhau, tk.MatKhauDangLuu))
            throw AppException.Unauthorized(SaiThongTin);
        return TaoPhien(tk.NguoiDung);
    }

    /// <summary>Thông tin người đang đăng nhập, đọc lại từ database.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<NguoiDungResponse> Me(CancellationToken ct)
        => (await TimTaiKhoanHienTaiAsync(ct)).NguoiDung;

    /// <summary>Đổi mật khẩu của chính mình (RB38, RB39).</summary>
    [HttpPut("doi-mat-khau")]
    [Authorize]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauRequest req, CancellationToken ct)
    {
        var tk = await TimTaiKhoanHienTaiAsync(ct);
        if (!_pw.KhopMatKhau(req.MatKhauCu, tk.MatKhauDangLuu))
            throw AppException.BadRequest("Mật khẩu hiện tại không đúng.");
        QuyTacMatKhau.KiemTra(req.MatKhauMoi);
        if (req.MatKhauMoi == req.MatKhauCu)
            throw AppException.BadRequest("Mật khẩu mới phải khác mật khẩu hiện tại.");

        await _repo.CapNhatMatKhauAsync(tk.NguoiDung.Loai, tk.NguoiDung.Ma, _pw.ChuanBiLuu(req.MatKhauMoi), ct);
        return Ok(new { message = "Đã đổi mật khẩu." });
    }

    private async Task<AuthRepository.TaiKhoan> TimTaiKhoanHienTaiAsync(CancellationToken ct)
    {
        var ma = User.MaNguoiDung();
        var tk = User.LaKhachHang()
            ? await _repo.TimKhachHangTheoMaAsync(ma, ct)
            : await _repo.TimNhanVienTheoMaAsync(ma, ct);
        return tk ?? throw AppException.Unauthorized("Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.");
    }

    private PhienDangNhapResponse TaoPhien(NguoiDungResponse nd)
    {
        var (token, hetHan) = _jwt.TaoToken(new NguoiDungDangNhap(nd.Ma, nd.Loai, nd.HoTen, nd.VaiTro, nd.MaKhoa));
        return new PhienDangNhapResponse(token, hetHan, nd);
    }
}
