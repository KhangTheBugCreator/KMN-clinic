using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

public static class TrangThaiLichHen
{
    public const string DaDat = "Đã đặt";
    public const string DaXacNhan = "Đã xác nhận";
    public const string DaKham = "Đã khám";
    public const string DaHuy = "Đã hủy";
    public static readonly string[] TatCa = { DaDat, DaXacNhan, DaKham, DaHuy };
}

/// <summary>Lịch hẹn khách hàng xem (M5). Không có thông tin nội bộ như mã lễ tân.</summary>
public record LichHenCuaToiItem(string MaLichHen, DateTime NgayGioDat, string? TrieuChungBanDau, string TrangThai,
    string MaLichLamViec, DateOnly NgayKham, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string MaNv, string TenBacSi, string? DuongDanAnh, string TenKhoa, string TenPhong, bool CoTheHuy);

public class DatLichRequest
{
    [Required(ErrorMessage = "Vui lòng chọn lịch làm việc của bác sĩ.")]
    [StringLength(10)]
    public string? MaLichLamViec { get; set; }

    [StringLength(255, ErrorMessage = "Triệu chứng ban đầu tối đa 255 ký tự.")]
    public string? TrieuChungBanDau { get; set; }
}
