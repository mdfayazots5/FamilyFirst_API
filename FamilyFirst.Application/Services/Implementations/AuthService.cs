using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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
    private readonly IApiLogService _apiLogService;
    private readonly IErrorCodeService _errorCodeService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IFamilyMemberRepository familyMemberRepository,
        IChildProfileRepository childProfileRepository,
        ITeacherProfileRepository teacherProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IOtpService otpService,
        IJwtTokenService jwtTokenService,
        IApiLogService apiLogService,
        IErrorCodeService errorCodeService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _familyMemberRepository = familyMemberRepository;
        _childProfileRepository = childProfileRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
        _apiLogService = apiLogService;
        _errorCodeService = errorCodeService;
    }

    public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber, request.CountryCode);
        var otpToken = await _otpService.SendOtpAsync(phoneNumber, request.CountryCode, cancellationToken);
        var response = new SendOtpResponse(otpToken);

        LogApiCall(
            nameof(SendOtpAsync),
            new
            {
                request.CountryCode,
                PhoneNumber = MaskPhoneNumber(phoneNumber)
            },
            new { HasOtpToken = !string.IsNullOrWhiteSpace(response.OtpToken) });

        return response;
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber, "+91");
        var isValidOtp = await _otpService.VerifyOtpAsync(phoneNumber, request.OtpToken, request.OtpCode, cancellationToken);

        if (!isValidOtp)
        {
            var msg = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Invalid_OTP, cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException(msg);
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

        var authResponse = await CreateAuthResponseAsync(user, UserRole.Parent.ToString(), cancellationToken);
        LogApiCall(
            nameof(VerifyOtpAsync),
            new
            {
                PhoneNumber = MaskPhoneNumber(phoneNumber),
                HasOtpToken = !string.IsNullOrWhiteSpace(request.OtpToken)
            },
            CreateAuthResponseLog(authResponse));

        return authResponse;
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken?.User is null || refreshToken.IsRevoked || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            var msg = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Session_Expired, cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException(msg);
        }

        refreshToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        refreshToken.User.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(refreshToken.User, cancellationToken);

        var refreshResponse = await CreateAuthResponseAsync(refreshToken.User, UserRole.Parent.ToString(), cancellationToken);
        LogApiCall(
            nameof(RefreshTokenAsync),
            new { HasRefreshToken = !string.IsNullOrWhiteSpace(request.RefreshToken) },
            CreateAuthResponseLog(refreshResponse));

        return refreshResponse;
    }

    public async Task<bool> RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            LogApiCall(
                nameof(RevokeTokenAsync),
                new { HasRefreshToken = !string.IsNullOrWhiteSpace(request.RefreshToken) },
                new { Revoked = false, Reason = "TokenNotFound" });
            return true;
        }

        refreshToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        LogApiCall(
            nameof(RevokeTokenAsync),
            new { HasRefreshToken = !string.IsNullOrWhiteSpace(request.RefreshToken) },
            new { Revoked = true });

        return true;
    }

    public async Task<bool> SetPinAsync(Guid currentUserId, SetPinRequest request, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            var msgToken = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException(msgToken);
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException(await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.User_Not_Found, cancellationToken: cancellationToken));

        user.PinHash = HashPin(request.Pin);
        await _userRepository.UpdateAsync(user, cancellationToken);

        LogApiCall(
            nameof(SetPinAsync),
            new
            {
                CurrentUserId = currentUserId,
                HasPin = !string.IsNullOrWhiteSpace(request.Pin)
            },
            new { Updated = true },
            createdByUserId: user.InternalId);

        return true;
    }

    public async Task<AuthResponse> VerifyPinAsync(VerifyPinRequest request, CancellationToken cancellationToken)
    {
        var invalidPinMsg = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Invalid_User, cancellationToken: cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException(invalidPinMsg);

        if (string.IsNullOrWhiteSpace(user.PinHash) || !VerifyPin(request.Pin, user.PinHash))
        {
            throw new UnauthorizedAccessException(invalidPinMsg);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var pinResponse = await CreateAuthResponseAsync(user, UserRole.Child.ToString(), cancellationToken);
        LogApiCall(
            nameof(VerifyPinAsync),
            new
            {
                request.UserId,
                HasPin = !string.IsNullOrWhiteSpace(request.Pin)
            },
            CreateAuthResponseLog(pinResponse));

        return pinResponse;
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

        var currentUserDto = new CurrentUserDto(
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

        LogApiCall(
            nameof(GetCurrentUserAsync),
            new { UserId = userId },
            new
            {
                currentUserDto.UserId,
                currentUserDto.Role,
                currentUserDto.FamilyId,
                currentUserDto.ChildProfileId,
                currentUserDto.TeacherProfileId
            });

        return currentUserDto;
    }

    private void LogApiCall(string methodName, object? request, object? response, long createdByUserId = 0)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response),
            createdByUserId: createdByUserId);
    }

    private static object CreateAuthResponseLog(AuthResponse response)
    {
        return new
        {
            response.Role,
            response.User.UserId,
            response.User.PhoneNumber,
            HasAccessToken = !string.IsNullOrWhiteSpace(response.AccessToken),
            HasRefreshToken = !string.IsNullOrWhiteSpace(response.RefreshToken)
        };
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
                UserId = user.InternalId,
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
            membership.Family?.Id,
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

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
        {
            return phoneNumber;
        }

        return $"{phoneNumber[..Math.Min(4, phoneNumber.Length)]}****{phoneNumber[^2..]}";
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var claimValue = FindClaimValue(principal, claimType);

        return Guid.TryParse(claimValue, out var value) ? value : null;
    }

    public async Task<AuthResponse> LoginWithPasswordAsync(LoginWithPasswordRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = NormalizePhoneNumber(request.PhoneNumber, request.CountryCode);
        var invalidMsg = await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Invalid_User, cancellationToken: cancellationToken);

        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken)
            ?? throw new UnauthorizedAccessException(invalidMsg);

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !VerifyPin(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(invalidMsg);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(invalidMsg);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var membership = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(user.Id, cancellationToken);
        var role = membership?.Role.ToString() ?? UserRole.Parent.ToString();

        var authResponse = await CreateAuthResponseAsync(user, role, cancellationToken);

        LogApiCall(
            nameof(LoginWithPasswordAsync),
            new { PhoneNumber = MaskPhoneNumber(phoneNumber), Role = role },
            CreateAuthResponseLog(authResponse));

        return authResponse;
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
