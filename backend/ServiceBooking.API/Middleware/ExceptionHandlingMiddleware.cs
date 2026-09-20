using System.Net;
using System.Text.Json;
using ServiceBooking.API.Common;

namespace ServiceBooking.API.Middleware;

/// <summary>
/// Bắt toàn bộ exception chưa được xử lý trong pipeline,
/// trả về ApiResponse chuẩn kèm status code phù hợp.
/// Đăng ký middleware này SỚM trong Program.cs (trước routing/auth).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ApiException apiEx => (apiEx.StatusCode, apiEx.Message, apiEx.Errors),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Bạn không có quyền thực hiện thao tác này.", null),
            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", (List<string>?)null)
        };

        // Log lỗi thực (kể cả 500) để dev debug, nhưng không lộ chi tiết ra client khi 500
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled API exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
