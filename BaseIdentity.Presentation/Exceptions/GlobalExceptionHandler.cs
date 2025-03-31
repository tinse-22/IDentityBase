using BaseIdentity.Application.DTOs.Response;
using System.Net;

namespace BaseIdentity.Presentation.Exceptions
{
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
            // Ghi log lỗi
            _logger.LogError(exception, exception.Message);

            // Tạo đối tượng response lỗi
            var response = new ErrorResponse
            {
                Message = exception.Message,
                // Chọn trạng thái và tiêu đề dựa vào loại exception
                StatusCode = exception is BadHttpRequestException
                    ? (int)HttpStatusCode.BadRequest
                    : (int)HttpStatusCode.InternalServerError,
                Title = exception is BadHttpRequestException
                    ? exception.GetType().Name
                    : "Internal Server Error"
            };

            context.Response.StatusCode = response.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
