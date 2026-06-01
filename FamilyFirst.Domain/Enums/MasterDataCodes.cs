namespace FamilyFirst.Domain.Enums;

/// <summary>
/// Matches the MasterDataCode column values registered in tblMasterData.
/// Enum name must EXACTLY match the MasterDataCode string — used via .ToString()
/// when calling uspGetMasterDataByCode and uspGetMasterDataByCodeInternal.
/// </summary>
public enum MasterDataCodes
{
    // ── Table-backed entity lookups ──────────────────────────────────────────
    Family                  = 0,    // tblFamily — used for ResolveFamilyIdAsync
    Role                    = 1,
    Module                  = 2,
    Permission              = 3,
    Plan                    = 4,
    User                    = 5,
    FamilyMember            = 6,
    ChildProfile            = 7,
    TeacherProfile          = 8,
    CustomAttendanceStatus  = 9,
    Reward                  = 10,

    // ── Simple lookup tables (tbl* created in 092_CreateLookupTables.sql) ───
    TaskType                = 11,
    TaskStatus              = 12,
    AttendanceStatus        = 13,
    RewardType              = 14,
    CoinTransactionType     = 15,
    FeedbackRating          = 16,
    CalendarEventType       = 17,
    NotificationType        = 18,
    OTPType                 = 19
}
