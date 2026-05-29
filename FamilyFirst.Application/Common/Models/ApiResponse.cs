namespace FamilyFirst.Application.Common.Models;

public sealed record ErrorDto(string Code, string Message);

public sealed class ApiResponse<T>
{
    public bool Succeeded { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyCollection<ErrorDto> Errors { get; init; } = Array.Empty<ErrorDto>();

    public static ApiResponse<T> Success(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Failure(IEnumerable<ErrorDto> errors, string? message = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = message,
            Errors = errors.ToArray()
        };
    }

    public static ApiResponse<T> Failure(string code, string message)
    {
        return Failure(new[] { new ErrorDto(code, message) }, message);
    }
}
