namespace KmnClinic.Api.Models;

/* Dữ liệu cho website công khai (M4). Chỉ gồm thông tin khách cần xem: không số điện thoại, không mã nội bộ khác,
   không số lượng lịch hẹn. */

public record TongQuanCongKhai(int SoKhoa, int SoBacSi, int SoBacSiCoLich7Ngay);

public record KhoaCongKhai(string MaKhoa, string TenKhoa, string? MoTa, int SoBacSi, List<string> CacPhong);

/// <summary>Một ca bác sĩ trực mà khách còn đặt được (ca chưa bắt đầu, RB16).</summary>
public record LichKhamCongKhai(string MaLichLamViec, DateOnly Ngay, string MaCa, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string MaNv, string HoTen, string MaKhoa, string TenKhoa, string TenPhong);

public record BacSiCongKhai(string MaNv, string HoTen, string MaKhoa, string TenKhoa, string? MoTaKhoa, string? DuongDanAnh,
    List<LichKhamCongKhai> LichSapToi);

public class BacSiCongKhaiQuery : TimKiemQuery
{
    public string? MaKhoa { get; set; }
    public DateOnly? Ngay { get; set; }
}

public class LichKhamQuery
{
    public DateOnly? TuNgay { get; set; }
    public int SoNgay { get; set; } = 7;
    public string? MaKhoa { get; set; }
    public string? MaNv { get; set; }
}
