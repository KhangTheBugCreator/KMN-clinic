namespace KmnClinic.Api.Models;

/* M9: thống kê cho dashboard quản trị và dữ liệu 5 báo cáo Excel. Mọi số liệu đếm, cộng trực tiếp từ database.
   "Doanh thu" chỉ tính hóa đơn Đã thanh toán, theo ngày thanh toán. */

/// <summary>Khoảng ngày [TuNgay, DenNgay], cả hai đầu đều tính. Bỏ trống: tháng hiện tại.</summary>
public class KyQuery
{
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
}

public class DoanhThuQuery : KyQuery
{
    /// <summary>ngay hoặc thang.</summary>
    public string? Kieu { get; set; }
}

public record TongQuanThongKe(DateOnly TuNgay, DateOnly DenNgay,
    int SoNhanVien, int SoBacSi, int SoKhachHang, int KhachCoKhamTrongKy,
    int LichHenTrongKy, int LichHenDaKham, int LichHenDaHuy, int LichHenChoXacNhan,
    int HoSoTrongKy, int VangLaiTrongKy,
    int HoaDonDaThanhToan, decimal DoanhThu, int HoaDonChoThu, decimal TienChoThu,
    int ThuocSapHet, int NguongSapHet);

public record DiemDoanhThu(string Nhan, decimal DoanhThu, int SoHoaDon);
public record DiemLuotKham(DateOnly Ngay, int CoHen, int VangLai);
public record ThuocKeNhieu(string MaThuoc, string TenThuoc, decimal HamLuong, string DonViTinh, int SoLuong, decimal ThanhTien);

/* ---------- Báo cáo ---------- */
public record BcDoanhThuDong(string MaHoaDon, DateTime NgayGioThanhToan, string MaKh, string TenKhach, string TenBacSi, string TenKhoa,
    decimal TienCongKham, decimal TienThuoc, decimal TongTien, string TenThuNgan);
public record BcLuotKhamDong(DateOnly Ngay, string TenKhoa, string MaNv, string TenBacSi, int CoHen, int VangLai, int Tong);
public record BcLichHenDong(string MaLichHen, DateTime NgayGioDat, string TenKhach, string SdtKhach, string TenBacSi, string TenKhoa,
    DateOnly NgayKham, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc, string TrangThai);
public record BcTonKhoDong(string MaThuoc, string TenThuoc, string Nsx, decimal HamLuong, string DonViTinh, decimal DonGia, int SoLuongTon, string CanhBao);
public record BcSuDungThuocDong(string MaThuoc, string TenThuoc, decimal HamLuong, string DonViTinh, int TongSoLuong, int SoDon, decimal ThanhTien);

public record BaoCao<T>(string TieuDe, DateOnly? TuNgay, DateOnly? DenNgay, DateTime LapLuc, List<T> Dong);
