using System.Security.Claims;
using FamilyFirst.Application.DTOs.Auth;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IAuthService
{
    Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<bool> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken);

    Task<bool> SetPinAsync(Guid currentUserId, SetPinRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyPinAsync(VerifyPinRequest request, CancellationToken cancellationToken);

    Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, AuthTokenContext tokenContext);

    string GenerateRefreshToken();

    string HashToken(string token);
}

public sealed record AuthTokenContext(
    string Role,
    Guid? FamilyId,
    Guid? FamilyMemberId,
    Guid? ChildProfileId,
    Guid? TeacherProfileId,
    IReadOnlyCollection<Guid> AssignedChildIds);

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> GetByIdForFamilyAsync(Guid id, long familyId, CancellationToken cancellationToken);

    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically records one failed credential login (single UPDATE, safe under parallel guesses). When the
    /// count reaches <paramref name="maxAttempts"/>, LockoutUntil is set and the counter restarts at 0.
    /// </summary>
    Task RegisterFailedLoginAsync(long userInternalId, int maxAttempts, DateTime lockoutUntilUtc, CancellationToken cancellationToken);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
