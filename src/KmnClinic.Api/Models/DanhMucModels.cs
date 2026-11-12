using System.ComponentModel.DataAnnotations;

namespace KmnClinic.Api.Models;

/// <summary>Kết quả có phân trang dùng chung.</summary>
public record TrangKetQua<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Tham số tìm kiếm + phân trang (?q=&page=&pageSize=).</summary>
/// <summary>Tham số tìm kiếm/phân trang. Tên tham số action không được là "q" (trùng khóa query sẽ làm binder đọc theo tiền tố q.Q).</summary>
public class TimKiemQuery
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int TrangHopLe => Math.Max(1, Page);
    public int CoTrangHopLe => Math.Clamp(PageSize, 1, 100);
    public int BoQua => (TrangHopLe - 1) * CoTrangHopLe;

    /// <summary>Chuỗi tìm kiếm dạng LIKE, đã escape ký tự đặc biệt; null nếu không tìm.</summary>
    public string? MauTim => string.IsNullOrWhiteSpace(Q) ? null
        : "%" + Q.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}

/* ---------------- Khoa ---------------- */
public record KhoaItem(string MaKhoa, string TenKhoa, string? MoTa, int SoPhong, int SoBacSi);

public class KhoaRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
    [MaxLength(50, ErrorMessage = "Tên khoa tối đa 50 ký tự.")]
    public string TenKhoa { get; set; } = "";

    [MaxLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
    public string? MoTa { get; set; }
}

/* ---------------- Phòng ---------------- */
public record PhongItem(string MaPhong, string TenPhong, string MaKhoa, string TenKhoa, int SoLichLamViec);

public class PhongRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [MaxLength(50, ErrorMessage = "Tên phòng tối đa 50 ký tự.")]
    public string TenPhong { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    [MaxLength(10)]
    public string MaKhoa { get; set; } = "";
}

public record LichSuDungPhongItem(string MaLichLamViec, DateOnly NgayLamViec, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc,
    string MaNv, string HoTenNv, int SoLichHen);

/* ---------------- Ca ---------------- */
public record CaItem(string MaCa, string TenCa, TimeOnly GioBatDau, TimeOnly GioKetThuc, int SoLichLamViec);

public class CaRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên ca.")]
    [MaxLength(20, ErrorMessage = "Tên ca tối đa 20 ký tự.")]
    public string TenCa { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu.")]
    public TimeOnly? GioBatDau { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giờ kết thúc.")]
    public TimeOnly? GioKetThuc { get; set; }
}

/* ---------------- Thuốc ---------------- */
public record ThuocItem(string MaThuoc, string TenThuoc, string Nsx, decimal HamLuong, string DonViTinh,
    decimal DonGiaThuoc, int SoLuongTon, string? MoTa, bool SapHet);

public class ThuocRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên thuốc.")]
    [MaxLength(100, ErrorMessage = "Tên thuốc tối đa 100 ký tự.")]
    public string TenThuoc { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập nhà sản xuất.")]
    [MaxLength(200, ErrorMessage = "Tên nhà sản xuất tối đa 200 ký tự.")]
    public string Nsx { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập hàm lượng.")]
    [Range(typeof(decimal), "0.01", "99999999.99", ErrorMessage = "Hàm lượng phải lớn hơn 0.")]
    public decimal? HamLuong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    public string DonViTinh { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập đơn giá.")]
    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Đơn giá phải lớn hơn 0.")]
    public decimal? DonGiaThuoc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn.")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không được âm.")]
    public int? SoLuongTon { get; set; }

    [MaxLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
    public string? MoTa { get; set; }
}
