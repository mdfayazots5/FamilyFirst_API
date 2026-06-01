using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

// Append-only. Auto-purged by WeeklyDigestWorker after 13 months.
public sealed class ChildPillarScoreHistory : AppendOnlyEntity
{
    public long ChildProfileId { get; set; }

    public long FamilyId { get; set; }

    public DateTime SnapshotMonth { get; set; }

    public int StudyScore { get; set; }

    public int CleanlinessScore { get; set; }

    public int DisciplineScore { get; set; }

    public int ScreenControlScore { get; set; }

    public int ResponsibilityScore { get; set; }

    public ChildProfile ChildProfile { get; set; } = null!;

    public Family Family { get; set; } = null!;
}
