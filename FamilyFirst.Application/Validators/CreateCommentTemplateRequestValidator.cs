using FamilyFirst.Application.DTOs.Attendance;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class CreateCommentTemplateRequestValidator : AbstractValidator<CreateCommentTemplateRequest>
{
    public CreateCommentTemplateRequestValidator()
    {
        RuleFor(request => request.TemplateText)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(500);

        RuleFor(request => request.Category)
            .NotEmpty()
            .Must(category => CommentTemplateCategories.TryNormalize(category, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", CommentTemplateCategories.AllowedValues)}.");
    }
}
