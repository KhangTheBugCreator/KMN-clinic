namespace KmnClinic.Api.Infrastructure;

/// <summary>Header bảo mật cơ bản cho cả trang và API.</summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // Ảnh bác sĩ là URL HTTPS bên ngoài (Nhan_vien.duong_dan_anh) nên img-src cho phép https:.
    // Bản đồ nhúng chỉ cho Google Maps.
    private const string Csp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' https: data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-src https://www.google.com https://maps.google.com; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "no-referrer";
        h["Content-Security-Policy"] = Csp;
        if (context.Request.Path.StartsWithSegments("/api"))
            h["Cache-Control"] = "no-store";
        return _next(context);
    }
}
