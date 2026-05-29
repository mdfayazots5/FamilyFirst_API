using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class ChildProfile : BaseEntity
{
    public Guid FamilyMemberId { get; set; }

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? GradeLevel { get; set; }

    public string? SchoolName { get; set; }

    public string AvatarCode { get; set; } = "avatar_01";

    public int CoinBalance { get; set; }

    public int TotalCoinsEarned { get; set; }

    public int CurrentStreakDays { get; set; }

    public int BestStreakDays { get; set; }

    public int StreakFreezesAvailable { get; set; }

    public int LevelCode { get; set; } = 1;

    public int StudyScore { get; set; }

    public int CleanlinessScore { get; set; }

    public int DisciplineScore { get; set; }

    public int ScreenControlScore { get; set; }

    public int ResponsibilityScore { get; set; }

    public DateTime? ScoreUpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public FamilyMember? FamilyMember { get; set; }

    public User? User { get; set; }

    public Family? Family { get; set; }
}
