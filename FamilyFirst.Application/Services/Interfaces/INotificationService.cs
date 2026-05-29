using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Domain.Entities;

namespace FamilyFirst.Application.Services.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationDto>> CreateManyAsync(
        IReadOnlyCollection<CreateNotificationRequest> requests,
        CancellationToken cancellationToken);

    Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken);

    Task<PaginatedList<NotificationDto>> ListNotificationsAsync(
        Guid currentUserId,
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isRead,
        CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(
        Guid currentUserId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);

    Task<MarkAllReadResultDto> MarkAllReadAsync(
        Guid currentUserId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationDto>> SendEmergencyAsync(
        Guid currentUserId,
        Guid? currentChildProfileId,
        Guid familyId,
        EmergencyNotificationRequest request,
        CancellationToken cancellationToken);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Notification>> ListByRecipientAsync(
        Guid recipientUserId,
        bool? isRead,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Notification>> ListDueForImmediateDeliveryAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Notification>> ListDueBatchedAsync(
        string batchGroup,
        DateTime asOfUtc,
        CancellationToken cancellationToken);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken);

    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);

    Task UpdateRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken);

    Task<int> MarkAllReadAsync(Guid recipientUserId, CancellationToken cancellationToken);

    Task PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
}

public sealed record NotificationRuleOverride(
    bool IsEnabled,
    FamilyFirst.Domain.Enums.NotificationPriority? PriorityOverride,
    int? DeliveryDelayMinutes);
