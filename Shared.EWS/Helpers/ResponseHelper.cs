using System.Net;

namespace Shared.EWS.Helpers
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
    }

    public static class ResponseHelper
    {
        public static ApiResponse<T> SuccessResponse<T>(T? data, string message = "Success",
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = (int)statusCode,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> FailedResponse<T>(T? data, string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new ApiResponse<T>
            {
                Success = false,
                StatusCode = (int)statusCode,
                Message = message,
                Data = data
            };
        }
    }
}
