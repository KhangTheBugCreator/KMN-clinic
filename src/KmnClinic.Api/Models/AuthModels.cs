using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

public class DangNhapRequest
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MaxLength(255)]
    public string MatKhau { get; set; } = "";
}

public class DoiMatKhauRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [MaxLength(255)]
    public string MatKhauCu { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    [MaxLength(255)]
    public string MatKhauMoi { get; set; } = "";
}

/// <summary>Thông tin phiên trả về frontend. Không bao giờ chứa mật khẩu.</summary>
public record PhienDangNhapResponse(
    string Token,
    DateTime HetHan,
    NguoiDungResponse NguoiDung);

public record NguoiDungResponse(
    string Ma,
    string Loai,          // "KH" hoặc "NV"
    string HoTen,
    string VaiTro,        // KhachHang | QuanTriVien | LeTan | BacSi | ThuNgan
    string? ChucVu,       // nguyên văn Nhan_vien.chuc_vu
    string? MaKhoa,
    string? TenKhoa,
    string SoDienThoai,
    string? DuongDanAnh);
