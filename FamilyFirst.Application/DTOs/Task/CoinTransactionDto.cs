namespace FamilyFirst.Application.DTOs.Task;

public sealed record CoinTransactionDto(
    Guid TransactionId,
    Guid ChildProfileId,
    Guid FamilyId,
    string TransactionType,
    int Amount,
    int BalanceAfter,
    string ReferenceType,
    Guid? ReferenceId,
    string? Note,
    Guid CreatedByUserId,
    DateTime CreatedAt);
