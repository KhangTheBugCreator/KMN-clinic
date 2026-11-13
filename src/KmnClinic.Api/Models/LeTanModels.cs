using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/* M6: lễ tân xác nhận, hủy lịch hẹn và tiếp nhận khách (có hẹn, vãng lai). */

public record LichHenLeTanItem(string MaLichHen, DateTime NgayGioDat, string? TrieuChungBanDau, string TrangThai,
    string MaKh, string TenKhach, string SdtKhach, string GioiTinh, DateOnly NgaySinh,
    string MaLichLamViec, DateOnly NgayKham, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string MaNvBacSi, string TenBacSi, string TenKhoa, string TenPhong,
    string? MaNvXacNhan, string? TenNvXacNhan, string? MaHoSo);

public class LichHenLeTanQuery : TimKiemQuery
{
    /// <summary>Ngày khám (ngày của lịch làm việc). Bỏ trống: tất cả ngày.</summary>
    public DateOnly? Ngay { get; set; }
    public string? TrangThai { get; set; }
    public string? MaNv { get; set; }
}

public class TiepNhanRequest
{
    [Required(ErrorMessage = "Vui lòng nhập triệu chứng.")]
    [StringLength(255, ErrorMessage = "Triệu chứng tối đa 255 ký tự.")]
    public string? TrieuChung { get; set; }
}

public class TiepNhanVangLaiRequest : TiepNhanRequest
{
    [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
    [StringLength(10)]
    public string? MaKh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn bác sĩ đang trực.")]
    [StringLength(10)]
    public string? MaNvKham { get; set; }
}

/// <summary>Bác sĩ có ca trực bao trùm thời điểm hiện tại (cho phép sớm 30 phút trước ca, RB41).</summary>
public record BacSiDangTrucItem(string MaNv, string HoTen, string? DuongDanAnh, string TenKhoa, string TenPhong,
    string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc, int SoChoKham);

/// <summary>Hồ sơ khám lễ tân đã lập. ChoKham = chưa có hóa đơn (bác sĩ chưa hoàn tất, Q4).</summary>
public record HoSoTiepNhanItem(string MaHoSo, DateTime NgayGioKham, string TrieuChung, string? MaLichHen,
    string MaKh, string TenKhach, string SdtKhach, string MaNvKham, string TenBacSi, string TenKhoa,
    string MaNvTiepNhan, string TenNvTiepNhan, bool ChoKham);

public record TongQuanLeTan(int ChoXacNhan, int LichHenHomNay, int DaXacNhanHomNay, int DaTiepNhanHomNay,
    int VangLaiHomNay, int BacSiDangTruc);
