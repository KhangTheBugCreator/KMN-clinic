using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace KmnClinic.Api.Infrastructure;

public class JwtOptions
{
    public string Issuer { get; set; } = "KmnClinic";
    public string Audience { get; set; } = "KmnClinic.Web";
    public int HanPhut { get; set; } = 480;
    public string Key { get; set; } = "";

    public SymmetricSecurityKey SigningKey()
    {
        var bytes = Encoding.UTF8.GetBytes(Key);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Chưa cấu hình Jwt:Key (tối thiểu 32 ký tự). Đặt bằng User Secrets hoặc biến môi trường Jwt__Key.");
        return new SymmetricSecurityKey(bytes);
    }
}

public record NguoiDungDangNhap(string Ma, string Loai, string HoTen, string VaiTro, string? MaKhoa);

public class JwtService
{
    private readonly JwtOptions _opt;

    public JwtService(JwtOptions opt) => _opt = opt;

    public (string Token, DateTime HetHan) TaoToken(NguoiDungDangNhap u)
    {
        var hetHan = DateTime.UtcNow.AddMinutes(_opt.HanPhut);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Ma),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimKmn.Ma, u.Ma),
            new(ClaimKmn.Loai, u.Loai),
            new(ClaimKmn.HoTen, u.HoTen),
            new(ClaimTypes.Role, u.VaiTro),
        };
        if (u.MaKhoa is not null) claims.Add(new Claim(ClaimKmn.MaKhoa, u.MaKhoa));

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: hetHan,
            signingCredentials: new SigningCredentials(_opt.SigningKey(), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), hetHan);
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Mã người đang đăng nhập (ma_kh hoặc ma_nv), lấy từ token đã xác thực, không lấy từ dữ liệu client gửi.</summary>
    public static string MaNguoiDung(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimKmn.Ma) ?? throw AppException.Unauthorized("Phiên làm việc không hợp lệ. Vui lòng chọn lại vai trò.");

    public static bool LaKhachHang(this ClaimsPrincipal user) => user.FindFirstValue(ClaimKmn.Loai) == "KH";
}
