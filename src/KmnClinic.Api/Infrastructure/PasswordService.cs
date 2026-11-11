using System.Security.Cryptography;
using System.Text;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Mọi thao tác mật khẩu đi qua đây. Theo quyết định Q5 của chủ dự án, hiện tạm lưu chuỗi gốc;
/// khi chuyển sang băm (RB35, RB36) chỉ cần thay lớp cài đặt đăng ký trong Program.cs.
/// </summary>
public interface IPasswordService
{
    /// <summary>Chuỗi sẽ được lưu vào cột mat_khau_*.</summary>
    string ChuanBiLuu(string matKhau);

    /// <summary>So mật khẩu người dùng nhập với giá trị đang lưu.</summary>
    bool KhopMatKhau(string matKhauNhap, string? giaTriDangLuu);
}

public class PlainTextPasswordService : IPasswordService
{
    public string ChuanBiLuu(string matKhau) => matKhau;

    public bool KhopMatKhau(string matKhauNhap, string? giaTriDangLuu)
    {
        if (string.IsNullOrEmpty(giaTriDangLuu)) return false;
        // So sánh thời gian cố định để không lộ thông tin qua thời gian phản hồi.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(matKhauNhap), Encoding.UTF8.GetBytes(giaTriDangLuu));
    }
}

public static class QuyTacMatKhau
{
    public const int DoDaiToiThieu = 8;   // RB38, RB39
    public const int DoDaiToiDa = 255;    // varchar(255)

    public static void KiemTra(string? matKhau)
    {
        if (string.IsNullOrEmpty(matKhau) || matKhau.Length < DoDaiToiThieu)
            throw AppException.BadRequest($"Mật khẩu phải có ít nhất {DoDaiToiThieu} ký tự.");
        if (matKhau.Length > DoDaiToiDa)
            throw AppException.BadRequest($"Mật khẩu không được dài quá {DoDaiToiDa} ký tự.");
    }
}
