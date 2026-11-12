using System.Security.Cryptography;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Băm mật khẩu nhân viên trước khi lưu vào mat_khau_nv (RB35: không lưu bản gốc).
/// Website giả lập vai trò nên không đọc lại mật khẩu; cột NOT NULL nên vẫn phải lưu một giá trị.
/// Định dạng: PBKDF2-SHA256$vòng-lặp$salt-base64$hash-base64 (khoảng 90 ký tự, vừa varchar(255)).
/// </summary>
public static class MatKhau
{
    private const int VongLap = 100_000;

    public static string Bam(string matKhau)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, VongLap, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${VongLap}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool KiemTra(string matKhau, string daBam)
    {
        var p = daBam.Split('$');
        if (p.Length != 4 || p[0] != "PBKDF2-SHA256" || !int.TryParse(p[1], out var vong)) return false;
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, Convert.FromBase64String(p[2]), vong, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(p[3]));
    }
}
