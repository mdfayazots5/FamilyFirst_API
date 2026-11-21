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

        public RewardService Rewards() => new(Get<IRewardRepository>(), Get<IRewardRedemptionRepository>(), Get<IFamilyMemberRepository>(),
            Get<IChildProfileRepository>(), Get<ICoinService>(), Get<IPushNotificationService>(), Get<IApiLogService>(),
            Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());

        public TaskService Tasks() => new(Get<ITaskItemRepository>(), Get<IFamilyMemberRepository>(), Get<IChildProfileRepository>(),
            Get<ICoinService>(), Get<ITaskCompletionRepository>(), Get<IPushNotificationService>(), Get<IS3StorageService>(),
            Get<IApiLogService>(), Get<IPermissionService>(), Get<IErrorCodeService>(), Get<IMasterDataResolver>());
    }
}
