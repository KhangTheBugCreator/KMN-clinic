using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/// <summary>Vào một vai trò giả lập: chọn người có sẵn trong database (không có mật khẩu).</summary>
public class VaoVaiTroRequest
{
    [Required(ErrorMessage = "Vui lòng chọn loại người dùng.")]
    [RegularExpression("^(NV|KH)$", ErrorMessage = "Loại người dùng chỉ nhận NV hoặc KH.")]
    public string Loai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn người dùng.")]
    [MaxLength(10, ErrorMessage = "Mã người dùng không hợp lệ.")]
    public string Ma { get; set; } = "";
}

/// <summary>Tạo khách hàng mới từ trang Chọn vai trò (mat_khau_kh để NULL).</summary>
public class TaoKhachHangRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(50, ErrorMessage = "Họ tên tối đa 50 ký tự.")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
    public DateOnly? NgaySinh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    [RegularExpression("^(Nam|Nữ)$", ErrorMessage = "Giới tính chỉ nhận Nam hoặc Nữ.")]
    public string GioiTinh { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string SoDienThoai { get; set; } = "";

    [MaxLength(100, ErrorMessage = "Địa chỉ tối đa 100 ký tự.")]
    public string? DiaChi { get; set; }
}

public record PhienResponse(string Token, DateTime HetHan, NguoiDungResponse NguoiDung);

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

public record NhanVienGiaLapItem(string Ma, string HoTen, string ChucVu, string? TenKhoa, string? DuongDanAnh);

public record KhachHangGiaLapItem(string Ma, string HoTen, string SoDienThoai, int NamSinh, string GioiTinh);
