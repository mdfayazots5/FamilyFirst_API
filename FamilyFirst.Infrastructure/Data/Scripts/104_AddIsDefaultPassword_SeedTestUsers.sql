-- =============================================================================
-- 104_AddIsDefaultPassword_SeedTestUsers.sql
-- Adds IsDefaultPassword flag to tblUser and seeds one test user per role.
-- Default password for all seeded accounts: 123456
-- Hash algorithm: PBKDF2-SHA256, 100 000 iterations, 16-byte salt (all zeros),
--                 32-byte output. Format: v1.<base64(salt)>.<base64(hash)>
-- Hash of "123456" with zero salt:
--   v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=
-- =============================================================================

-- ─── 1. Add IsDefaultPassword column ─────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblUser') AND name = N'IsDefaultPassword'
)
BEGIN
    ALTER TABLE dbo.tblUser
        ADD IsDefaultPassword BIT NOT NULL
            CONSTRAINT DF_tblUser_IsDefaultPassword DEFAULT (0);
END;
GO

-- ─── 2. Seed SuperAdmin user ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000001' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000001', N'+91', N'Super Admin', N'superadmin@familyfirst.app',
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 3. Seed FamilyAdmin user ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000002' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000002', N'+91', N'Rahul Sharma (Family Admin)', N'familyadmin@familyfirst.app',
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 4. Seed Parent user ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000003' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000003', N'+91', N'Priya Sharma (Parent)', N'parent@familyfirst.app',
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 5. Seed Child user ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000004' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000004', N'+91', N'Arjun Sharma (Child)', NULL,
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 6. Seed Teacher user ─────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000005' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000005', N'+91', N'Anjali Verma (Teacher)', N'teacher@familyfirst.app',
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 7. Seed Elder user ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE PhoneNumber = N'+919000000006' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblUser
    (
        PhoneNumber, CountryCode, FullName, Email,
        PasswordHash, IsDefaultPassword,
        IsPhoneVerified, IsActive, PreferredLanguage,
        CreatedBy, DateCreated
    )
    VALUES
    (
        N'+919000000006', N'+91', N'Dadi Ji (Elder)', NULL,
        N'v1.AAAAAAAAAAAAAAAAAAAAAA==.ncgEKxKdY+dMqZwLhTCJ6hXLw9qL9ARsXiy4lgz0uTo=', 1,
        1, 1, N'en',
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 8. Seed test family (linked to FamilyAdmin user) ────────────────────────
DECLARE @FamilyAdminUserId BIGINT = (
    SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000002' AND IsDeleted = 0
);
DECLARE @PremiumPlanId BIGINT = (
    SELECT TOP 1 PlanId FROM dbo.tblPlan WHERE PlanCode = N'premium' AND IsDeleted = 0
);

IF @FamilyAdminUserId IS NOT NULL
   AND @PremiumPlanId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamily WHERE JoinCode = N'TEST01' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamily
    (
        FamilyName, JoinCode, City,
        PlanId, FamilyAdminUserId,
        IsActive, CreatedBy, DateCreated
    )
    VALUES
    (
        N'Sharma Test Family', N'TEST01', N'Mumbai',
        @PremiumPlanId, @FamilyAdminUserId,
        1, N'Seed', GETDATE()
    );
END;
GO

-- ─── 9. Seed FamilyMember records ────────────────────────────────────────────
DECLARE @FamilyId BIGINT = (
    SELECT FamilyId FROM dbo.tblFamily WHERE JoinCode = N'TEST01' AND IsDeleted = 0
);

-- SuperAdmin (Role = 1)
DECLARE @UserId1 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000001' AND IsDeleted = 0);
IF @FamilyId IS NOT NULL AND @UserId1 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId1 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId1, 1, N'SuperAdmin', N'Super Admin', 1, N'Seed', GETDATE());
END;

-- FamilyAdmin (Role = 2)
DECLARE @UserId2 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000002' AND IsDeleted = 0);
IF @FamilyId IS NOT NULL AND @UserId2 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId2 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId2, 2, N'FamilyAdmin', N'Rahul Sharma', 1, N'Seed', GETDATE());
END;

-- Parent (Role = 3)
DECLARE @UserId3 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000003' AND IsDeleted = 0);
IF @FamilyId IS NOT NULL AND @UserId3 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId3 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId3, 3, N'Parent', N'Priya Sharma', 1, N'Seed', GETDATE());
END;

