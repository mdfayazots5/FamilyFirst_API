using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Common.Exceptions;

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Access to the requested resource is forbidden.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, FamilyFirstErrorCode errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public FamilyFirstErrorCode ErrorCode { get; } = FamilyFirstErrorCode.Permission_Denied;
}
