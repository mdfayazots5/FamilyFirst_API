using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Feedback;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;

namespace FamilyFirst.Application.Services.Interfaces;

public interface IFeedbackService
{
    Task<FeedbackDto> SubmitFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken);

    Task<PaginatedList<FeedbackDto>> ListFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid? childId,
        FeedbackType? feedbackType,
        bool? isAcknowledged,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<FeedbackDto> GetFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken);

    Task<FeedbackDto> UpdateFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        UpdateFeedbackRequest request,
        CancellationToken cancellationToken);

    Task<FeedbackDto> AcknowledgeFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken);

    Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        int periodDays,
        CancellationToken cancellationToken);
}

public interface IFeedbackRepository
{
    Task<TeacherFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TeacherFeedback>> ListByFamilyAsync(
        Guid familyId,
        Guid? teacherProfileId,
        Guid? childProfileId,
        FeedbackType? feedbackType,
        bool? isAcknowledged,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TeacherFeedback>> ListByChildSinceAsync(
        Guid familyId,
        Guid childProfileId,
        DateTime createdFromUtc,
        CancellationToken cancellationToken);

    Task<int> CountUnacknowledgedByFamilyAsync(Guid familyId, CancellationToken cancellationToken);

    Task AddAsync(TeacherFeedback feedback, CancellationToken cancellationToken);

    Task UpdateAsync(TeacherFeedback feedback, CancellationToken cancellationToken);
}
