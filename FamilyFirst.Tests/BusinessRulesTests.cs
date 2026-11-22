using System.Text.Json;
using FamilyFirst.API.Middleware;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Application.DTOs.Reward;
using FamilyFirst.Application.DTOs.Task;
using FamilyFirst.Application.Services.Implementations;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FamilyFirst.Tests;

public sealed class BusinessRulesTests
{
    [Theory]
    [InlineData(59, true)]
    [InlineData(61, false)]
    public async Task Teacher_attendance_edit_respects_one_hour_window(int minutesAgo, bool expected)
    {
        var setup = new Setup(UserRole.Teacher);
        var session = setup.Session(DateTime.UtcNow.AddMinutes(-minutesAgo));
        var record = new AttendanceRecord { AttendanceSession = session, Status = AttendanceStatus.Present };
        setup.Mock<IAttendanceRecordRepository>().Setup(repo => repo.GetByIdForFamilyAsync(record.Id, setup.Family.InternalId, default)).ReturnsAsync(record);
        var service = setup.Attendance();
        var edit = new EditAttendanceRequest { Status = AttendanceStatus.Present, TeacherComment = "Checked" };

        Func<Task> action = () => service.EditAttendanceRecordAsync(setup.UserId, setup.Family.Id, session.Id, record.Id, edit, default);
        if (expected)
        {
            await action.Should().NotThrowAsync();
            record.TeacherComment.Should().Be("Checked");
        }
        else
        {
            await action.Should().ThrowAsync<ForbiddenAccessException>().WithMessage("Edit_Window_Closed");
            setup.Mock<IAttendanceRecordRepository>().Verify(repo => repo.UpdateAsync(It.IsAny<AttendanceRecord>(), default), Times.Never);
        }
    }

