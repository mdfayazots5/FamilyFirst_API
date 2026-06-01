namespace FamilyFirst.Domain.Enums;

/// <summary>
/// Matches the ErrorCode column values in tblErrorCode.
/// BAL reads the user-facing message from DB via uspGetErrorCodeById — never hardcodes strings.
/// </summary>
public enum FamilyFirstErrorCode
{
    Success                     = 0,
    Failure                     = 1,
    Invalid_Token               = 2,
    Token_Required              = 3,
    User_Not_Found              = 4,
    Invalid_User                = 5,
    Session_Expired             = 6,
    Permission_Denied           = 7,
    Family_Not_Found            = 8,
    Invalid_FamilyId            = 9,
    Missing_Parameters          = 10,
    Invalid_OTP                 = 11,
    OTP_Expired                 = 12,
    OTP_Rate_Limit              = 13,
    Invalid_PhoneNumber         = 14,
    Attendance_Already_Submitted = 15,
    Edit_Window_Closed          = 16,
    Insufficient_Coins          = 17,
    Reward_Already_Redeemed     = 18,
    Task_Not_Found              = 19,
    Photo_Required              = 20,
    Feedback_Edit_Window_Closed = 21,
    Technical_Error             = 22,
    Invalid_MasterData          = 23,
    Invalid_Role                = 24,
    Plan_Limit_Exceeded         = 25,
    Invalid_GUID                = 26,
    Invalid_Module              = 27,
    Validation_Error            = 28,
    Duplicate_Record            = 29,
    Not_Found                   = 30
}
