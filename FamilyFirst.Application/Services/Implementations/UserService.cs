using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.User;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public UserService(
        IUserRepository userRepository,
        IFamilyMemberRepository familyMemberRepository,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _userRepository = userRepository;
        _familyMemberRepository = familyMemberRepository;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<UserDto> GetUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);
        await EnsureCanReadUserAsync(currentUserId, userId, cancellationToken);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var response = ToUserDto(user);
        LogApiCall(nameof(GetUserAsync), new { currentUserId, userId }, new { response.UserId });
        return response;
    }

    public async Task<UserDto> UpdateUserAsync(Guid currentUserId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);
        await EnsureWritePermissionAsync(currentUserId, cancellationToken);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.ProfilePhotoUrl = string.IsNullOrWhiteSpace(request.ProfilePhotoUrl) ? null : request.ProfilePhotoUrl.Trim();
        user.PreferredLanguage = request.PreferredLanguage.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);

        var response = ToUserDto(user);
        LogApiCall(nameof(UpdateUserAsync), new { currentUserId, userId, request.FullName, request.PreferredLanguage }, new { response.UserId });
        return response;
    }

    public async Task<bool> UpdateFcmTokenAsync(Guid currentUserId, Guid userId, FcmTokenRequest request, CancellationToken cancellationToken)
    {
        await EnsureOwnUserAsync(currentUserId, userId, cancellationToken);
        await EnsureWritePermissionAsync(currentUserId, cancellationToken);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        user.FcmToken = request.FcmToken.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);

        LogApiCall(nameof(UpdateFcmTokenAsync), new { currentUserId, userId, HasFcmToken = !string.IsNullOrWhiteSpace(request.FcmToken) }, new { Updated = true });
        return true;
    }

    private async Task EnsureCanReadUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        if (currentUserId == userId)
        {
            return;
        }

        var currentMember = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(currentUserId, cancellationToken);
        var targetMember = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(userId, cancellationToken);

        if (currentMember?.Role == UserRole.FamilyAdmin && currentMember.FamilyId == targetMember?.FamilyId)
        {
            await EnsurePermissionAsync(currentMember.Role, FamilyFirstPermission.AdminView, cancellationToken);
            return;
        }

        throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        var resolvedUserId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.User,
            userId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedUserId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.User_Not_Found, cancellationToken));
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsureOwnUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        if (currentUserId != userId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task EnsureWritePermissionAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var currentMember = await _familyMemberRepository.GetPrimaryActiveMembershipForUserAsync(currentUserId, cancellationToken);

        if (currentMember is null)
        {
            return;
        }

        await EnsurePermissionAsync(currentMember.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Family,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private async Task<ValidationException> CreateInvalidMasterDataExceptionAsync(CancellationToken cancellationToken)
    {
        var message = await _errorCodeService.GetMessageAsync(
            FamilyFirstErrorCode.Invalid_MasterData,
            cancellationToken: cancellationToken);

        return new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(MasterDataCodes)] = new[] { message }
        });
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto(
            user.Id,
            user.PhoneNumber,
            user.CountryCode,
            user.FullName,
            user.Email,
            user.ProfilePhotoUrl,
            user.PreferredLanguage,
            user.FcmToken,
            user.IsPhoneVerified,
            user.IsActive);
    }
}
