using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface INotificationPreferenceService
{
    Task<NotificationPreferenceDto> GetPreferencesAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken);

    Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        Guid currentUserId,
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken);

    Task<NotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken cancellationToken);
}

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken);

    Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken);
}
