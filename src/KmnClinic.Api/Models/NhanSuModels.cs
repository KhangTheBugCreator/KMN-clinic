using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/* ---------------- Nhân viên (M2) ---------------- */

/// <summary>Một dòng nhân viên trả về frontend. Không bao giờ có mat_khau_nv.</summary>
public record NhanVienItem(string MaNv, string HoTen, DateOnly? NgaySinh, string SoDienThoai, string ChucVu,
    string? MaKhoa, string? TenKhoa, string? DuongDanAnh, int SoLienKet);

/// <summary>Dùng chung cho thêm và sửa. Mật khẩu chỉ bắt buộc khi thêm (kiểm tra ở controller).</summary>
public class NhanVienRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(50, ErrorMessage = "Họ tên tối đa 50 ký tự.")]
    public string HoTen { get; set; } = "";

    public DateOnly? NgaySinh { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn chức vụ.")]
    [RegularExpression("^(Quản trị viên|Lễ tân|Bác sĩ|Thu ngân)$", ErrorMessage = "Chức vụ chỉ nhận Quản trị viên, Lễ tân, Bác sĩ hoặc Thu ngân.")]
    public string ChucVu { get; set; } = "";

    [MaxLength(10, ErrorMessage = "Mã khoa không hợp lệ.")]
    public string? MaKhoa { get; set; }

    [MaxLength(1000, ErrorMessage = "Đường dẫn ảnh tối đa 1000 ký tự.")]
    public string? DuongDanAnh { get; set; }

    /// <summary>RB38: tối thiểu 8 ký tự. Chỉ dùng khi thêm mới; website không đọc lại.</summary>
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
    [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự.")]
    public string? MatKhau { get; set; }
}

public class NhanVienQuery : TimKiemQuery
{
    public string? ChucVu { get; set; }
    public string? MaKhoa { get; set; }
}

/* ---------------- Khách hàng (M2) ---------------- */

/// <summary>Một dòng khách hàng. Không có mat_khau_kh.</summary>
public record KhachHangItem(string MaKh, string HoTen, DateOnly NgaySinh, string GioiTinh, string SoDienThoai,
    string? DiaChi, int SoLichHen, int SoHoSo);

public class KhachHangRequest
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

public class KhachHangQuery : TimKiemQuery
{
    public string? GioiTinh { get; set; }
}
