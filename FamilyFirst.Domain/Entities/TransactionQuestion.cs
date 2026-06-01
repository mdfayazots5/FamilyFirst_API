using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class TransactionQuestion : BaseEntity
{
    public long FamilyId { get; set; }

    public long TransactionId { get; set; }

    public string QuestionType { get; set; } = string.Empty;

    public string? ContextNote { get; set; }

    public DateTime MessageSentAt { get; set; } = DateTime.UtcNow;

    public string? MemberReply { get; set; }

    public DateTime? ReplyReceivedAt { get; set; }

    public string? ResolutionStatus { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public long? ResolvedByUserId { get; set; }

    public Family Family { get; set; } = null!;

    public Transaction Transaction { get; set; } = null!;

    public User? ResolvedByUser { get; set; }
}
