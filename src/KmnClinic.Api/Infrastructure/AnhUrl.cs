using System.Net;
using System.Net.Sockets;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Kiểm tra đường dẫn ảnh nhân viên (NHAN_VIEN.duong_dan_anh). Backend KHÔNG bao giờ tải URL này;
/// trình duyệt tự tải, nên chỉ cần chặn URL không phải HTTPS và URL trỏ vào mạng nội bộ.
/// </summary>
public static class AnhUrl
{
    /// <summary>Trả về URL đã chuẩn hóa, null nếu bỏ trống; ném 400 nếu không hợp lệ.</summary>
    public static string? ChuanHoa(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();
        if (url.Length > 1000) throw AppException.BadRequest("Đường dẫn ảnh tối đa 1000 ký tự.");
        if (url.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
            throw AppException.BadRequest("Đường dẫn ảnh không được chứa khoảng trắng.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw AppException.BadRequest("Đường dẫn ảnh không đúng định dạng.");
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw AppException.BadRequest("Đường dẫn ảnh phải bắt đầu bằng https://");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw AppException.BadRequest("Đường dẫn ảnh không được chứa tên đăng nhập hoặc mật khẩu.");
        if (!uri.IsDefaultPort)
            throw AppException.BadRequest("Đường dẫn ảnh không được chỉ định cổng riêng.");
        if (LaNoiBo(uri))
            throw AppException.BadRequest("Đường dẫn ảnh không được trỏ tới máy chủ nội bộ.");
        return uri.AbsoluteUri.Length <= 1000 ? uri.AbsoluteUri : url;
    }

    private static bool LaNoiBo(Uri uri)
    {
        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            return IPAddress.TryParse(host.Trim('[', ']'), out var ip) && IpNoiBo(ip);
        if (!host.Contains('.')) return true; // localhost, tên máy trong mạng LAN
        string[] duoiNoiBo = [".localhost", ".local", ".internal", ".lan", ".home", ".corp", ".intranet"];
        return duoiNoiBo.Any(host.EndsWith);
    }

    private static bool IpNoiBo(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] is 0 or 10 or 127
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)   // CGNAT
                || (b[0] == 169 && b[1] == 254)                 // link-local, metadata đám mây
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || b[0] >= 224;                                  // multicast, dành riêng
        }
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal || ip.IsIPv6Multicast
            || ip.Equals(IPAddress.IPv6Any);
    }
}