    [Fact]
    public async Task Duplicate_attendance_submission_returns_HTTP_409()
    {
        var setup = new Setup(UserRole.Teacher);
        var session = setup.Session(DateTime.UtcNow);
        var service = setup.Attendance();
        var context = new DefaultHttpContext();
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new ExceptionHandlingMiddleware(
            async _ => { await service.SubmitAttendanceAsync(setup.UserId, setup.Family.Id, session.Id, new SubmitAttendanceRequest(), default); },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        System.Text.Encoding.UTF8.GetString(responseBody.ToArray()).Should().Contain("Attendance_Already_Submitted");
    }

    [Theory]
    [InlineData(23, true)]
    [InlineData(25, false)]
    public async Task Teacher_feedback_delete_respects_24_hour_window(int hoursAgo, bool expected)
    {
        var setup = new Setup(UserRole.Teacher);
        var feedback = new TeacherFeedback { TeacherProfile = setup.Teacher, DateCreated = DateTime.UtcNow.AddHours(-hoursAgo), IsEditable = true };
        setup.Mock<IFeedbackRepository>().Setup(repo => repo.GetByIdForFamilyAsync(feedback.Id, setup.Family.InternalId, default)).ReturnsAsync(feedback);
        var service = setup.Feedback();

        Func<Task> action = () => service.DeleteFeedbackAsync(setup.UserId, setup.Family.Id, feedback.Id, default);
        if (expected)
        {
            await action.Should().NotThrowAsync();
            feedback.IsDeleted.Should().BeTrue();
        }
        else
        {
            await action.Should().ThrowAsync<ForbiddenAccessException>().WithMessage("Feedback_Edit_Window_Closed");
            feedback.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Family_admin_can_list_and_open_feedback_only_in_their_family()
    {
        var setup = new Setup(UserRole.FamilyAdmin);
        var familyId = setup.Family.Id;
        var feedback = new TeacherFeedback { Family = setup.Family, TeacherProfileId = 99 };
        var repository = setup.Mock<IFeedbackRepository>();
        repository.Setup(repo => repo.ListByFamilyAsync(familyId, null, null, null, null, default))
            .ReturnsAsync(new[] { feedback });
        repository.Setup(repo => repo.GetByIdForFamilyAsync(feedback.Id, setup.Family.InternalId, default))
            .ReturnsAsync(feedback);
        var service = setup.Feedback();

        var inbox = await service.ListFeedbackAsync(setup.UserId, familyId, null, null, null, 1, 20, default);
        inbox.Items.Should().ContainSingle().Which.FeedbackId.Should().Be(feedback.Id);
        var detail = await service.GetFeedbackAsync(setup.UserId, familyId, feedback.Id, default);
        detail.FeedbackId.Should().Be(feedback.Id);

        var otherFamilyId = Guid.NewGuid();
        Func<Task> listOtherFamily = () => service.ListFeedbackAsync(setup.UserId, otherFamilyId, null, null, null, 1, 20, default);
        Func<Task> readOtherFamily = () => service.GetFeedbackAsync(setup.UserId, otherFamilyId, feedback.Id, default);
        await listOtherFamily.Should().ThrowAsync<ForbiddenAccessException>();
        await readOtherFamily.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Emergency_access_reads_defaults_and_first_save_uses_the_authorized_family_id()
    {
        var setup = new Setup(UserRole.FamilyAdmin);
        var familyId = setup.Family.Id;
        var repository = setup.Mock<IFamilyAdminConfigRepository>();
        repository.Setup(repo => repo.GetActiveFamilyMemberAsync(familyId, setup.UserId, default))
            .ReturnsAsync(new FamilyMember { FamilyId = setup.Family.InternalId, UserId = 20, Role = UserRole.FamilyAdmin });
        VaultFamilySettings? saved = null;
        repository.Setup(repo => repo.GetVaultFamilySettingsAsync(familyId, default)).ReturnsAsync(() => saved);
        repository.Setup(repo => repo.UpsertVaultFamilySettingsAsync(It.IsAny<VaultFamilySettings>(), default))
            .Callback<VaultFamilySettings, CancellationToken>((settings, _) => saved = settings);
        var service = setup.Admin();

        var initial = await service.GetEmergencyAccessRulesAsync(setup.UserId, familyId, default);
        initial.AccessMode.Should().Be("LoginRequired");
        initial.EmergencyLinkExpiryHours.Should().Be(72);
        initial.EmergencyContacts.Should().BeEmpty();
        repository.Verify(repo => repo.UpsertVaultFamilySettingsAsync(It.IsAny<VaultFamilySettings>(), default), Times.Never);

        var updated = await service.UpdateEmergencyAccessRulesAsync(setup.UserId, familyId, new(null, 48, null), default);
        updated.EmergencyLinkExpiryHours.Should().Be(48);
        saved!.FamilyId.Should().Be(setup.Family.InternalId);
        repository.Verify(repo => repo.UpsertVaultFamilySettingsAsync(It.IsAny<VaultFamilySettings>(), default), Times.Once);
    }

    [Fact]
    public async Task Storage_save_records_valid_audit_snapshots_in_the_settings_write()
    {
        var setup = new Setup(UserRole.FamilyAdmin);
        var familyId = setup.Family.Id;
        var repository = setup.Mock<IFamilyAdminConfigRepository>();
        var member = new FamilyMember { FamilyId = setup.Family.InternalId, UserId = 20, Role = UserRole.FamilyAdmin };
        var settings = new VaultFamilySettings { FamilyId = member.FamilyId, StorageMode = "AppManaged", OfflineCacheSizeMb = 2000 };
        repository.Setup(repo => repo.GetActiveFamilyMemberAsync(familyId, setup.UserId, default)).ReturnsAsync(member);
        repository.Setup(repo => repo.GetVaultFamilySettingsAsync(familyId, default)).ReturnsAsync(settings);
        AuditLog? auditLog = null;
        repository.Setup(repo => repo.UpsertVaultFamilySettingsAsync(settings, It.IsAny<AuditLog>(), default))
            .Callback<VaultFamilySettings, AuditLog, CancellationToken>((_, audit, _) => auditLog = audit);

        var result = await setup.Admin().UpdateStorageConfigAsync(setup.UserId, familyId, new("Hybrid", 75, 1000, null), default);

        result.StorageMode.Should().Be("Hybrid");
        result.StorageQuotaAlertThresholdPct.Should().Be(75);
        result.OfflineCacheSizeMb.Should().Be(1000);
        repository.Verify(repo => repo.UpsertVaultFamilySettingsAsync(settings, It.IsAny<AuditLog>(), default), Times.Once);
        auditLog.Should().NotBeNull();
        auditLog!.FamilyId.Should().Be(member.FamilyId);
        auditLog.UserId.Should().Be(member.UserId);
        using var before = JsonDocument.Parse(auditLog.OldValues!);
        using var after = JsonDocument.Parse(auditLog.NewValues!);
        before.RootElement.GetProperty("storageMode").GetString().Should().Be("AppManaged");
        before.RootElement.GetProperty("offlineCacheSizeMb").GetInt32().Should().Be(2000);
        after.RootElement.GetProperty("storageMode").GetString().Should().Be("Hybrid");
        after.RootElement.GetProperty("storageQuotaAlertThresholdPct").GetInt32().Should().Be(75);
        after.RootElement.GetProperty("offlineCacheSizeMb").GetInt32().Should().Be(1000);
    }

    [Theory]
    [InlineData(50, 409)]
    [InlineData(5, 422)]
    public async Task Redemption_retry_or_insufficient_balance_does_not_add_an_extra_row(int balance, int expectedStatus)
    {
        var setup = new Setup(UserRole.Child);
        var reward = new Reward { Family = setup.Family, IsEnabled = true, CoinCost = 20 };
        var child = new ChildProfile { FamilyId = setup.Family.InternalId, CoinBalance = balance };
        setup.Mock<IRewardRepository>().Setup(repo => repo.GetByIdForFamilyAsync(reward.Id, setup.Family.InternalId, default)).ReturnsAsync(reward);
        setup.Mock<IChildProfileRepository>().Setup(repo => repo.GetByIdForFamilyAsync(child.Id, setup.Family.InternalId, default)).ReturnsAsync(child);
        var redemptions = setup.Mock<IRewardRedemptionRepository>();
        RewardRedemption? pending = null;
        redemptions.Setup(repo => repo.GetPendingByChildAndRewardAsync(child.Id, reward.Id, default)).ReturnsAsync(() => pending);
        redemptions.Setup(repo => repo.AddAsync(It.IsAny<RewardRedemption>(), default))
            .Callback<RewardRedemption, CancellationToken>((row, _) => pending = row);
        var service = setup.Rewards();
        var request = new RedeemRequest { ChildProfileId = child.Id };
        if (expectedStatus == 409)
        {
            var first = await service.RedeemAsync(setup.UserId, child.Id, setup.Family.Id, reward.Id, request, default);
            first.RedemptionId.Should().Be(pending!.Id);
        }
        var context = new DefaultHttpContext();
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new ExceptionHandlingMiddleware(
            async _ =>
            {
                await service.RedeemAsync(setup.UserId, child.Id, setup.Family.Id, reward.Id, request, default);
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(expectedStatus);
        System.Text.Encoding.UTF8.GetString(responseBody.ToArray()).Should().Contain(
            expectedStatus == 409 ? "Reward_Already_Redeemed" : "Insufficient_Coins");
        redemptions.Verify(repo => repo.AddAsync(It.IsAny<RewardRedemption>(), default), Times.Exactly(expectedStatus == 409 ? 1 : 0));
    }

    [Fact]
    public async Task Family_admin_can_list_tasks_for_a_child_in_their_family()
    {
        var setup = new Setup(UserRole.FamilyAdmin);
        var child = new ChildProfile { FamilyId = setup.Family.InternalId, InternalId = 40 };
        var date = new DateOnly(2026, 11, 21);
        var task = new TaskItem { TaskName = "Read 20 minutes", ChildProfileId = child.InternalId, ChildProfile = child, ActiveFromDate = date.ToDateTime(TimeOnly.MinValue) };
        setup.Mock<IChildProfileRepository>().Setup(repo => repo.GetByIdForFamilyAsync(child.Id, setup.Family.InternalId, default)).ReturnsAsync(child);
        setup.Mock<ITaskItemRepository>().Setup(repo => repo.ListFamilyTasksAsync(setup.Family.Id, default)).ReturnsAsync(new[] { task });
        var service = setup.Tasks();

        var results = await service.ListTasksAsync(setup.UserId, null, setup.Family.Id, child.Id, date, default);
        results.Should().ContainSingle().Which.TaskId.Should().Be(task.Id);

        Func<Task> otherFamily = () => service.ListTasksAsync(setup.UserId, null, Guid.NewGuid(), child.Id, date, default);
        await otherFamily.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Photo_required_task_rejects_submission_without_photo()
    {
        var setup = new Setup(UserRole.Child);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var task = new TaskItem { IsPhotoRequired = true, ActiveFromDate = today.ToDateTime(TimeOnly.MinValue) };
        setup.Mock<ITaskItemRepository>().Setup(repo => repo.GetByIdForFamilyAsync(task.Id, setup.Family.InternalId, default)).ReturnsAsync(task);
        var service = setup.Tasks();

        Func<Task> action = () => service.SubmitTaskCompletionAsync(setup.UserId, Guid.NewGuid(), setup.Family.Id, task.Id,
            new SubmitTaskCompletionRequest { ScheduledDate = today, PhotoUrl = " " }, default);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors["PhotoUrl"].Should().Equal("Photo_Required");
        setup.Mock<ITaskCompletionRepository>().Verify(repo => repo.AddAsync(It.IsAny<TaskCompletion>(), default), Times.Never);
    }

    private sealed class Setup
    {
        private readonly Dictionary<Type, Mock> _mocks = [];
        public Guid UserId { get; } = Guid.NewGuid();
        public Family Family { get; } = new() { InternalId = 10 };
        public TeacherProfile Teacher { get; }

        public Setup(UserRole role)
        {
            var member = new FamilyMember { Role = role, UserId = 20, FamilyId = Family.InternalId };
            Teacher = new TeacherProfile { InternalId = 30, Family = Family };
            Mock<IFamilyMemberRepository>().Setup(repo => repo.GetActiveByFamilyAndUserAsync(Family.Id, UserId, default)).ReturnsAsync(member);
            Mock<ITeacherProfileRepository>().Setup(repo => repo.GetByFamilyMemberIdAsync(member.Id, default)).ReturnsAsync(Teacher);
            Mock<IMasterDataResolver>().Setup(repo => repo.ResolveAsync(It.IsAny<MasterDataCodes>(), It.IsAny<string>(), It.IsAny<long>(), default))
                .ReturnsAsync(Family.InternalId);
            Mock<IErrorCodeService>().Setup(service => service.GetMessageAsync(It.IsAny<FamilyFirstErrorCode>(), It.IsAny<int>(), default))
                .ReturnsAsync((FamilyFirstErrorCode code, int _, CancellationToken _) => code.ToString());
            Mock<IPermissionService>().Setup(service => service.CheckAsync(role, It.IsAny<FamilyFirstModule>(), It.IsAny<FamilyFirstPermission>(), default))
                .ReturnsAsync(true);
        }

        public Mock<T> Mock<T>() where T : class
        {
            if (!_mocks.TryGetValue(typeof(T), out var mock))
                _mocks.Add(typeof(T), mock = new Mock<T>());
            return (Mock<T>)mock;
        }

        private T Get<T>() where T : class => Mock<T>().Object;

        public AttendanceSession Session(DateTime submittedAt)
        {
            var session = new AttendanceSession { IsSubmitted = true, SubmittedAt = submittedAt, Family = Family, TeacherProfileId = Teacher.InternalId };
            Mock<IAttendanceSessionRepository>().Setup(repo => repo.GetByIdForFamilyAsync(session.Id, Family.InternalId, default)).ReturnsAsync(session);
            return session;
        }

        public AttendanceService Attendance() => new(Get<IAttendanceSessionRepository>(), Get<IAttendanceRecordRepository>(),
            Get<IAuditLogRepository>(), Get<IFamilyMemberRepository>(), Get<ITeacherProfileRepository>(), Get<IChildProfileRepository>(),
            Get<ITeacherChildAssignmentRepository>(), Get<IPushNotificationService>(), Get<IApiLogService>(), Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());

        public FeedbackService Feedback() => new(Get<IFeedbackRepository>(), Get<IFamilyMemberRepository>(), Get<ITeacherProfileRepository>(),
            Get<ITeacherChildAssignmentRepository>(), Get<IChildProfileRepository>(), Get<ICommentTemplateRepository>(),
            Get<IAttendanceSessionRepository>(), Get<IPushNotificationService>(), Get<IApiLogService>(), Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());

        public FamilyAdminService Admin() => new(Get<IFamilyAdminConfigRepository>(), Get<IAuditLogRepository>(), Get<IApiLogService>(),
            Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());

        public RewardService Rewards() => new(Get<IRewardRepository>(), Get<IRewardRedemptionRepository>(), Get<IFamilyMemberRepository>(),
            Get<IChildProfileRepository>(), Get<ICoinService>(), Get<IPushNotificationService>(), Get<IApiLogService>(),
            Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());

        public TaskService Tasks() => new(Get<ITaskItemRepository>(), Get<IFamilyMemberRepository>(), Get<IChildProfileRepository>(),
            Get<ICoinService>(), Get<ITaskCompletionRepository>(), Get<IPushNotificationService>(), Get<IS3StorageService>(),
            Get<IApiLogService>(), Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());
    }
}
