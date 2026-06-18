using System.Net;

namespace Shared.EWS.Helpers
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public List<string> ErrorMessages { get; init; } = new();
    }

    public static class ResponseHelper
    {
        public static ApiResponse<T> SuccessResponse<T>(
            T? data,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = data,
                StatusCode = (int)statusCode,
                Message = message,
                ErrorMessages = new List<string>()
            };
        }

        public static ApiResponse<T> FailedResponse<T>(
            T? data,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            List<string>? errorMessages = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Data = data,
                StatusCode = (int)statusCode,
                Message = message,
                ErrorMessages = errorMessages ?? new List<string> { message }
            };
        }
    }
}