using System.Text.Json.Serialization;

namespace PRN232.LMSSystem.IdentityService.Models.Response;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public object? Errors { get; set; }

    public static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "Request processed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors
        };
    }
}
