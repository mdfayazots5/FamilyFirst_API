-- ============================================================
-- Script  : 084_SeedErrorCodes.sql
-- Purpose : Seed tblErrorCode with all FamilyFirst error/success
--           messages. BAL reads these in the finally block via
--           uspGetErrorCodeById instead of hardcoded strings.
--           ErrorCode values match the ErrorCode enum in
--           FamilyFirstEnums.cs.
--           LanguageId = 1 (English — only language for Level 1).
-- Depends : 074_CreateErrorCode.sql
-- ============================================================

INSERT INTO dbo.tblErrorCode
    (ErrorCode, ErrorName, ReturnCode, ReturnMessage, LanguageId,
     IsPublished, DisplayOnWeb, IsDeleted, DateCreated, CreatedBy)
SELECT
    source.ErrorCode, source.ErrorName, source.ReturnCode,
    source.ReturnMessage, 1, 1, 1, 0, GETDATE(), N'System'
FROM (VALUES
    ( 0, N'Success',                      0,  N'Success'),
    ( 1, N'Failure',                       1,  N'Operation failed. Please try again.'),
    ( 2, N'Invalid_Token',                 2,  N'Invalid or expired authentication token.'),
    ( 3, N'Token_Required',                3,  N'Authentication token is required.'),
    ( 4, N'User_Not_Found',                4,  N'User not found.'),
    ( 5, N'Invalid_User',                  5,  N'Invalid user credentials.'),
    ( 6, N'Session_Expired',               6,  N'Your session has expired. Please login again.'),
    ( 7, N'Permission_Denied',             7,  N'You do not have permission to perform this action.'),
    ( 8, N'Family_Not_Found',              8,  N'Family not found.'),
    ( 9, N'Invalid_FamilyId',              9,  N'Invalid family identifier.'),
    (10, N'Missing_Parameters',            10, N'One or more required parameters are missing.'),
    (11, N'Invalid_OTP',                   11, N'Invalid OTP. Please check and try again.'),
    (12, N'OTP_Expired',                   12, N'OTP has expired. Please request a new one.'),
    (13, N'OTP_Rate_Limit',                13, N'OTP request limit reached (3 per hour). Please try again later.'),
    (14, N'Invalid_PhoneNumber',           14, N'Invalid phone number format.'),
    (15, N'Attendance_Already_Submitted',  15, N'Attendance has already been submitted for this session.'),
    (16, N'Edit_Window_Closed',            16, N'Attendance can only be edited within 1 hour of submission.'),
    (17, N'Insufficient_Coins',            17, N'Insufficient coins for this redemption.'),
    (18, N'Reward_Already_Redeemed',       18, N'This reward has already been redeemed.'),
    (19, N'Task_Not_Found',                19, N'Task not found.'),
    (20, N'Photo_Required',                20, N'A photo proof is required to complete this task.'),
    (21, N'Feedback_Edit_Window_Closed',   21, N'Feedback can only be edited within 24 hours of posting.'),
    (22, N'Technical_Error',               22, N'A technical error occurred. Please try again.'),
    (23, N'Invalid_MasterData',            23, N'Invalid master data identifier.'),
    (24, N'Invalid_Role',                  24, N'Invalid role identifier.'),
    (25, N'Plan_Limit_Exceeded',           25, N'Your plan limit has been reached. Please upgrade your plan.'),
    (26, N'Invalid_GUID',                  26, N'Invalid identifier format.'),
    (27, N'Invalid_Module',                27, N'Invalid module identifier.'),
    (28, N'Validation_Error',              28, N'One or more validation errors occurred.'),
    (29, N'Duplicate_Record',              29, N'A record with this information already exists.'),
    (30, N'Not_Found',                     30, N'The requested resource was not found.')
) AS source (ErrorCode, ErrorName, ReturnCode, ReturnMessage)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblErrorCode t
    WHERE t.ErrorCode  = source.ErrorCode
      AND t.LanguageId = 1
      AND t.IsDeleted  = 0
);
GO
