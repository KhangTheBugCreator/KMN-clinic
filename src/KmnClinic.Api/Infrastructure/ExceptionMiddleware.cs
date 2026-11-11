using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Bắt mọi lỗi và trả JSON thống nhất: { "message": "..." }.
/// Không bao giờ trả chi tiết kỹ thuật (stack trace, câu SQL, chuỗi kết nối) cho trình duyệt.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Message);
        }
        catch (SqlException ex) when (SqlErrorTranslator.Translate(ex) is { } t)
        {
            _logger.LogInformation("Lỗi ràng buộc SQL {Number}: {Message}", ex.Number, ex.Message);
            await WriteAsync(context, t.Status, t.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ.");
            _logger.LogInformation(ex, "Yêu cầu không hợp lệ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.");
        }
    }

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // giữ nguyên chữ tiếng Việt trong JSON
    };

    internal static async Task WriteAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }, JsonOpt));
    }
}
