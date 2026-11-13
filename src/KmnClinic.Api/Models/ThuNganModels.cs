namespace KmnClinic.Api.Models;

/* M8: thu ngân xem hóa đơn, xác nhận thanh toán (RB30–RB33), in và xuất PDF. */

public static class TrangThaiHoaDon
{
    public const string ChuaThanhToan = "Chưa thanh toán";
    public const string DaThanhToan = "Đã thanh toán";
    public static readonly string[] TatCa = { ChuaThanhToan, DaThanhToan };
}

public record HoaDonItem(string MaHoaDon, decimal TongTien, string TrangThai, DateTime? NgayGioThanhToan,
    string MaHoSo, DateTime NgayGioKham, string MaKh, string TenKhach, string SdtKhach,
    string MaNvKham, string TenBacSi, string TenKhoa, int SoThuoc, string? MaNvThu, string? TenNvThu);

public record DongHoaDon(string MaThuoc, string TenThuoc, decimal HamLuong, string DonViTinh, int SoLuong, string LieuDung,
    decimal DonGia, decimal ThanhTien, int SoLuongTon);

public record HoaDonChiTiet(string MaHoaDon, decimal TongTien, string TrangThai, DateTime? NgayGioThanhToan,
    string? MaNvThu, string? TenNvThu,
    string MaHoSo, DateTime NgayGioKham, string TrieuChung, string? ChanDoanBenh, string? LoiDan, decimal TienCongKham,
    string MaKh, string TenKhach, string SdtKhach, string GioiTinh, DateOnly NgaySinh, string? DiaChi,
    string MaNvKham, string TenBacSi, string TenKhoa, List<DongHoaDon> Thuoc, decimal TienThuoc);

public class HoaDonQuery : TimKiemQuery
{
    public string? TrangThai { get; set; }
    /// <summary>Lọc theo ngày thanh toán (chỉ áp dụng cho hóa đơn đã thanh toán).</summary>
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    /// <summary>Chỉ hóa đơn do chính thu ngân đang đăng nhập thu.</summary>
    public bool CuaToi { get; set; }
}

public record TongQuanThuNgan(int ChoThanhToan, decimal TienChoThanhToan, int DaThuHomNay, decimal DoanhThuHomNay,
    int DoToiThuHomNay, int[] TheoGio);
