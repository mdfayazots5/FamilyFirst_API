using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.User;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public UserService(IUserRepository userRepository, IFamilyMemberRepository familyMemberRepository)
    {
        _userRepository = userRepository;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<UserDto> GetUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        await EnsureCanReadUserAsync(currentUserId, userId, cancellationToken);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);

        return ToUserDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(Guid currentUserId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.ProfilePhotoUrl = string.IsNullOrWhiteSpace(request.ProfilePhotoUrl) ? null : request.ProfilePhotoUrl.Trim();
        user.PreferredLanguage = request.PreferredLanguage.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ToUserDto(user);
    }

    public async Task<bool> UpdateFcmTokenAsync(Guid currentUserId, Guid userId, FcmTokenRequest request, CancellationToken cancellationToken)
    {
        EnsureOwnUser(currentUserId, userId);

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        user.FcmToken = request.FcmToken.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);

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
            return;
        }

        throw new ForbiddenAccessException("User profile access is forbidden.");
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);
    }

    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user context is required.");
        }
    }

    private static void EnsureOwnUser(Guid currentUserId, Guid userId)
    {
        EnsureAuthenticated(currentUserId);

        if (currentUserId != userId)
        {
            throw new ForbiddenAccessException("Only the owner can update this user profile.");
        }
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
