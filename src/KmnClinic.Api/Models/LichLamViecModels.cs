using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/// <summary>Một lịch làm việc kèm tên bác sĩ, khoa, phòng, ca và số lịch hẹn, hồ sơ liên quan.</summary>
public record LichLamViecItem(
    string MaLichLamViec, DateOnly NgayLamViec,
    string MaCa, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string MaNv, string HoTenNv, string? DuongDanAnh,
    string MaKhoa, string TenKhoa, string MaPhong, string TenPhong,
    int SoLichHen, int SoLichHenConHieuLuc, bool DaQua);

public class LichLamViecQuery
{
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public string? MaNv { get; set; }
    public string? MaKhoa { get; set; }
    public string? MaPhong { get; set; }
    public string? MaCa { get; set; }
}

public class LichLamViecRequest
{
    [Required(ErrorMessage = "Vui lòng chọn ngày làm việc.")]
    public DateOnly? NgayLamViec { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn bác sĩ.")]
    [MaxLength(10, ErrorMessage = "Mã bác sĩ không hợp lệ.")]
    public string MaNv { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn phòng.")]
    [MaxLength(10, ErrorMessage = "Mã phòng không hợp lệ.")]
    public string MaPhong { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn ca.")]
    [MaxLength(10, ErrorMessage = "Mã ca không hợp lệ.")]
    public string MaCa { get; set; } = "";
}

/// <summary>Xếp một bác sĩ vào cùng phòng, cùng ca cho nhiều ngày (lặp theo thứ trong tuần).</summary>
public class XepLichHangLoatRequest
{
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateOnly? TuNgay { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateOnly? DenNgay { get; set; }

    /// <summary>Thứ trong tuần: 1 = Thứ 2 ... 6 = Thứ 7, 0 = Chủ nhật (theo DayOfWeek).</summary>
    [Required(ErrorMessage = "Vui lòng chọn ít nhất một thứ trong tuần.")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một thứ trong tuần.")]
    public List<int> CacThu { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng chọn bác sĩ.")]
    [MaxLength(10, ErrorMessage = "Mã bác sĩ không hợp lệ.")]
    public string MaNv { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn phòng.")]
    [MaxLength(10, ErrorMessage = "Mã phòng không hợp lệ.")]
    public string MaPhong { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn ca.")]
    [MaxLength(10, ErrorMessage = "Mã ca không hợp lệ.")]
    public string MaCa { get; set; } = "";
}

public record XepLichHangLoatResponse(int SoLichDaTao, List<string> CacMa);

/// <summary>Bác sĩ để chọn khi xếp lịch hoặc lọc lịch (mọi nhân viên xem được, không có số điện thoại).</summary>
public record BacSiItem(string MaNv, string HoTen, string MaKhoa, string TenKhoa, string? DuongDanAnh);
