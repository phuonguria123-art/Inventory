using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, message) = exception switch
            {
                DbUpdateConcurrencyException => (
                    StatusCodes.Status409Conflict,
                    "Dữ liệu đã được thay đổi bởi một yêu cầu khác. Vui lòng tải lại và thử lại."),

                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    exception.Message),

                ConflictException => (
                    StatusCodes.Status409Conflict,
                    exception.Message),

                ValidationException => (
                    StatusCodes.Status400BadRequest,
                    exception.Message),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Đã xảy ra lỗi hệ thống.")
            };
            httpContext.Response.StatusCode = statusCode;
            //      context.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(new
            {
                success = false,
                message,
                errors = new[] { message ?? "Có lỗi xảy ra" }
            }, cancellationToken);

            return true;
        }
    }
}
