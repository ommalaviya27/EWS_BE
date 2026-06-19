using Shared.EWS.Exceptions;
using Shared.EWS.Helpers;
using System.Net;
using System.Text.Json;

namespace Api.EWS.Middleware
{
    public class GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception on {Method} {Path}: {Message}",
                    context.Request.Method, context.Request.Path, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                InvalidCredentialsException e => (HttpStatusCode.Unauthorized, e.Message),
                AccountInactiveException e => (HttpStatusCode.Forbidden, e.Message),
                TokenException e => (HttpStatusCode.Unauthorized, e.Message),
                ResetTokenException e => (HttpStatusCode.BadRequest, e.Message),
                DuplicateRecordException e => (HttpStatusCode.Conflict, e.Message),
                NotFoundException e => (HttpStatusCode.NotFound, e.Message),
                ForbiddenException e => (HttpStatusCode.Forbidden, e.Message),
                UnauthorizedAccessException e => (HttpStatusCode.Unauthorized, e.Message),
                InvalidOperationException e => (HttpStatusCode.BadRequest, e.Message),
                _                            => (HttpStatusCode.InternalServerError,
                                                 "An unexpected error occurred. Please try again.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            var body = JsonSerializer.Serialize(
                ResponseHelper.FailedResponse<object>(
                    null,
                    message,
                    statusCode,
                    errorMessages: new List<string> { message }),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return context.Response.WriteAsync(body);
        }
    }
}