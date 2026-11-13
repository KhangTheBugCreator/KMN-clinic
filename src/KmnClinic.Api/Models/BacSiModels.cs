using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/* M7: bác sĩ khám bệnh, kê đơn, hoàn tất (tạo hóa đơn Chưa thanh toán, Q4) và xem lịch sử (chỉ đọc, Q13). */

/// <summary>Trạng thái hồ sơ suy ra từ dữ liệu, không lưu trong database.</summary>
public static class TrangThaiHoSo
{
    /// <summary>Chưa có chẩn đoán, chưa có thuốc, chưa có hóa đơn.</summary>
    public const string ChoKham = "Chờ khám";
    /// <summary>Đã ghi chẩn đoán hoặc kê thuốc nhưng chưa hoàn tất.</summary>
    public const string DangKham = "Đang khám";
    /// <summary>Đã có hóa đơn Chưa thanh toán, hồ sơ bị khóa.</summary>
    public const string HoanTat = "Hoàn tất";
    public const string DaThanhToan = "Đã thanh toán";
    public static readonly string[] TatCa = { ChoKham, DangKham, HoanTat, DaThanhToan };
}

/// <summary>Một dòng hồ sơ trong hàng chờ hoặc lịch sử của bác sĩ.</summary>
public record HoSoBacSiItem(string MaHoSo, DateTime NgayGioKham, string TrieuChung, string? ChanDoanBenh,
    string? MaLichHen, string MaKh, string TenKhach, string GioiTinh, DateOnly NgaySinh,
    int SoThuoc, string TrangThai, string? MaHoaDon, decimal? TongTien,
    string MaNvKham, string TenBacSi, string TenKhoa);

public record ThuocTrongDon(string MaThuoc, string TenThuoc, decimal HamLuong, string DonViTinh, int SoLuong,
    string LieuDung, decimal DonGia, decimal ThanhTien, int SoLuongTon);

public record HoaDonTomTat(string MaHoaDon, decimal TongTien, string TrangThai, DateTime? NgayGioThanhToan);

public record HoSoChiTiet(string MaHoSo, DateTime NgayGioKham, string TrieuChung, string? ChanDoanBenh, string? LoiDan,
    decimal TienCongKham, string? MaLichHen, string MaKh, string TenKhach, string GioiTinh, DateOnly NgaySinh,
    string MaNvKham, string TenBacSi, string TenKhoa, string TenNvTiepNhan, string TrangThai,
    List<ThuocTrongDon> DonThuoc, decimal TienThuoc, HoaDonTomTat? HoaDon)
{
    public bool DaKhoa => HoaDon is not null;
}

/// <summary>Lần khám trước của bệnh nhân (mọi bác sĩ), chỉ lần đã hoàn tất.</summary>
public record LanKhamTruoc(string MaHoSo, DateTime NgayGioKham, string TenBacSi, string TenKhoa, string TrieuChung,
    string? ChanDoanBenh, string? LoiDan, List<string> Thuoc);

public class HoSoBacSiQuery : TimKiemQuery
{
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public string? TrangThai { get; set; }
}

/// <summary>Quản trị viên lọc hồ sơ của mọi bác sĩ (M10, chỉ đọc).</summary>
public class HoSoQuanTriQuery : HoSoBacSiQuery
{
    public string? MaNv { get; set; }
}

public class KhamBenhRequest
{
    [Required(ErrorMessage = "Vui lòng nhập triệu chứng.")]
    [StringLength(255, ErrorMessage = "Triệu chứng tối đa 255 ký tự.")]
    public string? TrieuChung { get; set; }

    [StringLength(255, ErrorMessage = "Chẩn đoán tối đa 255 ký tự.")]
    public string? ChanDoanBenh { get; set; }

    [StringLength(255, ErrorMessage = "Lời dặn tối đa 255 ký tự.")]
    public string? LoiDan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiền công khám.")]
    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Tiền công khám phải từ 0 đến 999.999.999.999 đ.")]
    public decimal? TienCongKham { get; set; }
}

public class SuaThuocTrongDonRequest
{
    [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
    [Range(1, 100000, ErrorMessage = "Số lượng phải từ 1 đến 100.000.")]
    public int? SoLuong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập liều dùng.")]
    [StringLength(255, ErrorMessage = "Liều dùng tối đa 255 ký tự.")]
    public string? LieuDung { get; set; }
}

public class KeThuocRequest : SuaThuocTrongDonRequest
{
    [Required(ErrorMessage = "Vui lòng chọn thuốc.")]
    [StringLength(10)]
    public string? MaThuoc { get; set; }
}

public record LichTrucSapToi(string MaLichLamViec, DateOnly Ngay, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string TenPhong, int SoLichHen);

public record TongQuanBacSi(int ChoKham, int DangKham, int HoanTatHomNay, int TongHoSoHomNay,
    LichTrucSapToi? CaHienTai, List<LichTrucSapToi> LichSapToi);
