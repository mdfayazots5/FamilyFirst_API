using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class SubmitAttendanceRequestValidator : AbstractValidator<SubmitAttendanceRequest>
{
    public SubmitAttendanceRequestValidator()
    {
        RuleFor(request => request.Records)
            .NotNull()
            .Must(HaveUniqueChildProfiles)
            .WithMessage("Records cannot contain duplicate ChildProfileId values.");

        RuleForEach(request => request.Records)
            .SetValidator(new SubmitAttendanceRecordRequestValidator());
    }

    private static bool HaveUniqueChildProfiles(IReadOnlyCollection<SubmitAttendanceRecordRequest>? records)
    {
        if (records is null)
        {
            return false;
        }

        return records.Select(record => record.ChildProfileId).Distinct().Count() == records.Count;
    }
}

internal sealed class SubmitAttendanceRecordRequestValidator : AbstractValidator<SubmitAttendanceRecordRequest>
{
    public SubmitAttendanceRecordRequestValidator()
    {
        RuleFor(record => record.ChildProfileId).NotEmpty();

        RuleFor(record => record.Status)
            .Must(status => Enum.IsDefined(typeof(AttendanceStatus), status))
            .WithMessage("Status must be a valid AttendanceStatus value.");

        RuleFor(record => record.TeacherComment)
            .MaximumLength(500)
            .When(record => !string.IsNullOrWhiteSpace(record.TeacherComment));
    }
}
