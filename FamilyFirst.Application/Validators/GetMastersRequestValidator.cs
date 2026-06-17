using FamilyFirst.Application.DTOs.StaticData;
using FamilyFirst.Domain.Enums;
using FluentValidation;

namespace FamilyFirst.Application.Validators;

public sealed class GetMastersRequestValidator : AbstractValidator<GetMastersRequest>
{
    public GetMastersRequestValidator()
    {
        RuleFor(request => request.MasterDataCode)
            .NotEmpty()
            .WithMessage("MasterDataCode is required.")
            .Must(code => Enum.TryParse<MasterDataCodes>(code, false, out _))
            .WithMessage(request => $"'{request.MasterDataCode}' is not a recognised master data category.");

        RuleFor(request => request.PageNumber)
            .GreaterThan(0);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 500);

        RuleFor(request => request.LanguageId)
            .GreaterThan(0);
    }
}
