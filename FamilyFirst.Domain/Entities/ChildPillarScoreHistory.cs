namespace FamilyFirst.Domain.Entities;

// NOT a BaseEntity — append-only, no IsDeleted/UpdatedAt.
// Auto-purged by WeeklyDigestWorker after 13 months (keeps 12 full months + current month in progress).
public sealed class ChildPillarScoreHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChildProfileId { get; set; }

    public Guid FamilyId { get; set; }

    public DateOnly SnapshotMonth { get; set; }

    public int StudyScore { get; set; }

    public int CleanlinessScore { get; set; }

    public int DisciplineScore { get; set; }

    public int ScreenControlScore { get; set; }

    public int ResponsibilityScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChildProfile ChildProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;
}
