using System.Security.Claims;
using System.Security.Cryptography;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Auth;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
namespace FamilyFirst.Application.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private const int RefreshTokenExpiryDays = 30;
    private const int PinHashIterations = 100_000;
    private const int PinSaltBytes = 16;
    private const int PinHashBytes = 32;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;
    private readonly ITeacherChildAssignmentRepository _teacherChildAssignmentRepository;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherProfileRepository teacherProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IOtpService otpService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber, request.CountryCode);
        var otpToken = await _otpService.SendOtpAsync(phoneNumber, request.CountryCode, cancellationToken);

        return new SendOtpResponse(otpToken);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber, "+91");
        var isValidOtp = await _otpService.VerifyOtpAsync(phoneNumber, request.OtpToken, request.OtpCode, cancellationToken);

        if (!isValidOtp)
        {
            throw new UnauthorizedAccessException("OTP is invalid or expired.");
        }

        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                PhoneNumber = phoneNumber,
                CountryCode = ExtractCountryCode(phoneNumber),
                FullName = "FamilyFirst User",
                IsPhoneVerified = true,
                IsActive = true,
                PreferredLanguage = "en",
                LastLoginAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            user.IsPhoneVerified = true;
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return await CreateAuthResponseAsync(user, UserRole.Parent.ToString(), cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken?.User is null || refreshToken.IsRevoked || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        refreshToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        refreshToken.User.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(refreshToken.User, cancellationToken);

        return await CreateAuthResponseAsync(refreshToken.User, UserRole.Parent.ToString(), cancellationToken);
    }

    public async Task<bool> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return true;
        }

        refreshToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        return true;
    }

    public async Task<bool> SetPinAsync(Guid currentUserId, SetPinRequest request, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required to set a PIN.");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), currentUserId);

        user.PinHash = HashPin(request.Pin);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return true;
    }

    public async Task<AuthResponse> VerifyPinAsync(VerifyPinRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("PIN is invalid.");

        if (string.IsNullOrWhiteSpace(user.PinHash) || !VerifyPin(request.Pin, user.PinHash))
        {
            throw new UnauthorizedAccessException("PIN is invalid.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return await CreateAuthResponseAsync(user, UserRole.Child.ToString(), cancellationToken);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        var user = userId == Guid.Empty
            ? null
            : await _userRepository.GetByIdAsync(userId, cancellationToken);

        var claimFamilyId = TryGetGuidClaim(principal, "familyId");
        var tokenContext = claimFamilyId is null && user is not null
            ? await CreateAuthTokenContextAsync(user.Id, UserRole.Parent.ToString(), cancellationToken)
            : null;

        return new CurrentUserDto(
            userId,
            user?.FullName ?? FindClaimValue(principal, ClaimTypes.Name),
            user?.PhoneNumber ?? FindClaimValue(principal, "phone"),
            tokenContext?.Role ?? FindClaimValue(principal, ClaimTypes.Role) ?? FindClaimValue(principal, "role"),
            claimFamilyId ?? tokenContext?.FamilyId,
            TryGetGuidClaim(principal, "familyMemberId") ?? tokenContext?.FamilyMemberId,
            FindClaimValue(principal, "planCode") ?? tokenContext?.PlanCode,
            TryGetGuidClaim(principal, "childProfileId") ?? tokenContext?.ChildProfileId,
            TryGetGuidClaim(principal, "teacherProfileId") ?? tokenContext?.TeacherProfileId,
            tokenContext?.AssignedChildIds ?? ParseGuidArrayClaim(FindClaimValue(principal, "assignedChildIds")));
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, string role, CancellationToken cancellationToken)
    {
        var tokenContext = await CreateAuthTokenContextAsync(user.Id, role, cancellationToken);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, tokenContext);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(refreshToken);

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                IsRevoked = false
            },
            cancellationToken);

        return new AuthResponse(accessToken, refreshToken, CreateUserDto(user, tokenContext.Role), tokenContext.Role);
    }

    private async Task<AuthTokenContext> CreateAuthTokenContextAsync(Guid userId, string fallbackRole, CancellationToken cancellationToken)
    {
        var membership = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(userId, cancellationToken);

        if (membership is null)
        {
            return new AuthTokenContext(fallbackRole, null, null, null, null, null, Array.Empty<Guid>());
        }

        Guid? childProfileId = null;
        Guid? teacherProfileId = null;
        IReadOnlyCollection<Guid> assignedChildIds = Array.Empty<Guid>();

        if (membership.Role == UserRole.Child)
        {
            childProfileId = (await _childProfileRepository.GetByFamilyMemberIdAsync(membership.Id, cancellationToken))?.Id;
        }

        if (membership.Role == UserRole.Teacher)
        {
            teacherProfileId = (await _teacherProfileRepository.GetByFamilyMemberIdAsync(membership.Id, cancellationToken))?.Id;

            if (teacherProfileId.HasValue)
            {
                assignedChildIds = await _teacherChildAssignmentRepository.ListActiveChildIdsByTeacherProfileIdAsync(
                    teacherProfileId.Value,
                    cancellationToken);
            }
        }

        return new AuthTokenContext(
            membership.Role.ToString(),
            membership.FamilyId,
            membership.Id,
            membership.Family?.Plan?.PlanCode,
            childProfileId,
            teacherProfileId,
            assignedChildIds);
    }

    public static Guid GetCurrentUserId(ClaimsPrincipal principal)
    {
        var subject = FindClaimValue(principal, ClaimTypes.NameIdentifier) ?? FindClaimValue(principal, "sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }

    private static UserDto CreateUserDto(User user, string role)
    {
        return new UserDto(
            user.Id,
            user.PhoneNumber,
            user.CountryCode,
            user.FullName,
            user.Email,
            user.ProfilePhotoUrl,
            user.IsPhoneVerified,
            user.IsActive,
            user.PreferredLanguage,
            role);
    }

    private static string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(PinSaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, PinHashIterations, HashAlgorithmName.SHA256, PinHashBytes);

        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPin(string pin, string pinHash)
    {
        var parts = pinHash.Split('.');

        if (parts.Length != 3 || parts[0] != "v1")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, PinHashIterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string NormalizePhoneNumber(string phoneNumber, string countryCode)
    {
        var trimmed = phoneNumber.Trim();

        if (trimmed.StartsWith('+'))
        {
            return trimmed;
        }

        var normalizedCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "+91" : countryCode.Trim();

        return $"{normalizedCountryCode}{trimmed}";
    }

    private static string ExtractCountryCode(string phoneNumber)
    {
        return phoneNumber.StartsWith("+91", StringComparison.Ordinal) ? "+91" : phoneNumber[..Math.Min(3, phoneNumber.Length)];
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var claimValue = FindClaimValue(principal, claimType);

        return Guid.TryParse(claimValue, out var value) ? value : null;
    }

    private static string? FindClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }

    private static IReadOnlyCollection<Guid> ParseGuidArrayClaim(string? claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return Array.Empty<Guid>();
        }

        return claimValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var guid) ? guid : Guid.Empty)
            .Where(value => value != Guid.Empty)
            .ToArray();
    }
}
