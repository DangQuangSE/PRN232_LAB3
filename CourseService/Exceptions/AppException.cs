namespace PRN232.LMSSystem.CourseService.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    protected AppException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with ID '{key}' was not found.", 404) { }
}

public class BadRequestException : AppException
{
    public object? Errors { get; }
    
    public BadRequestException(string message) : base(message, 400) { }

    public BadRequestException(string message, object? errors) : base(message, 400)
    {
        Errors = errors;
    }
}
