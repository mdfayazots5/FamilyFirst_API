namespace FamilyFirst.Application.Common.Exceptions;

public sealed class UnprocessableEntityException : Exception
{
    public UnprocessableEntityException(string message)
        : base(message)
    {
    }
}
