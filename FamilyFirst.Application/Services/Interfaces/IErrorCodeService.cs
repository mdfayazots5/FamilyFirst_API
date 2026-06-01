using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

/// <summary>
/// Reads error/success messages from tblErrorCode via uspGetErrorCodeById.
/// Called in the finally block of every service method instead of returning hardcoded strings.
/// Supports future multilingual messages via languageId (Level 1: always English = 1).
/// </summary>
public interface IErrorCodeService
{
    /// <summary>
    /// Returns the user-facing message for a given error code.
    /// Falls back to a generic technical message if the code is not found in DB.
    /// </summary>
    Task<string> GetMessageAsync(
        FamilyFirstErrorCode code,
        int languageId = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns both the return code and message in one call.
    /// </summary>
    Task<(int ReturnCode, string ReturnMessage)> GetAsync(
        FamilyFirstErrorCode code,
        int languageId = 1,
        CancellationToken cancellationToken = default);
}
