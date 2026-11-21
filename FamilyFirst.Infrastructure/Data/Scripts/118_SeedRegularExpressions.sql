-- Seed the regex rules already enforced by the request validators.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

INSERT INTO dbo.tblAPIMethod (MethodName, APIURL, HTTPMethod)
SELECT seed.MethodName, seed.APIURL, seed.HTTPMethod
FROM (VALUES
    (N'SendOtp', N'/api/auth/send-otp', N'POST'),
    (N'VerifyOtp', N'/api/auth/verify-otp', N'POST'),
    (N'SetPin', N'/api/auth/set-pin', N'POST'),
    (N'VerifyPin', N'/api/auth/verify-pin', N'POST'),
    (N'CreateFamily', N'/api/families', N'POST'),
    (N'UpdateFamily', N'/api/families/{familyId:guid}', N'PUT'),
    (N'AddMember', N'/api/families/{familyId:guid}/members', N'POST'),
    (N'JoinFamily', N'/api/families/join', N'POST'),
    (N'UpdateUser', N'/api/users/{userId:guid}', N'PUT'),
    (N'UpdateVaultSettings', N'/api/families/{familyId:guid}/vault/settings', N'PUT')
) AS seed (MethodName, APIURL, HTTPMethod)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tblAPIMethod AS existing
    WHERE existing.MethodName = seed.MethodName AND existing.IsDeleted = 0
);
GO

INSERT INTO dbo.tblRegularExpression (APIMethodId, FieldName, RegexPattern)
SELECT method.APIMethodId, seed.FieldName, seed.RegexPattern
FROM (VALUES
    (N'SendOtp', N'PhoneNumber', N'^\+[1-9]\d{7,14}$'),
    (N'VerifyOtp', N'PhoneNumber', N'^\+[1-9]\d{7,14}$'),
    (N'AddMember', N'PhoneNumber', N'^\+[1-9]\d{7,14}$'),
    (N'SendOtp', N'CountryCode', N'^\+[1-9]\d{0,4}$'),
    (N'VerifyOtp', N'OtpCode', N'^\d{6}$'),
    (N'SetPin', N'Pin', N'\A\d{4}\z'),
    (N'VerifyPin', N'Pin', N'\A\d{4}\z'),
    (N'UpdateVaultSettings', N'EmergencyPin', N'\A\d{4}\z'),
    (N'CreateFamily', N'FamilyName', N'^[\p{L}\d .''\-]+$'),
    (N'UpdateFamily', N'FamilyName', N'^[\p{L}\d .''\-]+$'),
    (N'CreateFamily', N'City', N'^[\p{L} ]+$'),
    (N'UpdateFamily', N'City', N'^[\p{L} ]+$'),
    (N'AddMember', N'FullName', N'^[\p{L} .''\-]+$'),
    (N'JoinFamily', N'FullName', N'^[\p{L} .''\-]+$'),
    (N'UpdateUser', N'FullName', N'^[\p{L} .''\-]+$')
) AS seed (MethodName, FieldName, RegexPattern)
INNER JOIN dbo.tblAPIMethod AS method
    ON method.MethodName = seed.MethodName AND method.IsDeleted = 0
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tblRegularExpression AS existing
    WHERE existing.APIMethodId = method.APIMethodId
      AND existing.FieldName = seed.FieldName
      AND existing.IsDeleted = 0
);
GO