-- Child (Role = 4)
DECLARE @UserId4 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000004' AND IsDeleted = 0);
DECLARE @ChildMemberId BIGINT;
IF @FamilyId IS NOT NULL AND @UserId4 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId4 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId4, 4, N'Child', N'Arjun', 1, N'Seed', GETDATE());
    SET @ChildMemberId = SCOPE_IDENTITY();
END;
ELSE
BEGIN
    SELECT @ChildMemberId = FamilyMemberId FROM dbo.tblFamilyMember
    WHERE FamilyId = @FamilyId AND UserId = @UserId4 AND IsDeleted = 0;
END;

-- Teacher (Role = 5)
DECLARE @UserId5 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000005' AND IsDeleted = 0);
DECLARE @TeacherMemberId BIGINT;
IF @FamilyId IS NOT NULL AND @UserId5 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId5 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId5, 5, N'Teacher', N'Anjali Verma', 1, N'Seed', GETDATE());
    SET @TeacherMemberId = SCOPE_IDENTITY();
END;
ELSE
BEGIN
    SELECT @TeacherMemberId = FamilyMemberId FROM dbo.tblFamilyMember
    WHERE FamilyId = @FamilyId AND UserId = @UserId5 AND IsDeleted = 0;
END;

-- Elder (Role = 6)
DECLARE @UserId6 BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000006' AND IsDeleted = 0);
IF @FamilyId IS NOT NULL AND @UserId6 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblFamilyMember WHERE FamilyId = @FamilyId AND UserId = @UserId6 AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType, DisplayName, IsActive, CreatedBy, DateCreated)
    VALUES (@FamilyId, @UserId6, 6, N'Elder', N'Dadi Ji', 1, N'Seed', GETDATE());
END;
GO

-- ─── 10. Seed ChildProfile for Child member ───────────────────────────────────
DECLARE @ChildFamilyMemberId BIGINT = (
    SELECT fm.FamilyMemberId
    FROM dbo.tblFamilyMember fm
    INNER JOIN dbo.tblUser u ON u.UserId = fm.UserId
    WHERE u.PhoneNumber = N'+919000000004' AND fm.IsDeleted = 0 AND u.IsDeleted = 0
);
DECLARE @ChildUserId BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000004' AND IsDeleted = 0);
DECLARE @ChildFamilyId BIGINT  = (SELECT FamilyId FROM dbo.tblFamily WHERE JoinCode = N'TEST01' AND IsDeleted = 0);

IF @ChildFamilyMemberId IS NOT NULL AND @ChildUserId IS NOT NULL AND @ChildFamilyId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblChildProfile WHERE FamilyMemberId = @ChildFamilyMemberId AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblChildProfile
    (
        FamilyMemberId, UserId, FamilyId,
        DateOfBirth, GradeLevel, SchoolName,
        AvatarCode, CoinBalance,
        CreatedBy, DateCreated
    )
    VALUES
    (
        @ChildFamilyMemberId, @ChildUserId, @ChildFamilyId,
        '2013-06-15', N'Grade 6', N'St. Xavier School',
        N'avatar_01', 0,
        N'Seed', GETDATE()
    );
END;
GO

-- ─── 11. Seed TeacherProfile for Teacher member ───────────────────────────────
DECLARE @TeacherFamilyMemberId BIGINT = (
    SELECT fm.FamilyMemberId
    FROM dbo.tblFamilyMember fm
    INNER JOIN dbo.tblUser u ON u.UserId = fm.UserId
    WHERE u.PhoneNumber = N'+919000000005' AND fm.IsDeleted = 0 AND u.IsDeleted = 0
);
DECLARE @TeacherUserId BIGINT = (SELECT UserId FROM dbo.tblUser WHERE PhoneNumber = N'+919000000005' AND IsDeleted = 0);
DECLARE @TeacherFamilyId BIGINT = (SELECT FamilyId FROM dbo.tblFamily WHERE JoinCode = N'TEST01' AND IsDeleted = 0);

IF @TeacherFamilyMemberId IS NOT NULL AND @TeacherUserId IS NOT NULL AND @TeacherFamilyId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tblTeacherProfile WHERE FamilyMemberId = @TeacherFamilyMemberId AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.tblTeacherProfile
    (
        FamilyMemberId, UserId, FamilyId,
        SubjectName, TeacherType,
        IsActive, CreatedBy, DateCreated
    )
    VALUES
    (
        @TeacherFamilyMemberId, @TeacherUserId, @TeacherFamilyId,
        N'Mathematics', N'School',
        1, N'Seed', GETDATE()
    );
END;
GO
