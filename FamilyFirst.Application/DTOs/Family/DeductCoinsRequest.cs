namespace FamilyFirst.Application.DTOs.Family;

public sealed class DeductCoinsRequest
{
    public int Amount { get; init; }

    public string? Note { get; init; }
}

public sealed record CoinDeductionResultDto(
    Guid ChildProfileId,
    int Amount,
    int CoinBalance,
    string? Note,
    bool IsRecorded);
