using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Entities.Base;
using FamilyFirst.Infrastructure.Data;
using FamilyFirst.Infrastructure.Data.Repositories.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Tests;

public sealed class FamilyIsolationTests
{
    [Theory]
    [InlineData("AttendanceRecord")]
    [InlineData("AttendanceSession")]
    [InlineData("CalendarEvent")]
    [InlineData("ChildProfile")]
    [InlineData("FamilyMember")]
    [InlineData("Feedback")]
    [InlineData("Notification")]
    [InlineData("Reward")]
    [InlineData("RewardRedemption")]
    [InlineData("TaskCompletion")]
    [InlineData("TaskItem")]
    [InlineData("TeacherProfile")]
    [InlineData("User")]
    public async Task Family_lookup_returns_active_record_only_in_its_own_family(string repository)
    {
        var options = new DbContextOptionsBuilder<FamilyFirstDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new FamilyFirstDbContext(options);
        var user = new User { InternalId = 1, PhoneNumber = "+919000000001" };
        var plan = new Plan { InternalId = 1 };
        var family = new Family { InternalId = 1, FamilyAdminUserId = user.InternalId, JoinCode = "ABC001", PlanId = plan.InternalId };
        var otherFamily = new Family { InternalId = 2, FamilyAdminUserId = user.InternalId, JoinCode = "ABC002", PlanId = plan.InternalId };
        var member = new FamilyMember { InternalId = 1, FamilyId = family.InternalId, UserId = user.InternalId };
        var child = new ChildProfile { InternalId = 1, FamilyId = family.InternalId, FamilyMemberId = member.InternalId, UserId = user.InternalId };
        var teacher = new TeacherProfile { InternalId = 1, FamilyId = family.InternalId, FamilyMemberId = member.InternalId, UserId = user.InternalId };
        var session = new AttendanceSession { InternalId = 1, FamilyId = family.InternalId, TeacherProfileId = teacher.InternalId };
        var attendance = new AttendanceRecord { FamilyId = family.InternalId, AttendanceSessionId = session.InternalId, ChildProfileId = child.InternalId, MarkedByUserId = user.InternalId };
        var calendar = new CalendarEvent { FamilyId = family.InternalId, CreatedByUserId = user.InternalId };
        var feedback = new TeacherFeedback { FamilyId = family.InternalId, ChildProfileId = child.InternalId, TeacherProfileId = teacher.InternalId };
        var notification = new Notification { FamilyId = family.InternalId, RecipientUserId = user.InternalId };
        var reward = new Reward { InternalId = 1, FamilyId = family.InternalId };
        var redemption = new RewardRedemption { FamilyId = family.InternalId, ChildProfileId = child.InternalId, RewardId = reward.InternalId };
        var task = new TaskItem { InternalId = 1, FamilyId = family.InternalId, CreatedByUserId = user.InternalId };
        var completion = new TaskCompletion { FamilyId = family.InternalId, ChildProfileId = child.InternalId, TaskItemId = task.InternalId };

        db.AddRange(plan, user, family, otherFamily, member, child, teacher, session, attendance, calendar, feedback,
            notification, reward, redemption, task, completion);
        await db.SaveChangesAsync();

        (BaseEntity Record, Func<long, Task<BaseEntity?>> Find) lookup = repository switch
        {
            "AttendanceRecord" => (attendance, async id => await new AttendanceRecordRepository(db).GetByIdForFamilyAsync(attendance.Id, id, default)),
            "AttendanceSession" => (session, async id => await new AttendanceSessionRepository(db).GetByIdForFamilyAsync(session.Id, id, default)),
            "CalendarEvent" => (calendar, async id => await new CalendarEventRepository(db).GetByIdForFamilyAsync(calendar.Id, id, default)),
            "ChildProfile" => (child, async id => await new ChildProfileRepository(db).GetByIdForFamilyAsync(child.Id, id, default)),
            "FamilyMember" => (member, async id => await new FamilyMemberRepository(db).GetByIdForFamilyAsync(member.Id, id, default)),
            "Feedback" => (feedback, async id => await new FeedbackRepository(db).GetByIdForFamilyAsync(feedback.Id, id, default)),
            "Notification" => (notification, async id => await new NotificationRepository(db).GetByIdForFamilyAsync(notification.Id, id, default)),
            "Reward" => (reward, async id => await new RewardRepository(db).GetByIdForFamilyAsync(reward.Id, id, default)),
            "RewardRedemption" => (redemption, async id => await new RewardRedemptionRepository(db).GetByIdForFamilyAsync(redemption.Id, id, default)),
            "TaskCompletion" => (completion, async id => await new TaskCompletionRepository(db).GetByIdForFamilyAsync(completion.Id, id, default)),
            "TaskItem" => (task, async id => await new TaskItemRepository(db).GetByIdForFamilyAsync(task.Id, id, default)),
            "TeacherProfile" => (teacher, async id => await new TeacherProfileRepository(db).GetByIdForFamilyAsync(teacher.Id, id, default)),
            "User" => (user, async id => await new UserRepository(db).GetByIdForFamilyAsync(user.Id, id, default)),
            _ => throw new ArgumentOutOfRangeException(nameof(repository))
        };

        var (record, find) = lookup;
        var found = await find(family.InternalId);
        found.Should().NotBeNull();
        found!.Id.Should().Be(record.Id);
        (await find(otherFamily.InternalId)).Should().BeNull();
        record.IsDeleted = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        (await find(family.InternalId)).Should().BeNull();
    }
}
