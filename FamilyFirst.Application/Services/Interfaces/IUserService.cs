using FamilyFirst.Application.DTOs.User;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken);

    Task<UserDto> UpdateUserAsync(Guid currentUserId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateFcmTokenAsync(Guid currentUserId, Guid userId, FcmTokenRequest request, CancellationToken cancellationToken);
}
