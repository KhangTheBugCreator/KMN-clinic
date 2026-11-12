namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Tên vai trò dùng trong JWT và [Authorize(Roles = ...)].
/// Nhân viên lấy vai trò từ Nhan_vien.chuc_vu (CK_Nhan_vien_chuc_vu), khách hàng luôn là KhachHang.
/// </summary>
public static class VaiTro
{
    public const string KhachHang = "KhachHang";
    public const string QuanTriVien = "QuanTriVien";
    public const string LeTan = "LeTan";
    public const string BacSi = "BacSi";
    public const string ThuNgan = "ThuNgan";

    /// <summary>Mọi nhân viên (dùng cho các API nội bộ chỉ cần là nhân viên).</summary>
    public const string NhanVien = QuanTriVien + "," + LeTan + "," + BacSi + "," + ThuNgan;

    /// <summary>Đổi giá trị chuc_vu trong database sang tên vai trò. Trả null nếu không khớp giá trị nào.</summary>
    public static string? TuChucVu(string chucVu) => chucVu switch
    {
        "Quản trị viên" => QuanTriVien,
        "Lễ tân" => LeTan,
        "Bác sĩ" => BacSi,
        "Thu ngân" => ThuNgan,
        _ => null
    };
}

/// <summary>Tên claim tự định nghĩa trong JWT.</summary>
public static class ClaimKmn
{
    public const string Ma = "ma";          // ma_kh hoặc ma_nv
    public const string Loai = "loai";      // "KH" hoặc "NV"
    public const string HoTen = "ho_ten";
    public const string MaKhoa = "ma_khoa"; // chỉ bác sĩ
}
