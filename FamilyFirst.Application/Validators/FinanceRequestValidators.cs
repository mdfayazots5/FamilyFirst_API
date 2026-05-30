using FamilyFirst.Application.DTOs.Finance;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class InviteConsentRequestValidator : AbstractValidator<InviteConsentRequest>
{
    public InviteConsentRequestValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.PrivacyTier)
            .Must(PrivacyTier.IsValid)
            .WithMessage("PrivacyTier must be 1, 2, or 3.");
    }
}

public sealed class AcceptFinanceConsentRequestValidator : AbstractValidator<AcceptFinanceConsentRequest>
{
    public AcceptFinanceConsentRequestValidator()
    {
        RuleFor(x => x.ConsentToken).NotEmpty();
        RuleFor(x => x.ConsentVersion).NotEmpty().MaximumLength(10);
    }
}

public sealed class QuestionTransactionRequestValidator : AbstractValidator<QuestionTransactionRequest>
{
    private static readonly IReadOnlySet<string> ValidTypes = new HashSet<string>
    {
        "FamilyExpense", "PersonalUnderstood", "NeedToKnowMore", "PossibleError"
    };

    public QuestionTransactionRequestValidator()
    {
        RuleFor(x => x.QuestionType)
            .Must(ValidTypes.Contains)
            .WithMessage($"QuestionType must be one of: {string.Join(", ", ValidTypes)}.");
        RuleFor(x => x.ContextNote).MaximumLength(500).When(x => x.ContextNote is not null);
    }
}

public sealed class SetBudgetRequestValidator : AbstractValidator<SetBudgetRequest>
{
    public SetBudgetRequestValidator()
    {
        RuleFor(x => x.Category)
            .Must(FinanceCategory.All.Contains)
            .WithMessage($"Category must be one of the 14 supported categories.");
        RuleFor(x => x.BudgetAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("BudgetAmount must be zero or greater.");
    }
}

public sealed class UpdateFinanceSettingsRequestValidator : AbstractValidator<UpdateFinanceSettingsRequest>
{
    public UpdateFinanceSettingsRequestValidator()
    {
        RuleForEach(x => x.MemberTierChanges)
            .ChildRules(t =>
            {
                t.RuleFor(x => x.MemberId).NotEmpty();
                t.RuleFor(x => x.PrivacyTier)
                    .Must(PrivacyTier.IsValid)
                    .WithMessage("PrivacyTier must be 1, 2, or 3.");
            })
            .When(x => x.MemberTierChanges is not null);
    }
}
