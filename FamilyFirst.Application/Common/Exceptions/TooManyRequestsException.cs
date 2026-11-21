using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Common.Exceptions;

/// <summary>Maps to HTTP 429 with the given error code (e.g. Account_Locked). Message comes from IErrorCodeService.</summary>
public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException(FamilyFirstErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public FamilyFirstErrorCode ErrorCode { get; }
}
