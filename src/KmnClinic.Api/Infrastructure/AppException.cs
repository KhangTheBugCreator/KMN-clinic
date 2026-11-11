namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Lỗi nghiệp vụ có chủ đích: trả về mã HTTP và câu thông báo tiếng Việt cho người dùng.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message) => StatusCode = statusCode;

    public static AppException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);
    public static AppException Unauthorized(string message) => new(StatusCodes.Status401Unauthorized, message);
    public static AppException Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.") => new(StatusCodes.Status403Forbidden, message);
    public static AppException NotFound(string message) => new(StatusCodes.Status404NotFound, message);
    public static AppException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
