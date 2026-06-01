# FamilyFirst — Flow_Change.md Database Gap Analysis & Deployment Plan
**Prepared By:** Claude Project AI Engineer  
**Date:** 2026-06-01  
**Reference DB:** `RevalPOS_RevalERPlocalDB` (localdb)\MSSQLLocalDB  
**Target DB:** FamilyFirst (SQL Server 2022 · LocalDB dev / SQL Server prod)  
**Source Analysis:** `API/Docs/Task/Flow_Change_Analysis.md` + `API/Docs/Task/Flow_Change.md`

---

## 1. EXECUTIVE SUMMARY

The `Flow_Change.md` reference implementation (Revalsys SAAS POS) uses a foundation of infrastructure tables and stored procedures that FamilyFirst does not yet have. These are **non-negotiable prerequisites** for:

- GUID-only UI contract (Rule 1 from founder's notes)
- DB-driven master data dropdowns (Rule 2)
- DB-driven error messages (Rule 6)
- DB-driven regex validation (Rule 5)
- Per-operation permission checking in BAL (Rule 4)
- Async API logging (Rule 3C)
- Memory/Redis cache warmup at startup (Rule 3)

**Current FamilyFirst state:** 66 scripts deployed (tables 001–066). Zero infrastructure/foundation tables.  
**Gap:** 11 tables + 6 stored procedures + seed data + API layer changes.  
**Scripts required:** 067 through 090 (24 scripts total).

---

## 2. DATABASE OBJECTS — REFERENCE vs. FamilyFirst

### 2.1 Reference DB Tables — Existence Check

| Table Name | Exists in Ref DB | Exists in FamilyFirst | Action |
|---|---|---|---|
| `tblMasterData` | ✅ YES | ❌ NO | CREATE |
| `tblErrorCode` | ✅ YES | ❌ NO | CREATE |
| `tblRegularExpression` | ❌ NO (typo in notes) | ❌ NO | CREATE (new — no ref template) |
| `tblAPIMethod` | ✅ YES | ❌ NO | CREATE |
| `tblAPILog` (`tblAPILogDetail` in ref) | ✅ YES | ❌ NO | CREATE (adapted) |
| `tblModule` | ✅ YES | ❌ NO | CREATE |
| `tblSubModule` | ✅ YES | ❌ NO | CREATE |
| `tblModulePermission` | ✅ YES | ❌ NO | CREATE |
| `tblRolePermission` | ✅ YES | ❌ NO | CREATE |
| `tblPermission` | ✅ YES | ❌ NO | CREATE |
| `tblRole` | ✅ YES | ❌ NO | CREATE |
| `tblStaticAPITemplate` | ✅ YES | ❌ NO | DEFER (Phase C) |
| `tblSMSTemplate` / `tblSMSLog` | ✅ YES | Planned (MSG91) | Already in FF roadmap |
| `tblNotificationTemplate` / `tblNotificationLog` | ✅ YES | ✅ Partially (033) | Extend as needed |

### 2.2 Reference DB Stored Procedures — Existence Check

| SP Name (Reference) | Exists in Ref DB | FamilyFirst Equivalent | Action |
|---|---|---|---|
| `usp_MasterData_GetMasterDataByCode` | ✅ YES | `uspGetMasterDataByCode` | CREATE |
| `usp_MasterData_GetMasterDataByCodeInternal` | ✅ YES | `uspGetMasterDataByCodeInternal` | CREATE |
| `usp_MasterData_GetSpNameByCode` | ✅ YES | `uspGetMasterDataSpNameByCode` | CREATE |
| `usp_ErrorCode_GetErrorCodeById` | ✅ YES | `uspGetErrorCodeById` | CREATE |
| `usp_CommonBAL_CheckRolePermission` | Inferred | `uspCheckRolePermission` | CREATE |
| `usp_APILog_InsertAPILog` | Inferred | `uspInsertAPILog` | CREATE |

---

## 3. WHAT TO REUSE vs. WHAT IS FamilyFirst-SPECIFIC

### 3.1 Reusable from Reference (same pattern, different data)

| Object | Reference Table | FamilyFirst Equivalent | Reuse Level |
|---|---|---|---|
| Master data pattern | `tblMasterData` | `tblMasterData` | Full — same schema adapted |
| Error code pattern | `tblErrorCode` | `tblErrorCode` | Full — same schema, FF error codes |
| API log pattern | `tblAPILogDetail` | `tblAPILog` | Full — simplified (no multi-tenant fields) |
| API method registry | `tblAPIMethod` | `tblAPIMethod` | Full — same structure |
| Permission table | `tblPermission` | `tblPermission` | Full — same 5 permission types |
| Role-permission mapping | `tblRolePermission` | `tblRolePermission` | Structural reuse, FF roles as data |
| Module-permission mapping | `tblModulePermission` | `tblModulePermission` | Full |
| GetMasterDataByCode pattern | `usp_MasterData_GetMasterDataByCode` | `uspGetMasterDataByCode` | Pattern reuse — FF-specific module list |
| GetErrorCodeById pattern | `usp_ErrorCode_GetErrorCodeById` | `uspGetErrorCodeById` | Full pattern reuse |

### 3.2 NOT Reused (reference pattern excluded)

| Reference Object | Reason Not Adopted |
|---|---|
| `tblCountry / tblState / tblCity / tblPincode` | India-only. Static data or handled in mobile |
| `tblWhatsAppProvider/Template/Log` | Not in Level 1 scope |
| `tblStoreEmailTemplate/Log` | No store concept in FamilyFirst |
| `tblUserType` | FamilyFirst uses `tblRole` for this |
| `tblStaticAPITemplate` | Phase C — defer until generic search is needed |
| Multi-DB routing (MsSql/MySql branching) | FamilyFirst: SQL Server only |
| Multi-tenant connection string per request | FamilyFirst: FamilyId scope, single DB |
| CountryCode/CurrencyCode per-request validation | India-only, not needed |
| SiteCode / customer private key flow | No customer concept in FF |
| Subscriptions | Explicitly removed by founder |

---

## 4. REQUIRED TABLES — FULL STRUCTURE DETAILS

> All tables follow `New SQL Format.txt`: `tbl<EntityName>` prefix, `BIGINT IDENTITY` PK named `<EntityName>Id`, GUID column `Id UNIQUEIDENTIFIER NOT NULL DEFAULT(NEWID())`, mandatory audit columns.

### 4.1 tblPermission (Script 067)
Stores the 5 operation-level permission types.

| Column | Type | Notes |
|---|---|---|
| PermissionId | BIGINT IDENTITY | PK — internal |
| Id | UNIQUEIDENTIFIER | GUID — API-facing |
| PermissionName | NVARCHAR(128) | View, Create/Update, Delete, Approve/Reject, Admin View |
| PermissionCode | NVARCHAR(64) | V, CU, D, AR, AV |
| + standard audit columns | | |

Seed: 5 rows (View=1, Create/Update=2, Delete=3, Approve/Reject=4, Admin View=5)

### 4.2 tblRole (Script 068)
FamilyFirst role definitions.

| Column | Type | Notes |
|---|---|---|
| RoleId | BIGINT IDENTITY | PK — internal |
| Id | UNIQUEIDENTIFIER | GUID — API-facing |
| RoleName | NVARCHAR(128) | SuperAdmin, FamilyAdmin, Parent, Child, Teacher, Elder |
| RoleCode | NVARCHAR(64) | SA, FA, PA, CH, TE, EL |
| RoleDescription | NVARCHAR(512) | NULL |
| + standard audit columns | | |

Seed: 6 rows matching CLAUDE.md role definitions.

### 4.3 tblModule (Script 069)
FamilyFirst Level 1 module registry.

| Column | Type | Notes |
|---|---|---|
| ModuleId | BIGINT IDENTITY | PK — internal |
| Id | UNIQUEIDENTIFIER | GUID — API-facing |
| ModuleName | NVARCHAR(128) | Authentication, Family, etc. |
| ModuleCode | NVARCHAR(64) | AUTH, FAMILY, DASH, ATTEND, TASK, FEEDBACK, REWARDS, CALENDAR, NOTIF, ADMIN |
| ParentModuleId | BIGINT | NULL for root modules |
| + standard audit columns | | |

Seed: 10 rows (Level 1 modules per CLAUDE.md).

### 4.4 tblSubModule (Script 070)
Sub-module groupings within each module.

| Column | Type | Notes |
|---|---|---|
| SubModuleId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| ModuleId | BIGINT | FK → tblModule |
| SubModuleName | NVARCHAR(128) | |
| SubModuleCode | NVARCHAR(64) | |
| + standard audit columns | | |

### 4.5 tblModulePermission (Script 071)
Maps which permission types apply to each module.

| Column | Type | Notes |
|---|---|---|
| ModulePermissionId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| ModuleId | BIGINT | FK → tblModule |
| PermissionId | BIGINT | FK → tblPermission |
| + standard audit columns | | |

### 4.6 tblRolePermission (Script 072)
Defines which roles can perform which operations on which modules.

| Column | Type | Notes |
|---|---|---|
| RolePermissionId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| RoleId | BIGINT | FK → tblRole |
| ModuleId | BIGINT | FK → tblModule |
| PermissionId | BIGINT | FK → tblPermission |
| + standard audit columns | | |

Seed: Per CLAUDE.md role-scope rules. ~60 rows.

### 4.7 tblMasterData (Script 073)
Single source of truth for all dropdown/lookup values.

| Column | Type | Notes |
|---|---|---|
| MasterDataId | BIGINT IDENTITY | PK — NEVER exposed to API |
| Id | UNIQUEIDENTIFIER | GUID — only identifier sent to UI |
| MasterDataName | NVARCHAR(128) | Display name |
| MasterDataCode | NVARCHAR(64) | Category code: Role, TaskType, AttendanceStatus, etc. |
| MasterCodeSpName | NVARCHAR(256) | NULL — optional custom SP per category |
| IsMasterData | BIT | 1 = lookup value, 0 = category header |
| ModuleId | BIGINT | NULL — optional module scope |
| + standard audit columns | | |

Seed: FamilyFirst master data codes (Role, TaskType, TaskStatus, AttendanceStatus, RewardType, CoinTransactionType, FeedbackRating, CalendarEventType, NotificationType).

### 4.8 tblErrorCode (Script 074)
All API error/success messages. BAL reads these instead of hardcoded strings.

| Column | Type | Notes |
|---|---|---|
| ErrorCodeId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| ErrorCode | INT | Numeric error code (enum value) |
| ErrorName | NVARCHAR(256) | Internal name |
| ReturnCode | INT | Same as ErrorCode (alias for response) |
| ReturnMessage | NVARCHAR(1024) | User-facing message |
| LanguageId | INT | 1=English (only language for Level 1) |
| + standard audit columns | | |

Seed: 30+ FamilyFirst error codes.

### 4.9 tblRegularExpression (Script 075)
Per-API, per-field regex patterns. Loaded into IMemoryCache at startup.

| Column | Type | Notes |
|---|---|---|
| RegularExpressionId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| APIMethodId | BIGINT | FK → tblAPIMethod |
| FieldName | NVARCHAR(128) | e.g. PhoneNumber, OTPCode, TaskTitle |
| RegexPattern | NVARCHAR(1024) | The actual regex |
| Description | NVARCHAR(512) | Human description of rule |
| + standard audit columns | | |

Seed: After tblAPIMethod is seeded. Key patterns for phone, OTP, names, PIN.

### 4.10 tblAPIMethod (Script 076)
Registry of all API endpoints — used for logging, rate limiting, regex lookup.

| Column | Type | Notes |
|---|---|---|
| APIMethodId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| MethodName | NVARCHAR(128) | e.g. SendOTP, VerifyOTP, SubmitAttendance |
| APIURL | NVARCHAR(512) | e.g. /api/auth/send-otp |
| HTTPMethod | NVARCHAR(16) | GET, POST, PUT, DELETE |
| ContentType | NVARCHAR(128) | application/json |
| RequestMaxCount | BIGINT | Rate limit max requests (default 100) |
| RequestTimeSpan | BIGINT | Rate limit window in seconds (default 3600) |
| + standard audit columns | | |

Seed: All Level 1 API endpoints (~40 methods).

### 4.11 tblAPILog (Script 077)
Async API request/response log. Fire-and-forget from every service method.

| Column | Type | Notes |
|---|---|---|
| APILogId | BIGINT IDENTITY | PK |
| Id | UNIQUEIDENTIFIER | GUID |
| APIMethodId | BIGINT | NULL (if unknown) |
| MethodName | NVARCHAR(256) | Service method name |
| RequestJSON | NVARCHAR(MAX) | Serialized request DTO |
| ResponseJSON | NVARCHAR(MAX) | Serialized response |
| Token | NVARCHAR(2048) | JWT token (masked in prod) |
| CreatedByUserId | BIGINT | NULL — user who made the call |
| IPAddress | NVARCHAR(64) | Caller IP |
| + reduced audit columns (no IsDeleted — never deleted) | | |

---

## 5. REQUIRED STORED PROCEDURES — FULL SPECIFICATION

### 5.1 uspGetMasterDataByCode (Script 085)
**Purpose:** UI-facing. Given a MasterDataCode (e.g. 'TaskType'), returns list of {Id (GUID), Name} pairs. UI displays these as dropdown options. NEVER returns INT primary keys.

**Parameters:**
```
@MasterDataCode  NVARCHAR(64)  -- e.g. 'Role', 'TaskType'
@LanguageId      INT = 1
@SearchWord      NVARCHAR(256) = NULL
@IsPublished     BIT = 1
```

**Returns:** `Id (GUID), MasterDataName, MasterDataCode, SortOrder`

**Logic:** `SELECT Id, MasterDataName, MasterDataCode, SortOrder FROM tblMasterData WHERE MasterDataCode = @MasterDataCode AND IsDeleted = 0 AND IsPublished = 1 ORDER BY SortOrder`

**Dependency:** tblMasterData

### 5.2 uspGetMasterDataByCodeInternal (Script 086)
**Purpose:** BAL-internal only. Given a MasterDataCode + GUID from UI, validates the GUID and returns the INT PK for use in save SPs. If GUID invalid → returns NULL → BAL sets error code.

**Parameters:**
```
@MasterDataCode  NVARCHAR(64)
@GuidValue       NVARCHAR(64)  -- GUID sent from UI
@LanguageId      INT = 1
```

**Returns:** `MasterDataId (INT)` — the internal PK. NULL if not found.

**Logic:** `SELECT MasterDataId FROM tblMasterData WHERE MasterDataCode = @MasterDataCode AND Id = @GuidValue AND IsDeleted = 0 AND IsPublished = 1`

**Dependency:** tblMasterData

### 5.3 uspGetMasterDataSpNameByCode (Script 087)
**Purpose:** Returns the custom SP name (`MasterCodeSpName`) for a given MasterDataCode. Used when a category has its own lookup SP (e.g. dynamic employee lists).

**Parameters:**
```
@MasterDataCode  NVARCHAR(64)
```

**Returns:** `MasterCodeSpName NVARCHAR(256)` — NULL if no custom SP configured.

**Dependency:** tblMasterData

### 5.4 uspGetErrorCodeById (Script 088)
**Purpose:** Given an error code INT, returns the user-facing message. Called in the `finally` block of every service method.

**Parameters:**
```
@ErrorCode   INT
@LanguageId  INT = 1
```

**Returns:** `ErrorCode, ReturnCode, ReturnMessage`

**Dependency:** tblErrorCode

### 5.5 uspCheckRolePermission (Script 089)
**Purpose:** Checks whether a given RoleId has a specific Permission on a given Module. Called in BAL before every write operation.

**Parameters:**
```
@RoleId       BIGINT
@ModuleId     BIGINT
@PermissionId BIGINT
```

**Returns:** `IsAuthorized BIT` — 1 if authorized, 0 if not.

**Logic:** `SELECT CASE WHEN EXISTS (...WHERE RoleId=@RoleId AND ModuleId=@ModuleId AND PermissionId=@PermissionId AND IsDeleted=0) THEN 1 ELSE 0 END`

**Dependency:** tblRolePermission

### 5.6 uspInsertAPILog (Script 090)
**Purpose:** Fire-and-forget async log insert. Called via `Task.Run(...)` from every service method.

**Parameters:**
```
@APIMethodId     BIGINT = 0
@MethodName      NVARCHAR(256) = NULL
@RequestJSON     NVARCHAR(MAX) = NULL
@ResponseJSON    NVARCHAR(MAX) = NULL
@Token           NVARCHAR(2048) = NULL
@CreatedByUserId BIGINT = 0
@IPAddress       NVARCHAR(64) = NULL
@CreatedBy       NVARCHAR(128) = NULL
```

**Returns:** Newly created `Id` (GUID)

**Dependency:** tblAPILog

---

## 6. SEED DATA PLAN

### 6.1 tblPermission Seed (Script 078) — 5 rows
| PermissionId | PermissionName | PermissionCode |
|---|---|---|
| 1 | View | V |
| 2 | Create/Update | CU |
| 3 | Delete | D |
| 4 | Approve/Reject | AR |
| 5 | Admin View | AV |

### 6.2 tblRole Seed (Script 079) — 6 rows
| RoleId | RoleName | RoleCode |
|---|---|---|
| 1 | SuperAdmin | SA |
| 2 | FamilyAdmin | FA |
| 3 | Parent | PA |
| 4 | Child | CH |
| 5 | Teacher | TE |
| 6 | Elder | EL |

### 6.3 tblModule Seed (Script 080) — 10 rows
| ModuleId | ModuleName | ModuleCode |
|---|---|---|
| 1 | Authentication | AUTH |
| 2 | Family Management | FAMILY |
| 3 | Family Dashboard | DASH |
| 4 | Attendance | ATTEND |
| 5 | Tasks | TASK |
| 6 | Feedback | FEEDBACK |
| 7 | Rewards | REWARDS |
| 8 | Calendar | CALENDAR |
| 9 | Notifications | NOTIF |
| 10 | Admin Configuration | ADMIN |

### 6.4 tblRolePermission Seed (Script 082) — Summary
| Role | Module Access | Permissions |
|---|---|---|
| SuperAdmin | ALL | AV (Admin View) + V |
| FamilyAdmin | All within FamilyId | V + CU + D + AR |
| Parent | FAMILY, DASH, ATTEND, TASK, FEEDBACK, REWARDS, CALENDAR, NOTIF | V + CU + AR |
| Child | TASK (self), REWARDS (self) | V |
| Teacher | ATTEND, FEEDBACK | CU (within time window — enforced in BAL) |
| Elder | DASH, CALENDAR | V |

### 6.5 tblMasterData Seed (Script 083) — FamilyFirst Categories
| MasterDataCode | Values |
|---|---|
| Role | SuperAdmin, FamilyAdmin, Parent, Child, Teacher, Elder |
| TaskType | Academic, Physical, Household, Creative, Social |
| TaskStatus | Pending, InProgress, Completed, Approved, Rejected |
| AttendanceStatus | Present, Absent, Late, HalfDay |
| RewardType | Digital, Physical, Experience |
| CoinTransactionType | Earn, Spend, Bonus, Deduct |
| FeedbackRating | Excellent, Good, Satisfactory, NeedsImprovement |
| CalendarEventType | Family, School, Holiday, Personal, Medical |
| NotificationType | Attendance, Task, Reward, Feedback, Calendar, System |
| OTPType | Login, SetPIN |

### 6.6 tblErrorCode Seed (Script 084) — 30 codes
| Code | Name | Message |
|---|---|---|
| 0 | Success | Success |
| 1 | Failure | Operation failed |
| 2 | Invalid_Token | Invalid or expired token |
| 3 | Token_Required | Authentication token is required |
| 4 | User_Not_Found | User not found |
| 5 | Invalid_User | Invalid user credentials |
| 6 | Session_Expired | Your session has expired. Please login again |
| 7 | Permission_Denied | You do not have permission to perform this action |
| 8 | Family_Not_Found | Family not found |
| 9 | Invalid_FamilyId | Invalid family identifier |
| 10 | Missing_Parameters | Required parameters are missing |
| 11 | Invalid_OTP | Invalid OTP code |
| 12 | OTP_Expired | OTP has expired. Please request a new one |
| 13 | OTP_Rate_Limit | OTP request limit reached. Try again in 1 hour |
| 14 | Invalid_PhoneNumber | Invalid phone number format |
| 15 | Attendance_Already_Submitted | Attendance already submitted for this session |
| 16 | Edit_Window_Closed | Attendance edit window has closed (1-hour limit) |
| 17 | Insufficient_Coins | Insufficient coins for this redemption |
| 18 | Reward_Already_Redeemed | This reward has already been redeemed |
| 19 | Task_Not_Found | Task not found |
| 20 | Photo_Required | A photo proof is required to complete this task |
| 21 | Feedback_Edit_Window_Closed | Feedback can only be edited within 24 hours |
| 22 | Technical_Error | A technical error occurred. Please try again |
| 23 | Invalid_MasterData | Invalid master data identifier |
| 24 | Invalid_Role | Invalid role identifier |
| 25 | Plan_Limit_Exceeded | Your plan limit has been reached |
| 26 | Invalid_GUID | Invalid identifier format |
| 27 | Invalid_Module | Invalid module identifier |
| 28 | Validation_Error | One or more validation errors occurred |
| 29 | Duplicate_Record | A record with this information already exists |
| 30 | Not_Found | The requested resource was not found |

---

## 7. REQUIRED API CHANGES / ADDITIONS

### 7.1 New API Endpoints Required (Phase A)

| Endpoint | Method | Purpose | Roles |
|---|---|---|---|
| `GET /api/masterdata/{code}` | GET | Get dropdown values by code | All authenticated |
| `GET /api/masterdata/{code}/{guid}` | GET (internal use) | Validate GUID — BAL only | Internal |

### 7.2 C# Infrastructure Changes Required

| Component | File | Action |
|---|---|---|
| `FamilyFirstEnums.cs` | `Domain/Enums/` | CREATE — MasterDataCodes, ErrorCode, Permissions, Module enums |
| `IMasterDataRepository` | `Application/Interfaces/` | CREATE — GetByCode, GetByCodeInternal, GetSpNameByCode |
| `IErrorCodeRepository` | `Application/Interfaces/` | CREATE — GetById |
| `IRolePermissionRepository` | `Application/Interfaces/` | CREATE — CheckPermission |
| `IApiLogRepository` | `Application/Interfaces/` | CREATE — InsertAsync (fire-and-forget) |
| `MasterDataRepository` | `Infrastructure/Repositories/` | CREATE — SP calls |
| `CacheWarmupService` | `Infrastructure/Services/` | CREATE — IHostedService, loads cache at startup |
| `DependencyInjection.cs` | `Infrastructure/` | UPDATE — register all new services |
| `MasterDataController` | `API/Controllers/` | CREATE — GET /api/masterdata/{code} |

### 7.3 Cache Warmup Service Keys
```csharp
// Keys loaded into IMemoryCache at startup
"MasterData:{Code}"          // e.g. "MasterData:TaskType" → List<MasterDataDto>
"ErrorCode:{code}:{langId}"  // e.g. "ErrorCode:7:1" → ErrorCodeDto
"RegexPattern:{methodId}:{field}" // e.g. "RegexPattern:1:PhoneNumber" → string pattern
"RolePermission:{roleId}:{moduleId}" // e.g. "RolePermission:3:4" → List<int> permissionIds
```

---

## 8. DEPLOYMENT SCRIPTS — EXECUTION ORDER

All scripts must be run in sequence. No dependency violations if run in order 067→090.

| Script # | File Name | Type | Depends On |
|---|---|---|---|
| 067 | `067_CreatePermission.sql` | Table | None |
| 068 | `068_CreateRole.sql` | Table | None |
| 069 | `069_CreateModule.sql` | Table | None |
| 070 | `070_CreateSubModule.sql` | Table | 069 |
| 071 | `071_CreateModulePermission.sql` | Table | 069, 067 |
| 072 | `072_CreateRolePermission.sql` | Table | 068, 069, 067 |
| 073 | `073_CreateMasterData.sql` | Table | 069 (optional FK) |
| 074 | `074_CreateErrorCode.sql` | Table | None |
| 075 | `075_CreateRegularExpression.sql` | Table | 076 (FK to APIMethod) — CREATE first |
| 076 | `076_CreateAPIMethod.sql` | Table | None |
| 077 | `077_CreateAPILog.sql` | Table | 076 (optional FK) |
| 078 | `078_SeedPermissions.sql` | Seed | 067 |
| 079 | `079_SeedRoles.sql` | Seed | 068 |
| 080 | `080_SeedModules.sql` | Seed | 069 |
| 081 | `081_SeedModulePermissions.sql` | Seed | 071, 078, 080 |
| 082 | `082_SeedRolePermissions.sql` | Seed | 072, 079, 080, 078 |
| 083 | `083_SeedMasterData.sql` | Seed | 073 |
| 084 | `084_SeedErrorCodes.sql` | Seed | 074 |
| 085 | `085_CreateSP_GetMasterDataByCode.sql` | SP | 073 |
| 086 | `086_CreateSP_GetMasterDataByCodeInternal.sql` | SP | 073 |
| 087 | `087_CreateSP_GetMasterDataSpNameByCode.sql` | SP | 073 |
| 088 | `088_CreateSP_GetErrorCodeById.sql` | SP | 074 |
| 089 | `089_CreateSP_CheckRolePermission.sql` | SP | 072 |
| 090 | `090_CreateSP_InsertAPILog.sql` | SP | 077 |

**Note on Script 075:** tblRegularExpression has FK to tblAPIMethod. Since 075 is created before 076, the FK is added as a deferred ALTER in script 076, or 075 omits the FK and 076 adds it. The scripts handle this safely with `IF NOT EXISTS` guards.

---

## 9. DEPENDENCY MAP

```
tblPermission (067)
    └─ tblModulePermission (071)
    └─ tblRolePermission (072)

tblRole (068)
    └─ tblRolePermission (072)

tblModule (069)
    └─ tblSubModule (070)
    └─ tblModulePermission (071)
    └─ tblRolePermission (072)
    └─ tblMasterData (073) [optional FK]

tblMasterData (073)
    └─ uspGetMasterDataByCode (085)
    └─ uspGetMasterDataByCodeInternal (086)
    └─ uspGetMasterDataSpNameByCode (087)

tblErrorCode (074)
    └─ uspGetErrorCodeById (088)

tblAPIMethod (076)
    └─ tblRegularExpression (075) [FK reverse — 076 adds the FK]
    └─ tblAPILog (077) [optional FK]
    └─ uspInsertAPILog (090)

tblRolePermission (072)
    └─ uspCheckRolePermission (089)

tblAPILog (077)
    └─ uspInsertAPILog (090)
```

---

## 10. DRIFT ANALYSIS

### 10.1 Known Differences from Reference System

| Concern | Reference | FamilyFirst Adaptation |
|---|---|---|
| PK type | `INT IDENTITY` in reference | `BIGINT IDENTITY` per FF SQL Format |
| GUID column type | `nvarchar(64)` in reference | `UNIQUEIDENTIFIER` per FF SQL Format |
| Audit column `CompanyId` | Maps to their SiteId hierarchy | FF keeps CompanyId INT DEFAULT(1) |
| SP naming | `usp_MasterData_GetMasterDataByCode` (underscores) | `uspGetMasterDataByCode` (PascalCase per FF format) |
| API log table | `tblAPILogDetail` (has PoolingIn, AccrualPoints) | `tblAPILog` (simplified, no POS-specific fields) |
| RegularExpression table | `tblregularexpresion` (typo, MISSING in ref DB) | `tblRegularExpression` (new, correctly named) |
| Module permission data | Based on POS roles | Based on FF 6 roles from CLAUDE.md |
| Multi-language support | Full language table | LanguageId = 1 (English only, Level 1) |

### 10.2 Typo Noted in Reference
The founder's notes reference `tblregularexpresion` (missing 's', missing 'i' in expression). This table does **not exist** in the reference DB (`RevalPOS_RevalERPlocalDB`). FamilyFirst creates it fresh as `tblRegularExpression` with correct spelling.

---

## 11. IMPLEMENTATION PRIORITY

### Phase A — Foundation (run before ANY new module)
Scripts: 067 → 090
All 24 scripts in sequence.

### Phase B — Retrofit Existing Controllers (after Phase A)
1. Update `AuthController` — use `uspGetErrorCodeById` in service `finally`, add `uspInsertAPILog`
2. Update `AttendanceController` — add `uspCheckRolePermission(roleId, ATTEND, CU)` before submit
3. Update `TasksController` — add permission check, GUID resolution for TaskType, TaskStatus
4. Update `FeedbackController`, `RewardsController`, `CalendarController` — same pattern

### Phase C — Generic Infrastructure (after Phase B is stable)
- Implement `tblStaticAPITemplate` and generic search endpoint
- Wire `GET /api/masterdata/{code}` to `uspGetMasterDataByCode`

---

*FamilyFirst — Flow_Change DB Gap Analysis — 2026-06-01*  
*Scripts: 067–090 in `API/FamilyFirst.Infrastructure/Data/Scripts/`*
