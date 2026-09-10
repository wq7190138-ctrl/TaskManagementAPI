using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementAPI.Middleware
{
    /// <summary>
    /// 全局异常处理
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(exception, "发生未处理的异常");

            var response = context.Response;
            response.ContentType = "application/json";

            var (statusCode, title) = exception switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, "资源不存在"),
                ArgumentException => (HttpStatusCode.BadRequest, "请求参数无效"),
                InvalidOperationException => (HttpStatusCode.BadRequest, "操作无效"),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "无权限访问"),
                DbUpdateException => (HttpStatusCode.InternalServerError, "数据更新失败"),
                _ => (HttpStatusCode.InternalServerError, "服务器内部错误")
            };

            response.StatusCode = (int)statusCode;

            var errorResponse = new
            {
                status = (int)statusCode,
                title = title,
                detail = exception.Message,
                traceId = context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            };

            await response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
