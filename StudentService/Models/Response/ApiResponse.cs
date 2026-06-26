using System.Text.Json.Serialization;

namespace PRN232.LMSSystem.StudentService.Models.Response;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public object? Errors { get; set; }
    public PaginationMetadata? Pagination { get; set; }

    public static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "Request processed successfully",
        PaginationMetadata? pagination = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            Pagination = pagination
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors,
            Pagination = null
        };
    }
}
