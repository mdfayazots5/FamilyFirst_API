using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.DTOs.Feedback;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using System.Text.Json;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class FeedbackService : IFeedbackService
{
    private const string OpenResolutionStatus = "Open";
    private const string ElderFeedbackSubject = "Family appreciation";

    private readonly IAttendanceSessionRepository _attendanceSessionRepository;
    private readonly IChildProfileRepository _childProfileRepository;
    private readonly ICommentTemplateRepository _commentTemplateRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITeacherChildAssignmentRepository _teacherChildAssignmentRepository;
    private readonly ITeacherProfileRepository _teacherProfileRepository;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public FeedbackService(
        IFeedbackRepository feedbackRepository,
        IFamilyMemberRepository familyMemberRepository,
        ITeacherProfileRepository teacherProfileRepository,
        ITeacherChildAssignmentRepository teacherChildAssignmentRepository,
        IChildProfileRepository childProfileRepository,
        ICommentTemplateRepository commentTemplateRepository,
        IAttendanceSessionRepository attendanceSessionRepository,
        IPushNotificationService pushNotificationService,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _feedbackRepository = feedbackRepository;
        _familyMemberRepository = familyMemberRepository;
        _teacherProfileRepository = teacherProfileRepository;
        _teacherChildAssignmentRepository = teacherChildAssignmentRepository;
        _childProfileRepository = childProfileRepository;
        _commentTemplateRepository = commentTemplateRepository;
        _attendanceSessionRepository = attendanceSessionRepository;
        _pushNotificationService = pushNotificationService;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<FeedbackDto> SubmitFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var child = await GetChildInFamilyOrThrowAsync(request.ChildProfileId, familyId, cancellationToken);
        var authorProfile = await ResolveAuthorProfileAsync(member, familyId, child.Id, request, cancellationToken);
        var commentTemplate = await GetCommentTemplateOrThrowAsync(request.CommentTemplateId, familyId, cancellationToken);
        var session = await GetSessionOrThrowAsync(request.SessionId, familyId, cancellationToken);

        if (session is not null
            && member.Role == UserRole.Teacher
            && session.TeacherProfileId != authorProfile.InternalId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var feedback = new TeacherFeedback
        {
            TeacherProfileId = authorProfile.InternalId,
            ChildProfileId = child.InternalId,
            FamilyId = member.FamilyId,
            AttendanceSessionId = session is not null ? session.InternalId : (long?)null,
            FeedbackType = request.FeedbackType,
            Severity = request.Severity,
            Subject = ResolveSubject(request, member.Role),
            Message = request.Message.Trim(),
            CommentTemplateId = null, // CommentTemplateId is long? in entity; Guid? from DTO — skip
            WeeklySummaryJson = NormalizeOptional(request.WeeklySummaryJson),
            IsAcknowledged = false,
            ResolutionStatus = OpenResolutionStatus,
            IsEditable = true
        };

        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        feedback.TeacherProfile = authorProfile;
        feedback.ChildProfile = child;
        feedback.CommentTemplate = commentTemplate;
        feedback.AttendanceSession = session;

        await SendParentNotificationsAsync(feedback, cancellationToken);

        var response = ToDto(feedback);
        LogApiCall(nameof(SubmitFeedbackAsync), new { currentUserId, familyId, request.ChildProfileId, request.FeedbackType }, new { response.FeedbackId });
        return response;
    }

    public async Task<PaginatedList<FeedbackDto>> ListFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid? childId,
        FeedbackType? feedbackType,
        bool? isAcknowledged,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.Teacher)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (childId.HasValue)
        {
            await GetChildInFamilyOrThrowAsync(childId.Value, familyId, cancellationToken);
        }

        Guid? teacherProfileId = null;

        if (member.Role == UserRole.Teacher)
        {
            teacherProfileId = (await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken)).Id;
        }

        var feedbackItems = await _feedbackRepository.ListByFamilyAsync(
            familyId,
            teacherProfileId,
            childId,
            feedbackType,
            isAcknowledged,
            cancellationToken);

        var response = PaginatedList<FeedbackDto>.Create(
            feedbackItems.Select(ToDto),
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize <= 0 ? 20 : pageSize);
        LogApiCall(nameof(ListFeedbackAsync), new { currentUserId, familyId, childId, feedbackType, isAcknowledged, pageNumber, pageSize }, new { response.TotalCount });
        return response;
    }

    public async Task<FeedbackDto> GetFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);
        var feedback = await GetFamilyFeedbackOrThrowAsync(feedbackId, familyId, cancellationToken);

        if (member.Role == UserRole.Parent)
        {
            var response = ToDto(feedback);
            LogApiCall(nameof(GetFeedbackAsync), new { currentUserId, familyId, feedbackId }, new { response.FeedbackId });
            return response;
        }

        if (member.Role == UserRole.Teacher)
        {
            var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);

            if (feedback.TeacherProfileId == teacherProfile.InternalId)
            {
                var response = ToDto(feedback);
                LogApiCall(nameof(GetFeedbackAsync), new { currentUserId, familyId, feedbackId }, new { response.FeedbackId });
                return response;
            }
        }

        throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    public async Task<FeedbackDto> UpdateFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        UpdateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureTeacherMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.CreateUpdate, cancellationToken);
        var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);
        var feedback = await GetOwnedEditableFeedbackOrThrowAsync(feedbackId, familyId, teacherProfile.Id, cancellationToken);

        if (RequiresSeverity(feedback.FeedbackType) && !request.Severity.HasValue)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Severity"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                });
        }

        feedback.Message = request.Message.Trim();
        feedback.Severity = request.Severity;

        await _feedbackRepository.UpdateAsync(feedback, cancellationToken);

        var response = ToDto(feedback);
        LogApiCall(nameof(UpdateFeedbackAsync), new { currentUserId, familyId, feedbackId }, new { response.FeedbackId, response.Severity });
        return response;
    }

    public async Task<FeedbackDto> AcknowledgeFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role is not UserRole.Parent and not UserRole.FamilyAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var feedback = await GetFamilyFeedbackOrThrowAsync(feedbackId, familyId, cancellationToken);

        if (feedback.IsAcknowledged)
        {
            return ToDto(feedback);
        }

        var ackMember = await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken);
        feedback.IsAcknowledged = true;
        feedback.AcknowledgedAt = DateTime.UtcNow;
        feedback.AcknowledgedByUserId = ackMember?.UserId;
        feedback.ParentResponseText = NormalizeOptional(request.ParentResponseText);
        feedback.ResolutionStatus = "Acknowledged";

        await _feedbackRepository.UpdateAsync(feedback, cancellationToken);
        await SendTeacherAcknowledgementNotificationAsync(feedback, cancellationToken);

        var response = ToDto(feedback);
        LogApiCall(nameof(AcknowledgeFeedbackAsync), new { currentUserId, familyId, feedbackId }, new { response.FeedbackId, response.IsAcknowledged });
        return response;
    }

    public async Task<bool> DeleteFeedbackAsync(
        Guid currentUserId,
        Guid familyId,
        Guid feedbackId,
        CancellationToken cancellationToken)
    {
        var member = await EnsureTeacherMemberAsync(currentUserId, familyId, cancellationToken);
        await EnsurePermissionAsync(member.Role, FamilyFirstPermission.Delete, cancellationToken);
        var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);
        var feedback = await GetOwnedEditableFeedbackOrThrowAsync(feedbackId, familyId, teacherProfile.Id, cancellationToken);

        feedback.IsDeleted = true;
        feedback.DateDeleted = DateTime.UtcNow;

        await _feedbackRepository.UpdateAsync(feedback, cancellationToken);

        LogApiCall(nameof(DeleteFeedbackAsync), new { currentUserId, familyId, feedbackId }, new { Deleted = true });
        return true;
    }

    public async Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(
        Guid currentUserId,
        Guid familyId,
        Guid childId,
        int periodDays,
        CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Parent)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        await GetChildInFamilyOrThrowAsync(childId, familyId, cancellationToken);
        var normalizedPeriodDays = periodDays <= 0 ? 7 : periodDays;
        var createdFromUtc = DateTime.UtcNow.AddDays(-normalizedPeriodDays);
        var feedbackItems = await _feedbackRepository.ListByChildSinceAsync(
            familyId,
            childId,
            createdFromUtc,
            cancellationToken);

        var response = new FeedbackSummaryDto(
            childId,
            normalizedPeriodDays,
            feedbackItems.Count,
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Appreciation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Complaint),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.Observation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.HomeworkIssue),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.UrgentEscalation),
            feedbackItems.Count(feedback => feedback.FeedbackType == FeedbackType.WeeklySummary));
        LogApiCall(nameof(GetFeedbackSummaryAsync), new { currentUserId, familyId, childId, normalizedPeriodDays }, new { response.TotalCount });
        return response;
    }

    private async Task<TeacherFeedback> GetFamilyFeedbackOrThrowAsync(
        Guid feedbackId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (feedback.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return feedback;
    }

    private async Task<TeacherFeedback> GetOwnedEditableFeedbackOrThrowAsync(
        Guid feedbackId,
        Guid familyId,
        Guid teacherProfileId,
        CancellationToken cancellationToken)
    {
        var feedback = await GetFamilyFeedbackOrThrowAsync(feedbackId, familyId, cancellationToken);

        if (feedback.TeacherProfile?.Id != teacherProfileId)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        if (!feedback.IsEditable || feedback.DateCreated <= DateTime.UtcNow.AddHours(-24))
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Feedback_Edit_Window_Closed, cancellationToken));
        }

        return feedback;
    }

    private async Task<FamilyMember> EnsureFamilyMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(currentUserId, cancellationToken);

        return await _familyMemberRepository.GetActiveByFamilyAndUserAsync(familyId, currentUserId, cancellationToken)
            ?? throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task<FamilyMember> EnsureTeacherMemberAsync(Guid currentUserId, Guid familyId, CancellationToken cancellationToken)
    {
        var member = await EnsureFamilyMemberAsync(currentUserId, familyId, cancellationToken);

        if (member.Role != UserRole.Teacher)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return member;
    }

    private async Task<TeacherProfile> ResolveAuthorProfileAsync(
        FamilyMember member,
        Guid familyId,
        Guid childId,
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (member.Role == UserRole.Teacher)
        {
            var teacherProfile = await GetTeacherProfileForMemberAsync(member, familyId, cancellationToken);
            var assignment = await _teacherChildAssignmentRepository.GetActiveByTeacherAndChildAsync(
                teacherProfile.Id,
                childId,
                cancellationToken);

            if (assignment is null)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            return teacherProfile;
        }

        if (member.Role == UserRole.Elder)
        {
            if (request.FeedbackType != FeedbackType.Appreciation)
            {
                throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
            }

            return await GetOrCreateElderAuthorProfileAsync(member, familyId, cancellationToken);
        }

        throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
    }

    private async Task<TeacherProfile> GetTeacherProfileForMemberAsync(
        FamilyMember member,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var teacherProfile = await _teacherProfileRepository.GetByFamilyMemberIdAsync(member.Id, cancellationToken);

        if (teacherProfile is null || teacherProfile.Family?.Id != familyId || !teacherProfile.IsActive)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        return teacherProfile;
    }

    private async Task<TeacherProfile> GetOrCreateElderAuthorProfileAsync(
        FamilyMember member,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var existingProfile = await _teacherProfileRepository.GetByFamilyMemberIdAsync(member.Id, cancellationToken);

        if (existingProfile is not null && existingProfile.Family?.Id == familyId)
        {
            return existingProfile;
        }

        var elderProfile = new TeacherProfile
        {
            FamilyMemberId = member.InternalId,
            UserId = member.UserId,
            FamilyId = member.FamilyId,
            SubjectName = ElderFeedbackSubject,
            TeacherType = "Other",
            IsActive = true
        };

        await _teacherProfileRepository.AddAsync(elderProfile, cancellationToken);

        return elderProfile;
    }

    private async Task<ChildProfile> GetChildInFamilyOrThrowAsync(Guid childId, Guid familyId, CancellationToken cancellationToken)
    {
        var familyInternalId = await GetFamilyInternalIdAsync(familyId, cancellationToken);
        var resolvedChildId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.ChildProfile,
            childId.ToString(),
            familyInternalId,
            cancellationToken);

        if (!resolvedChildId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        var child = await _childProfileRepository.GetByIdAsync(childId, cancellationToken);

        if (child is null || child.Family?.Id != familyId)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return child;
    }

    private async Task<CommentTemplate?> GetCommentTemplateOrThrowAsync(
        Guid? templateId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (!templateId.HasValue)
        {
            return null;
        }

        var template = await _commentTemplateRepository.GetByIdAsync(templateId.Value, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (!(template.IsSystem || template.Family?.Id == familyId))
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        if (!string.Equals(template.Category, CommentTemplateCategories.Feedback, StringComparison.Ordinal))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["CommentTemplateId"] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Validation_Error, cancellationToken) }
                });
        }

        return template;
    }

    private async Task<AttendanceSession?> GetSessionOrThrowAsync(
        Guid? sessionId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        if (!sessionId.HasValue)
        {
            return null;
        }

        var session = await _attendanceSessionRepository.GetByIdAsync(sessionId.Value, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));

        if (session.Family?.Id != familyId || !session.IsActive)
        {
            throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Not_Found, cancellationToken));
        }

        return session;
    }

    private async Task SendParentNotificationsAsync(TeacherFeedback feedback, CancellationToken cancellationToken)
    {
        var parentFcmTokens = (await _familyMemberRepository.ListActiveByFamilyAsync(feedback.Family?.Id ?? Guid.Empty, cancellationToken))
            .Where(member => member.Role is UserRole.Parent or UserRole.FamilyAdmin)
            .Select(member => member.User?.FcmToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct()
            .ToArray();

        if (parentFcmTokens.Length == 0)
        {
            return;
        }

        var childName = feedback.ChildProfile?.FamilyMember?.User?.FullName
            ?? feedback.ChildProfile?.User?.FullName
            ?? "your child";
        var title = feedback.FeedbackType == FeedbackType.UrgentEscalation
            ? "Urgent feedback escalation"
            : "New teacher feedback";
        var body = $"{feedback.FeedbackType}: {childName} - {feedback.Message}";

        foreach (var fcmToken in parentFcmTokens)
        {
            await _pushNotificationService.SendPushAsync(fcmToken!, title, body, cancellationToken);
        }
    }

    private async Task SendTeacherAcknowledgementNotificationAsync(TeacherFeedback feedback, CancellationToken cancellationToken)
    {
        var teacherFcmToken = feedback.TeacherProfile?.User?.FcmToken
            ?? feedback.TeacherProfile?.FamilyMember?.User?.FcmToken;

        if (string.IsNullOrWhiteSpace(teacherFcmToken))
        {
            return;
        }

        var childName = feedback.ChildProfile?.FamilyMember?.User?.FullName
            ?? feedback.ChildProfile?.User?.FullName
            ?? "the child";

        await _pushNotificationService.SendPushAsync(
            teacherFcmToken,
            "Feedback acknowledged",
            $"{childName} feedback was acknowledged by a parent.",
            cancellationToken);
    }

    private static string? ResolveSubject(SubmitFeedbackRequest request, UserRole role)
    {
        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            return request.Subject.Trim();
        }

        if (role == UserRole.Elder)
        {
            return ElderFeedbackSubject;
        }

        return $"{request.FeedbackType} feedback";
    }

    private static bool RequiresSeverity(FeedbackType feedbackType)
    {
        return feedbackType is FeedbackType.Complaint or FeedbackType.UrgentEscalation;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static FeedbackDto ToDto(TeacherFeedback feedback)
    {
        var teacherName = feedback.TeacherProfile?.FamilyMember?.User?.FullName
            ?? feedback.TeacherProfile?.User?.FullName
            ?? string.Empty;
        var childName = feedback.ChildProfile?.FamilyMember?.User?.FullName
            ?? feedback.ChildProfile?.User?.FullName
            ?? string.Empty;

        return new FeedbackDto(
            feedback.Id,
            feedback.TeacherProfile?.Id ?? Guid.Empty,
            feedback.ChildProfile?.Id ?? Guid.Empty,
            feedback.Family?.Id ?? Guid.Empty,
            feedback.AttendanceSession?.Id,
            feedback.FeedbackType,
            feedback.Severity,
            feedback.Subject,
            feedback.Message,
            null, // CommentTemplateId: long? in entity, Guid? in DTO — not mappable directly
            feedback.CommentTemplate?.TemplateText,
            feedback.WeeklySummaryJson,
            feedback.IsAcknowledged,
            feedback.AcknowledgedAt,
            feedback.AcknowledgedByUser?.Id,
            feedback.ParentResponseText,
            feedback.ResolutionStatus,
            feedback.IsEditable || feedback.DateCreated > DateTime.UtcNow.AddHours(-24),
            feedback.DateCreated,
            feedback.LastUpdated ?? feedback.DateCreated,
            teacherName,
            childName);
    }

    private async Task<long> GetFamilyInternalIdAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw await CreateInvalidMasterDataExceptionAsync(cancellationToken);
        }

        return resolvedFamilyId.Value;
    }

    private async Task EnsureAuthenticatedAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(await GetMessageAsync(FamilyFirstErrorCode.Invalid_Token, cancellationToken));
        }
    }

    private async Task EnsurePermissionAsync(UserRole role, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.Feedback,
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
}
