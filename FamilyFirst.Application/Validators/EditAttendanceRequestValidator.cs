using FamilyFirst.Application.DTOs.Attendance;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class EditAttendanceRequestValidator : AbstractValidator<EditAttendanceRequest>
{
    public EditAttendanceRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(typeof(AttendanceStatus), status))
            .WithMessage("Status must be a valid AttendanceStatus value.");

        RuleFor(request => request.TeacherComment)
            .MaximumLength(500)
            .When(request => !string.IsNullOrWhiteSpace(request.TeacherComment));
    }
}
