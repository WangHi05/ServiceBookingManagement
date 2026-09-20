using System.Net;

namespace ServiceBooking.API.Common;

/// <summary>
/// Exception nghiệp vụ, có kèm HTTP status code mong muốn.
/// Dùng để service/controller ném lỗi rõ ràng (401, 403, 404, 409, ...)
/// và middleware xử lý tập trung sẽ format lại thành ApiResponse chuẩn.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public List<string>? Errors { get; }

    public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static ApiException NotFound(string message) => new(message, HttpStatusCode.NotFound);
    public static ApiException Unauthorized(string message) => new(message, HttpStatusCode.Unauthorized);
    public static ApiException Forbidden(string message) => new(message, HttpStatusCode.Forbidden);
    public static ApiException Conflict(string message) => new(message, HttpStatusCode.Conflict);
    public static ApiException BadRequest(string message, List<string>? errors = null) => new(message, HttpStatusCode.BadRequest, errors);
}
