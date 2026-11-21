namespace FamilyFirst.Domain.Enums;

public static class StaticMasterData
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        nameof(MasterDataCodes.Role), nameof(MasterDataCodes.Module), nameof(MasterDataCodes.Permission), nameof(MasterDataCodes.Plan),
        nameof(MasterDataCodes.TaskType), nameof(MasterDataCodes.TaskStatus), nameof(MasterDataCodes.AttendanceStatus),
        nameof(MasterDataCodes.RewardType), nameof(MasterDataCodes.CoinTransactionType), nameof(MasterDataCodes.FeedbackRating),
        nameof(MasterDataCodes.CalendarEventType), nameof(MasterDataCodes.NotificationType), nameof(MasterDataCodes.OTPType),
        nameof(MasterDataCodes.DocumentCategory), nameof(MasterDataCodes.HealthRecordType), nameof(MasterDataCodes.SafeZoneType),
        nameof(MasterDataCodes.FinanceCategory)
    };

    public static bool Contains(string code) => Codes.Contains(code);
}
