namespace FamilyFirst.Application.DTOs.Task;

public sealed class DeductCoinsRequest
{
    public int Amount { get; init; }

    public string? Note { get; init; }
}
