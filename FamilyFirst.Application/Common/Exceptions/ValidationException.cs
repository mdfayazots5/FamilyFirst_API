namespace FamilyFirst.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
        StatusCode = 400;
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors, int statusCode)
        : this(errors)
    {
        StatusCode = statusCode;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public int StatusCode { get; }
}
