# FamilyFirst — Project Overview
Version: 2.0 | Status: Active | Last Updated: 2026-05-29

---

## 1. Project Architecture

### 1.1 Platform Stack

| Surface | Technology |
|---|---|
| Backend API | .NET 8 (C#) · ASP.NET Core Web API · Clean Architecture |
| Mobile App | Flutter — iOS + Android — single shared codebase |
| Web Admin Panel | Angular 17+ · Standalone Components |
| Database | SQL Server 2022 · Manual .sql scripts — no EF migrations |
| Authentication | JWT Bearer + Refresh Tokens · Phone OTP via MSG91 |
| Push Notifications | Firebase Cloud Messaging (FCM) — HTTP v1 credentials flow |
| Storage | AWS S3 — task photo verifications · Region: ap-south-1 |

---

### 1.2 Solution Structure

Backend root: `Backend/`

```
Backend/
  FamilyFirst.sln
  FamilyFirst.Domain/
    Entities/
      Base/                      ← BaseEntity.cs
    Enums/                       ← UserRole, AttendanceStatus, TaskStatus, TaskTimeBlock,
                                    FeedbackType, FeedbackSeverity, EventType,
                                    RedemptionStatus, NotificationChannel,
                                    NotificationPriority, SubscriptionPlan
  FamilyFirst.Application/
    Common/
      Models/                    ← ApiResponse.cs, PaginatedList.cs, Result.cs
      Exceptions/                ← ValidationException, NotFoundException,
                                    ForbiddenAccessException, ConflictException
    DTOs/
      Auth/
      Family/
      Task/
      Feedback/
      Reward/
      Calendar/
      Notification/
      Reports/
      Admin/
    Services/
      Interfaces/
      Implementations/
    Validators/                  ← FluentValidation validators, one file per module
  FamilyFirst.Infrastructure/
    Data/
      Configurations/            ← EF entity type configurations
      Repositories/
        Implementations/
      Scripts/                   ← 001_CreateUsers.sql → 040_SeedDefaultModuleVisibility.sql
      BackgroundServices/        ← ReminderDeliveryWorker, BirthdayEventGeneratorWorker,
                                    NotificationDeliveryWorker, WeeklyDigestWorker,
                                    MorningDigestWorker, EveningDigestWorker
    Services/                    ← JwtTokenService, OtpService,
                                    FcmPushNotificationService, S3StorageService
  FamilyFirst.API/
    Controllers/
      v1/                        ← All controllers — versioned from day one
    Middleware/                  ← ExceptionHandlingMiddleware, RequestLoggingMiddleware,
                                    RateLimitingMiddleware, MaintenanceModeMiddleware
    Filters/                     ← ValidationFilter, FamilyModuleVisibilityFilter
    appsettings.json
```

Flutter root: `Flutter/`

```
Flutter/
  pubspec.yaml
  android/                           ← Android app config, ProGuard rules, icons
  ios/                               ← iOS entitlements, app icons
  lib/
    core/
      config/                        ← app_config.dart, app_config_prod.dart
      theme/                         ← app_theme.dart, app_colors.dart, app_text_styles.dart
      router/                        ← app_router.dart (GoRouter), route_names.dart
      state/                         ← auth_notifier.dart, auth_state.dart
      models/                        ← user_model.dart, role_enum.dart
      network/                       ← api_client.dart, demo_interceptor.dart,
                                        auth_interceptor.dart, token_interceptor.dart,
                                        retry_interceptor.dart
      storage/                       ← secure_storage_service.dart
      mock/                          ← mock_data_service.dart (all module mock methods)
      connectivity/                  ← connectivity_service.dart, offline_banner_widget.dart
      cache/                         ← hive_cache_service.dart
      local/                         ← offline_queue_service.dart (sqflite attendance queue)
      master_api_reference.dart      ← all 103 Level 1 endpoints mapped
    features/
      auth/         screens/, widgets/, repositories/
      parent/       screens/, widgets/, providers/, repositories/
      family/       screens/, providers/, repositories/
      family_admin/ screens/
      teacher/      screens/, widgets/, providers/, repositories/
      tasks/        screens/, widgets/, providers/, repositories/
      child/        screens/, widgets/, providers/, repositories/
      elder/        screens/, widgets/, providers/
      calendar/     screens/, widgets/, providers/, repositories/
      notifications/screens/, widgets/, providers/, repositories/
      reports/      screens/, widgets/, providers/, repositories/
      admin/        screens/, providers/, repositories/
      settings/     screens/
    shared/
      widgets/                       ← FFButton, FFCard, FFAvatar, FFBadge,
                                        FFStatusPill, FFEmptyState, FFShimmerLoader,
                                        FFErrorState, AppNavShell
    l10n/                            ← app_en.arb (base), app_hi/ta/te/mr.arb (stubs)
```

**→ See Section 20 for full file-level detail per feature folder.**

Angular root: `Angular/`
[VERIFY] — Angular admin panel was not part of Level 1 backend phases (01–20).
No Angular implementation exists in the current codebase. Read the Angular spec
when Level 2+ admin panel work begins to populate this section.

Docs root: `API/Docs/Flow/` — ProjectOverview.md, ModuleIndex.md, Rule.txt,
                               New API Format.txt, New SQL Format.txt
Source docs: `API/Docs/Source/` — all `.docx` spec and dev-plan files

---

### 1.3 API Conventions

**Base URL:** `/api/v1/`

**Response Envelope — every response, no exceptions:**
```csharp
ApiResponse<T>
{
    bool       Success;
    T          Data;
    string     Message;
    ErrorDto[] Errors;
}
```

**Pagination — all list endpoints:**

| Direction | Fields |
|---|---|
| Request | `page`, `pageSize` |
| Response | `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage` |

C# type: `PaginatedList<T>`

**HTTP Status Code Map:**

| Status | When Used |
|---|---|
| 200 OK | Successful GET, PUT |
| 201 Created | Successful POST that creates a resource |
| 204 No Content | Successful DELETE with no body |
| 400 Bad Request | FluentValidation failure — returns `ErrorDto[]` |
| 401 Unauthorized | Missing or invalid JWT |
| 403 Forbidden | Valid JWT, insufficient role or family scope |
| 404 Not Found | Resource not found or soft-deleted |
| 409 Conflict | Duplicate submit, already-submitted session, duplicate pending redemption |
| 422 Unprocessable Entity | Business rule violation — insufficient coins, plan child limit, template category cap |
| 429 Too Many Requests | Rate limit triggered — OTP endpoint only |
| 500 Internal Server Error | Unhandled exception — generic message, no stack trace exposed |
| 503 Service Unavailable | `MaintenanceMode` feature flag enabled — non-admin traffic blocked |

**Validation:** FluentValidation on all request DTOs. Controllers never validate directly.
Applied globally via `ValidationFilter`. `ValidationException` carries an explicit HTTP status
code so 422 and 400 responses are distinguished at the middleware layer.

**Authentication:** JWT Bearer. Standard claims across all tokens:
`UserId`, `FamilyId`, `FamilyMemberId`, `PlanCode`, `Role`

Role-specific additional claims:
- Child tokens: `ChildProfileId`
- Teacher tokens: `TeacherProfileId`, `AssignedChildIds`

**Versioning:** All endpoints under `/api/v1/`. No unversioned endpoints.

---

### 1.4 Database Standards

**Engine:** SQL Server 2022. Raw `.sql` scripts only — no EF migrations, no `SELECT *`.

**Naming Conventions:**

| Object | Rule | Example |
|---|---|---|
| Table | PascalCase singular, no prefix | `Users`, `AttendanceSessions`, `TaskCompletions` |
| Column | PascalCase | `FamilyId`, `CreatedAt`, `IsDeleted` |
| Primary Key | `Id` — UNIQUEIDENTIFIER DEFAULT NEWID() | `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()` |
| Foreign Key column | `<Entity>Id` | `FamilyId`, `ChildProfileId` |
| Script file | `NNN_Action.sql` (3-digit zero-padded) | `001_CreateUsers.sql` → `040_SeedDefaultModuleVisibility.sql` |
| Index | `IX_<Table>_<Col1>[_<Col2>]` | `IX_AttendanceSessions_FamilyId_SessionDate` |

**Data Types:**

| Use Case | Type |
|---|---|
| All text | `NVARCHAR(n)` |
| Boolean | `BIT` |
| Date / Time | `DATETIME2` — always UTC |
| PKs and FKs | `UNIQUEIDENTIFIER` |
| Plans table (exception) | `INT IDENTITY` |
| Money / Currency | `DECIMAL(18,2)` |
| Enum storage | `INT` |

**Mandatory Audit Columns — every business table:**
```sql
CreatedAt    DATETIME2    NOT NULL  DEFAULT GETUTCDATE()
UpdatedAt    DATETIME2    NOT NULL  DEFAULT GETUTCDATE()
IsDeleted    BIT          NOT NULL  DEFAULT 0
DeletedAt    DATETIME2    NULL
```

**BaseEntity (C# base class):**
`Id (GUID)`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt` — enforced at domain base class.
All entities derive from BaseEntity. No entity may omit these fields.

**Soft Delete:** `IsDeleted = 1, DeletedAt = GETUTCDATE()`. Hard delete requires explicit
approval only. `WHERE IsDeleted = 0` enforced in every repository query.

**Row-Level Security:** All repositories filter `WHERE FamilyId = @currentFamilyId`
AND `WHERE IsDeleted = 0`. No cross-family data access permitted at the repository layer.

**Optimistic Concurrency:** `RowVersion` column on `ChildProfiles` table —
added in `023_AlterChildProfiles_RowVersion.sql` (Phase 10). Required for all coin balance
mutations to prevent race conditions. `DbUpdateConcurrencyException` translated to a 409
conflict response at the service layer.

**Script Execution Rules:**
- Scripts execute in order 001 → 040. FK dependencies resolvable by sequential execution.
- All scripts use `IF NOT EXISTS` guards — idempotent where possible.
- All timestamps use `GETUTCDATE()` — never `GETDATE()`.
- Scripts are not run by the application. Manual execution only.

---

### 1.5 Flutter App Architecture

**Source confirmed:** `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` (read 2026-05-29)
**→ Full detail in Section 20. This subsection is a summary only.**

Single Flutter app — iOS + Android — all 6 roles inside one binary.
20 phases · 42 Level 1 screens · 103 API endpoints · Demo + Live mode.

**State management:** Riverpod (`flutter_riverpod`)
- `AuthNotifier` (`lib/core/state/auth_notifier.dart`) — global; holds Role, UserId, FamilyId,
  FamilyMemberId, ChildProfileId, PlanCode, isAuthenticated
- Feature-level `StateNotifier` per module (16 confirmed providers — see Section 20.2)
- No `setState` for API data. `const` constructors wherever possible.

**Navigation:** GoRouter (`lib/core/router/app_router.dart`)
- All 42 routes declared at Phase 01. Named constants in `route_names.dart`.
- Role redirects: `null` → `/splash` · SuperAdmin → `/admin/dashboard` ·
  Parent → `/parent/home` · Teacher → `/teacher/home` · Child → `/child/home` ·
  Elder → `/elder/home`
- No `MaterialPageRoute.push` anywhere in the codebase.

**HTTP client:** Dio singleton (`lib/core/network/api_client.dart`)
- Interceptor stack: `DemoInterceptor` → `TokenInterceptor` → `RetryInterceptor`
- `TokenInterceptor`: adds Bearer token; auto-refreshes on 401; navigates to login on
  refresh failure. User never sees a raw 401.
- `RetryInterceptor` (Phase 19): 3 retries with 1 s / 2 s / 4 s exponential backoff.
- `SecureStorageService` (`flutter_secure_storage`): stores `accessToken`, `refreshToken`.

**Demo mode:** `AppConfig.isDemo` is a **`const` bool** — rebuild required to change.
- Demo login: 6 role cards on launch. OTP always `123456`. PIN always `1234`.
- `DemoInterceptor` intercepts all Dio calls — no network requests in demo mode.
- Every repository has two implementations: Demo (reads `MockDataService`) and Live (Dio).
- No blank screens in demo mode — `MockDataService` returns meaningful data for all modules.

**Key packages confirmed:**
`flutter_riverpod` · `go_router` · `dio` · `flutter_secure_storage` · `firebase_messaging` ·
`fl_chart` · `table_calendar` · `hive_flutter` · `sqflite` · `connectivity_plus` ·
`cached_network_image` · `image_picker` · `flutter_image_compress`

**flutter analyze must return 0 errors, 0 warnings after every phase.**
Never modify files from a previous phase — only ADD new methods.

---

### 1.6 Angular Admin Architecture

**[VERIFY] — Not yet built.** Angular admin panel was not part of Level 1 backend
phases 01–20. No Angular source files exist in the current codebase.

**Standards defined in CLAUDE.md (to be applied when Angular work begins):**
- Angular 17+ · Standalone Components · Lazy loading · Route guards

Read the Angular spec / dev plan when admin panel work begins to populate this section
with confirmed folder structure, module map, and guard configuration.

---

### 1.7 External Services & Configuration

**JWT (Phase 02 — confirmed implemented):**
- Access token expiry: 60 minutes
- Refresh token expiry: 30 days
- Refresh tokens stored as SHA-256 hash in `RefreshTokens` table — plaintext never stored
- Token rotation: new refresh token issued on every refresh call; old token revoked
- Revocation: explicit `POST /api/v1/auth/revoke-token` endpoint

**OTP via MSG91 (Phase 02 — confirmed implemented):**
- Provider: MSG91 — HTTP API delivery
- OTP TTL: 5 minutes from generation
- Rate limit: 3 OTP requests per phone number per hour — enforced in `RateLimitingMiddleware`
  on `POST /api/v1/auth/send-otp`
- Development fallback: when MSG91 config values are unset, OTP is logged server-side
  instead of dispatching external SMS

**FCM — Push Notifications (Phase 02 initial → Phase 16 updated):**
- Provider: Firebase Cloud Messaging
- Protocol: HTTP v1 credentials flow (updated from legacy server-key in Phase 16)
- Deep-link data payload added for calendar reminder pushes (Phase 16)
- Service class: `FcmPushNotificationService`
- Retry on failed sends: up to 3 attempts with 1-minute backoff (in `ReminderDeliveryWorker`)

**AWS S3 — File Storage (Phase 09 — confirmed implemented):**
- Use: Task completion photo verification uploads
- Presigned upload URL TTL: 15 minutes
- S3 key format: `family/{familyId}/tasks/{taskId}/{GUID}.jpg`
- SDK: `AWSSDK.S3`
- Region: ap-south-1
- Bucket name and region resolved from `appsettings.json` — not hardcoded

**appsettings.json configuration sections (Phase 01 scaffold — confirmed):**

| Section | Contents |
|---|---|
| `ConnectionStrings` | SQL Server connection string |
| `Jwt` | Signing key, issuer, audience, access token expiry, refresh token expiry |
| `Otp` | MSG91 API key, sender ID, template ID |
| `Fcm` | Firebase project ID, credentials (HTTP v1) |
| `Aws` | S3 bucket name, region, access key ID, secret access key |
| `App` | App-level config — maintenance mode, minimum app version, etc. |

---

## 2. Authentication & Session

### 2.1 Module Purpose

Handles all authentication entry points for the FamilyFirst platform. Covers:
- Phone-number OTP login for Parent, Teacher, FamilyAdmin, and SuperAdmin roles
- PIN-based login for Child and Elder roles (no OTP required)
- JWT access token issuance (60-min expiry)
- Refresh token lifecycle (30-day expiry, rotation on every use)
- Session revocation
- Current-user identity endpoint

Implemented in Phase 02 (Backend). Controller: `AuthController` at `/api/v1/auth/`.
Rate limiting and token rotation are enforced at the middleware layer, not at the service layer.

---

### 2.2 Key APIs

---

#### POST /api/v1/auth/send-otp

| Field | Value |
|---|---|
| Auth required | NO |
| Rate limit | 3 requests per phone number per hour — enforced in `RateLimitingMiddleware` |

**Request DTO — `SendOtpRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `PhoneNumber` | `string` | YES | Regex: `^\+[1-9]\d{7,14}$`. India (+91): 10 digits after country code. |
| `CountryCode` | `string` | YES | Default `+91`. Also accepts: `+971`, `+966`, `+1`, `+44`. |

**Response DTO — `ApiResponse<SendOtpResponse>`:**

| Field | Type | Notes |
|---|---|---|
| `OtpToken` | `string` | Reference token to include in the verify-otp call. Not the OTP code itself. |

**Business rules:**
- `PhoneNumber` must pass E.164 format validation (FluentValidation).
- Rate limit checked before OTP generation. Exceeding limit returns 429.
- OTP stored in-memory only (no DB table). TTL: 5 minutes.
- MSG91 HTTP API dispatches the SMS. When MSG91 config values are placeholder/absent, OTP is logged server-side instead.

**Error cases:**

| Condition | Status |
|---|---|
| Invalid phone format | 400 |
| Rate limit exceeded (>3/hr/phone) | 429 |

---

#### POST /api/v1/auth/verify-otp

| Field | Value |
|---|---|
| Auth required | NO |
| Rate limit | None |

**Request DTO — `VerifyOtpRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `PhoneNumber` | `string` | YES | E.164 format |
| `OtpToken` | `string` | YES | The `OtpToken` value returned by `send-otp` |
| `OtpCode` | `string` | YES | Exactly 6 numeric digits. Max age 5 minutes (server-side Redis TTL check). |

**Response DTO — `ApiResponse<AuthResponse>`:**

| Field | Type | Notes |
|---|---|---|
| `AccessToken` | `string` | JWT — expires in 60 minutes |
| `RefreshToken` | `string` | Plaintext token — 30-day expiry. Stored as hash in DB. |
| `UserDto` | `UserDto` | User profile — see below |
| `Role` | `string` | Role name string (e.g. `"Parent"`) — mirrors the JWT `role` claim |

**`UserDto` confirmed fields (from `FamilyFirst.Application/DTOs/Auth/AuthResponse.cs`):**

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | — |
| `PhoneNumber` | `string` | — |
| `CountryCode` | `string` | e.g. `+91` |
| `FullName` | `string` | — |
| `Email` | `string?` | Nullable |
| `ProfilePhotoUrl` | `string?` | S3 URL |
| `IsPhoneVerified` | `bool` | — |
| `IsActive` | `bool` | — |
| `PreferredLanguage` | `string` | e.g. `en`, `hi` |
| `Role` | `string` | Role name — e.g. `"Parent"`, `"Child"` |

**Business rules:**
- OTP must match the in-memory record for that phone number.
- OTP expired after 5 minutes → 400.
- If no `Users` row exists for this phone number, one is created automatically (first login).
- JWT claims populated: `UserId`, `Role`. `FamilyId`, `FamilyMemberId`, `PlanCode` added only when a `FamilyMembers` row exists (Phase 03 onwards).

**Error cases:**

| Condition | Status |
|---|---|
| OTP does not match or expired (>5 min) | 401 — `UnauthorizedAccessException("OTP is invalid or expired.")` → ExceptionHandlingMiddleware |

---

#### POST /api/v1/auth/refresh-token

| Field | Value |
|---|---|
| Auth required | NO (refresh token in body) |
| Rate limit | None |

**Request DTO — `RefreshTokenRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `RefreshToken` | `string` | YES | Plaintext refresh token received from prior auth call |

**Response DTO — `ApiResponse<AuthResponse>`:** Same shape as verify-otp response.

**Business rules:**
- Token looked up by SHA-256 hash in `RefreshTokens` table.
- If token is expired (`ExpiresAt < UTC now`) or revoked (`RevokedAt IS NOT NULL`) → 401.
- Rotation: old `RefreshTokens` row is marked `RevokedAt = GETUTCDATE()`. New row inserted with new hash.
- New JWT issued with refreshed claims (re-reads FamilyMember/ChildProfile/TeacherProfile state at refresh time).

**Error cases:**

| Condition | Status |
|---|---|
| Token not found in DB | 401 |
| Token expired | 401 |
| Token already revoked | 401 |

---

#### POST /api/v1/auth/revoke-token

| Field | Value |
|---|---|
| Auth required | YES — valid JWT |
| Rate limit | None |

**Request DTO — `RevokeTokenRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `RefreshToken` | `string` | YES | Plaintext refresh token to revoke |

**Response DTO:** `ApiResponse<bool>` — returns `true` on success.

**Business rules:**
- No ownership check: any authenticated user with a valid JWT can call this endpoint with any token hash.
  The service does not validate that the JWT `UserId` matches the token's `UserId`.
- If token found: sets `IsRevoked = true` on the matching `RefreshTokens` row.
- If token not found: returns `true` — idempotent (200 OK), not a 404 error.
- Revoking an already-revoked token is also idempotent (200 OK).

**Error cases:**

| Condition | Status |
|---|---|
| Missing / invalid JWT | 401 |

---

#### POST /api/v1/auth/set-pin

| Field | Value |
|---|---|
| Auth required | YES — valid JWT |
| Rate limit | None |

**Request DTO — `SetPinRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Pin` | `string` | YES | 4-digit numeric |

**Response DTO:** `ApiResponse<bool>` — returns `true` on success.

**Business rules:**
- `Pin` must be exactly 4 numeric digits (FluentValidation).
- Cannot be a repeated-digit pattern: `0000`, `1111`, `2222`, … `9999` → 400.
- Stored as PBKDF2/SHA256 hash in `Users.PinHash`. Format: `v1.{base64(16-byte-salt)}.{base64(32-byte-hash)}`.
  100,000 iterations. Spec specified bcrypt — Phase 02 implemented PBKDF2. See Drift Entry 011 (resolved).
- Used by Child and Elder roles. No role restriction at the API layer — any authenticated user can set a PIN.
- Overwrites existing PIN if one was already set.

**Error cases:**

| Condition | Status |
|---|---|
| Invalid PIN format (not 4-digit numeric) | 400 |
| Missing / invalid JWT | 401 |

---

#### POST /api/v1/auth/verify-pin

| Field | Value |
|---|---|
| Auth required | NO |
| Rate limit | None |

**Request DTO — `VerifyPinRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `UserId` | `Guid` | YES | The child's UserId — resolved from join code + name picker flow |
| `Pin` | `string` | YES | 4-digit numeric |

**Response DTO — `ApiResponse<AuthResponse>`:** Same shape as verify-otp response.

**Business rules:**
- User looked up by `UserId`. If not found → 400.
- PIN not yet set (`Users.PinHash IS NULL`) → 400.
- PBKDF2/SHA256 comparison using `CryptographicOperations.FixedTimeEquals` (timing-attack safe).
  See Drift Entry 011 (resolved: PBKDF2 confirmed in Phase 02 implementation).
- JWT and refresh token issued on success, same as OTP verify flow.
- **Child login flow:** Client first uses join code → `GET /families/children` to get name list →
  child picks name → client gets `UserId` → then calls `POST /auth/verify-pin { UserId, Pin }`.

**Error cases:**

| Condition | Status |
|---|---|
| UserId not found, PIN not set, or wrong PIN | 401 — all throw `UnauthorizedAccessException("PIN is invalid.")`. Error message is deliberately vague to prevent user enumeration. |

---

#### GET /api/v1/auth/me

| Field | Value |
|---|---|
| Auth required | YES — valid JWT |
| Rate limit | None |

**Request:** No body.

**Response DTO — `ApiResponse<CurrentUserDto>`** (mirrors JWT claims):

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | JWT `sub` claim |
| `Name` | `string?` | JWT `name` claim (reads from DB then falls back to claim) |
| `PhoneNumber` | `string?` | JWT `phone` claim (reads from DB then falls back to claim) |
| `Role` | `string` | JWT `role` claim — string enum name (e.g. `"Parent"`, `"Child"`) |
| `FamilyId` | `Guid?` | JWT `familyId` claim — null until family created/joined |
| `FamilyMemberId` | `Guid?` | JWT `familyMemberId` claim |
| `PlanCode` | `string?` | JWT `planCode` claim (e.g. `"family"`, `"premium"`) |
| `ChildProfileId` | `Guid?` | Child tokens only |
| `TeacherProfileId` | `Guid?` | Teacher tokens only |
| `AssignedChildIds` | `Guid[]?` | Teacher tokens only |

**Business rules:**
- Reads the `Users` row by `UserId` claim to populate `Name` and `PhoneNumber` from DB.
  Falls back to JWT claims if DB read returns null (e.g. token issued before row existed).
- Role, FamilyId, FamilyMemberId, PlanCode, ChildProfileId, TeacherProfileId, AssignedChildIds
  are read directly from JWT claims — no additional DB reads for those fields.
- Returns 401 if JWT is missing or expired.

**Error cases:**

| Condition | Status |
|---|---|
| Missing / invalid / expired JWT | 401 |

---

### 2.3 DB Tables

#### Users
- **Scripts:** `001_CreateUsers.sql` (Phase 01) · `009_AlterUsers_AddIndexes.sql` (Phase 02)
- **Note:** Actual DB column is `UserId` — matching the TechSpec. BaseEntity `Id` naming convention
  is not applied to this table. The C# entity maps `Id` → `UserId` column via EF configuration.
  See Drift Entry 010 (resolved: DB uses `UserId`).

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `UserId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() | C# entity property: `Id` |
| `PhoneNumber` | `NVARCHAR(20)` | NOT NULL | E.164 format e.g. `+91XXXXXXXXXX` |
| `CountryCode` | `NVARCHAR(5)` | NOT NULL, DEFAULT `+91` | — |
| `FullName` | `NVARCHAR(200)` | NOT NULL | — |
| `Email` | `NVARCHAR(300)` | NULL | Optional at registration |
| `ProfilePhotoUrl` | `NVARCHAR(500)` | NULL | S3 URL |
| `PinHash` | `NVARCHAR(500)` | NULL | PBKDF2/SHA256. Format: `v1.{base64(salt)}.{base64(hash)}` |
| `PasswordHash` | `NVARCHAR(500)` | NULL | Admin/Teacher accounts |
| `FcmToken` | `NVARCHAR(500)` | NULL | Updated on each login via `PUT /users/{id}/fcm-token` |
| `IsPhoneVerified` | `BIT` | NOT NULL, DEFAULT 0 | — |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 | Set to 0 when family blocked (Phase 19) |
| `PreferredLanguage` | `NVARCHAR(10)` | NOT NULL, DEFAULT `en` | Values: `en`, `hi`, `ta`, `te`, `mr` |
| `LastLoginAt` | `DATETIME2` | NULL | — |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() | — |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() | — |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 | — |
| `DeletedAt` | `DATETIME2` | NULL | — |

**Indexes (confirmed from SQL scripts):**
- `UX_Users_PhoneNumber` — unique, filtered: `WHERE IsDeleted = 0` — from `001_CreateUsers.sql`
- `UX_Users_Email` — unique, filtered: `WHERE Email IS NOT NULL AND IsDeleted = 0` — from `001_CreateUsers.sql`
- `IX_Users_PhoneNumber_OtpLookup` — non-unique, `INCLUDE (UserId, IsPhoneVerified, IsActive)`, filtered: `WHERE IsDeleted = 0` — from `009_AlterUsers_AddIndexes.sql`

#### RefreshTokens
- **Script:** `002_CreateRefreshTokens.sql` (Phase 01)
- **Note:** Column names match the TechSpec: `TokenId` (PK) and `Token` (hash column).
  No `UpdatedAt`, `IsDeleted`, or `DeletedAt` — uses `IsRevoked BIT` instead of soft delete.
  No `RevokedAt` timestamp column exists. FK references `dbo.Users (UserId)`.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `TokenId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() | — |
| `UserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId | — |
| `Token` | `NVARCHAR(500)` | NOT NULL | SHA-256 hash of plaintext refresh token |
| `DeviceInfo` | `NVARCHAR(500)` | NULL | OS and device model — sent by client |
| `ExpiresAt` | `DATETIME2` | NOT NULL | SYSUTCDATETIME() + 30 days |
| `IsRevoked` | `BIT` | NOT NULL, DEFAULT 0 | Set to 1 on revoke or rotation. No `RevokedAt` column. |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() | — |

**Indexes (confirmed from SQL script):**
- `UX_RefreshTokens_Token` — unique — from `002_CreateRefreshTokens.sql`
- `IX_RefreshTokens_UserId` — non-unique — from `002_CreateRefreshTokens.sql`

#### OTP Storage
- **Storage:** TechSpec specifies **Redis** (TTL 5 minutes). Phase 02 implementation uses
  **in-memory** (`OtpService` in-process) due to no Redis connection in the build environment.
  See Drift Entry 009. Confirm which is active in production.
- TTL: 5 minutes in both cases.
- MSG91 reference IDs not stored in DB.

---

### 2.4 Business Rules

1. **OTP rate limit:** Max 3 OTP requests per phone number per rolling hour window.
   Enforced in `RateLimitingMiddleware` on `POST /api/v1/auth/send-otp`.
   Exceeded limit → `429 Too Many Requests`.

2. **OTP expiry:** 5 minutes from generation (in-memory TTL).
   Expired OTP submitted to `POST /api/v1/auth/verify-otp` → `400 Bad Request`.

3. **OTP storage:** In-memory only. No DB table. Not persisted across process restarts.
   Development fallback: when `appsettings.json Otp` section has unset/placeholder values,
   OTP is written to server log instead of SMS dispatch.

4. **Auto user creation:** A new `Users` row is created on the first successful `POST /auth/verify-otp`
   for a phone number that has no existing record. No explicit registration endpoint.

5. **JWT access token expiry:** 60 minutes from issuance.

6. **Refresh token expiry:** 30 days from issuance.

7. **Refresh token storage:** SHA-256 hash stored in `RefreshTokens`. Plaintext token returned to client only at issuance — never stored.

8. **Refresh token rotation:** Every `POST /auth/refresh-token` call revokes the submitted token
   and issues a new one. Re-using a revoked token → `401 Unauthorized`.

9. **PIN format:** Exactly 4 numeric digits. Validated by FluentValidation in `PinRequestValidators`.

10. **PIN storage:** PBKDF2/SHA256 hash in `Users.PinHash`. Format: `v1.{base64(16-byte-salt)}.{base64(32-byte-hash)}`.
    100,000 iterations. Comparison uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.
    Plaintext never stored.

11. **Child PIN auth:** Child role uses `POST /auth/verify-pin` — no OTP required.
    PIN must be set first via `POST /auth/set-pin` (requires a valid JWT from the parent or the child account).

12. **Elder PIN auth:** Same as Child — `POST /auth/verify-pin`. No OTP.

13. **JWT standard claims:** `UserId`, `FamilyId`, `FamilyMemberId`, `PlanCode`, `Role`.
    `FamilyId`, `FamilyMemberId`, `PlanCode` are populated only when a `FamilyMembers` row exists
    (after Phase 03 family creation). Absent until then.
    **Role fallback when no FamilyMembers row exists:**
    - `POST /auth/verify-otp` → `Role = "Parent"` in JWT
    - `POST /auth/verify-pin` → `Role = "Child"` in JWT
    Actual role is always resolved from the active `FamilyMembers` row when one exists.

14. **JWT Child claims:** `ChildProfileId` — added to Child tokens after Phase 04 creates `ChildProfiles`.

15. **JWT Teacher claims:** `TeacherProfileId`, `AssignedChildIds` — added after Phase 04.

16. **Maintenance mode bypass:** `POST /auth/*` routes bypass `MaintenanceModeMiddleware`
    so SuperAdmin can sign in while maintenance mode is active.

---

### 2.5 Flow Summaries

#### Flow 1 — OTP Login (Phone)

```
Trigger       : User enters phone number on login screen
→ API call    : POST /api/v1/auth/send-otp { PhoneNumber }
→ Validation  : E.164 format check (FluentValidation);
                Rate limit check (3/hr/phone via RateLimitingMiddleware) → 429 if exceeded
→ DB operation: None. OTP generated and stored in-memory (TTL 5 min).
→ Response    : 200 ApiResponse<SendOtpResponse>
→ Side effect : MSG91 HTTP API dispatches OTP SMS to PhoneNumber.
                Dev fallback: OTP logged server-side when config is absent.
```

#### Flow 2 — OTP Verification (Login / Register)

```
Trigger       : User submits the OTP received via SMS
→ API call    : POST /api/v1/auth/verify-otp { PhoneNumber, Otp }
→ Validation  : In-memory OTP match for PhoneNumber; expiry check (5 min) → 400 if failed
→ DB operation: If new user → INSERT into Users (PhoneNumber, CreatedAt, IsActive=1).
                INSERT into RefreshTokens (UserId, TokenHash=SHA256(newToken), ExpiresAt=+30d).
→ Response    : 200 ApiResponse<AuthResponse> { AccessToken, RefreshToken, ExpiresIn, User }
→ Side effect : JWT (60 min) and refresh token (30 days) issued to client.
```

#### Flow 3 — Token Refresh

```
Trigger       : Client receives 401 on an authenticated call (or proactive refresh before expiry)
→ API call    : POST /api/v1/auth/refresh-token { RefreshToken }
→ Validation  : SHA-256 hash lookup in RefreshTokens.Token;
                ExpiresAt check → 401 if expired;
                IsRevoked = 1 check → 401 if already revoked
→ DB operation: UPDATE RefreshTokens SET IsRevoked=1 WHERE TokenId=<old>;
                INSERT new RefreshTokens row (new Token=SHA256(newToken), ExpiresAt=SYSUTCDATETIME()+30d).
                UPDATE Users SET LastLoginAt=GETUTCDATE() WHERE UserId=<userId>.
→ Response    : 200 ApiResponse<AuthResponse> { AccessToken, RefreshToken, Role, User }
→ Side effect : Old token invalidated. New JWT (60 min) and new refresh token issued.
```

#### Flow 4 — Session Revocation (Logout)

```
Trigger       : User taps logout
→ API call    : POST /api/v1/auth/revoke-token { RefreshToken } — JWT required
→ Validation  : No ownership check — any valid JWT can call this endpoint.
                Token not found → returns true (200), not an error.
→ DB operation: If found: UPDATE RefreshTokens SET IsRevoked=1 WHERE Token=SHA256(token).
→ Response    : 200 ApiResponse<bool> { Data: true }
→ Side effect : Refresh token marked revoked. Client must discard both tokens locally.
```

#### Flow 5 — PIN Set

```
Trigger       : Parent/FamilyAdmin sets PIN for Child, or any user sets their own PIN
→ API call    : POST /api/v1/auth/set-pin { Pin } — JWT required
→ Validation  : Pin must be exactly 4 numeric digits → 400 if not
→ DB operation: UPDATE Users SET PinHash=PBKDF2(pin), UpdatedAt=GETUTCDATE() WHERE Id=<userId>.
→ Response    : 200 ApiResponse<>
→ Side effect : None.
```

#### Flow 6 — PIN Login

```
Trigger       : Child or Elder enters PIN on login screen
→ API call    : POST /api/v1/auth/verify-pin { UserId, Pin }
→ Validation  : Users lookup by UserId → 401 if not found (error message: "PIN is invalid.");
                Users.PinHash IS NULL or PBKDF2 mismatch → 401 (same message — no enumeration)
→ DB operation: UPDATE Users SET LastLoginAt=GETUTCDATE() WHERE UserId=<userId>.
                INSERT into RefreshTokens (UserId, Token=SHA256(newToken), ExpiresAt=+30d).
→ Response    : 200 ApiResponse<AuthResponse> { AccessToken, RefreshToken, Role="Child", User }
→ Side effect : JWT (60 min) and refresh token (30 days) issued. Role fallback = "Child" if
                no FamilyMembers row exists.
```

#### Flow 7 — Get Current User

```
Trigger       : App boot / profile screen load
→ API call    : GET /api/v1/auth/me — JWT required
→ Validation  : JWT signature and expiry check → 401 if invalid
→ DB operation: READ Users WHERE UserId = <JWT sub claim> — to get fresh Name and PhoneNumber.
                Falls back to JWT claims if DB row not found.
                Role, FamilyId, FamilyMemberId, PlanCode, ChildProfileId, TeacherProfileId,
                AssignedChildIds are read from JWT claims — no extra DB reads for those.
→ Response    : 200 ApiResponse<CurrentUserDto> { UserId, Name, PhoneNumber, Role, FamilyId,
                FamilyMemberId, PlanCode, ChildProfileId, TeacherProfileId, AssignedChildIds }
→ Side effect : None.
```

---

### 2.6 Flutter Integration

**Status: Flutter app not yet built.** No `Flutter/` directory exists in the repository.
Screen names, route constants, and MockDataService methods below are from the DevPlan spec
(`FamilyFirst_Flutter_AI_Studio_DevPlan.docx`) — not confirmed implemented.

**Planned auth screens (from DevPlan — [VERIFY] against implementation when built):**

| Screen | File path (planned) | Route |
|---|---|---|
| Phone entry | `lib/features/auth/screens/phone_entry_screen.dart` | `/auth/phone` |
| OTP verification | `lib/features/auth/screens/otp_verify_screen.dart` | `/auth/otp` |
| PIN entry (Child/Elder) | `lib/features/auth/screens/pin_login_screen.dart` | `/auth/pin` |
| PIN set | `lib/features/auth/screens/set_pin_screen.dart` | `/auth/set-pin` |
| Role select (demo) | `lib/features/auth/screens/role_select_screen.dart` | `/auth/role-select` |

**Planned MockDataService methods (demo mode — [VERIFY] against implementation):**
- `mockSendOtp(phone)` → returns `SendOtpResponse` with dummy `OtpToken`
- `mockVerifyOtp(phone, otpToken, otpCode)` → returns `AuthResponse` for selected role
- `mockVerifyPin(userId, pin)` → returns `AuthResponse` for Child/Elder role
- Demo OTP always `123456`. Demo PIN always `1234`.

**AuthNotifier (planned — [VERIFY] against implementation):**
- File: `lib/core/state/auth_notifier.dart`
- Provider: global Riverpod `StateNotifier<AuthState>`
- State shape: `AuthState { isAuthenticated, userId, familyId, familyMemberId, role, planCode,
  childProfileId, teacherProfileId, assignedChildIds, accessToken, refreshToken }`
- Populated from JWT claims on login; cleared on logout.

**Confirmed integration constraints (from CLAUDE.md standards):**
- `AuthNotifier` is global (Riverpod). Auth state available across all features.
- GoRouter redirects based on `AuthState.role` to role-specific home screens.
- `TokenInterceptor` (Dio) handles 401 → auto-refresh → retry. User never sees a raw 401.
- Demo mode: `AppConfig.isDemo = true` (const bool) → `DemoInterceptor` blocks all Dio calls;
  `MockDataService` returns pre-set auth data.
- Folder: `lib/features/auth/screens/`, `/repositories/`

---

### 2.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| MSG91 SMS gateway | `Otp` config section in `appsettings.json` (API key, sender ID, template ID) | OTP SMS dispatch for phone login |
| JWT signing infrastructure | `Jwt` config section (signing key, issuer, audience, expiry values) | Access token and refresh token generation |
| `FamilyMembers` table (Phase 03) | Active membership row for current user | `FamilyId`, `FamilyMemberId`, `PlanCode`, `Role` JWT claims — absent until Phase 03 |
| `ChildProfiles` table (Phase 04) | Active child profile for current user | `ChildProfileId` JWT claim — absent until Phase 04 |
| `TeacherProfiles` + `TeacherChildAssignment` (Phase 04) | Active teacher profile and assignments | `TeacherProfileId`, `AssignedChildIds` JWT claims — absent until Phase 04 |
| `FeatureFlags` table (Phase 19) | `MaintenanceMode` flag | `/auth/*` routes must bypass maintenance mode check |

**No cross-module runtime dependencies at auth time.** The auth endpoints themselves do not call
attendance, task, feedback, or reward services. Family/profile context is read at token
generation only (not at every auth call).

---

## 3. Family & User Management

### 3.1 Module Purpose

Covers all family lifecycle operations — creation, membership management, child and teacher
profiles, and user account updates. Implemented across Phase 03 (FamiliesController,
UsersController) and Phase 04 (ChildrenController).

Responsibilities:
- Family creation with automatic FreeTrial subscription and FamilyAdmin membership
- Join-code based family invitation system
- Member add / update / remove with role-level enforcement
- User profile read and update (display name, FCM token)
- Child profile management (auto-created on member add to Child role)
- Teacher profile and assignment management (auto-created on member add to Teacher role)
- Plan-based child-count limits enforced at member-add time

Family Dashboard (`GET /families/{familyId}/dashboard`) is documented in **Section 4**.

---

### 3.2 Key APIs

---

#### POST /api/v1/families

| Field | Value |
|---|---|
| Auth required | YES — valid JWT |
| Role gate | Any authenticated user (creates their own family; caller becomes FamilyAdmin) |

**Request DTO — `CreateFamilyRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `FamilyName` | `string` | YES | Min 2, max 200 chars. Regex: letters/digits/spaces/apostrophes/hyphens. |
| `City` | `string` | NO | Max 100 chars. Letters and spaces only. Stored in `Families.City`. |

**Response DTO — `ApiResponse<FamilyDto>`:**

| Field | Type | Notes |
|---|---|---|
| `FamilyId` | `Guid` | — |
| `FamilyName` | `string` | — |
| `City` | `string?` | — |
| `JoinCode` | `string` | 6-char alphanumeric, system-generated |
| `FamilyScore` | `int` | 0–100, calculated weekly |
| `IsActive` | `bool` | False if blocked by SuperAdmin |
| `TimezoneId` | `string` | e.g. `Asia/Kolkata` |
| `PlanId` | `int` | FK to Plans table |
| `CurrentStreakDays` | `int` | — |
| `BestStreakDays` | `int` | — |

**Business rules:**
- Creates three records atomically: `Families` row, `Subscriptions` row (Plan = FreeTrial,
  14-day trial), and `FamilyMembers` row for the caller (Role = FamilyAdmin, IsActive = true).
- A user who already owns a family cannot create another (duplicate ownership → 409).
- JWT re-issued after creation with `FamilyId`, `FamilyMemberId`, `PlanCode = FreeTrial`, `Role = FamilyAdmin`.

**Error cases:**

| Condition | Status |
|---|---|
| Caller already owns a family | 409 |
| Validation failure | 400 |

---

#### GET /api/v1/families/{familyId}

| Field | Value |
|---|---|
| Auth required | YES — valid JWT scoped to this `familyId` |
| Role gate | FamilyAdmin, Parent, Teacher, Child, Elder (any active member) |

**Response DTO — `ApiResponse<FamilyDto>`:** Same shape as `POST /families` response (FamilyDto above).

**Error cases:** 403 (not a member), 404 (not found / soft-deleted).

---

#### PUT /api/v1/families/{familyId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Request DTO — `UpdateFamilyRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `FamilyName` | `string` | YES | Min 2, max 200 chars — same rules as CreateFamilyRequest |
| `City` | `string` | NO | Max 100 chars. Letters and spaces only. |

**Response DTO:** `ApiResponse<FamilyDto>` — same shape as above.

**Error cases:** 400 (validation), 403 (insufficient role), 404.

---

#### GET /api/v1/families/{familyId}/join-code

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Response DTO — `ApiResponse<{ JoinCode }>`:**

| Field | Type | Notes |
|---|---|---|
| `JoinCode` | `string` | Current active join code — 6-char alphanumeric. |

---

#### POST /api/v1/families/{familyId}/join-code/regenerate

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Response DTO:** `ApiResponse<JoinCodeDto>` — new join code.

**Business rules:** Generates a new unique join code and invalidates the previous one.

---

#### POST /api/v1/families/join

| Field | Value |
|---|---|
| Auth required | YES — valid JWT (user must exist, no active family membership) |
| Role gate | Any authenticated user without existing membership in target family |

**Request DTO — `JoinFamilyRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `JoinCode` | `string` | YES | Exactly 6 alphanumeric chars. Case-insensitive. Must match an active family's join code. |
| `FullName` | `string` | YES | Min 2, max 200 chars. Regex: `^[\p{L} .'\-]+$` |
| `Role` | `int` | YES | Maps to `UserRole` enum — cannot be SuperAdmin or FamilyAdmin |
| `LinkType` | `string` | YES | Father\|Mother\|Son\|Daughter\|Grandfather\|Grandmother\|Tutor\|ArabicTeacher\|MusicTeacher\|Driver\|Caregiver\|Uncle\|Aunt |

**Business rules:**
- Duplicate membership (user already in this family) → 409.
- Role `SuperAdmin` or `FamilyAdmin` cannot be self-assigned via join code → 403.
- Child role join: `ChildProfile` row auto-created.
- Teacher role join: `TeacherProfile` row auto-created.
- Plan child-count limit enforced if joining as Child role (see Section 3.4, Rule 6).

**Error cases:**

| Condition | Status |
|---|---|
| Invalid / expired join code | 404 |
| Already a member | 409 |
| Plan child limit reached | 422 |
| Role = SuperAdmin or FamilyAdmin | 403 |

---

#### GET /api/v1/families/{familyId}/members

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any active family member (role-filtered: Teacher sees own assignments only) |

**Response DTO — `ApiResponse<PaginatedList<FamilyMemberDto>>`:** Paginated member list.

**Request query params:** `page`, `pageSize` (standard pagination).

---

#### POST /api/v1/families/{familyId}/members

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Request DTO — `AddMemberRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `PhoneNumber` | `string` | YES | E.164 format — user looked up or created |
| `FullName` | `string` | YES | Min 2, max 200 chars. Regex: `^[\p{L} .'\-]+$` |
| `Role` | `int` | YES | Cannot be SuperAdmin |
| `LinkType` | `string` | YES | Father\|Mother\|Son\|Daughter\|Grandfather\|Grandmother\|Tutor\|ArabicTeacher\|MusicTeacher\|Driver\|Caregiver\|Uncle\|Aunt |

**Business rules:**
- SuperAdmin role cannot be assigned via this endpoint → 403.
- Duplicate membership → 409.
- Child role add: `ChildProfile` auto-created. Plan child-count limit enforced → 422 if exceeded.
- Teacher role add: `TeacherProfile` auto-created.
- If the phone number has no `Users` row, one is created.
- Sends invite SMS to the member's phone number after successful add.

**Error cases:**

| Condition | Status |
|---|---|
| Role = SuperAdmin | 403 |
| Already a member | 409 |
| Plan child limit reached | 422 |

---

#### PUT /api/v1/families/{familyId}/members/{memberId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Request DTO — `UpdateMemberRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Role` | `int` | NO | New role for this member |
| `LinkType` | `string` | NO | Must be from allowed LinkType list |
| `DisplayName` | `string` | NO | Override display name within the family |

**Response DTO:** `ApiResponse<FamilyMemberDto>`

**Business rules:**
- Cannot remove FamilyAdmin role from the sole FamilyAdmin in the family → 422.
- Role change to Child: `ChildProfile` auto-created if not already exists.
- Role change to Teacher: `TeacherProfile` auto-created if not already exists.

**Error cases:** 400 (validation), 403, 404, 422 (sole FamilyAdmin demotion).

---

#### DELETE /api/v1/families/{familyId}/members/{memberId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Business rules:**
- Cannot remove the sole FamilyAdmin → 422.
- Soft-deletes the `FamilyMembers` row (`IsDeleted = 1, DeletedAt = GETUTCDATE()`).
- Associated `ChildProfile` / `TeacherProfile` soft-deleted when member is removed (if applicable).

**Error cases:** 403, 404, 422 (sole FamilyAdmin removal).

---

#### GET /api/v1/users/{userId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Own profile only, or FamilyAdmin / SuperAdmin |

**Response DTO — `ApiResponse<UserDto>`:** Same `UserDto` shape as `User/UserDto.cs`:

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | — |
| `PhoneNumber` | `string` | — |
| `CountryCode` | `string` | — |
| `FullName` | `string` | — |
| `Email` | `string?` | — |
| `ProfilePhotoUrl` | `string?` | S3 URL |
| `PreferredLanguage` | `string` | — |
| `FcmToken` | `string?` | — |
| `IsPhoneVerified` | `bool` | — |
| `IsActive` | `bool` | — |

---

#### PUT /api/v1/users/{userId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Own profile only |

**Request DTO — `UpdateUserRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `FullName` | `string` | NO | Min 2, max 200 chars |
| `Email` | `string` | NO | Valid email, max 300 chars, unique in Users |
| `ProfilePhotoUrl` | `string` | NO | S3 URL |
| `PreferredLanguage` | `string` | NO | Must be in: `en`, `hi`, `ta`, `te`, `mr` |

**Response DTO:** `ApiResponse<UserDto>` — updated user shape.

---

#### PUT /api/v1/users/{userId}/fcm-token

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Own profile only |

**Request DTO:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `FcmToken` | `string` | YES | Firebase device token |

**Response DTO:** `ApiResponse<bool>` — returns `true` on success.

**Business rules:** Updates `Users.FcmToken`. Called on app launch / token refresh by the client.

---

#### GET /api/v1/families/{familyId}/children

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — full list. Child — own profile only. Teacher — assigned children only. |

**Response DTO — `ApiResponse<List<ChildSummaryDto>>`:**

| Field | Type | Notes |
|---|---|---|
| `ChildProfileId` | `Guid` | — |
| `DisplayName` | `string` | From `FamilyMembers.DisplayName` |
| `AvatarCode` | `string` | `avatar_01` through `avatar_10` |
| `CoinBalance` | `int` | Current coin balance |
| `TotalCoinsEarned` | `int` | Lifetime earned |
| `CurrentStreakDays` | `int` | Current streak |
| `LevelCode` | `int` | 1=Beginner…5=Legend |

---

#### GET /api/v1/families/{familyId}/children/{childId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin (any child); Child (own profile only via `childProfileId` JWT claim); Teacher (assigned children only) |

**Response DTO — `ApiResponse<ChildDetailDto>`:** Full profile from `ChildProfiles`:

| Field | Type | Notes |
|---|---|---|
| `ChildProfileId` | `Guid` | — |
| `FamilyMemberId` | `Guid` | — |
| `UserId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `DisplayName` | `string` | From `FamilyMembers.DisplayName` |
| `DateOfBirth` | `date?` | — |
| `AgeYears` | `int?` | Computed column — DATEDIFF(year, DateOfBirth, GETDATE()) |
| `GradeLevel` | `string?` | e.g. `Class 7` |
| `SchoolName` | `string?` | — |
| `AvatarCode` | `string` | `avatar_01` through `avatar_10` |
| `CoinBalance` | `int` | — |
| `TotalCoinsEarned` | `int` | — |
| `CurrentStreakDays` | `int` | — |
| `BestStreakDays` | `int` | — |
| `StreakFreezesAvailable` | `int` | — |
| `LevelCode` | `int` | 1=Beginner…5=Legend |
| `StudyScore` | `int` | 0–20 |
| `CleanlinessScore` | `int` | 0–20 |
| `DisciplineScore` | `int` | 0–20 |
| `ScreenControlScore` | `int` | 0–20 |
| `ResponsibilityScore` | `int` | 0–20 |
| `ScoreUpdatedAt` | `datetime?` | — |

---

#### PUT /api/v1/families/{familyId}/children/{childId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `UpdateChildRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `DateOfBirth` | `date` | NO | Must be past date. Min: 100 years ago. Max: today minus 3 years (age ≥ 3). 17-year max is UI/business rule only. |
| `GradeLevel` | `string` | NO | Max 50 chars. e.g. `Class 7` |
| `SchoolName` | `string` | NO | Max 200 chars |
| `AvatarCode` | `string` | NO | Must be exactly one of: `avatar_01` through `avatar_10` → 400 if invalid |

**Response DTO:** `ApiResponse<ChildDetailDto>` — updated full profile.

**Error cases:** 400 (invalid avatar code, age out of range), 403, 404.

---

#### GET /api/v1/families/{familyId}/children/{childId}/score-history

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin, Child (own) |

**Request query params:** `periodDays` (optional, default 30) — number of days to look back.

**Response DTO — `ApiResponse<ScoreHistoryDto[]>`:** Array of score snapshots (one per recorded update):

| Field | Type | Notes |
|---|---|---|
| `StudyScore` | `int` | 0–20 |
| `CleanlinessScore` | `int` | 0–20 |
| `DisciplineScore` | `int` | 0–20 |
| `ScreenControlScore` | `int` | 0–20 |
| `ResponsibilityScore` | `int` | 0–20 |
| `RecordedAt` | `datetime` | [VERIFY] exact field name — snapshot timestamp |

---

#### POST /api/v1/families/{familyId}/children/{childId}/coin-deduction

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `DeductCoinsRequest`** (moved to `DTOs/Task/` in Phase 10):

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Amount` | `int` | YES | Must be positive |
| `Note` | `string` | YES | Mandatory reason — [VERIFY] min/max length |

**Business rules:**
- Phase 10 update: coin deduction now writes a `CoinTransactions` ledger entry (Phase 10
  replaced the Phase 04 stub that only updated `ChildProfile.CoinBalance` directly).
- Insufficient balance → 422.

**Error cases:** 400 (validation), 403, 404, 422 (insufficient coins).

---

#### GET /api/v1/families/{familyId}/children/{childId}/coin-history

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin, Child (own only) |

**Request query params:** `page`, `pageSize` (standard pagination).

**Response DTO — `ApiResponse<PaginatedList<CoinTransactionDto>>`:** Paginated coin transaction history.

**Note:** This endpoint was in TechSpec (4.3 Children section) but was missing from the original Section 3 documentation. See Drift Entry 016.

---

#### POST /api/v1/families/{familyId}/children/{childId}/teachers

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `TeacherAssignRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `TeacherProfileId` | `Guid` | YES | Must be an active Teacher member of the same family |

**Business rules:**
- Assignment uniqueness enforced: `(TeacherProfileId, ChildProfileId)` pair where `IsActive = 1`
  must not already exist → 409 on duplicate.
- Teacher must be an active member of the same family.

**Error cases:** 409 (duplicate assignment), 404 (teacher or child not found), 403.

---

#### DELETE /api/v1/families/{familyId}/children/{childId}/teachers/{teacherProfileId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Business rules:** Sets `TeacherChildAssignments.IsActive = false` for the matching row. No soft-delete — uses `IsActive` flag (no `IsDeleted` column on this table).

**Error cases:** 404 (assignment not found), 403.

---

### 3.3 DB Tables

#### Plans
- **Scripts:** `003_CreatePlans.sql` (Phase 01) · `007_SeedPlans.sql` (Phase 01)
- **Note:** PK is `PlanId` INT IDENTITY — exception to GUID PK rule. Column `PriceMonthly` (not `MonthlyPrice`).

| Column | Type | Notes |
|---|---|---|
| `PlanId` | `INT IDENTITY(1,1)` | PK — 1=FreeTrial, 2=Basic, 3=Family, 4=Premium |
| `PlanName` | `NVARCHAR(100)` | NOT NULL, UNIQUE |
| `PlanCode` | `NVARCHAR(50)` | NOT NULL, UNIQUE — `free_trial\|basic\|family\|premium` |
| `PriceMonthly` | `DECIMAL(10,2)` | NOT NULL — ₹0 / ₹99 / ₹199 / ₹299 |
| `MaxChildren` | `INT` | NOT NULL — 1 / 2 / 4 / 99 (99 = effectively unlimited) |
| `MaxTeachers` | `INT` | NOT NULL — 0 / 1 / 2 / 10 |
| `HasElderMode` | `BIT` | NOT NULL, DEFAULT 0 |
| `HasWeeklyDigest` | `BIT` | NOT NULL, DEFAULT 0 |
| `HasAdvancedReports` | `BIT` | NOT NULL, DEFAULT 0 |
| `StorageQuotaMb` | `INT` | NOT NULL, DEFAULT 0 |
| `TrialDays` | `INT` | NOT NULL, DEFAULT 0 — 14 for FreeTrial |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 |

**Seeded rows (Phase 01):**

| PlanId | PlanCode | Price | MaxChildren | MaxTeachers | TrialDays |
|---|---|---|---|---|---|
| 1 | `free_trial` | ₹0 | 1 | 0 | 14 |
| 2 | `basic` | ₹99/mo | 2 | 1 | 0 |
| 3 | `family` | ₹199/mo | 4 | 2 | 0 |
| 4 | `premium` | ₹299/mo | 99 | 10 | 0 |

#### Families
- **Scripts:** `004_CreateFamilies.sql` (Phase 01) · `011_AlterFamilies_JoinCode.sql` (Phase 03)
- **Note:** PK column is `FamilyId` (spec convention) — follows same EF mapping pattern as `Users.UserId`.

| Column | Type | Notes |
|---|---|---|
| `FamilyId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyName` | `NVARCHAR(200)` | NOT NULL — max 200 chars |
| `JoinCode` | `NVARCHAR(10)` | NOT NULL, UNIQUE — 6-char alphanumeric, system-generated. Index added in `011`. |
| `City` | `NVARCHAR(100)` | NULL |
| `PlanId` | `INT` | NOT NULL, FK → Plans.PlanId |
| `SubscriptionId` | `UNIQUEIDENTIFIER` | NULL, FK → Subscriptions.SubscriptionId |
| `FamilyAdminUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `FamilyScore` | `INT` | NOT NULL, DEFAULT 0 — 0–100, calculated weekly |
| `FamilyScoreUpdatedAt` | `DATETIME2` | NULL |
| `CurrentStreakDays` | `INT` | NOT NULL, DEFAULT 0 |
| `BestStreakDays` | `INT` | NOT NULL, DEFAULT 0 |
| `TimezoneId` | `NVARCHAR(100)` | NOT NULL, DEFAULT `Asia/Kolkata` |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 — set to 0 when SuperAdmin blocks family |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

#### Subscriptions
- **Script:** `005_CreateSubscriptions.sql` (Phase 01)
- **Note:** PK is `SubscriptionId`. `Status` is `NVARCHAR(20)` (string), not `INT`. Dates are `DATE`, not `DATETIME2`.

| Column | Type | Notes |
|---|---|---|
| `SubscriptionId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `PlanId` | `INT` | NOT NULL, FK → Plans.PlanId |
| `Status` | `NVARCHAR(20)` | NOT NULL — `Active\|Trial\|Expired\|Cancelled` |
| `StartDate` | `DATE` | NOT NULL |
| `EndDate` | `DATE` | NULL — null = active until cancelled |
| `TrialEndDate` | `DATE` | NULL — null for paid plans; extendable via Phase 19 admin |
| `RazorpaySubscriptionId` | `NVARCHAR(200)` | NULL — payment gateway reference |
| `RazorpayCustomerId` | `NVARCHAR(200)` | NULL — payment gateway customer |
| `AutoRenew` | `BIT` | NOT NULL, DEFAULT 1 |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |

#### FamilyMembers
- **Scripts:** `006_CreateFamilyMembers.sql` (Phase 01) · `010_CreateFamilyMemberIndexes.sql` (Phase 03)
- **Note:** PK is `FamilyMemberId`. Missing columns confirmed from TechSpec: `LinkType`, `JoinedAt`, `InvitedByUserId`.

| Column | Type | Notes |
|---|---|---|
| `FamilyMemberId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `UserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `Role` | `INT` | NOT NULL — `UserRole` enum |
| `LinkType` | `NVARCHAR(50)` | NOT NULL — Father\|Mother\|Son\|Daughter\|Grandfather\|Grandmother\|Tutor\|ArabicTeacher\|MusicTeacher\|Driver\|Caregiver\|Uncle\|Aunt |
| `DisplayName` | `NVARCHAR(200)` | NULL — override display name within the family |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 — set to 0 on removal or family block |
| `JoinedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `InvitedByUserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Index (confirmed from TechSpec):** `IX_FamilyMembers_FamilyId_UserId` — UNIQUE (FamilyId, UserId) WHERE IsDeleted = 0

#### ChildProfiles
- **Scripts:** `012_CreateChildProfiles.sql` (Phase 04) · `023_AlterChildProfiles_RowVersion.sql` (Phase 10)
- **Note:** PK is `ChildProfileId`. `DateOfBirth` is `DATE` (not `DATETIME2`). No `DisplayName` column — display name lives in `FamilyMembers.DisplayName`. `AgeYears` is a computed column.

| Column | Type | Notes |
|---|---|---|
| `ChildProfileId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyMemberId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → FamilyMembers.FamilyMemberId, UNIQUE — 1-to-1 |
| `UserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `DateOfBirth` | `DATE` | NULL |
| `AgeYears` | computed | `AS (DATEDIFF(year, DateOfBirth, GETDATE()))` — not a stored column |
| `GradeLevel` | `NVARCHAR(50)` | NULL — e.g. `Class 7` |
| `SchoolName` | `NVARCHAR(200)` | NULL |
| `AvatarCode` | `NVARCHAR(20)` | NOT NULL, DEFAULT `avatar_01` |
| `CoinBalance` | `INT` | NOT NULL, DEFAULT 0 — current spendable balance |
| `TotalCoinsEarned` | `INT` | NOT NULL, DEFAULT 0 — lifetime earned, drives level |
| `CurrentStreakDays` | `INT` | NOT NULL, DEFAULT 0 |
| `BestStreakDays` | `INT` | NOT NULL, DEFAULT 0 |
| `StreakFreezesAvailable` | `INT` | NOT NULL, DEFAULT 0 — max 2 |
| `LevelCode` | `INT` | NOT NULL, DEFAULT 1 — 1=Beginner…5=Legend |
| `StudyScore` | `INT` | NOT NULL, DEFAULT 0 — 0–20 |
| `CleanlinessScore` | `INT` | NOT NULL, DEFAULT 0 — 0–20 |
| `DisciplineScore` | `INT` | NOT NULL, DEFAULT 0 — 0–20 |
| `ScreenControlScore` | `INT` | NOT NULL, DEFAULT 0 — 0–20 |
| `ResponsibilityScore` | `INT` | NOT NULL, DEFAULT 0 — 0–20 |
| `ScoreUpdatedAt` | `DATETIME2` | NULL |
| `RowVersion` | `rowversion` | Added Phase 10 — optimistic concurrency for coin mutations |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 — from BaseEntity standard (not explicitly in TechSpec table) |
| `DeletedAt` | `DATETIME2` | NULL — from BaseEntity standard |

**Level thresholds (Phase 10 — `TotalCoinsEarned`):**

| Range | Level | LevelCode |
|---|---|---|
| 0–499 | Beginner | 1 |
| 500–1,499 | Explorer | 2 |
| 1,500–2,999 | Achiever | 3 |
| 3,000–4,999 | Champion | 4 |
| 5,000+ | Legend | 5 |

#### TeacherProfiles
- **Script:** `013_CreateTeacherProfiles.sql` (Phase 04)
- **Note:** PK is `TeacherProfileId`. No `DisplayName` column — display name lives in `FamilyMembers.DisplayName`. `SubjectName` column was missing from previous documentation.

| Column | Type | Notes |
|---|---|---|
| `TeacherProfileId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyMemberId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → FamilyMembers.FamilyMemberId, UNIQUE — 1-to-1 |
| `UserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `SubjectName` | `NVARCHAR(200)` | NOT NULL — e.g. `Mathematics` |
| `TeacherType` | `NVARCHAR(50)` | NOT NULL — `School\|Tuition\|Arabic\|Music\|Other` |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 — from BaseEntity standard |
| `DeletedAt` | `DATETIME2` | NULL — from BaseEntity standard |

#### TeacherChildAssignments
- **Script:** `014_CreateTeacherChildAssignments.sql` (Phase 04)
- **Note:** PK is `AssignmentId`. Has `FamilyId` column (missing from previous docs). No `IsDeleted`/`DeletedAt` — uses `IsActive` flag only.

| Column | Type | Notes |
|---|---|---|
| `AssignmentId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `TeacherProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → TeacherProfiles.TeacherProfileId |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `AssignedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 — set to false to deactivate, no soft-delete |

**Index (confirmed from TechSpec):** `IX_TeacherChildAssignments_Teacher_Child` — UNIQUE (TeacherProfileId, ChildProfileId) WHERE IsActive = 1

---

### 3.4 Business Rules

1. **Family creation atomicity:** `POST /families` creates `Families`, `Subscriptions` (FreeTrial),
   and `FamilyMembers` (caller as FamilyAdmin) in a single transaction.

2. **Duplicate family ownership:** A user who already owns a family (has a FamilyAdmin
   FamilyMembers row as owner) cannot create another → 409.

3. **Duplicate membership:** Same `(UserId, FamilyId)` cannot appear twice in `FamilyMembers`
   (where `IsDeleted = 0`) → 409.

4. **SuperAdmin assignment blocked:** SuperAdmin role cannot be added or assigned via any
   family-management endpoint → 403.

5. **Sole FamilyAdmin protection:** The last FamilyAdmin in a family cannot be removed or
   have their role downgraded → 422.

6. **Plan child-count limits:**

   | Plan | Max Children |
   |---|---|
   | FreeTrial | 1 |
   | Basic | 2 |
   | Family | 4 |
   | Premium | Unlimited |

   Enforced at `POST /members` and `POST /families/join` when Role = Child.
   Exceeding limit → 422 Unprocessable Entity.

7. **Auto-profile creation:** When a member is added or joins with Role = Child,
   a `ChildProfiles` row is auto-created. When Role = Teacher, a `TeacherProfiles` row
   is auto-created. No separate profile-create endpoint exists.

8. **Role change auto-profile:** If `PUT /members/{memberId}` changes a member's role to
   Child or Teacher, the corresponding profile row is auto-created if it does not exist.

9. **Avatar code constraint:** `AvatarCode` must be one of `avatar_01` through `avatar_10`.
   Any other value → 400.

10. **Child age constraint:** `DateOfBirth` when provided must result in an age of 3–17 years.
    Outside this range → 400.

11. **Teacher assignment uniqueness:** `(TeacherProfileId, ChildProfileId)` must be unique
    where `IsActive = 1`. Duplicate active assignment → 409.
    Enforced in both service layer and DB index.

12. **JWT re-issuance after family creation:** After `POST /families`, the response includes
    a new `FamilyDto`. The JWT is NOT automatically re-issued inline — the client must call
    `POST /auth/refresh-token` to get a new token with `FamilyId`, `FamilyMemberId`,
    `PlanCode = free_trial`, `Role = FamilyAdmin` claims populated.
    (TechSpec Section 6.1 shows family creation as a separate step before token refresh.)

13b. **LinkType required:** All `POST /families/join` and `POST /families/{familyId}/members`
    calls must include a `LinkType` from the allowed list:
    Father|Mother|Son|Daughter|Grandfather|Grandmother|Tutor|ArabicTeacher|MusicTeacher|Driver|Caregiver|Uncle|Aunt.
    Missing or invalid `LinkType` → 400.

13. **Family block (Phase 19):** SuperAdmin blocking a family sets `Families.IsActive = 0`
    and `FamilyMembers.IsActive = 0` for all members. Blocked members cannot authenticate.

---

### 3.5 Flow Summaries

#### Flow 1 — Create Family

```
Trigger       : New user creates a family from the app
→ API call    : POST /api/v1/families { FamilyName }
→ Validation  : FamilyName length; duplicate ownership check → 409 if already owns a family
→ DB operation: INSERT Families; INSERT Subscriptions (PlanId=FreeTrial, Status=Trial,
                TrialEndDate=+14d); INSERT FamilyMembers (UserId=caller, Role=FamilyAdmin).
→ Response    : 201 ApiResponse<FamilyDto>
→ Side effect : JWT re-issued with FamilyId, FamilyMemberId, PlanCode, Role claims.
```

#### Flow 2 — Join Family via Code

```
Trigger       : User receives a join code (shared by FamilyAdmin) and enters it in the app
→ API call    : POST /api/v1/families/join { JoinCode, FullName, Role, LinkType }
→ Validation  : JoinCode — exactly 6 alphanumeric chars, must match active family → 404 if invalid;
                Role gate (no SuperAdmin/FamilyAdmin) → 403;
                duplicate membership check → 409;
                LinkType must be from allowed list → 400 if invalid;
                plan child-count check if Role=Child → 422 if exceeded
→ DB operation: INSERT FamilyMembers (Role, LinkType, JoinedAt=now, InvitedByUserId=null);
                if Role=Child → INSERT ChildProfiles;
                if Role=Teacher → INSERT TeacherProfiles.
→ Response    : 200 ApiResponse<FamilyMemberDto>
→ Side effect : Client calls POST /auth/refresh-token to get JWT with updated family context.
```

#### Flow 3 — Add Member (FamilyAdmin)

```
Trigger       : FamilyAdmin adds a member manually from the family management screen
→ API call    : POST /api/v1/families/{familyId}/members { PhoneNumber, FullName, Role, LinkType }
→ Validation  : Role gate (FamilyAdmin only); SuperAdmin role blocked → 403;
                duplicate membership → 409; plan child limit → 422;
                LinkType must be from allowed list → 400
→ DB operation: Lookup or create Users row for PhoneNumber;
                INSERT FamilyMembers (Role, LinkType, InvitedByUserId=caller, JoinedAt=now);
                auto-create ChildProfile or TeacherProfile if needed.
→ Response    : 201 ApiResponse<FamilyMemberDto>
→ Side effect : Invite SMS dispatched to member's phone number via MSG91.
```

#### Flow 4 — Teacher Assignment

```
Trigger       : Parent assigns a teacher to a child
→ API call    : POST /api/v1/families/{familyId}/children/{childId}/teachers
                { TeacherProfileId }
→ Validation  : Role gate (Parent, FamilyAdmin); teacher must be active member of same family;
                duplicate active assignment check → 409
→ DB operation: INSERT TeacherChildAssignments (IsActive=true).
→ Response    : 201 ApiResponse<TeacherAssignmentDto> — [VERIFY] exact DTO field names
→ Side effect : Teacher's JWT claims (AssignedChildIds) updated on next token refresh.
```

#### Flow 5 — Coin Deduction by Parent

```
Trigger       : Parent deducts coins from a child's balance (penalty or correction)
→ API call    : POST /api/v1/families/{familyId}/children/{childId}/coin-deduction
                { Amount, Note }
→ Validation  : Role gate (Parent, FamilyAdmin); Note 5–500 chars;
                sufficient balance → 422 if insufficient
→ DB operation: (Phase 10) INSERT CoinTransactions (TransactionType=Deduction);
                UPDATE ChildProfiles SET CoinBalance -= Amount using optimistic concurrency.
→ Response    : 200 ApiResponse<CoinTransactionDto>
→ Side effect : None.
```

---

### 3.6 Flutter Integration

**Status: Flutter app not yet built.** Screen names, route constants, and MockDataService methods below are planned from the DevPlan spec — not confirmed implemented.

**Planned screens (from DevPlan — [VERIFY] against implementation when built):**

| Screen | Planned path | Notes |
|---|---|---|
| Family creation | `lib/features/family/screens/create_family_screen.dart` | FamilyAdmin only |
| Family members list | `lib/features/family/screens/members_screen.dart` | — |
| Add member | `lib/features/family/screens/add_member_screen.dart` | FamilyAdmin only |
| Join family | `lib/features/auth/screens/join_family_screen.dart` | Via join code |
| Child profile detail | `lib/features/family/screens/child_detail_screen.dart` | Parent/Child view |
| Child profile edit | `lib/features/family/screens/edit_child_screen.dart` | Parent/FamilyAdmin |
| Teacher assignment | `lib/features/family/screens/assign_teacher_screen.dart` | Parent/FamilyAdmin |

**Confirmed constraints (from CLAUDE.md standards):**
- Folder: `lib/features/family/screens/`, `/providers/`, `/repositories/`
- Two repository implementations per feature: Demo (`MockDataService`) and Live (Dio)
- All role-conditional action buttons check `AuthNotifier.currentRole` before rendering
- `FamilyId` from `AuthNotifier` is used as the scoping param for all family endpoints

---

### 3.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `Users` table (Phase 02) | Existing `Users` row for the phone number | Member add/join creates or looks up `Users` by phone |
| `RefreshTokens` / JWT service (Phase 02) | JWT re-issuance capability | New family context claims must be reflected in a refreshed token |
| `FeatureFlags` table (Phase 19) | `MaintenanceMode` flag | Family APIs blocked during maintenance for non-admin roles |
| Plan limits (Phase 01 seed) | `Plans` table with child limits | Child-count enforcement at member-add time |
| `CoinService` (Phase 10) | `ICoinService.DeductCoinsAsync` | Coin deduction endpoint delegates to Phase 10 coin ledger service |

---

## 4. Family Dashboard

### 4.1 Module Purpose

Provides a single aggregated read endpoint that gives Parent and FamilyAdmin a real-time
snapshot of the family — member role counts, family score, streak metrics, and
unacknowledged feedback count.

Implemented in `FamiliesController` as part of Phase 03 (Family & User Management).
Extended in Phase 12 to add `UnacknowledgedFeedbackCount`. No separate controller —
the dashboard endpoint lives inside `FamiliesController` alongside family management.

**Scope boundary (confirmed from Codex DevPlan Phase 03):** Child summaries, task/attendance
data, calendar events, and reward redemption counts are explicitly out of scope for the current
dashboard implementation. Only role counts, family score, streak, and feedback badge are returned.

---

### 4.2 Key APIs

---

#### GET /api/v1/families/{familyId}/dashboard

| Field | Value |
|---|---|
| Auth required | YES — valid JWT scoped to this `familyId` |
| Role gate | Parent, FamilyAdmin |

**Request:** No body. No query parameters.

**Response DTO — `ApiResponse<FamilyDashboardDto>`:**

**All fields confirmed from `FamilyDashboardDto.cs` (Phase 03 + Phase 12):**

| Field | Type | Source | Notes |
|---|---|---|---|
| `FamilyId` | `Guid` | `Families.Id` | — |
| `FamilyName` | `string` | `Families.FamilyName` | — |
| `Date` | `DateOnly` | `DateOnly.FromDateTime(DateTime.UtcNow)` | Today's UTC date — server-generated |
| `FamilyScore` | `int` | `Families.FamilyScore` | 0–100, pre-calculated weekly |
| `CurrentStreakDays` | `int` | `Families.CurrentStreakDays` | — |
| `BestStreakDays` | `int` | `Families.BestStreakDays` | — |
| `UnacknowledgedFeedbackCount` | `int` | `TeacherFeedback` — `CountUnacknowledgedByFamilyAsync` | Added Phase 12 |
| `TotalMembers` | `int` | `FamilyMembers` — count of active rows | — |
| `ParentCount` | `int` | Count of members where Role = Parent **OR FamilyAdmin** | Includes FamilyAdmin in parent count |
| `ChildCount` | `int` | Count of members where Role = Child | — |
| `TeacherCount` | `int` | Count of members where Role = Teacher | — |
| `ElderCount` | `int` | Count of members where Role = Elder | — |

**Not included in current dashboard (confirmed out of scope):**
- Child profile summaries (CoinBalance, streak, avatar) — not in Phase 03 scope
- TaskCompletion counts — not added to dashboard in any phase
- Today's attendance data — not added to dashboard in any phase
- Upcoming calendar events — not added to dashboard in any phase
- Reward redemption counts — not added to dashboard in any phase

**Business rules:**
- Response is read-only. No writes performed by this endpoint.
- Row-level security applies: only data within `FamilyId = @currentFamilyId` is returned.
- `UnacknowledgedFeedbackCount` counts `TeacherFeedback` rows for this family where
  `IsAcknowledged = 0` AND `IsDeleted = 0`.
- Data is real-time — no caching. All three queries run on every dashboard call.
- Dashboard polling interval (from TechSpec P-04 screen): 60 seconds or WebSocket push.

**Error cases:**

| Condition | Status |
|---|---|
| Missing / invalid JWT | 401 |
| Valid JWT but not a member of this family | 403 |
| Family not found / soft-deleted | 404 |

---

### 4.3 DB Tables

The dashboard reads from exactly three tables and writes to none (confirmed from `GetDashboardAsync`):

| Table | Data Read | Confirmed |
|---|---|---|
| `Families` | `FamilyId`, `FamilyName`, `FamilyScore`, `CurrentStreakDays`, `BestStreakDays` | Phase 03 |
| `FamilyMembers` | All active rows (`IsDeleted=0`, `IsActive=1`) — counted by role | Phase 03 |
| `TeacherFeedback` | Count where `IsAcknowledged=0` AND `FamilyId=@familyId` AND `IsDeleted=0` | Phase 12 |

**Tables NOT read by the current dashboard** (explicitly out of scope — confirmed):
`ChildProfiles`, `TaskCompletions`, `CalendarEvents`, `RewardRedemptions`, `AttendanceSessions`

Full table definitions are in the modules that own each table (Sections 3, 5, 6, 7, 8, 9).

---

### 4.4 Business Rules

1. **Read-only endpoint.** `GET /families/{familyId}/dashboard` performs no writes.

2. **Family scope enforced.** All queries filter `WHERE FamilyId = @currentFamilyId`
   AND `WHERE IsDeleted = 0`. No cross-family data ever returned.

3. **Role gate:** Parent and FamilyAdmin only — enforced in service layer via
   `ForbiddenAccessException` if role is anything else → 403. Child, Teacher, Elder use
   their own role-specific screens.

4. **UnacknowledgedFeedbackCount** (Phase 12): counts `TeacherFeedback` rows for this family
   where `IsAcknowledged = 0` and `IsDeleted = 0`. Surfaces a notification badge for parents
   who have unread teacher feedback.

5. **FamilyScore calculation:** `Families.FamilyScore` is a pre-stored value (0–100),
   updated by a background job (weekly calculation — `FamilyScoreUpdatedAt` tracks last update).
   The dashboard endpoint reads the stored value — it does NOT recalculate on each call.
   Score update triggers: [VERIFY] exact calculation logic — likely driven by task completion
   rate and streak data across the family.

6. **`ParentCount` includes FamilyAdmin:** The dashboard counts members with Role = Parent
   OR Role = FamilyAdmin in the `ParentCount` field. There is no separate `FamilyAdminCount`.

7. **Data is real-time:** No caching. Every dashboard call executes three DB reads
   (`Families`, `FamilyMembers`, `TeacherFeedback`). TechSpec recommends 60-second polling
   from the client.

8. **Scope is final for Phase 03 + 12:** No subsequent phase added child summaries,
   task counts, calendar events, or redemption counts to `FamilyDashboardDto`.
   Confirmed by reading the actual DTO — only 12 fields.

---

### 4.5 Flow Summaries

#### Flow 1 — Dashboard Load

```
Trigger       : Parent or FamilyAdmin opens the home/dashboard screen
→ API call    : GET /api/v1/families/{familyId}/dashboard — JWT required
→ Validation  : Membership check → ForbiddenAccessException (403) if user is not
                an active FamilyMember of this family.
                Role check → ForbiddenAccessException (403) if Role ≠ Parent or FamilyAdmin.
→ DB operation: 3 read queries (no writes):
                  1. SELECT * FROM Families WHERE Id=@familyId AND IsDeleted=0
                     → reads FamilyId, FamilyName, FamilyScore, CurrentStreakDays, BestStreakDays
                  2. SELECT * FROM FamilyMembers WHERE FamilyId=@familyId
                     AND IsDeleted=0 AND IsActive=1
                     → counted in-memory by role (Parent+FamilyAdmin, Child, Teacher, Elder)
                  3. CountUnacknowledgedByFamilyAsync(familyId)
                     → SELECT COUNT(*) FROM TeacherFeedback WHERE FamilyId=@familyId
                        AND IsAcknowledged=0 AND IsDeleted=0
→ Response    : 200 ApiResponse<FamilyDashboardDto> — 12-field confirmed shape
→ Side effect : None. No caching. Client polls every 60 seconds per TechSpec.
```

---

### 4.6 Flutter Integration

**Status: Flutter app not yet built.** Screen names and MockDataService methods are planned
from `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` — not confirmed implemented.

**Planned screens (from TechSpec P-04 — [VERIFY] against implementation when built):**

| Screen | Planned path | Route |
|---|---|---|
| Parent Home / Dashboard | `lib/features/parent/screens/parent_home_screen.dart` | `/parent/home` |

**What P-04 displays (from TechSpec screen spec):**
- Header: Good morning [Name] + date + Family Streak badge (uses `CurrentStreakDays`)
- Child Cards: Avatar + Name + task progress bar (done/total) + status pill + last active time
  → Note: child task data is NOT in `FamilyDashboardDto`; Flutter must make separate
  calls to task/completion endpoints per child to populate these cards
- Alert Strip: up to 3 items — unacknowledged feedback (`UnacknowledgedFeedbackCount` badge),
  upcoming exams, document expiry
- Today's Events: next 3 calendar events (requires separate calendar API call)
- Pending Verifications count badge: requires separate verification-queue API call

**MockDataService (planned — [VERIFY]):**
- `mockGetDashboard(familyId)` → returns `FamilyDashboardDto` with meaningful mock values

**Confirmed constraints:**
- Folder: `lib/features/parent/screens/` and `lib/features/dashboard/`
- Dashboard polls every 60 seconds (TechSpec) — client-side timer, not WebSocket
- Demo mode must show non-blank dashboard — role-appropriate mock data required
- `UnacknowledgedFeedbackCount > 0` should surface badge on bottom nav or header

---

### 4.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `Families` table (Phase 01/03) | `FamilyId`, `FamilyName`, `FamilyScore`, `CurrentStreakDays`, `BestStreakDays` | Core dashboard metrics |
| `FamilyMembers` table (Phase 01/03) | All active rows — counted by role | Member breakdown |
| `TeacherFeedback` table (Phase 11) | `CountUnacknowledgedByFamilyAsync` | Feedback badge — added Phase 12 |

**Not depended on by current dashboard (confirmed):**
`ChildProfiles`, `TaskCompletions`, `CalendarEvents`, `RewardRedemptions`, `AttendanceSessions`

---

## 5. Attendance System

### 5.1 Module Purpose

Manages the full attendance lifecycle: session creation by teachers, bulk attendance marking
with per-child status, a time-windowed edit path for corrections, and family-admin override
with audit trail. Includes comment templates shared across attendance and feedback.

Implemented across three phases:
- **Phase 05** — `AttendanceController`: session create, list, detail
- **Phase 06** — `AttendanceController` extended: submit, edit, child history, session records
- **Phase 07** — `CommentTemplatesController`: template CRUD (uses `CommentTemplates` table from Phase 01)

Note: `GET /families/{familyId}/attendance/statuses` (custom status config) is added to
`AttendanceController` in Phase 20 and documented in **Section 11**.

---

### 5.2 Key APIs

---

#### POST /api/v1/families/{familyId}/attendance/sessions

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (must have an active `TeacherProfile` in this family); FamilyAdmin only when they also hold an active `TeacherProfile` |

**Request DTO — `CreateSessionRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `SessionName` | `string` | YES | Max 200 chars |
| `SubjectName` | `string` | YES | Max 200 chars — e.g. `Mathematics` |
| `BatchName` | `string?` | NO | Max 100 chars — e.g. `Batch A` |
| `ScheduledDate` | `DateOnly` | YES | Must be within 7 days past and 30 days future |
| `StartTime` | `TimeOnly` | YES | — |
| `EndTime` | `TimeOnly?` | NO | Must be after `StartTime` when provided |
| `IsRecurring` | `bool` | YES | — |
| `RecurringDays` | `int[]?` | NO | Required when `IsRecurring = true`; values 1–7 (Mon=1…Sun=7); no duplicates |

**Response DTO — `ApiResponse<AttendanceSessionDto>`:**

| Field | Type | Notes |
|---|---|---|
| `SessionId` | `Guid` | — |
| `TeacherProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `TeacherName` | `string` | From `TeacherProfile → FamilyMember.DisplayName` |
| `SessionName` | `string` | — |
| `SubjectName` | `string` | — |
| `BatchName` | `string?` | — |
| `ScheduledDate` | `DateOnly` | — |
| `StartTime` | `TimeOnly` | — |
| `EndTime` | `TimeOnly?` | — |
| `IsSubmitted` | `bool` | — |
| `SubmittedAt` | `DateTime?` | UTC — null until submitted |
| `IsRecurring` | `bool` | — |
| `RecurringDays` | `IReadOnlyCollection<int>` | Empty when not recurring |
| `IsActive` | `bool` | — |

**Business rules:**
- Session creation is scoped to the caller's active `TeacherProfile` within the requested family.
  A pure FamilyAdmin with no `TeacherProfile` row cannot create sessions.
- `RecurringDays` stored as JSON text in `AttendanceSessions.RecurringDays` column.
- `ScheduledDate` ± window: max 7 days in the past, max 30 days in the future → 400 outside window.
- `EndTime > StartTime` enforced when both provided → 400.
- `RecurringDays` values must be integers 1–7 with no duplicates when `IsRecurring = true` → 400.

**Error cases:**

| Condition | Status |
|---|---|
| Caller has no active TeacherProfile in this family | 403 |
| ScheduledDate outside ±7/+30 day window | 400 |
| EndTime ≤ StartTime | 400 |
| Invalid or duplicate RecurringDays | 400 |

---

#### GET /api/v1/families/{familyId}/attendance/sessions

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own sessions only); Parent, FamilyAdmin (sessions for teachers assigned to family's children) |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `date` | `DateOnly?` | Optional — filter sessions by scheduled date |

**Response DTO — `ApiResponse<IReadOnlyCollection<AttendanceSessionDto>>`** — not paginated.

**Business rules:**
- Teacher: returns only their own sessions (filtered by `TeacherProfileId`).
- Parent / FamilyAdmin: returns sessions whose teacher has an active `TeacherChildAssignment`
  for at least one child in the family.

---

#### GET /api/v1/families/{familyId}/attendance/sessions/{sessionId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own session); Parent, FamilyAdmin (any session visible to family) |

**Response DTO — `ApiResponse<AttendanceSessionDto>`:** Full session detail including
`RecurringDays` (parsed from JSON), `IsSubmitted`, `SubmittedAt`.

**Error cases:** 403, 404.

---

#### POST /api/v1/families/{familyId}/attendance/sessions/{sessionId}/submit

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own session only) |

**Request DTO — `SubmitAttendanceRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Records` | `AttendanceEntryDto[]` | YES | One entry per child being marked |
| `Records[].ChildProfileId` | `Guid` | YES | Must be assigned to this teacher |
| `Records[].Status` | `AttendanceStatus` | YES | Enum: Present=1, Absent=2, Late=3, LeftEarly=4 |
| `Records[].TeacherComment` | `string?` | NO | Max 500 characters |
| `Records[].CommentTemplateId` | `Guid?` | NO | References `CommentTemplates.TemplateId` |

**Response DTO — `ApiResponse<AttendanceSessionDto>`** (session marked as submitted).

**Business rules:**
- Submission is idempotent-blocked: a session already submitted → 409 Conflict.
- Each `ChildProfileId` in `Records` must be in the teacher's active `TeacherChildAssignments`
  for this family. Unassigned child IDs → 400.
- Child IDs must be unique within the request → 400 if duplicates.
- Active children assigned to the teacher but **omitted** from `Records` are auto-created
  with `Status = Present`.
- Marks `AttendanceSessions.IsSubmitted = true`, `SubmittedAt = GETUTCDATE()`.
- Parent push alert sent (via FCM) for each child where `Status = Absent` or `Status = Late`.

**Error cases:**

| Condition | Status |
|---|---|
| Session already submitted | 409 |
| ChildProfileId not assigned to this teacher | 400 |
| Duplicate ChildProfileId in Records | 400 |
| TeacherComment > 500 chars | 400 |
| Invalid AttendanceStatus value | 400 |

---

#### PUT /api/v1/families/{familyId}/attendance/sessions/{sessionId}/records/{recordId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own record, within 1-hour edit window); FamilyAdmin (any record, any time) |

**Request DTO — `EditAttendanceRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Status` | `AttendanceStatus` | YES | Enum: Present=1, Absent=2, Late=3, LeftEarly=4 |
| `TeacherComment` | `string?` | NO | Max 500 characters |
| `CommentTemplateId` | `Guid?` | NO | References `CommentTemplates.TemplateId` |

**Response DTO — `ApiResponse<AttendanceRecordDto>`:**

| Field | Type | Notes |
|---|---|---|
| `RecordId` | `Guid` | — |
| `SessionId` | `Guid` | — |
| `ChildProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `ChildName` | `string` | From `ChildProfile → FamilyMember.DisplayName` |
| `Status` | `AttendanceStatus` | Enum — Present=1, Absent=2, Late=3, LeftEarly=4 |
| `TeacherComment` | `string?` | — |
| `CommentTemplateId` | `Guid?` | References `CommentTemplates.TemplateId` |
| `MarkedAt` | `DateTime` | UTC — time the record was created |
| `MarkedByUserId` | `Guid` | UserId of the teacher or admin who marked |
| `EditedAt` | `DateTime?` | UTC — null if never edited |
| `EditedByUserId` | `Guid?` | UserId of editor — null if never edited |

**Business rules:**
- **Teacher edit window:** Teacher may only edit a record from their own submitted session
  while `AttendanceSessions.SubmittedAt` is less than **1 hour** old (UTC). Outside the window
  → 403.
- **FamilyAdmin override:** FamilyAdmin may edit any record at any time, with no window
  restriction. Every FamilyAdmin edit writes an `AuditLogs` row with `OldValues` and
  `NewValues` as JSON.
- Teachers cannot edit records from sessions submitted by other teachers.

**Error cases:**

| Condition | Status |
|---|---|
| Teacher edit outside 1-hour window | 403 |
| Teacher editing another teacher's record | 403 |
| Invalid AttendanceStatus value | 400 |
| Record not found / soft-deleted | 404 |

---

#### GET /api/v1/families/{familyId}/children/{childId}/attendance

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | **Parent or Child only.** FamilyAdmin throws 403. Child can only view own profile (`ChildProfileId` claim must match). |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `fromDate` | `DateOnly?` | Optional lower bound for record date filter |
| `toDate` | `DateOnly?` | Optional upper bound for record date filter |

**Response DTO — `ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>`** — not paginated.

**Business rules:** Returns all `AttendanceRecords` for the requested child filtered by `FamilyId` and optional date range. `IsDeleted = 0` enforced. FamilyAdmin access is explicitly blocked by the service layer (`ForbiddenAccessException` if role ≠ Parent and role ≠ Child).

---

#### GET /api/v1/families/{familyId}/attendance/sessions/{sessionId}/records

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own session); Parent, FamilyAdmin |

**Response DTO — `ApiResponse<List<AttendanceRecordDto>>`:** All records for the session.

---

#### GET /api/v1/families/{familyId}/comment-templates

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher, FamilyAdmin |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `category` | `string` | Optional filter: `Attendance`, `Feedback`, or `Homework` |

**Response DTO — `ApiResponse<List<CommentTemplateDto>>`:**

| Field | Type | Notes |
|---|---|---|
| `TemplateId` | `Guid` | PK column is `TemplateId` — matches DB column name |
| `TemplateText` | `string` | The comment text |
| `Category` | `string` | `Attendance` / `Feedback` / `Homework` |
| `IsSystem` | `bool` | System templates are read-only |
| `SortOrder` | `int` | Sort priority |
| `FamilyId` | `Guid?` | NULL for system templates |

**Business rules:**
- Returns merged list: system templates (`FamilyId IS NULL`) plus family-specific templates
  (`FamilyId = @currentFamilyId`).
- Results sorted by `SortOrder` then `TemplateText`.
- Optional `?category` filter limits to one category.

---

#### POST /api/v1/families/{familyId}/comment-templates

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Request DTO — `CreateCommentTemplateRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `TemplateText` | `string` | YES | Min 5, max 500 characters |
| `Category` | `string` | YES | Must be `Attendance`, `Feedback`, or `Homework` — case-insensitive |

**Business rules:**
- Family template count capped at **20 per category** → 422 if exceeded.
- System templates (`IsSystem = 1`) are not modifiable via this endpoint.

**Error cases:**

| Condition | Status |
|---|---|
| Category cap (>20 templates per category) | 422 |
| Invalid category value | 400 |

---

#### PUT /api/v1/families/{familyId}/comment-templates/{templateId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Request DTO — `UpdateCommentTemplateRequest`:** Same fields as create — `{ TemplateText, Category }`. No `SortOrder` field in either request.

**Business rules:**
- System templates (`IsSystem = 1`) cannot be updated → 403.
- Only family-scoped templates (`FamilyId = @currentFamilyId`) are editable.

**Error cases:** 400, 403 (system template), 404.

---

#### DELETE /api/v1/families/{familyId}/comment-templates/{templateId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin only |

**Business rules:**
- System templates cannot be deleted → 403.
- Soft-delete: `IsDeleted = 1, DeletedAt = GETUTCDATE()`.

**Error cases:** 403 (system template), 404.

---

### 5.3 DB Tables

#### AttendanceSessions
- **Scripts:** `015_CreateAttendanceSessions.sql` · `016_CreateAttendanceSessionIndexes.sql` (Phase 05)
- **Note:** PK is `SessionId`. `ScheduledDate` is `DATE`. `StartTime`/`EndTime` are `TIME`. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `SessionId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `TeacherProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → TeacherProfiles.TeacherProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `SessionName` | `NVARCHAR(200)` | NOT NULL |
| `SubjectName` | `NVARCHAR(200)` | NOT NULL — e.g. `Mathematics` |
| `BatchName` | `NVARCHAR(100)` | NULL — e.g. `Batch A` |
| `ScheduledDate` | `DATE` | NOT NULL |
| `StartTime` | `TIME` | NOT NULL |
| `EndTime` | `TIME` | NULL — CHECK: EndTime > StartTime when not null |
| `IsSubmitted` | `BIT` | NOT NULL, DEFAULT 0 — set to 1 on submit |
| `SubmittedAt` | `DATETIME2` | NULL until submitted; used for edit-window check |
| `IsRecurring` | `BIT` | NOT NULL, DEFAULT 0 |
| `RecurringDays` | `NVARCHAR(50)` | NULL — JSON text array of ints 1–7; CHECK: ISJSON=1 |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**DB CHECK constraints:**
- `CK_AttendanceSessions_TimeRange` — `EndTime IS NULL OR EndTime > StartTime`
- `CK_AttendanceSessions_RecurringDaysJson` — `RecurringDays IS NULL OR ISJSON(RecurringDays) = 1`
- `CK_AttendanceSessions_RecurringDaysRequired` — `IsRecurring = 0 OR RecurringDays IS NOT NULL`

**Indexes (both from `016`, both filtered `WHERE IsDeleted=0 AND IsActive=1`):**
- `IX_AttendanceSessions_TeacherProfileId_ScheduledDate` — (TeacherProfileId, ScheduledDate)
- `IX_AttendanceSessions_FamilyId_ScheduledDate` — (FamilyId, ScheduledDate)

#### AttendanceRecords
- **Script:** `017_CreateAttendanceRecords.sql` (Phase 06)
- **Note:** PK is `RecordId`. No `SubmittedAt` column — uses `MarkedAt`. No `CreatedByTeacherProfileId` — uses `MarkedByUserId`. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `RecordId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `SessionId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → AttendanceSessions.SessionId |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId — denormalized for row-level security |
| `Status` | `INT` | NOT NULL — CHECK (1,2,3,4) — AttendanceStatus enum |
| `TeacherComment` | `NVARCHAR(500)` | NULL — max 500 chars |
| `CommentTemplateId` | `UNIQUEIDENTIFIER` | NULL, FK → CommentTemplates.TemplateId |
| `MarkedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `MarkedByUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `EditedAt` | `DATETIME2` | NULL — set on correction or admin override |
| `EditedByUserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId — set on correction |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Indexes (3 total, all filtered `WHERE IsDeleted=0`):**
- `IX_AttendanceRecords_Session_Child` — UNIQUE (SessionId, ChildProfileId) — one record per child per session
- `IX_AttendanceRecords_FamilyId_ChildProfileId` — (FamilyId, ChildProfileId)
- `IX_AttendanceRecords_SessionId` — (SessionId)

#### AuditLogs
- **Script:** `018_CreateAuditLogs.sql` (Phase 06)
- **Shared table** — also used by Phase 20 (family admin config mutations).
- **Note:** PK is `AuditId BIGINT IDENTITY` — exception to GUID PK rule. `EntityId` is `NVARCHAR` (string). No `IsDeleted`/`UpdatedAt`. Uses `SYSUTCDATETIME()`. Column is `UserId` (not `ChangedByUserId`).

| Column | Type | Notes |
|---|---|---|
| `AuditId` | `BIGINT IDENTITY(1,1)` | PK — exception to GUID rule |
| `UserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId — who made the change |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL, FK → Families.FamilyId |
| `Action` | `NVARCHAR(100)` | NOT NULL — e.g. `AttendanceEdited` |
| `EntityType` | `NVARCHAR(100)` | NOT NULL — e.g. `AttendanceRecord`, `ModuleVisibilityConfig` |
| `EntityId` | `NVARCHAR(100)` | NOT NULL — string ID of the audited entity |
| `OldValues` | `NVARCHAR(MAX)` | NULL — JSON snapshot before change; CHECK: ISJSON=1 |
| `NewValues` | `NVARCHAR(MAX)` | NULL — JSON snapshot after change; CHECK: ISJSON=1 |
| `IpAddress` | `NVARCHAR(45)` | NULL — client IP |
| `UserAgent` | `NVARCHAR(500)` | NULL — client user agent |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |

**Indexes:**
- `IX_AuditLogs_FamilyId_CreatedAt` — (FamilyId, CreatedAt)
- `IX_AuditLogs_UserId` — (UserId)

- **Written by:** FamilyAdmin attendance record edits (Phase 06) with `Action='AttendanceEdited'`; all Phase 20 family-admin config mutations.

#### CommentTemplates
- **Script:** `008_SeedCommentTemplates.sql` (Phase 01 — table created AND seed data inserted)
- **No new script in Phase 07** — table already exists from Phase 01.
- **Note:** PK is `TemplateId`. NOT a full BaseEntity — no `UpdatedAt`, `IsDeleted`, `DeletedAt`. Uses `SYSUTCDATETIME()`. `IsActive` is used for filtering.

| Column | Type | Notes |
|---|---|---|
| `TemplateId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL for system templates; FK → Families.FamilyId for family templates |
| `TemplateText` | `NVARCHAR(500)` | NOT NULL — max 500 chars, min 5 chars (FluentValidation) |
| `Category` | `NVARCHAR(50)` | NOT NULL — CHECK: `Attendance\|Feedback\|Homework` |
| `IsSystem` | `BIT` | NOT NULL, DEFAULT 0 — 1 = read-only system template |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 — used for filtering (inactive templates not returned) |
| `SortOrder` | `INT` | NOT NULL, DEFAULT 0 — sort priority |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |

**Index:** `IX_CommentTemplates_FamilyId_Category` — (FamilyId, Category, IsActive)

**Seeded system templates (12 total — 4 per category):**

| Category | TemplateText | SortOrder |
|---|---|---|
| Attendance | Great punctuality today. | 10 |
| Attendance | Arrived late and needs support with timing. | 20 |
| Attendance | Absent today. Please follow up at home. | 30 |
| Attendance | Left early with parent awareness. | 40 |
| Feedback | Excellent focus and participation. | 10 |
| Feedback | Showed kindness and responsibility today. | 20 |
| Feedback | Needs gentle reminders to stay on task. | 30 |
| Feedback | Please discuss today's concern at home. | 40 |
| Homework | Homework completed on time. | 10 |
| Homework | Homework needs correction and resubmission. | 20 |
| Homework | Homework was incomplete today. | 30 |
| Homework | Please support homework practice at home. | 40 |

**AttendanceStatus enum values (confirmed from `AttendanceStatus.cs`):**

| Int | Name |
|---|---|
| 1 | `Present` |
| 2 | `Absent` |
| 3 | `Late` |
| 4 | `LeftEarly` |

---

### 5.4 Business Rules

1. **Session ownership:** Only the Teacher who owns a session (matched via `TeacherProfileId`)
   can submit it. FamilyAdmin can view and edit records but not submit sessions unless
   they also hold an active `TeacherProfile`.

2. **Session date window:**
   - Minimum: `ScheduledDate` must not be more than **7 days in the past** → 400.
   - Maximum: `ScheduledDate` must not be more than **30 days in the future** → 400.

3. **RecurringDays constraint:** When `IsRecurring = true`, `RecurringDays` is required and
   must contain unique integers in the range 1–7 (Monday = 1 through Sunday = 7) → 400.

4. **Duplicate submission:** A session already marked `IsSubmitted = true` returns
   **409 Conflict** — not 400. Re-submission is blocked entirely.

5. **Auto-present for omitted children:** Bulk submission auto-creates `AttendanceRecord`
   rows with `Status = Present` for all children assigned to the teacher who are not
   explicitly listed in the request.

6. **Unassigned child IDs rejected:** `ChildProfileId` values in the submission request that
   are not in the teacher's active `TeacherChildAssignments` → 400.

7. **Teacher edit window: 1 hour.** Teacher can edit their own submitted record only while
   `AttendanceSessions.SubmittedAt + 1 hour > GETUTCDATE()`. Outside window → 403.

8. **FamilyAdmin override:** FamilyAdmin can edit any attendance record at any time.
   Every FamilyAdmin edit writes an `AuditLogs` row with `OldValues` and `NewValues` JSON.

9. **Parent push alerts:** FCM push sent to the child's parent(s) when a child's
   `AttendanceRecord.Status` is `Absent` or `Late`. Delivered inline during `submit`.

10. **Comment template category cap:** Maximum **20 family-scoped templates per category**.
    Exceeding the cap → 422 Unprocessable Entity.

11. **System template immutability:** `CommentTemplates` with `IsSystem = 1` are read-only.
    Any update or delete attempt → 403 Forbidden.

12. **Template category values:** Exactly three valid categories: `Attendance`, `Feedback`,
    `Homework`. Any other value → 400.

---

### 5.5 Flow Summaries

#### Flow 1 — Create Attendance Session

```
Trigger       : Teacher creates a session (class/tuition) from the attendance screen
→ API call    : POST /api/v1/families/{familyId}/attendance/sessions
                { SessionName, SubjectName, BatchName?, ScheduledDate, StartTime,
                  EndTime?, IsRecurring, RecurringDays? }
→ Validation  : TeacherProfile existence check → 403 if absent;
                ScheduledDate window (−7d / +30d) → 400;
                EndTime > StartTime → 400; RecurringDays format → 400
→ DB operation: INSERT AttendanceSessions (TeacherProfileId=caller's TeacherProfile.TeacherProfileId,
                SubjectName, BatchName, ScheduledDate, StartTime, EndTime,
                IsSubmitted=0, RecurringDays as JSON string).
→ Response    : 201 ApiResponse<AttendanceSessionDto>
→ Side effect : None.
```

#### Flow 2 — Submit Attendance (Bulk)

```
Trigger       : Teacher marks all children present/absent/late at end of session
→ API call    : POST /families/{familyId}/attendance/sessions/{sessionId}/submit
                { Records: [ { ChildProfileId, Status, TeacherComment } ] }
→ Validation  : Session not already submitted → 409 if IsSubmitted=true;
                all ChildProfileIds in teacher's active assignments → 400 if any unassigned;
                no duplicate ChildProfileIds in request → 400;
                AttendanceStatus valid → 400; TeacherComment ≤ 500 chars → 400
→ DB operation: INSERT AttendanceRecord per supplied child (with given Status);
                INSERT AttendanceRecord with Status=Present for assigned children omitted;
                UPDATE AttendanceSessions SET IsSubmitted=true, SubmittedAt=GETUTCDATE().
→ Response    : 200 ApiResponse<AttendanceSessionDto>
→ Side effect : FCM push to parents of children with Status=Absent or Status=Late.
```

#### Flow 3 — Teacher Corrects a Record (Within Window)

```
Trigger       : Teacher realises an error within 1 hour of submitting
→ API call    : PUT /families/{familyId}/attendance/sessions/{sessionId}/records/{recordId}
                { Status, TeacherComment?, CommentTemplateId? }
→ Validation  : Record belongs to teacher's own session → 403 if not;
                GETUTCDATE() < SubmittedAt + 1hr → 403 if outside window;
                AttendanceStatus valid → 400
→ DB operation: UPDATE AttendanceRecords SET Status, TeacherComment, UpdatedAt.
→ Response    : 200 ApiResponse<AttendanceRecordDto>
→ Side effect : FCM push re-sent to parents IF old status ≠ new status AND new status is
                Absent or Late. No push if status unchanged or changed to Present/LeftEarly.
```

#### Flow 4 — FamilyAdmin Overrides a Record

```
Trigger       : FamilyAdmin corrects an attendance error (any time, any teacher)
→ API call    : PUT /families/{familyId}/attendance/sessions/{sessionId}/records/{recordId}
                { Status, TeacherComment?, CommentTemplateId? }
→ Validation  : Role = FamilyAdmin → no time-window restriction
→ DB operation: Snapshot OldValues JSON; UPDATE AttendanceRecords;
                INSERT AuditLogs (EntityType='AttendanceRecord', OldValues, NewValues,
                ChangedByUserId=caller).
→ Response    : 200 ApiResponse<AttendanceRecordDto>
→ Side effect : FCM push re-sent to parents IF old status ≠ new status AND new status is
                Absent or Late (same logic as teacher correction).
```

---

### 5.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens are from the DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Role | Planned path |
|---|---|---|
| Session list (today) | Teacher | `lib/features/teacher/screens/attendance_sessions_screen.dart` |
| Mark attendance | Teacher | `lib/features/teacher/screens/mark_attendance_screen.dart` |
| Attendance history (child) | Parent | `lib/features/parent/screens/child_attendance_screen.dart` |
| Comment template picker | Teacher | `lib/features/attendance/widgets/comment_template_picker.dart` |

**Known constraints:**
- Folder: `lib/features/attendance/` and `lib/features/teacher/`
- Teacher role renders mark-attendance actions; Parent/Child render read-only history.
- Teacher offline queue: `sqflite` (`offline_queue_service.dart`) stores pending
  attendance submissions when network is unavailable — submitted when online.
- `CommentTemplateId` should be sent alongside free-text comment when a template is selected.
- Demo mode: `MockDataService` must return non-empty session lists and child lists.
- All role-conditional buttons check `AuthNotifier.currentRole` before rendering.

---

### 5.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `TeacherProfiles` table (Phase 04) | Active `TeacherProfile` for calling user | Session create and submit gated on TeacherProfileId |
| `TeacherChildAssignments` table (Phase 04) | Active assignments for the teacher | Submit validates assigned child IDs; Parent listing filters by teacher-child links |
| `ChildProfiles` table (Phase 04) | Child records for the family | Auto-present creation on submit; child history endpoint |
| `IPushNotificationService` / FCM (Phase 02) | FCM token on parent `Users` row | Parent push alerts on Absent/Late status |
| `AuditLogs` table (Phase 06 — this module) | Shared audit store | FamilyAdmin edits write here; also used by Phase 20 |
| `NotificationPreferences` (Phase 16) | [VERIFY] whether attendance push respects quiet hours | Attendance alerts use inline FCM — quiet-hours check not confirmed |
| `CustomAttendanceStatuses` (Phase 20) | Family-configured status extensions | Status list exposed via `GET /attendance/statuses` — see Section 11 |

---

## 6. Task & Routine System

### 6.1 Module Purpose

Manages the full task lifecycle: task creation by parents/admins, daily task listing for
children, completion submission with optional photo proof, parent review, coin awarding,
and admin-managed task templates. S3 presigned URLs handle photo uploads client-side.

Implemented across two phases, both in `TasksController`:
- **Phase 08** — Task CRUD, date-based listing, admin task-template catalog
- **Phase 09** — Task completion submission, parent review, verification queue,
  batch approval, S3 upload URL generation

Coin award on approval was refactored in **Phase 10** to route through `ICoinService`
(writes `CoinTransactions` ledger entry). Phase 09 approval logic is current state only
when read together with Phase 10 updates.

---

### 6.2 Key APIs

---

#### GET /api/v1/families/{familyId}/tasks

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — full family task list; Child — own tasks plus family-wide tasks (`ChildProfileId = NULL`). **Teacher → 403** (excluded by service layer). |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `date` | `date` | Filter tasks active on this date (checks `RecurringDays` and `ActiveFromDate`/`ActiveToDate`) |
| `childId` | `Guid` | Optional — filter to one child's tasks |
| `page`, `pageSize` | `int` | Standard pagination |

**Response DTO — `ApiResponse<IReadOnlyCollection<TaskItemDto>>`** — not paginated. `TaskItemDto` shape:

| Field | Type | Notes |
|---|---|---|
| `TaskId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `ChildProfileId` | `Guid?` | NULL = family-wide task |
| `TaskName` | `string` | — |
| `Instructions` | `string?` | — |
| `IconCode` | `string?` | — |
| `TimeBlock` | `TaskTimeBlock` | Enum — Morning=1, School=2, Evening=3, Night=4 |
| `DurationMinutes` | `int` | 5–120 |
| `CoinValue` | `int` | 5–200 |
| `IsPhotoRequired` | `bool` | — |
| `PillarTag` | `string?` | Study\|Cleanliness\|Discipline\|ScreenControl\|Responsibility |
| `IsRecurring` | `bool` | — |
| `RecurringDays` | `IReadOnlyCollection<int>` | 1–7, Mon–Sun |
| `ActiveFromDate` | `DateOnly` | — |
| `ActiveToDate` | `DateOnly?` | — |
| `IsActive` | `bool` | — |

**Business rules:**
- Date filter evaluates recurring tasks against `RecurringDays` (day-of-week match) and
  date-range tasks against `ActiveFromDate`/`ActiveToDate`.
- Child JWT: `childProfileId` claim limits results to that child's tasks plus tasks where
  `ChildProfileId IS NULL` (family-wide tasks).
- `WHERE IsDeleted = 0` and `WHERE IsActive = 1` enforced.

---

#### POST /api/v1/families/{familyId}/tasks

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `CreateTaskRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `TaskName` | `string` | YES | Min 2, max 200 chars |
| `ChildProfileId` | `Guid?` | NO | NULL = family-wide task visible to all children |
| `Instructions` | `string?` | NO | Max 1000 chars |
| `IconCode` | `string?` | NO | Max 50 chars — free-form icon code (no enum constraint) |
| `TimeBlock` | `TaskTimeBlock` | YES | Morning=1, Evening=3, Night=4 allowed. School=2 is **not allowed** → 400 |
| `DurationMinutes` | `int` | YES | 5–120 inclusive |
| `CoinValue` | `int` | YES | 5–200 inclusive |
| `IsPhotoRequired` | `bool` | YES | If true, `PhotoUrl` required in completion submission |
| `PillarTag` | `string?` | NO | Study\|Cleanliness\|Discipline\|ScreenControl\|Responsibility (case-insensitive) |
| `IsRecurring` | `bool` | YES | Defaults to `true` |
| `RecurringDays` | `int[]?` | NO | Required when `IsRecurring = true`; values 1–7, no duplicates |
| `ActiveFromDate` | `DateOnly` | YES | Must be within 30 days past and 1 year future |

**Note:** No `ActiveToDate` field in `CreateTaskRequest`. `ActiveToDate` is an entity field updated separately.

**Response DTO — `ApiResponse<TaskItemDto>`:** Returns 201. Same shape as listed above.

**Error cases:**

| Condition | Status |
|---|---|
| `TimeBlock` = School | 400 |
| `CoinValue` outside 5–200 | 400 |
| `DurationMinutes` outside 5–120 | 400 |
| Invalid or duplicate `RecurringDays` | 400 |
| Invalid `PillarTag` | 400 |
| `ActiveToDate` < `ActiveFromDate` | 400 |

---

#### PUT /api/v1/families/{familyId}/tasks/{taskId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `UpdateTaskRequest`:** Identical fields to `CreateTaskRequest` — same validation rules apply. All fields are required on update (same DTO class, no partial-update semantics).

**Business rules:** Same validation rules as create. Task must belong to `familyId` in route.

**Error cases:** 400 (validation), 403, 404.

---

#### DELETE /api/v1/families/{familyId}/tasks/{taskId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Business rules:**
- Soft delete: `IsDeleted = 1`, `DeletedAt = GETUTCDATE()`, `IsActive = false`.
- No cascading delete of existing `TaskCompletions` — historical records preserved.

**Error cases:** 403, 404.

---

#### GET /api/v1/admin/task-templates

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `category` | `string` | Filter by `TemplateCategory` |
| `ageGroup` | `string` | Filter by `AgeGroup` |

**Response DTO — `ApiResponse<List<TaskTemplateDto>>`:** System task template list.

**Business rules:**
- System templates are stored in `TaskItems` with `IsSystemTemplate = 1`, `FamilyId = NULL`,
  `ChildProfileId = NULL`. They share the same table as family tasks.
- Two extra columns added for template filtering: `TemplateCategory` and `AgeGroup`.

---

#### POST /api/v1/admin/task-templates

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Request DTO — `CreateTaskTemplateRequest`:**

| Field | Type | Required | Notes |
|---|---|---|---|
| Same task fields as `CreateTaskRequest` | — | YES | `TaskName`, `TimeBlock`, `DurationMinutes`, `CoinValue`, `IsPhotoRequired`, `IsRecurring`, `RecurringDays`, `ActiveFromDate` |
| `Category` | `string` | YES | Template category — free-form string |
| `AgeGroup` | `string?` | NO | Target age group — free-form string |
| `Instructions?`, `IconCode?`, `PillarTag?` | `string?` | NO | Optional fields as in regular task |

**Response DTO — `ApiResponse<TaskTemplateDto>`:** Returns 201 on success.

---

#### GET /api/v1/families/{familyId}/tasks/completions

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — all completions for family; Child — own completions only (**Teacher → 403**) |

**Request query params:** `page`, `pageSize` (standard pagination).

**Response DTO — `ApiResponse<PaginatedList<TaskCompletionDto>>`:**

| Field | Type | Notes |
|---|---|---|
| `CompletionId` | `Guid` | — |
| `TaskId` | `Guid` | — |
| `ChildProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `ScheduledDate` | `DateOnly` | — |
| `TaskName` | `string` | Denormalized from `TaskItems.TaskName` |
| `ChildName` | `string` | From `ChildProfile → FamilyMember.DisplayName` |
| `Status` | `TaskStatus` | Pending=1, InProgress=2, SubmittedForReview=3, Approved=4, Flagged=5, Missed=6 |
| `PhotoUrl` | `string?` | S3 URL |
| `SubmittedAt` | `DateTime?` | UTC — null until submitted |
| `ReviewedByUserId` | `Guid?` | — |
| `ReviewedAt` | `DateTime?` | — |
| `ReviewNote` | `string?` | Set on Flag |
| `CoinsAwarded` | `int` | 0 until approved |

---

#### POST /api/v1/families/{familyId}/tasks/{taskId}/completions

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Child only (own `childProfileId` from JWT) |

**Request DTO — `SubmitTaskCompletionRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `ScheduledDate` | `DateOnly` | YES | The date this task is being completed for |
| `PhotoUrl` | `string?` | Conditional | **Required** when `TaskItem.IsPhotoRequired = true` |

**Response DTO — `ApiResponse<TaskCompletionDto>`:** Returns 201 on success.

**Business rules:**
- One completion per `(TaskId, ChildProfileId, ScheduledDate)` — duplicate → **409 Conflict**.
- Child can only submit for their own `childProfileId` (JWT claim). Family-wide tasks (`ChildProfileId IS NULL`) are also gated — child can only submit for themselves.
- `PhotoUrl` is required when `TaskItem.IsPhotoRequired = true` → 400 if absent.
- New completion created with **`Status = SubmittedForReview`** (not `Pending`).
- Push notification sent to parent(s) of the child on submission.

**Error cases:**

| Condition | Status |
|---|---|
| Duplicate `(TaskId, ChildProfileId, ScheduledDate)` | 409 |
| `PhotoUrl` absent when `IsPhotoRequired = true` | 400 |
| Child submitting for another child's task | 403 |

---

#### PUT /api/v1/families/{familyId}/tasks/completions/{completionId}/review

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | **Parent only.** FamilyAdmin → 403 (confirmed from `EnsureParentAsync` in service). |

**Request DTO — `ReviewTaskCompletionRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Status` | `TaskStatus` | YES | Must be `Approved` (=4) or `Flagged` (=5) — enum value, not a string |
| `ReviewNote` | `string?` | Conditional | **Required** when `Status = Flagged`; min 5, max 500 chars |

**Response DTO — `ApiResponse<TaskCompletionDto>`.**

**Business rules (current state — Phase 09 + Phase 10 updates):**
- **Only `SubmittedForReview` completions can be reviewed.** Any other status → **409 Conflict**.
- **Approve (`Status=4`):** Sets `Status = Approved`, `CoinsAwarded = TaskItem.CoinValue`, `ReviewedAt`,
  `ReviewedByUserId`. Then calls `ICoinService` (Phase 10) which:
  - Inserts `CoinTransactions` row (`TransactionType = Earn`, `ReferenceType = TaskCompletion`,
    `ReferenceId = CompletionId`)
  - Updates `ChildProfile.CoinBalance += CoinValue` and `TotalCoinsEarned += CoinValue`
    using optimistic concurrency (RowVersion) → 409 on concurrency conflict
  - Evaluates level thresholds from `TotalCoinsEarned`
  - Increments matching pillar score (capped at 20)
  - Awards streak freeze at each 10-day streak milestone (max 2 freezes)
- **Flag (`Status=5`):** Sets `Status = Flagged`, `ReviewNote` stored, `CoinsAwarded = 0`. No coin ledger entry.
- Push notification sent to child on both Approve and Flag (different messages).

**Error cases:**

| Condition | Status |
|---|---|
| `Status = Flagged` with no `ReviewNote` or < 5 chars | 400 |
| `ReviewNote` > 500 chars | 400 |
| Completion not in `SubmittedForReview` status | 409 |
| Completion not found / wrong family | 404 |
| Concurrency conflict on coin balance | 409 |
| Role not Parent (including FamilyAdmin) | 403 |

---

#### GET /api/v1/families/{familyId}/tasks/verification-queue

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | **Parent only.** FamilyAdmin → 403. |

**Response DTO — `ApiResponse<IReadOnlyCollection<TaskCompletionDto>>`** — not paginated.
Completions with `Status = SubmittedForReview` for the family.

**Business rules:** Returns only `Status = SubmittedForReview` completions (not `Pending`). No pagination params.

---

#### POST /api/v1/families/{familyId}/tasks/verification-queue/approve-all

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | **Parent only.** FamilyAdmin → 403. |

**Request:** No body — approves all `Status = SubmittedForReview` completions for the family.

**Response DTO — `ApiResponse<BatchApproveResultDto>`:**

| Field | Type | Notes |
|---|---|---|
| `ApprovedCount` | `int` | Number of completions approved. No other fields. |

**Business rules:**
- Approves all `SubmittedForReview` completions for the family in a loop.
- Each completion goes through the same coin-award path as single review (`ICoinService` called per completion).
- Push notification sent to each child on approval.
- Optimistic concurrency (RowVersion) applied per child balance update. Any individual failure is not retried — partial success is possible (approved up to the failing item).

---

#### POST /api/v1/families/{familyId}/tasks/completions/upload-url

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Child (uploading for own completion) |

**Request DTO — `TaskCompletionUploadUrlRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `TaskId` | `Guid` | YES | The task this photo is for |

**Response DTO — `ApiResponse<TaskCompletionUploadUrlDto>`:**

| Field | Type | Notes |
|---|---|---|
| `TaskId` | `Guid` | Echoed back |
| `UploadUrl` | `string` | AWS S3 presigned PUT URL — valid 15 minutes |
| `ObjectKey` | `string` | S3 object key — use this as `PhotoUrl` in the subsequent completion submit |
| `ExpiresAtUtc` | `DateTime` | UTC expiry of the presigned URL |

**Business rules:**
- Presigned URL TTL: **15 minutes** (`ExpiresAtUtc`).
- S3 key format: `family/{familyId}/tasks/{taskId}/{GUID}.jpg`
- Bucket and region resolved from `appsettings.json Aws` section — not hardcoded.
- Client flow: call this endpoint → upload photo directly to S3 via `UploadUrl` →
  include `ObjectKey` (as the `PhotoUrl` value) in the subsequent `POST /tasks/{taskId}/completions` call.

---

### 6.3 DB Tables

#### TaskItems
- **Scripts:** `019_CreateTaskItems.sql` · `020_CreateTaskItemIndexes.sql` (Phase 08)
- **Note:** PK is `TaskId`. `ActiveFromDate`/`ActiveToDate` are `DATE`. `RecurringDays` is NOT NULL with default. Uses `SYSUTCDATETIME()`. `IsPhotoRequired` is the confirmed column name (resolves Drift Entry 004).

| Column | Type | Notes |
|---|---|---|
| `TaskId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL — FK → Families.FamilyId; NULL for system templates |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NULL — FK → ChildProfiles.ChildProfileId; NULL for family-wide tasks |
| `CreatedByUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `TaskName` | `NVARCHAR(200)` | NOT NULL |
| `Instructions` | `NVARCHAR(1000)` | NULL |
| `IconCode` | `NVARCHAR(50)` | NULL — free-form icon code |
| `TimeBlock` | `INT` | NOT NULL — Morning=1, School=2, Evening=3, Night=4 |
| `DurationMinutes` | `INT` | NOT NULL, DEFAULT 15 |
| `CoinValue` | `INT` | NOT NULL, DEFAULT 10 |
| `IsPhotoRequired` | `BIT` | NOT NULL, DEFAULT 0 |
| `PillarTag` | `NVARCHAR(50)` | NULL — CHECK: Study\|Cleanliness\|Discipline\|ScreenControl\|Responsibility |
| `IsRecurring` | `BIT` | NOT NULL, DEFAULT 1 |
| `RecurringDays` | `NVARCHAR(50)` | NOT NULL, DEFAULT `[1,2,3,4,5,6,7]` — JSON array; CHECK: ISJSON=1 |
| `ActiveFromDate` | `DATE` | NOT NULL, DEFAULT CAST(SYSUTCDATETIME() AS DATE) |
| `ActiveToDate` | `DATE` | NULL — CHECK: ActiveToDate > ActiveFromDate when not null |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 |
| `IsSystemTemplate` | `BIT` | NOT NULL, DEFAULT 0 |
| `TemplateCategory` | `NVARCHAR(50)` | NULL — system templates only |
| `AgeGroup` | `NVARCHAR(50)` | NULL — system templates only |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**DB CHECK constraints:**
- `CK_TaskItems_RecurringDaysJson` — `ISJSON(RecurringDays) = 1`
- `CK_TaskItems_ActiveDateRange` — `ActiveToDate IS NULL OR ActiveToDate > ActiveFromDate`
- `CK_TaskItems_PillarTag` — `PillarTag IS NULL OR PillarTag IN ('Study','Cleanliness','Discipline','ScreenControl','Responsibility')`
- `CK_TaskItems_TemplateShape` — family tasks must have `FamilyId IS NOT NULL`; system templates must have `FamilyId IS NULL AND ChildProfileId IS NULL AND TemplateCategory IS NOT NULL`

**Index:** `IX_TaskItems_FamilyId_ChildProfileId_IsActive` — (FamilyId, ChildProfileId, IsActive) WHERE IsDeleted=0

#### TaskCompletions
- **Script:** `021_CreateTaskCompletions.sql` (Phase 09)
- **Note:** PK is `CompletionId`. `ScheduledDate` is `DATE`. Status DEFAULT 1 (Pending). Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `CompletionId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `TaskId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → TaskItems.TaskId |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId — denormalized for row-level security |
| `ScheduledDate` | `DATE` | NOT NULL |
| `Status` | `INT` | NOT NULL, DEFAULT 1 — TaskStatus enum |
| `PhotoUrl` | `NVARCHAR(500)` | NULL — S3 object key; required when `IsPhotoRequired = true` |
| `SubmittedAt` | `DATETIME2` | NULL — set when child submits |
| `ReviewedByUserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId |
| `ReviewedAt` | `DATETIME2` | NULL until reviewed |
| `ReviewNote` | `NVARCHAR(500)` | NULL — required on Flag; min 5 chars |
| `CoinsAwarded` | `INT` | NOT NULL, DEFAULT 0 — snapshotted from `TaskItem.CoinValue` on Approve |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Index:** `IX_TaskCompletions_Task_Child_Date` — UNIQUE (TaskId, ChildProfileId, ScheduledDate) WHERE IsDeleted=0

**TaskTimeBlock enum (confirmed from `TaskTimeBlock.cs`):**

| Int | Name | Allowed on create? |
|---|---|---|
| 1 | Morning | YES |
| 2 | School | **NO** → 400 |
| 3 | Evening | YES |
| 4 | Night | YES |

**TaskStatus enum (confirmed from `TaskStatus.cs`):**

| Int | Name | Set when |
|---|---|---|
| 1 | Pending | Task row exists but child hasn't submitted yet |
| 2 | InProgress | Child started but not yet submitted |
| 3 | SubmittedForReview | **Child submits completion** — this is the reviewable status |
| 4 | Approved | Parent approves |
| 5 | Flagged | Parent flags with note |
| 6 | Missed | Task date passed without submission |

---

### 6.4 Business Rules

1. **TimeBlock restriction:** `TimeBlock = School` (int value 2) cannot be assigned to a family task → 400.
   Allowed values: Morning=1, Evening=3, Night=4. School=2 is reserved for attendance sessions.

2. **CoinValue range:** 5–200 inclusive → 400 outside range.

3. **DurationMinutes range:** 5–120 inclusive → 400 outside range.

4. **Photo proof requirement:** When `TaskItem.IsPhotoRequired = true`, the `PhotoUrl` field
   is required in the completion submission. Missing photo → 400.
   Upload flow: client calls `POST /completions/upload-url` to get presigned S3 URL → uploads
   directly to S3 → includes returned `PhotoUrl` in the completion submit call.

5. **Completion uniqueness:** One `TaskCompletion` per `(TaskId, ChildProfileId, ScheduledDate)`.
   Duplicate submit → **409 Conflict**.

5b. **Completion status on submit:** Child submission sets `Status = SubmittedForReview` (enum value 3),
    not `Pending` (1). Only completions in `SubmittedForReview` can be reviewed. Attempting to review
    a Pending, Approved, or Flagged completion → **409 Conflict**.

6. **Review role gate:** Only **Parent** role may review completions via `PUT /completions/{id}/review`.
   FamilyAdmin → 403. Confirmed from `EnsureParentAsync` in TaskService. Same applies to verification
   queue (`GET /verification-queue`) and batch approve (`POST /approve-all`).

7. **Flag note requirement:** `ReviewNote` is mandatory when `Action = Flag`.
   Min 5 / max 500 characters → 400 if absent or out of range.

8. **Coin award on approval (Phase 10 current state):**
   - `CoinsAwarded` snapshotted from `TaskItem.CoinValue`.
   - `ICoinService` writes `CoinTransactions` ledger entry (`TransactionType = Earn`,
     `ReferenceType = TaskCompletion`).
   - `ChildProfile.CoinBalance` and `TotalCoinsEarned` incremented with optimistic
     concurrency (RowVersion) → 409 on conflict.

9. **Level threshold evaluation (Phase 10):** Applied to `TotalCoinsEarned` after every
   coin award. Thresholds: 0–499 = Level 1, 500–1,499 = Level 2, 1,500–2,999 = Level 3,
   3,000–4,999 = Level 4, 5,000+ = Level 5.

10. **Pillar score on approval (Phase 10):** If `TaskItem.PillarTag` is set, the matching
    pillar score on `ChildProfiles` is incremented. Each pillar score is capped at 20.
    Valid `PillarTag` values: `Study`, `Cleanliness`, `Discipline`, `ScreenControl`, `Responsibility`
    (case-insensitive; enforced by FluentValidation and DB CHECK constraint).

11. **Streak freeze award (Phase 10):** A new streak freeze is awarded at each 10-day
    consecutive streak milestone. Maximum 2 freezes held at any time.

12. **System task templates:** Stored in `TaskItems` with `IsSystemTemplate = 1`,
    `FamilyId = NULL`, `ChildProfileId = NULL`. Only SuperAdmin can create/manage via
    `/admin/task-templates`. Not visible to family users in the family task list.

13. **S3 presigned URL TTL:** 15 minutes. Key format:
    `family/{familyId}/tasks/{taskId}/{GUID}.jpg`. Bucket and region from `appsettings.json`.

---

### 6.5 Flow Summaries

#### Flow 1 — Create Task

```
Trigger       : Parent creates a daily task for a child
→ API call    : POST /api/v1/families/{familyId}/tasks
                { TaskName, ChildProfileId, TimeBlock, DurationMinutes, CoinValue,
                  IsPhotoRequired, IsRecurring, RecurringDays, ActiveFromDate }
→ Validation  : Role gate (Parent/FamilyAdmin); TimeBlock ≠ School;
                CoinValue 5–200; DurationMinutes 5–120; RecurringDays format
→ DB operation: INSERT TaskItems (FamilyId, ChildProfileId, IsActive=true, IsSystemTemplate=false).
→ Response    : 201 ApiResponse<TaskItemDto>
→ Side effect : None.
```

#### Flow 2 — Child Submits Completion (With Photo)

```
Trigger       : Child marks a task done and uploads proof photo
→ API call 1  : POST /families/{familyId}/tasks/completions/upload-url { TaskId }
→ Response 1  : { UploadUrl (presigned S3 PUT, 15-min TTL), PhotoUrl (final S3 key URL) }
→ Action      : Client uploads photo directly to S3 via UploadUrl
→ API call 2  : POST /families/{familyId}/tasks/{taskId}/completions
                { ScheduledDate, PhotoUrl }
→ Validation  : childProfileId from JWT; duplicate check → 409;
                PhotoUrl required when IsPhotoRequired=true → 400 if absent
→ DB operation: INSERT TaskCompletions (Status=SubmittedForReview, PhotoUrl, SubmittedAt=SYSUTCDATETIME()).
→ Response    : 201 ApiResponse<TaskCompletionDto>
→ Side effect : Push notification sent to parent(s) of the child.
```

#### Flow 3 — Parent Approves Completion

```
Trigger       : Parent reviews pending completion in verification queue
→ API call    : PUT /families/{familyId}/tasks/completions/{completionId}/review
                { Status: 4 }   ← TaskStatus.Approved
→ Validation  : Role = Parent (FamilyAdmin → 403); completion Status = SubmittedForReview → 409 if not
→ DB operation: UPDATE TaskCompletions SET Status=Approved, CoinsAwarded=TaskItem.CoinValue,
                  ReviewedAt=GETUTCDATE(), ReviewedByUserId;
                INSERT CoinTransactions (TransactionType=Earn, ReferenceType=TaskCompletion);
                UPDATE ChildProfiles SET CoinBalance+=CoinValue, TotalCoinsEarned+=CoinValue
                  (optimistic concurrency via RowVersion → 409 on conflict);
                Evaluate level; increment pillar score (capped 20); check streak milestone.
→ Response    : 200 ApiResponse<TaskCompletionDto>
→ Side effect : Push notification sent to child (approval confirmed).
```

#### Flow 4 — Parent Flags Completion

```
Trigger       : Parent rejects a task completion (wrong photo, incomplete task)
→ API call    : PUT /families/{familyId}/tasks/completions/{completionId}/review
                { Status: 5, ReviewNote: "..." }   ← TaskStatus.Flagged
→ Validation  : Role = Parent (FamilyAdmin → 403); ReviewNote required; 5–500 chars → 400 if missing or out of range
→ DB operation: UPDATE TaskCompletions SET Status=Flagged, ReviewNote, CoinsAwarded=0,
                  ReviewedAt, ReviewedByUserId. No coin ledger entry.
→ Response    : 200 ApiResponse<TaskCompletionDto>
→ Side effect : Push notification sent to child (flagged with note).
```

---

### 6.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens are from the DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Role | Planned path |
|---|---|---|
| Task list (child's today) | Child | `lib/features/child/screens/child_home_screen.dart` |
| Task create/edit | Parent | `lib/features/tasks/screens/create_task_screen.dart` |
| Verification queue | Parent | `lib/features/tasks/screens/verification_queue_screen.dart` |
| Task completion submit + photo | Child | `lib/features/tasks/screens/submit_completion_screen.dart` |
| Admin template catalog | SuperAdmin | `lib/features/admin/screens/task_templates_screen.dart` |

**Critical integration notes:**
- Review request uses `Status` (int enum) — **not** a string `Action` field. Send `{ "status": 4 }` for Approve, `{ "status": 5, "reviewNote": "..." }` for Flag.
- Upload flow: call `POST /completions/upload-url` → upload to `UploadUrl` → submit `ObjectKey` as `PhotoUrl` in completion request.
- Verification queue returns `SubmittedForReview` completions — not `Pending`. Filter/display accordingly.
- Task `RecurringDays` defaults to `[1,2,3,4,5,6,7]` (all days) when `IsRecurring = true`.

**Confirmed constraints:**
- Folder: `lib/features/tasks/` and `lib/features/child/`
- Child role renders submit action; Parent role renders review/approve/flag actions.
- No `setState` for API data; use Riverpod `StateNotifier`.
- Demo mode: `MockDataService` must return non-empty task lists and completions.

---

### 6.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `ChildProfiles` table (Phase 04) | `CoinBalance`, `TotalCoinsEarned`, `RowVersion`, pillar score columns | Coin award on approval mutates child profile with optimistic concurrency |
| `ICoinService` (Phase 10) | `EarnCoinsForTaskCompletionAsync` | Approval routes through coin ledger service — not direct DB write |
| `CoinTransactions` table (Phase 10) | Append-only ledger | Written by coin service on every approval |
| `IPushNotificationService` / FCM (Phase 02) | Parent and child FCM tokens | Push alerts on submission, approval, and flag |
| `S3StorageService` / AWS S3 (Phase 09) | Presigned URL generation | Photo upload URLs for `IsPhotoRequired` tasks |
| `TeacherChildAssignments` (Phase 04) | Not used by task completion endpoints | Teacher role → 403 on all task completion endpoints. Teacher sees child tasks via GET /tasks (Parent/Child only — Teacher also excluded). |

---

## 7. Teacher Feedback

### 7.1 Module Purpose

Manages structured feedback from teachers (and elders) to parents about a child.
Covers submission, listing, editing within a 24-hour window, soft delete, parent
acknowledgement with optional response text, and child-level feedback summaries.

Implemented across two phases in `FeedbackController`:
- **Phase 11** — Feedback CRUD, child feedback summary, parent push on submission
- **Phase 12** — Acknowledgement endpoint, teacher push on acknowledgement,
  `UnacknowledgedFeedbackCount` added to Family Dashboard (documented in Section 4)

**Elder note (Phase 11 implementation):** Elder users may submit `Appreciation` type
feedback. Because `TeacherFeedback` stores `TeacherProfileId`, the service auto-creates
a `TeacherProfile` row (`TeacherType = Other`) for the elder's `FamilyMember` on first
submission if none exists.

---

### 7.2 Key APIs

---

#### POST /api/v1/families/{familyId}/feedback

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (assigned children only); Elder (FeedbackType = Appreciation only) |

**Request DTO — `SubmitFeedbackRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `ChildProfileId` | `Guid` | YES | Must be in caller's active `TeacherChildAssignments` |
| `FeedbackType` | `int` | YES | Valid `FeedbackType` enum value |
| `Severity` | `int` | Conditional | Required for `Complaint` and `UrgentEscalation` types |
| `Subject` | `string` | NO | Max 300 characters |
| `Message` | `string` | YES | 5–2,000 characters |
| `SessionId` | `Guid?` | NO | Optional link to an `AttendanceSessions` row |
| `CommentTemplateId` | `Guid?` | NO | Optional link to a `CommentTemplates` row |
| `WeeklySummaryJson` | `string?` | Conditional | Required when `FeedbackType = WeeklySummary` (=6) |

**WeeklySummaryJson structure (when FeedbackType = WeeklySummary):**

| Field | Type | Constraint |
|---|---|---|
| `attendanceRate` | `int` | 0–100 |
| `homeworkRate` | `int` | 0–100 |
| `standoutMoment` | `string` | Required, non-empty |
| `focusArea` | `string` | Required, non-empty |

**Response DTO — `ApiResponse<FeedbackDto>`:** Returns 201. Full `FeedbackDto` shape:

| Field | Type | Notes |
|---|---|---|
| `FeedbackId` | `Guid` | — |
| `TeacherProfileId` | `Guid` | — |
| `ChildProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `SessionId` | `Guid?` | — |
| `FeedbackType` | `FeedbackType` | Enum value |
| `Severity` | `FeedbackSeverity?` | Null unless Complaint or UrgentEscalation |
| `Subject` | `string?` | — |
| `Message` | `string` | — |
| `CommentTemplateId` | `Guid?` | — |
| `CommentTemplateText` | `string?` | Denormalized template text for display |
| `WeeklySummaryJson` | `string?` | Raw JSON |
| `IsAcknowledged` | `bool` | — |
| `AcknowledgedAt` | `DateTime?` | UTC |
| `AcknowledgedByUserId` | `Guid?` | — |
| `ParentResponseText` | `string?` | — |
| `ResolutionStatus` | `string` | `Open` \| `Acknowledged` \| `Resolved` |
| `IsEditable` | `bool` | Computed — true when `< 24 hours` since `CreatedAt` |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |
| `TeacherName` | `string` | From `TeacherProfile → FamilyMember.DisplayName` |
| `ChildName` | `string` | From `ChildProfile → FamilyMember.DisplayName` |

**Business rules:**
- Teacher restricted to children in their active `TeacherChildAssignments` → 403.
- `Severity` required for `Complaint` and `UrgentEscalation` types → 400 if absent.
- Parent push notification sent inline on submission.
- `UrgentEscalation` uses a dedicated urgent push title/body and bypasses later
  batching/quiet-hours handling — delivered immediately.
- Elder auto-profile: if submitter is Elder role with no `TeacherProfile`, one is created
  on demand (`TeacherType = Other`) so feedback can be stored.

**Error cases:**

| Condition | Status |
|---|---|
| ChildProfileId not in teacher's assignments | 403 |
| Severity absent for Complaint / UrgentEscalation | 400 |
| Message < 5 or > 2,000 chars | 400 |
| Subject > 300 chars | 400 |
| Invalid FeedbackType or Severity value | 400 |

---

#### GET /api/v1/families/{familyId}/feedback

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — all family feedback; Teacher — own submissions only |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `childId` | `Guid?` | Optional — filter by child |
| `type` | `FeedbackType?` | Optional — filter by feedback type enum value |
| `isAcknowledged` | `bool?` | Optional — filter by acknowledgement status |
| `page` | `int` | Default 1 |
| `pageSize` | `int` | Default 20 |

**Response DTO — `ApiResponse<PaginatedList<FeedbackDto>>`.**

**Business rules:**
- Teacher: filtered to `TeacherProfileId = caller's profile` only.
- Parent / FamilyAdmin: all feedback where `FamilyId = @currentFamilyId`.
- `WHERE IsDeleted = 0` enforced.

---

#### GET /api/v1/families/{familyId}/feedback/{feedbackId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin (any); Teacher (own submissions only) |

**Response DTO — `ApiResponse<FeedbackDto>`.**

**Error cases:** 403 (teacher accessing another's feedback), 404.

---

#### PUT /api/v1/families/{familyId}/feedback/{feedbackId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own feedback, within 24-hour edit window only) |

**Request DTO — `UpdateFeedbackRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Message` | `string` | YES | Same rules as submit: 5–2,000 chars |
| `Severity` | `FeedbackSeverity?` | NO | Valid enum value when provided |

**Note:** Subject and WeeklySummaryJson are **not** updatable. Only Message and Severity can be changed.

**Business rules:**
- Only the submitting teacher may update their own feedback.
- Edit window: **24 hours** from `CreatedAt`. Enforced via `IsEditable` computed DB column
  AND `CreatedAt` timestamp check in service. Outside window → 403.

**Error cases:**

| Condition | Status |
|---|---|
| Edit window expired (> 24 hrs) | 403 |
| Teacher editing another teacher's feedback | 403 |
| Validation failure | 400 |
| Not found | 404 |

---

#### DELETE /api/v1/families/{familyId}/feedback/{feedbackId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Teacher (own feedback, within 24-hour window only) |

**Response DTO:** `ApiResponse<bool>` — returns `true` on success.

**Business rules:**
- Delete window: **24 hours** from `CreatedAt`. Same window as edit → 403 outside.
- Soft delete: `IsDeleted = 1, DeletedAt = GETUTCDATE()`.
- Teacher cannot delete another teacher's feedback.

**Error cases:** 403 (window expired or wrong teacher), 404.

---

#### GET /api/v1/families/{familyId}/children/{childId}/feedback-summary

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin, Teacher (assigned children only) |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `periodDays` | `int` | Default 7 — number of days to look back |

**Response DTO — `ApiResponse<FeedbackSummaryDto>`:**

| Field | Type | Notes |
|---|---|---|
| `ChildProfileId` | `Guid` | — |
| `PeriodDays` | `int` | Echoed back from request |
| `TotalCount` | `int` | Total feedback entries for this child in period |
| `AppreciationCount` | `int` | Count of FeedbackType.Appreciation |
| `ComplaintCount` | `int` | Count of FeedbackType.Complaint |
| `ObservationCount` | `int` | Count of FeedbackType.Observation |
| `HomeworkIssueCount` | `int` | Count of FeedbackType.HomeworkIssue |
| `UrgentEscalationCount` | `int` | Count of FeedbackType.UrgentEscalation |
| `WeeklySummaryCount` | `int` | Count of FeedbackType.WeeklySummary |

**Business rules:**
- Counts `TeacherFeedback` rows for `ChildProfileId` within `periodDays` days.
- `WHERE IsDeleted = 0` enforced.
- No separate Teacher scope filter — all non-deleted feedback for the child in the period is counted.

---

#### POST /api/v1/families/{familyId}/feedback/{feedbackId}/acknowledge

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `AcknowledgeRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `ParentResponseText` | `string` | NO | Max 1,000 characters |

**Response DTO — `ApiResponse<FeedbackDto>`.**

**Business rules:**
- Sets `IsAcknowledged = true`, `AcknowledgedAt = GETUTCDATE()`,
  `AcknowledgedByUserId = current user`, `ParentResponseText`, `ResolutionStatus = 'Acknowledged'`.
- Feedback must belong to the requested `familyId` → 404 if not.
- **Idempotent:** second acknowledgement on already-acknowledged feedback returns the
  existing `FeedbackDto` with no error and does **not** re-send the teacher notification.
- **First acknowledgement only:** push notification sent to the feedback author
  (teacher or elder) via their `FcmToken`.

**Error cases:**

| Condition | Status |
|---|---|
| `ParentResponseText` > 1,000 chars | 400 |
| Feedback not in this family | 404 |
| Missing / invalid JWT | 401 |
| Insufficient role | 403 |

---

### 7.3 DB Tables

#### TeacherFeedback
- **Scripts:** `024_CreateTeacherFeedback.sql` · `025_CreateFeedbackIndexes.sql` (Phase 11)
- **Note:** PK is `FeedbackId`. `ResolutionStatus` is `NVARCHAR(20)` string (not INT). `IsEditable` is a DB computed column. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `FeedbackId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `TeacherProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → TeacherProfiles.TeacherProfileId (elder auto-profile stored here too) |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `SessionId` | `UNIQUEIDENTIFIER` | NULL, FK → AttendanceSessions.SessionId |
| `FeedbackType` | `INT` | NOT NULL — FeedbackType enum |
| `Severity` | `INT` | NULL — FeedbackSeverity enum; required for Complaint/UrgentEscalation |
| `Subject` | `NVARCHAR(300)` | NULL |
| `Message` | `NVARCHAR(2000)` | NOT NULL — 5–2,000 chars |
| `CommentTemplateId` | `UNIQUEIDENTIFIER` | NULL, FK → CommentTemplates.TemplateId |
| `WeeklySummaryJson` | `NVARCHAR(MAX)` | NULL — CHECK: ISJSON=1 when not null |
| `IsAcknowledged` | `BIT` | NOT NULL, DEFAULT 0 |
| `AcknowledgedAt` | `DATETIME2` | NULL |
| `AcknowledgedByUserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId |
| `ParentResponseText` | `NVARCHAR(1000)` | NULL |
| `ResolutionStatus` | `NVARCHAR(20)` | NOT NULL, DEFAULT `Open` — CHECK: `Open\|Acknowledged\|Resolved` |
| `IsEditable` | computed | `AS (CASE WHEN DATEDIFF(HOUR, CreatedAt, GETUTCDATE()) < 24 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END)` — not stored |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Index:** `IX_TeacherFeedback_FamilyId_ChildProfileId_FeedbackType` — (FamilyId, ChildProfileId, FeedbackType) WHERE IsDeleted=0

**FeedbackType enum (confirmed from `FeedbackType.cs`):**

| Int | Name | Notes |
|---|---|---|
| 1 | Appreciation | Only type allowed for Elder submitters |
| 2 | Complaint | Severity required |
| 3 | Observation | General note |
| 4 | HomeworkIssue | Homework-related feedback |
| 5 | UrgentEscalation | Severity required; urgent FCM push, bypasses quiet hours |
| 6 | WeeklySummary | Requires `WeeklySummaryJson` payload |

**FeedbackSeverity enum (confirmed from `FeedbackSeverity.cs`):**

| Int | Name |
|---|---|
| 1 | Low |
| 2 | Medium |
| 3 | Urgent |

---

### 7.4 Business Rules

1. **Teacher scope:** Teacher may only submit feedback for children in their active
   `TeacherChildAssignments`. Submitting for an unassigned child → 403.

2. **Elder scope:** Elder role may submit only `FeedbackType = Appreciation`. Service
   auto-creates `TeacherProfile` (`TeacherType = Other`) for the elder on first submission.

3. **Severity requirement:** `Severity` field is required when `FeedbackType` is
   `Complaint` (=2) or `UrgentEscalation` (=5) → 400 if absent.
   Valid values: Low=1, Medium=2, Urgent=3.

4. **Message constraints:** 5 characters minimum, 2,000 characters maximum → 400.

5. **Subject constraint:** Max 300 characters when provided → 400.

6. **Edit window: 24 hours.** Teacher may edit own feedback within 24 hours of `CreatedAt`
   only. Window enforced by both the `IsEditable` computed DB column and a `CreatedAt`
   timestamp check in the service layer. Outside window → 403.

7. **Delete window: 24 hours.** Same window as edit. Same enforcement mechanism → 403.

8. **Visibility scope:**
   - Teacher: can only list/view/edit/delete their own submissions.
   - Parent / FamilyAdmin: can list and view all feedback in the family.

9. **UrgentEscalation delivery:** Delivered via FCM with a dedicated urgent push title/body.
   Bypasses later batching and quiet-hours handling — sent inline on submission.

10. **Acknowledgement idempotency:** Second `POST /acknowledge` on an already-acknowledged
    feedback record returns the existing `FeedbackDto` with no error and no push re-sent.

11. **Acknowledgement notification:** On first acknowledgement, push sent to the feedback
    author's FCM token (`TeacherProfile.User.FcmToken` or `TeacherProfile.FamilyMember.User.FcmToken`).

12. **ParentResponseText limit:** Max 1,000 characters → 400.

13. **ResolutionStatus lifecycle:** Default is `'Open'` on creation (not `'Pending'`).
    Changes to `'Acknowledged'` on first `POST /acknowledge`. `'Resolved'` is a
    third allowed value but not set by any current endpoint — reserved for future use.

14. **WeeklySummaryJson requirement:** Required when `FeedbackType = WeeklySummary` (=6).
    Must be valid JSON with integer fields `attendanceRate` and `homeworkRate` (0–100),
    and non-empty string fields `standoutMoment` and `focusArea` → 400 if invalid.

15. **Dashboard impact:** `UnacknowledgedFeedbackCount` in `FamilyDashboardDto` (Section 4)
    reflects the count of `IsAcknowledged = 0` records for the family. Decrements after
    acknowledgement.

---

### 7.5 Flow Summaries

#### Flow 1 — Teacher Submits Feedback

```
Trigger       : Teacher submits an observation or concern about a child
→ API call    : POST /api/v1/families/{familyId}/feedback
                { ChildProfileId, FeedbackType, Severity?, Subject?, Message,
                  SessionId?, CommentTemplateId?, WeeklySummaryJson? }
→ Validation  : Teacher's TeacherChildAssignments check → 403 if unassigned;
                Severity required for Complaint/UrgentEscalation → 400;
                Message 5–2000 chars → 400; Subject ≤ 300 → 400
→ DB operation: INSERT TeacherFeedback (IsAcknowledged=false,
                ResolutionStatus=Pending, IsEditable computed).
→ Response    : 201 ApiResponse<FeedbackDto>
→ Side effect : FCM push to parent(s) of the child.
                UrgentEscalation: dedicated urgent push, bypasses quiet hours.
```

#### Flow 2 — Teacher Edits Feedback (Within Window)

```
Trigger       : Teacher corrects a feedback entry within 24 hours of submission
→ API call    : PUT /api/v1/families/{familyId}/feedback/{feedbackId}
                { Message, Severity? }
→ Validation  : Caller = feedback author → 403 if not;
                IsEditable = 1 (DB computed: DATEDIFF(HOUR, CreatedAt, GETUTCDATE()) < 24) → 403 if expired;
                Message 5–2000 chars → 400
→ DB operation: UPDATE TeacherFeedback SET Message, Severity, UpdatedAt.
                Note: Subject and WeeklySummaryJson are NOT updatable.
→ Response    : 200 ApiResponse<FeedbackDto>
→ Side effect : None.
```

#### Flow 3 — Parent Acknowledges Feedback

```
Trigger       : Parent reads feedback and optionally writes a response
→ API call    : POST /api/v1/families/{familyId}/feedback/{feedbackId}/acknowledge
                { ParentResponseText? }
→ Validation  : Role = Parent or FamilyAdmin → 403 if not;
                Feedback belongs to familyId → 404 if not;
                ParentResponseText ≤ 1000 chars → 400
→ DB operation: If first acknowledgement:
                  UPDATE TeacherFeedback SET IsAcknowledged=1, AcknowledgedAt=GETUTCDATE(),
                  AcknowledgedByUserId, ParentResponseText, ResolutionStatus='Acknowledged'.
                If already acknowledged: no write.
→ Response    : 200 ApiResponse<FeedbackDto>
→ Side effect : First acknowledgement only: FCM push to feedback author (teacher/elder).
```

---

### 7.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens are from the DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Role | Planned path |
|---|---|---|
| Feedback list | Parent/Teacher | `lib/features/feedback/screens/feedback_list_screen.dart` |
| Feedback detail | Parent/Teacher | `lib/features/feedback/screens/feedback_detail_screen.dart` |
| Submit feedback | Teacher/Elder | `lib/features/teacher/screens/submit_feedback_screen.dart` |
| Child feedback summary | Parent | Part of child detail screen |

**Critical integration notes:**
- `ResolutionStatus` is a **string** in the response — compare against `'Open'`, `'Acknowledged'`, `'Resolved'` (not int values).
- `IsEditable` is a `bool` field in `FeedbackDto` — use it to show/hide edit/delete actions; do not recalculate the 24-hour window client-side.
- `FeedbackType` and `FeedbackSeverity` are sent as **int enum values** in requests.
- `UpdateFeedbackRequest` only accepts `{ Message, Severity? }` — Subject and WeeklySummaryJson cannot be changed after submission.
- Feedback-summary `periodDays` defaults to 7; display per-type count breakdown.

**Confirmed constraints:**
- Folder: `lib/features/feedback/` and `lib/features/teacher/`
- Teacher role renders submit/edit/delete (within `IsEditable` window).
- Parent role renders acknowledge with optional response text.
- UrgentEscalation feedback should be visually highlighted.
- Elder role: submit Appreciation only.

---

### 7.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `TeacherChildAssignments` (Phase 04) | Active assignment check for submitter | Validates teacher is assigned to the child being written about |
| `TeacherProfiles` (Phase 04) | Author's `TeacherProfileId` | Required FK on `TeacherFeedback`; auto-created for Elder on first submit |
| `AttendanceSessions` (Phase 05) | Optional `SessionId` linkage | Feedback can reference the session it relates to |
| `CommentTemplates` (Phase 07) | Optional `CommentTemplateId` | Feedback can reference a comment template |
| `IPushNotificationService` / FCM (Phase 02) | Parent and teacher FCM tokens | Parent alert on submit; teacher alert on acknowledgement |
| `NotificationPreferences` (Phase 16) | Quiet-hours config | Standard feedback push respects quiet hours; UrgentEscalation bypasses |
| `FamilyDashboardDto` (Phase 03/12) | `UnacknowledgedFeedbackCount` field | Dashboard badge decrements when feedback acknowledged — see Section 4 |

---

## 8. Rewards & Coins

### 8.1 Module Purpose

Manages the child motivational economy: coin earning (via task approval), coin spending
(via reward redemption), parent-initiated deductions, streak tracking, streak freeze usage,
reward catalog management, and the full redemption lifecycle.

Implemented across three phases:
- **Phase 10** — `ICoinService`, `CoinTransactions` ledger, coin-history and deduction
  endpoints in `ChildrenController`, streak-freeze endpoint, optimistic concurrency on
  `ChildProfiles` via `RowVersion`
- **Phase 13** — `RewardsController`: admin reward catalog, family reward management
- **Phase 14** — `RewardsController` extended: child redemption, parent review,
  redemption lifecycle

**Controller split:** Coin history, deduction, and streak endpoints live in
`ChildrenController` (Phase 10); reward catalog and redemption endpoints live in
`RewardsController` (Phase 13–14).

---

### 8.2 Key APIs

---

#### GET /api/v1/families/{familyId}/children/{childId}/coin-history

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — any child; Child — own profile only (`childProfileId` JWT claim) |

**Request query params:** `page`, `pageSize` (standard pagination).

**Response DTO — `ApiResponse<PaginatedList<CoinTransactionDto>>`:**

| Field | Type | Notes |
|---|---|---|
| `TransactionId` | `Guid` | — |
| `ChildProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `TransactionType` | `string` | `"Earned"` \| `"Spent"` \| `"Deducted"` — string (not int enum) |
| `Amount` | `int` | Positive for Earned/Deducted; **negative** for Spent |
| `BalanceAfter` | `int` | Child's coin balance after this transaction |
| `ReferenceType` | `string` | `"TaskCompletion"` \| `"RewardRedemption"` \| `"ManualDeduction"` |
| `ReferenceId` | `Guid?` | FK to the referenced record; null for ManualDeduction |
| `Note` | `string?` | Present on Deducted type; 5–500 chars. Null for other types. |
| `CreatedByUserId` | `Guid` | — |
| `CreatedAt` | `DateTime` | UTC |

---

#### POST /api/v1/families/{familyId}/children/{childId}/coin-deduction

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `DeductCoinsRequest`** (located in `DTOs/Task/` — moved in Phase 10):

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Amount` | `int` | YES | Positive integer |
| `Note` | `string?` | YES | 5–500 characters — validated as required by FluentValidation even though nullable on DTO |

**Response DTO — `ApiResponse<CoinTransactionDto>`.**

**Business rules:**
- Deducts `Amount` from `ChildProfile.CoinBalance`. Insufficient balance → 422.
- Writes a `CoinTransactions` row (`TransactionType = Deduction`).
- Uses optimistic concurrency (`RowVersion`) → 409 on concurrency conflict.

**Error cases:**

| Condition | Status |
|---|---|
| Insufficient coin balance | 422 |
| `Note` < 5 or > 500 chars | 400 |
| Concurrency conflict on child balance | 409 |

---

#### POST /api/v1/families/{familyId}/children/{childId}/streak/use-freeze

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Child (own profile only via `childProfileId` JWT claim) |

**Request:** No body.

**Response DTO — `ApiResponse<bool>`** — returns `true` on success.

**Business rules:**
- Decrements `ChildProfiles.StreakFreezesAvailable` by 1.
- Requires `StreakFreezesAvailable ≥ 1` → 422 if none available.
- Preserves `CurrentStreakDays` for the day a freeze is used — prevents streak reset.
- Restricted to the logged-in child's own `childProfileId` → 403 otherwise.

**Error cases:**

| Condition | Status |
|---|---|
| No freezes available | 422 |
| Child accessing another child's profile | 403 |

---

#### GET /api/v1/admin/rewards/catalog

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Response DTO — `ApiResponse<IReadOnlyCollection<RewardDto>>`:** All system rewards (`IsSystem = 1`).

---

#### POST /api/v1/admin/rewards/catalog

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Request DTO — `CreateRewardRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `RewardName` | `string` | YES | Max 200 characters |
| `Description` | `string` | NO | Max 500 characters |
| `IconCode` | `string` | NO | Max 50 characters |
| `Category` | `string` | YES | `ScreenTime` / `FoodTreat` / `Outing` / `Purchase` / `FamilyActivity` |
| `CoinCost` | `int` | YES | 10–9,999 inclusive |

**Response DTO — `ApiResponse<RewardDto>`:** Returns 201 on success.

**Error cases:**

| Condition | Status |
|---|---|
| Invalid category | 400 |
| `CoinCost` outside 10–9,999 | 400 |
| `Description` > 500 chars | 400 |

---

#### PUT /api/v1/admin/rewards/catalog/{rewardId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Request DTO — `UpdateRewardRequest`:** Same field constraints as `CreateRewardRequest`.

**Error cases:** 400 (validation), 403, 404.

---

#### GET /api/v1/families/{familyId}/rewards

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — family rewards + unenabled system templates; Child — enabled family rewards only |

**Response DTO — `ApiResponse<IReadOnlyCollection<RewardDto>>`.**

**Business rules:**
- **Parent / FamilyAdmin:** returns family-scoped reward rows (`FamilyId = @familyId`)
  plus system reward templates (`IsSystem = 1`) that do **not** yet have a family copy
  for this family (i.e., not yet enabled).
- **Child:** returns only enabled family reward rows (`FamilyId = @familyId`, `IsEnabled = 1`).

---

#### POST /api/v1/families/{familyId}/rewards

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `CreateRewardRequest`:** Same shape as admin create.

**Business rules:**
- Creates a family-scoped reward (`FamilyId = @familyId`, `IsSystem = false`).
- To **enable a system reward**: use the same `CreateRewardRequest` fields as creating a custom reward.
  The service auto-sets `MasterRewardId` internally — it is not a request field.

**Response DTO — `ApiResponse<RewardDto>`:** Returns 201 on success.

---

#### PUT /api/v1/families/{familyId}/rewards/{rewardId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `UpdateRewardRequest`:** Same constraints as create.

**Business rules:**
- Only family-scoped rewards (`FamilyId = @familyId`) are editable.
- System rewards (`IsSystem = 1`) cannot be modified via family endpoints → 404 (treated as not found for the family scope).

**Error cases:** 400, 403, 404.

---

#### POST /api/v1/families/{familyId}/rewards/{rewardId}/redeem

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Child (own `childProfileId` from JWT only) |

**Request DTO — `RedeemRequest`** (colocated in `RedemptionDto.cs`):

| Field | Type | Required | Notes |
|---|---|---|---|
| `ChildProfileId` | `Guid` | YES | The child redeeming the reward — validated against JWT `childProfileId` claim |

**Response DTO — `ApiResponse<RedemptionDto>`:** Returns 201 on success.

**Business rules:**
- Reward must be enabled in this family (`FamilyId = @familyId`, `IsEnabled = true`) → 404 if not.
- Child must have sufficient `CoinBalance ≥ Reward.CoinCost` → 422 if insufficient.
- Duplicate pending redemption: same `(ChildProfileId, RewardId)` with `Status = Pending`
  already exists → **409 Conflict** (enforced by filtered unique index in DB).
- Creates `RewardRedemptions` row with `Status = Pending`, `CoinsSpent = Reward.CoinCost`,
  `RequestedAt = GETUTCDATE()`. Coins are **not** deducted at request time — deducted
  only on parent approval.

**Error cases:**

| Condition | Status |
|---|---|
| Reward not enabled for this family | 404 |
| Insufficient coin balance | 422 |
| Duplicate pending redemption for same reward | 409 |

---

#### GET /api/v1/families/{familyId}/rewards/redemptions

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — all redemptions; Child — own redemptions only |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `childId` | `Guid?` | Optional — filter by child |
| `status` | `RedemptionStatus?` | Optional — Pending=1, Approved=2, Rejected=3, Fulfilled=4 |

**Response DTO — `ApiResponse<IReadOnlyCollection<RedemptionDto>>`** — not paginated.

`RedemptionDto` shape:

| Field | Type | Notes |
|---|---|---|
| `RedemptionId` | `Guid` | — |
| `RewardId` | `Guid` | — |
| `ChildProfileId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `CoinsSpent` | `int` | Snapshotted from `Reward.CoinCost` at request time |
| `Status` | `RedemptionStatus` | Pending=1, Approved=2, Rejected=3, Fulfilled=4 |
| `RequestedAt` | `DateTime` | UTC |
| `ReviewedByUserId` | `Guid?` | — |
| `ReviewedAt` | `DateTime?` | — |
| `ParentNote` | `string?` | — |
| `RewardName` | `string` | Denormalized from Rewards |
| `ChildName` | `string` | From `ChildProfile → FamilyMember.DisplayName` |

---

#### PUT /api/v1/families/{familyId}/rewards/redemptions/{redemptionId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `ReviewRedemptionRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `Status` | `RedemptionStatus` | YES | `Approved` (=2) or `Rejected` (=3) only — enum value, not a string |
| `ParentNote` | `string?` | NO | Max 500 characters |

**Response DTO — `ApiResponse<RedemptionDto>`.**

**Business rules — Approve:**
- Re-validates `ChildProfile.CoinBalance ≥ CoinsSpent` → 422 if now insufficient.
- Calls `ICoinService.SpendCoinsForRewardRedemptionAsync`:
  - Deducts `CoinsSpent` from `ChildProfile.CoinBalance` (optimistic concurrency via
    `RowVersion` → 409 on conflict).
  - Inserts `CoinTransactions` row (`TransactionType = Spent`,
    `ReferenceType = RewardRedemption`, `ReferenceId = RedemptionId`).
- Increments `Reward.TimesRedeemedTotal`.
- All writes (redemption update, coin deduction, coin transaction) applied in a single
  DB transaction via `RewardRedemptionRepository.ApplyApprovalAsync`.
- Push notification sent to child (reward approved).

**Business rules — Reject:**
- Coin balance unchanged — no `CoinTransactions` entry.
- Sets `Status = Rejected`, persists `ParentNote`.
- Push notification sent to child (reward rejected with note).

**Error cases:**

| Condition | Status |
|---|---|
| `Status` not Approved(2) or Rejected(3) | 400 |
| Redemption not in Pending status | 409 |
| `ParentNote` > 500 chars | 400 |
| Insufficient balance on approval | 422 |
| Concurrency conflict on child balance | 409 |
| Redemption not found / wrong family | 404 |

---

### 8.3 DB Tables

#### CoinTransactions
- **Script:** `022_CreateCoinTransactions.sql` (Phase 10)
- **Append-only.** Records are never updated or deleted. No `IsDeleted`, `UpdatedAt`, `DeletedAt`. Uses `SYSUTCDATETIME()`.
- **Note:** PK is `TransactionId`. `TransactionType` and `ReferenceType` are `NVARCHAR` strings (not INT enums).

| Column | Type | Notes |
|---|---|---|
| `TransactionId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `TransactionType` | `NVARCHAR(30)` | NOT NULL — `"Earned"` \| `"Spent"` \| `"Deducted"` |
| `Amount` | `INT` | NOT NULL — positive for Earned/Deducted; negative for Spent |
| `BalanceAfter` | `INT` | NOT NULL — snapshotted balance after transaction |
| `ReferenceType` | `NVARCHAR(50)` | NOT NULL — `"TaskCompletion"` \| `"RewardRedemption"` \| `"ManualDeduction"` |
| `ReferenceId` | `UNIQUEIDENTIFIER` | NULL — null for ManualDeduction type |
| `Note` | `NVARCHAR(500)` | NULL — required (via FluentValidation) for Deducted type |
| `CreatedByUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |

**Index:** `IX_CoinTransactions_ChildProfileId_CreatedAt` — (ChildProfileId, CreatedAt)

**ChildProfiles — Phase 10 additions** (table owned by Section 3):
- `RowVersion` column added via `023_AlterChildProfiles_RowVersion.sql` — used for
  optimistic concurrency on all coin balance mutations.

#### Rewards
- **Scripts:** `026_CreateRewards.sql` · `027_SeedSystemRewards.sql` (Phase 13)
- **Note:** PK is `RewardId`. `IsEnabled` defaults to 1. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `RewardId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL, FK → Families.FamilyId — null for system rewards |
| `MasterRewardId` | `UNIQUEIDENTIFIER` | NULL, FK → Rewards.RewardId (self-ref) — set on family clone |
| `RewardName` | `NVARCHAR(200)` | NOT NULL |
| `Description` | `NVARCHAR(500)` | NULL |
| `IconCode` | `NVARCHAR(50)` | NULL |
| `Category` | `NVARCHAR(50)` | NOT NULL — CHECK: ScreenTime\|FoodTreat\|Outing\|Purchase\|FamilyActivity |
| `CoinCost` | `INT` | NOT NULL — CHECK: 10 ≤ CoinCost ≤ 9999 |
| `IsSystem` | `BIT` | NOT NULL, DEFAULT 0 |
| `IsEnabled` | `BIT` | NOT NULL, DEFAULT 1 |
| `TimesRedeemedTotal` | `INT` | NOT NULL, DEFAULT 0 — CHECK: ≥ 0 |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Index:** `IX_Rewards_FamilyId_IsEnabled` — (FamilyId, IsEnabled) WHERE IsDeleted=0

- **Seeded system rewards (Phase 13 — 10 rows, 2 per category):**

| RewardName | Category |
|---|---|
| Extra 15 Minutes Screen Time | ScreenTime |
| Choose Movie Night | ScreenTime |
| Ice Cream Treat | FoodTreat |
| Favorite Snack Pick | FoodTreat |
| Park Visit Choice | Outing |
| Mini Outing Pick | Outing |
| Small Toy Purchase | Purchase |
| Book of Choice | Purchase |
| Choose Family Game Night | FamilyActivity |
| Pick Weekend Activity | FamilyActivity |

#### RewardRedemptions
- **Script:** `028_CreateRewardRedemptions.sql` (Phase 14)
- **Note:** PK is `RedemptionId`. Status INT DEFAULT 1 (Pending). Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `RedemptionId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `RewardId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Rewards.RewardId |
| `ChildProfileId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → ChildProfiles.ChildProfileId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `CoinsSpent` | `INT` | NOT NULL — CHECK: ≥ 0 — snapshotted from `Reward.CoinCost` |
| `Status` | `INT` | NOT NULL, DEFAULT 1 — RedemptionStatus enum |
| `RequestedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `ReviewedByUserId` | `UNIQUEIDENTIFIER` | NULL, FK → Users.UserId |
| `ReviewedAt` | `DATETIME2` | NULL |
| `ParentNote` | `NVARCHAR(500)` | NULL |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Indexes:**
- `IX_RewardRedemptions_FamilyId_Status` — (FamilyId, Status) WHERE IsDeleted=0
- `UX_RewardRedemptions_ChildProfileId_RewardId_Pending` — UNIQUE (ChildProfileId, RewardId, Status) WHERE IsDeleted=0 AND Status=1

**RedemptionStatus enum (confirmed from `RedemptionStatus.cs`):**

| Int | Name | Set when |
|---|---|---|
| 1 | Pending | Child submits redemption request |
| 2 | Approved | Parent approves — coins deducted |
| 3 | Rejected | Parent rejects — no coin change |
| 4 | Fulfilled | Reserved — no current endpoint sets this value |

---

### 8.4 Business Rules

1. **Coin mutation path (Phase 10):** All coin balance changes — earn, spend, or deduct —
   flow through `ICoinService`. No service or controller may write to `ChildProfile.CoinBalance`
   directly. Every mutation also inserts an append-only `CoinTransactions` row.
   `TransactionType` string values: `"Earned"`, `"Spent"`, `"Deducted"`.
   `ReferenceType` string values: `"TaskCompletion"`, `"RewardRedemption"`, `"ManualDeduction"`.

2. **Optimistic concurrency:** `ChildProfiles.RowVersion` is checked on every coin mutation.
   `DbUpdateConcurrencyException` → **409 Conflict**. Clients must retry.

3. **Level thresholds (applied on every Earn):**

   | `TotalCoinsEarned` range | Level |
   |---|---|
   | 0–499 | 1 |
   | 500–1,499 | 2 |
   | 1,500–2,999 | 3 |
   | 3,000–4,999 | 4 |
   | 5,000+ | 5 |

4. **Pillar score on task approval:** Matching pillar score incremented by 1 when
   `TaskItem.PillarTag` is set. Each pillar score capped at **20**.

5. **Streak freeze award:** New freeze awarded at each 10-consecutive-day task completion
   milestone. Maximum **2 freezes** held at any time. Freeze use decrements
   `StreakFreezesAvailable` by 1; requires ≥ 1 available → 422 if none.

6. **Reward categories — exactly five valid values:**
   `ScreenTime`, `FoodTreat`, `Outing`, `Purchase`, `FamilyActivity` → 400 on invalid.

7. **CoinCost range:** 10–9,999 inclusive → 400 outside range.

8. **System reward immutability:** System rewards (`IsSystem = 1`) are read-only from
   family scope. Families enable a system reward by creating a family-scoped clone
   (`MasterRewardId` set, `FamilyId` set, `IsSystem = false`).

9. **Redemption — coins held, not deducted at request:** Coins are deducted only on
   parent **Approval**, not at the time the child submits the redemption request.

10. **Redemption idempotency:** Only one `Pending` redemption per `(ChildProfileId, RewardId)`
    allowed at a time. Duplicate request → **409 Conflict** (filtered unique DB index).

11. **Insufficient coins:** `CoinBalance < Reward.CoinCost` at request time → 422.
    Balance also re-validated at approval time → 422 if depleted in the interim.

12. **Approval atomicity:** Redemption status update, coin deduction, and `CoinTransactions`
    insert are committed in a single DB transaction via `ApplyApprovalAsync`. No partial writes.

13. **Push on redemption events:**
    - Child submits: **no push notification** sent to parent on new redemption request (confirmed from service code — no `SendPush` call in `RedeemAsync`).
    - Approval: push to child ("Reward approved").
    - Rejection: push to child ("Reward rejected" with `ParentNote` if present).

14. **Review only processes Pending redemptions:** `ReviewRedemptionAsync` throws `ConflictException` (→ 409) if `redemption.Status != Pending`. A parent cannot Approve or Reject an already-reviewed redemption.

---

### 8.5 Flow Summaries

#### Flow 1 — Child Redeems a Reward

```
Trigger       : Child taps "Redeem" on a reward in the rewards screen
→ API call    : POST /api/v1/families/{familyId}/rewards/{rewardId}/redeem
→ Validation  : Reward enabled for family → 404 if not;
                CoinBalance ≥ CoinCost → 422 if insufficient;
                No existing Pending redemption for (ChildProfileId, RewardId) → 409
→ DB operation: INSERT RewardRedemptions (Status=Pending(1), CoinsSpent=Reward.CoinCost,
                RequestedAt=SYSUTCDATETIME()). No coin deduction yet.
→ Response    : 201 ApiResponse<RedemptionDto>
→ Side effect : No push notification sent to parent on redemption request (confirmed).
```

#### Flow 2 — Parent Approves Redemption

```
Trigger       : Parent reviews pending redemption in the rewards screen
→ API call    : PUT /families/{familyId}/rewards/redemptions/{redemptionId}
                { Status: 2 }   ← RedemptionStatus.Approved
→ Validation  : Role = Parent or FamilyAdmin;
                Redemption Status must be Pending → 409 if not;
                Re-validate CoinBalance ≥ CoinsSpent → 422 if insufficient
→ DB operation (single transaction via ApplyApprovalAsync):
                  UPDATE RewardRedemptions SET Status=Approved, ReviewedAt, ReviewedByUserId;
                  UPDATE ChildProfiles SET CoinBalance -= CoinsSpent (RowVersion check → 409);
                  INSERT CoinTransactions (TransactionType=Spent, ReferenceType=RewardRedemption);
                  UPDATE Rewards SET TimesRedeemedTotal += 1.
→ Response    : 200 ApiResponse<RedemptionDto>
→ Side effect : Push to child (reward approved).
```

#### Flow 3 — Parent Rejects Redemption

```
Trigger       : Parent declines a redemption request
→ API call    : PUT /families/{familyId}/rewards/redemptions/{redemptionId}
                { Status: 3, ParentNote: "..." }   ← RedemptionStatus.Rejected
→ Validation  : Role = Parent or FamilyAdmin;
                Redemption Status must be Pending → 409 if not;
                ParentNote ≤ 500 chars
→ DB operation: UPDATE RewardRedemptions SET Status=Rejected, ParentNote,
                ReviewedAt, ReviewedByUserId. No coin mutation.
→ Response    : 200 ApiResponse<RedemptionDto>
→ Side effect : Push to child (reward rejected with note).
```

#### Flow 4 — Parent Deducts Coins

```
Trigger       : Parent manually deducts coins (penalty or correction)
→ API call    : POST /families/{familyId}/children/{childId}/coin-deduction
                { Amount, Note }
→ Validation  : Role = Parent or FamilyAdmin; Note 5–500 chars;
                CoinBalance ≥ Amount → 422 if insufficient
→ DB operation: UPDATE ChildProfiles SET CoinBalance -= Amount (RowVersion → 409 on conflict);
                INSERT CoinTransactions (TransactionType=Deduction, Note).
→ Response    : 200 ApiResponse<CoinTransactionDto>
→ Side effect : None.
```

#### Flow 5 — Child Uses a Streak Freeze

```
Trigger       : Child uses a freeze to preserve their streak on a missed day
→ API call    : POST /families/{familyId}/children/{childId}/streak/use-freeze
→ Validation  : childProfileId from JWT = childId in route → 403 if mismatch;
                StreakFreezesAvailable ≥ 1 → 422 if none
→ DB operation: UPDATE ChildProfiles SET StreakFreezesAvailable -= 1.
→ Response    : 200 ApiResponse<>
→ Side effect : None.
```

---

### 8.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens are from the DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Role | Planned path |
|---|---|---|
| Rewards catalog / child shop | Child | `lib/features/rewards/screens/rewards_screen.dart` |
| Redemption request | Child | Part of rewards screen |
| Parent redemption review | Parent | `lib/features/rewards/screens/redemptions_screen.dart` |
| Coin history | Parent/Child | `lib/features/rewards/screens/coin_history_screen.dart` |
| Streak display | Child | Part of child home screen |

**Critical integration notes:**
- `ReviewRedemptionRequest.Status` is an **int enum** — send `{ "status": 2 }` for Approve, `{ "status": 3 }` for Reject.
- `CoinTransactionDto.TransactionType` is a **string** — compare `"Earned"`, `"Spent"`, `"Deducted"`.
- `CoinTransactionDto.Amount` is **negative** for `"Spent"` transactions.
- No push is sent to parent when child submits redemption — parent must poll or receive it via dashboard refresh.
- `GET /rewards/redemptions` is NOT paginated — all results returned.
- `RedeemRequest` requires `ChildProfileId` in the body (not just route params).

**Confirmed constraints:**
- Folder: `lib/features/rewards/` and `lib/features/child/`
- Child role renders redeem action; Parent role renders approve/reject actions.
- Coin balance and streak displayed in child home — driven by `ChildProfiles` data.
- Demo mode must show non-zero coin balances and a seeded reward catalog.

---

### 8.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `ChildProfiles.RowVersion` (Phase 10) | Optimistic concurrency column | All coin mutations require RowVersion check |
| `TaskCompletions` (Phase 09) | `CompletionId` as `ReferenceId` | CoinTransactions references task completion on Earn |
| `RewardRedemptions` (Phase 14) | `RedemptionId` as `ReferenceId` | CoinTransactions references redemption on Spent |
| `IPushNotificationService` / FCM (Phase 02) | Child and parent FCM tokens | Push on redemption approval, rejection |
| `NotificationPreferences` (Phase 16) | Quiet-hours config | [VERIFY] whether rewards push respects quiet hours |
| `IRewardService` (Phase 13) | Reward enabled/cost lookup | Redeem validates enabled state and coin cost |
| `ICoinService` (Phase 10 — this module) | Used by Phase 09 task approval and Phase 14 redemption | Centralises all coin balance mutations |

---

## 9. Family Calendar

### 9.1 Module Purpose

Manages family calendar events — creation, listing, editing, deletion — with per-event
reminders delivered via push notification. Includes recurring event support (RRULE),
child-scoped visibility, and two background workers for automated delivery and birthday
event generation.

Implemented across two phases in `CalendarController`:
- **Phase 15** — Event CRUD, EventReminder persistence, upcoming-events endpoint
- **Phase 16** — `ReminderDeliveryWorker` (background service, polls every 5 min),
  `BirthdayEventGeneratorWorker` (daily UTC boundary), FCM updated to HTTP v1
  credentials flow with deep-link data payload

Notification preferences (`GET`/`PUT /users/{userId}/notification-preferences`) are
implemented in Phase 16 but documented in **Section 10** (Notification Engine).

---

### 9.2 Key APIs

---

#### GET /api/v1/families/{familyId}/calendar/events

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any active family member |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `fromDate` | `DateTime?` | Optional — start of UTC date range filter |
| `toDate` | `DateTime?` | Optional — end of UTC date range filter |

**Response DTO — `ApiResponse<IReadOnlyCollection<EventDto>>`** — not paginated. `EventDto` shape:

| Field | Type | Notes |
|---|---|---|
| `EventId` | `Guid` | — |
| `FamilyId` | `Guid` | — |
| `CreatedByUserId` | `Guid` | — |
| `EventTitle` | `string` | — |
| `EventType` | `EventType` | Enum — DoctorAppointment=1…Other=8 |
| `Description` | `string?` | — |
| `StartDateTime` | `DateTime` | UTC |
| `EndDateTime` | `DateTime?` | UTC |
| `IsAllDay` | `bool` | — |
| `Location` | `string?` | — |
| `ColorHex` | `string?` | `#RRGGBB` format |
| `IsRecurring` | `bool` | — |
| `RecurrenceRule` | `string?` | RRULE-style format |
| `VisibilityScope` | `string` | `Family\|Parent\|Child\|Elder\|Caregiver` |
| `LinkedChildProfileId` | `Guid?` | — |
| `IsActive` | `bool` | — |
| `Reminders` | `IReadOnlyCollection<EventReminderDto>` | See below |

`EventReminderDto`:

| Field | Type | Notes |
|---|---|---|
| `ReminderId` | `Guid` | — |
| `RemindBeforeMinutes` | `int` | One of: 5, 10, 15, 30, 60, 120, 480, 1440, 4320 |
| `Channel` | `NotificationChannel` | Push=1, SMS=2, Email=3, InApp=4 |
| `IsSent` | `bool` | — |
| `ScheduledFor` | `DateTime` | UTC — StartDateTime minus RemindBeforeMinutes |

**Business rules:**
- Child role: returns only events where `VisibilityScope = Family` or `VisibilityScope = Child`
  AND (`LinkedChildProfileId IS NULL` OR `LinkedChildProfileId = @childProfileId` from JWT).
- All other roles: returns all events for `FamilyId = @currentFamilyId`.
- `WHERE IsDeleted = 0` enforced.

---

#### POST /api/v1/families/{familyId}/calendar/events

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin, Teacher |

**Request DTO — `CreateEventRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `EventTitle` | `string` | YES | Min 2, max 300 characters |
| `EventType` | `EventType` | YES | Enum — DoctorAppointment=1, SchoolEvent=2, TuitionClass=3, Birthday=4, MedicineReminder=5, ExamDate=6, FamilyTravel=7, Other=8 |
| `Description` | `string?` | NO | Max 1,000 characters |
| `StartDateTime` | `DateTime` | YES | Not more than 1 year in the past (UTC) |
| `EndDateTime` | `DateTime?` | NO | Must be ≥ `StartDateTime` when provided |
| `IsAllDay` | `bool` | YES | Default false |
| `Location` | `string?` | NO | Max 300 characters |
| `ColorHex` | `string?` | NO | Must match `#RRGGBB` regex → 400 if invalid |
| `VisibilityScope` | `string` | YES | `Family\|Parent\|Child\|Elder\|Caregiver` (case-insensitive, default `"Family"`) |
| `IsRecurring` | `bool` | YES | — |
| `RecurrenceRule` | `string?` | Conditional | Required when `IsRecurring = true`. Must start with `FREQ=`; max 200 chars. Must be empty when `IsRecurring = false`. |
| `LinkedChildProfileId` | `Guid?` | NO | Links event to a specific child profile |
| `Reminders` | `EventReminderRequest[]` | NO | Max 5 per event; no duplicate `(RemindBeforeMinutes, Channel)` pairs |
| `Reminders[].RemindBeforeMinutes` | `int` | YES | Must be one of: 5, 10, 15, 30, 60, 120, 480, 1440, 4320 |
| `Reminders[].Channel` | `NotificationChannel` | YES | Push=1, SMS=2, Email=3, InApp=4 |

**Response DTO — `ApiResponse<EventDto>`:** Returns 201 on success.

**Business rules:**
- `StartDateTime` not more than 1 year in the past (UTC) → 400.
- `EndDateTime ≥ StartDateTime` when both provided (CHECK constraint + FluentValidation) → 400.
- `RecurrenceRule` must start with `FREQ=`, key=value pairs separated by `;`, max 200 chars. Must be empty when `IsRecurring = false` → 400.
- `ColorHex` must match `#RRGGBB` (7-char hex) → 400 if invalid.
- Max 5 reminders per event → 400. No duplicate `(RemindBeforeMinutes, Channel)` pairs → 400.
- `RemindBeforeMinutes` must be one of the 9 allowed values → 400.
- On create: `EventReminder` rows inserted for each reminder, with
  `ScheduledFor = StartDateTime − RemindBeforeMinutes minutes`, `IsSent = false`.

**Error cases:**

| Condition | Status |
|---|---|
| `StartDateTime` > 1 year in the past | 400 |
| `EndDateTime` < `StartDateTime` | 400 |
| `RecurrenceRule` invalid / > 200 chars | 400 |
| `ColorHex` not `#RRGGBB` format | 400 |
| `VisibilityScope` not in allowed list | 400 |
| More than 5 reminders or duplicate pairs | 400 |
| Invalid `RemindBeforeMinutes` value | 400 |
| Insufficient role | 403 |

---

#### GET /api/v1/families/{familyId}/calendar/events/{eventId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any active family member (child visibility rules apply as above) |

**Response DTO — `ApiResponse<EventDto>`:** Full event detail including reminders.

**Error cases:** 403 (child accessing non-visible event), 404.

---

#### PUT /api/v1/families/{familyId}/calendar/events/{eventId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Event creator OR FamilyAdmin |

**Request DTO — `UpdateEventRequest`:** Identical fields to `CreateEventRequest` — same validation rules apply. All fields are sent on every update (no partial-update semantics).

**Business rules:**
- Only the user who created the event (`CreatedByUserId`) or a `FamilyAdmin` may update.
  Other roles → 403.
- Reminder update: existing active (`IsSent = false`) reminder rows for the event are
  replaced with the new reminders list. Sent reminders are not modified.
- Validation rules identical to create.

**Error cases:** 400 (validation), 403 (not creator or FamilyAdmin), 404.

---

#### DELETE /api/v1/families/{familyId}/calendar/events/{eventId}

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Event creator OR FamilyAdmin |

**Business rules:**
- Soft-deletes the `CalendarEvents` row.
- Also soft-deletes all active (`IsSent = false`) `EventReminders` rows for the event
  in the same operation.
- Sent reminders (`IsSent = true`) are not modified — historical record preserved.

**Response DTO:** `ApiResponse<bool>` — returns `true` on success.

**Error cases:** 403 (not creator or FamilyAdmin), 404.

---

#### GET /api/v1/families/{familyId}/calendar/upcoming

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any active family member (child visibility rules apply) |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `days` | `int` | Default 7 — number of days ahead to include |

**Response DTO — `ApiResponse<IReadOnlyCollection<EventDto>>`** — not paginated. Events sorted by `StartDateTime` ascending.

**Business rules:**
- Returns events with `StartDateTime ≥ GETUTCDATE()` within the next `days` days for the family.
- Visibility scoping applies per role (same as GET /events).
- Default window: **7 days** ahead.

---

### 9.3 DB Tables

#### CalendarEvents
- **Scripts:** `029_CreateCalendarEvents.sql` · `031_CreateCalendarIndexes.sql` (Phase 15)
- **Note:** PK is `EventId`. Column is `EventTitle` (not `Title`). `VisibilityScope` is `NVARCHAR(50)` string. Missing from previous docs: `IsAllDay`, `ColorHex`, `IsActive`. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `EventId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `CreatedByUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `EventTitle` | `NVARCHAR(300)` | NOT NULL — 2–300 chars |
| `EventType` | `INT` | NOT NULL — EventType enum |
| `Description` | `NVARCHAR(1000)` | NULL |
| `StartDateTime` | `DATETIME2` | NOT NULL — UTC |
| `EndDateTime` | `DATETIME2` | NULL — CHECK: EndDateTime IS NULL OR EndDateTime >= StartDateTime |
| `IsAllDay` | `BIT` | NOT NULL, DEFAULT 0 |
| `Location` | `NVARCHAR(300)` | NULL |
| `ColorHex` | `NVARCHAR(7)` | NULL — `#RRGGBB` format |
| `IsRecurring` | `BIT` | NOT NULL, DEFAULT 0 |
| `RecurrenceRule` | `NVARCHAR(200)` | NULL — RRULE-style; must start with `FREQ=` |
| `VisibilityScope` | `NVARCHAR(50)` | NOT NULL, DEFAULT `Family` — CHECK: Family\|Parent\|Child\|Elder\|Caregiver |
| `LinkedChildProfileId` | `UNIQUEIDENTIFIER` | NULL, FK → ChildProfiles.ChildProfileId |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

**Index:** `IX_CalendarEvents_FamilyId_StartDateTime` — (FamilyId, StartDateTime) WHERE IsDeleted=0

**EventType enum (confirmed from `EventType.cs`):**

| Int | Name | Notes |
|---|---|---|
| 1 | DoctorAppointment | — |
| 2 | SchoolEvent | — |
| 3 | TuitionClass | — |
| 4 | Birthday | Auto-created by `BirthdayEventGeneratorWorker` |
| 5 | MedicineReminder | Bypasses quiet hours — urgent delivery |
| 6 | ExamDate | — |
| 7 | FamilyTravel | — |
| 8 | Other | — |

#### EventReminders
- **Script:** `030_CreateEventReminders.sql` (Phase 15)
- **Note:** PK is `ReminderId`. Column is `RemindBeforeMinutes` (not `ReminderMinutes`). Has `FamilyId` and `Channel` columns — both missing from previous docs. Full BaseEntity. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `ReminderId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `EventId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → CalendarEvents.EventId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `RemindBeforeMinutes` | `INT` | NOT NULL — CHECK: IN (5,10,15,30,60,120,480,1440,4320) |
| `Channel` | `INT` | NOT NULL — NotificationChannel enum: Push=1, SMS=2, Email=3, InApp=4 |
| `ScheduledFor` | `DATETIME2` | NOT NULL — `StartDateTime − RemindBeforeMinutes` minutes |
| `IsSent` | `BIT` | NOT NULL, DEFAULT 0 — set to 1 after delivery |
| `SentAt` | `DATETIME2` | NULL until delivered |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |
| `IsDeleted` | `BIT` | NOT NULL, DEFAULT 0 |
| `DeletedAt` | `DATETIME2` | NULL |

- **Delivery logic:** Handled by `ReminderDeliveryWorker` (Phase 16) — not a direct API response.

---

### 9.4 Business Rules

1. **Create permission:** Parent, FamilyAdmin, Teacher may create events. Child and Elder
   cannot create calendar events.

2. **Edit / Delete permission:** Restricted to the event's `CreatedByUserId` OR a
   `FamilyAdmin`. Other roles → 403.

3. **StartDateTime constraint:** Must not be more than 1 year in the past → 400.

4. **EndDateTime constraint:** When provided, must be ≥ `StartDateTime` → 400.

5. **Reminder minutes allowed values:** Exactly: 5, 10, 15, 30, 60, 120, 480, 1440, 4320.
   Any other value → 400.

6. **Max 5 reminders per event** → 400 if exceeded.

7. **Reminder update on edit:** Existing unsent reminders are replaced by the new list.
   Sent reminders are not touched.

8. **Delete cascades to unsent reminders:** Soft-deleting an event also soft-deletes
   all `EventReminders` where `IsSent = false`.

9. **Visibility rules per role (confirmed from `CanViewEvent` in CalendarService):**
   - `IsActive = false` → never visible to any role.
   - **Parent / FamilyAdmin:** sees ALL active family events (no scope restriction).
   - **Teacher:** sees events they created (`CreatedByUserId`) OR where `VisibilityScope` is `Family`, `Child`, or `Caregiver`.
   - **Elder:** sees events where `VisibilityScope` is `Family` or `Elder`.
   - **Child:** sees events where `VisibilityScope` is `Family` or `Child` AND `LinkedChildProfileId IS NULL` OR `LinkedChildProfileId = @childProfileId` from JWT.

10. **RecurrenceRule validation:** Must start with `FREQ=` (case-insensitive). All components must be key=value pairs separated by `;`. Max 200 chars. Must be **empty** when `IsRecurring = false`. Server validates structure via `LooksLikeRRule()` in `CreateEventRequestValidator`.

11. **Reminder delivery — quiet hours:** `ReminderDeliveryWorker` evaluates the recipient's
    `NotificationPreferences.QuietHours` before sending. Delivery postponed until quiet
    hours end when active.

12. **Urgent reminder bypass:** `EventType = MedicineReminder` bypasses quiet hours.
    Delivered at exact `ScheduledFor` time regardless of quiet-hours setting.

13. **Reminder retry:** Up to **3 FCM send attempts** with **1-minute backoff** per reminder.
    After 3 failures: [VERIFY] whether reminder is marked sent-failed or remains pending.

14. **Birthday event auto-generation:** `BirthdayEventGeneratorWorker` runs daily on the
    UTC boundary. Creates a birthday `CalendarEvent` for each child whose birthday falls
    within the next 7 days, if no matching birthday event already exists for that date
    (idempotent).

15. **FCM protocol (Phase 16 update):** Push notification service updated from legacy
    server-key to **Firebase HTTP v1 credentials flow**. Deep-link data payload included
    for calendar reminder pushes to enable in-app navigation.

---

### 9.5 Flow Summaries

#### Flow 1 — Create Event with Reminders

```
Trigger       : Parent creates a family event (e.g. doctor appointment)
→ API call    : POST /families/{familyId}/calendar/events
                { EventTitle, EventType, StartDateTime, EndDateTime?, IsAllDay,
                  VisibilityScope, ColorHex?,
                  Reminders: [{ RemindBeforeMinutes: 1440, Channel: 1 }, { RemindBeforeMinutes: 60, Channel: 1 }] }
→ Validation  : Role gate (Parent/FamilyAdmin/Teacher);
                StartDateTime ≤ 1yr past → 400; EndDateTime ≥ Start → 400;
                ≤ 5 reminders → 400 if exceeded; valid ReminderMinutes → 400
→ DB operation: INSERT CalendarEvents (EventTitle, EventType, IsAllDay, VisibilityScope, ColorHex, ...);
                INSERT EventReminders × 2 (RemindBeforeMinutes, Channel, ScheduledFor = StartDateTime − minutes, IsSent=0).
→ Response    : 201 ApiResponse<EventDto>
→ Side effect : None at create time. ReminderDeliveryWorker polls and sends
                when ScheduledFor ≤ GETUTCDATE().
```

#### Flow 2 — Reminder Delivery (Background Worker)

```
Trigger       : ReminderDeliveryWorker tick (every 5 minutes)
→ DB query    : SELECT EventReminders WHERE IsSent=0 AND ScheduledFor ≤ GETUTCDATE()
→ For each due reminder:
    1. Load recipient's NotificationPreferences — check quiet hours.
    2. If quiet hours active AND EventType ≠ MedicineReminder → defer.
    3. Send FCM push (HTTP v1, deep-link payload) to event creator's FcmToken.
    4. On success: UPDATE EventReminders SET IsSent=true, SentAt=GETUTCDATE().
    5. On failure: retry up to 3 times with 1-minute backoff.
→ Side effect : Push notification delivered to recipient's device.
```

#### Flow 3 — Birthday Event Auto-Generation (Background Worker)

```
Trigger       : BirthdayEventGeneratorWorker tick (daily, UTC midnight boundary)
→ DB query    : SELECT ChildProfiles WHERE DateOfBirth IS NOT NULL
                AND upcoming birthday (next 7 days)
→ For each child with upcoming birthday:
    Check if birthday CalendarEvent already exists for that date → skip if found.
    INSERT CalendarEvents (EventType=Birthday, LinkedChildProfileId=child,
    StartDateTime=birthday date, auto-generated title).
→ Side effect : Birthday event appears in family calendar 7 days ahead.
```

#### Flow 4 — Delete Event

```
Trigger       : Creator or FamilyAdmin removes an event
→ API call    : DELETE /families/{familyId}/calendar/events/{eventId}
→ Validation  : caller = CreatedByUserId OR Role = FamilyAdmin → 403 if neither
→ DB operation: UPDATE CalendarEvents SET IsDeleted=1, DeletedAt=GETUTCDATE();
                UPDATE EventReminders SET IsDeleted=1, DeletedAt=GETUTCDATE()
                WHERE EventId=@eventId AND IsSent=0.
→ Response    : 200 ApiResponse<bool> { Data: true }
→ Side effect : Pending reminders cancelled (soft-deleted). Sent reminders preserved.
```

---

### 9.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens are from the DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Role | Planned path |
|---|---|---|
| Calendar view (monthly/weekly) | All | `lib/features/calendar/screens/calendar_screen.dart` |
| Event create/edit | Parent/Teacher | `lib/features/calendar/screens/create_event_screen.dart` |
| Event detail | All | `lib/features/calendar/screens/event_detail_screen.dart` |
| Upcoming events strip | All | Part of parent home / dashboard |

**Critical integration notes:**
- Field names: `EventTitle` (not `Title`), `RemindBeforeMinutes` (not `ReminderMinutes`).
- `VisibilityScope` is sent as a **string** — not an int. Send `"Family"`, `"Child"`, etc.
- Each reminder requires both `RemindBeforeMinutes` AND `Channel` fields.
- `GET /calendar/events` and `GET /calendar/upcoming` are NOT paginated — send date range params for paging effect.
- `DELETE /calendar/events/{id}` returns `ApiResponse<bool>`, not 204.
- Child role can only see events with `VisibilityScope = Family` or `Child`.
- Teacher role can see `Family`, `Child`, and `Caregiver` scoped events.

**Confirmed constraints:**
- Folder: `lib/features/calendar/`
- Parent/Teacher role renders create/edit/delete actions; Child and Elder render read-only.
- Demo mode must show upcoming events — no empty calendar state permitted.

---

### 9.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `ChildProfiles` (Phase 04) | `DateOfBirth`, `LinkedChildProfileId` | Birthday worker reads DoB; child visibility uses `LinkedChildProfileId` |
| `IPushNotificationService` / FCM (Phase 02/16) | HTTP v1 credentials, FcmToken on Users | Reminder delivery; FCM updated to v1 in Phase 16 |
| `NotificationPreferences` (Phase 16 — Section 10) | `QuietHoursStart`, `QuietHoursEnd` | Reminder worker evaluates quiet hours before sending |
| `Users.FcmToken` (Phase 02) | Recipient device token | Target for reminder push notification |
| `ReminderDeliveryWorker` (Phase 16 — background) | Hosted service registered in Program.cs | Polls due `EventReminders` every 5 minutes |
| `BirthdayEventGeneratorWorker` (Phase 16 — background) | Hosted service registered in Program.cs | Auto-creates birthday events 7 days ahead |

---

## 10. Notification Engine

### 10.1 Module Purpose

Manages user notification preferences, push delivery pipeline, notification history,
and digest scheduling. Spans two phases:

- **Phase 16** — `NotificationsController`: notification-preferences endpoints;
  `NotificationPreferences` table; `ReminderDeliveryWorker` and
  `BirthdayEventGeneratorWorker` (documented in Section 9); FCM updated to HTTP v1.
- **Phase 17** — `[VERIFY]` — Notification Engine: push batching, notification history,
  `NotificationDeliveryWorker`, `MorningDigestWorker`, `EveningDigestWorker`.
  **Phase 17 raw notes are absent from ProjectOverview.md.**
  Read `FamilyFirst_L1_Codex_DevPlan.docx` to populate Phase 17 details.

**What is confirmed from cross-phase references (Phases 18–20):**
- A `Notifications` table exists (per-user, per-notification rows).
- A `NotificationDeliveryWorker` processes queued `Notifications` rows.
- `MorningDigestWorker` and `EveningDigestWorker` are registered background services.
- `INotificationService` / `NotificationService` centralises notification creation.
- Per-family `NotificationRules` (Phase 20) are applied at creation time —
  `IsEnabled`, `PriorityOverride`, `DeliveryDelayMinutes`.
- Weekly digest delivery uses the notification pipeline and respects
  `NotificationPreferences.WeeklyDigest` flag.

Controller: `NotificationsController`

---

### 10.2 Key APIs

---

#### GET /api/v1/users/{userId}/notification-preferences

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | **Own profile only.** FamilyAdmin cannot read another user's preferences. `EnsureOwnUser` throws 403 if `currentUserId != userId`. |

**Request:** No body.

**Response DTO — `ApiResponse<NotificationPreferenceDto>`:**

| Field | Type | Notes |
|---|---|---|
| `PreferenceId` | `Guid` | PK of the preferences row |
| `UserId` | `Guid` | Linked user |
| `FamilyId` | `Guid` | From user's primary active family membership |
| `AttendanceAlerts` | `bool` | Default: true |
| `FeedbackAlerts` | `bool` | Default: true |
| `TaskVerificationAlerts` | `bool` | Default: true |
| `RewardAlerts` | `bool` | Default: true |
| `CalendarAlerts` | `bool` | Default: true |
| `WeeklyDigest` | `bool` | Controls weekly digest push. Default: true |
| `QuietHoursEnabled` | `bool` | Default: true |
| `QuietHoursStartTime` | `TimeOnly` | Default: 22:00 |
| `QuietHoursEndTime` | `TimeOnly` | Default: 07:00 |
| `MorningDigestTime` | `TimeOnly` | Default: 07:00 |
| `EveningDigestTime` | `TimeOnly` | Default: 20:00 |
| `UpdatedAt` | `DateTime` | UTC |

**Business rules:**
- **Auto-created on GET or PUT:** `GetOrCreatePreferencesAsync` inserts a defaults row if none exists.
  Requires an active family membership (`FamilyId` populated from primary membership).
  No membership → 403.
- `NotificationPreferences` has a **unique index** on `UserId` — one row per user.

**Error cases:** 401, 403 (not own profile or no family membership), 404.

---

#### PUT /api/v1/users/{userId}/notification-preferences

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Own profile only |

**Request DTO — `UpdatePreferencesRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `AttendanceAlerts` | `bool` | YES | Default true |
| `FeedbackAlerts` | `bool` | YES | Default true |
| `TaskVerificationAlerts` | `bool` | YES | Default true |
| `RewardAlerts` | `bool` | YES | Default true |
| `CalendarAlerts` | `bool` | YES | Default true |
| `WeeklyDigest` | `bool` | YES | Default true |
| `QuietHoursEnabled` | `bool` | YES | Default true |
| `QuietHoursStartTime` | `TimeOnly` | YES | Default 22:00 — `HH:mm:ss` format |
| `QuietHoursEndTime` | `TimeOnly` | YES | Default 07:00 |
| `MorningDigestTime` | `TimeOnly` | YES | Default 07:00 |
| `EveningDigestTime` | `TimeOnly` | YES | Default 20:00 |

**Note:** All fields required — full preferences object sent on every update (no partial-update semantics).

**Response DTO — `ApiResponse<NotificationPreferenceDto>`.**

**Business rules:**
- Quiet hours: when `QuietHoursStart` and `QuietHoursEnd` are set, the
  `ReminderDeliveryWorker` defers non-urgent pushes until quiet hours end.
- `EventType = MedicineReminder` bypasses quiet hours regardless of this setting
  (documented in Section 9).

**Error cases:** 400 (validation), 401, 403, 404.

---

#### Phase 17 — Notification History Endpoints

**Status: NOT IMPLEMENTED.** The `Notifications` table, `NotificationDeliveryWorker`, `NotificationService`, and `NotificationDto` all exist in the codebase — but no API controller endpoints expose notification history or mark-as-read to clients. `NotificationsController` contains only the 2 notification-preferences endpoints above.

`NotificationDto` and `CreateNotificationRequest` are used internally by `NotificationService` (called from task/feedback/redemption/calendar modules) but are not exposed via any HTTP route.

**If notification history endpoints are needed in future:** they would be added to `NotificationsController` or a new controller with routes like:
- `GET /api/v1/users/{userId}/notifications` — paginated list
- `PUT /api/v1/users/{userId}/notifications/{id}/read` — mark one read
- `PUT /api/v1/users/{userId}/notifications/mark-all-read` — `MarkAllReadResultDto { Count }` already defined

---

### 10.3 DB Tables

#### NotificationPreferences
- **Script:** `032_CreateNotificationPreferences.sql` (Phase 16)
- **Note:** PK is `PreferenceId`. Has `FamilyId` column (missing from previous docs). Time columns are `TIME` (not NVARCHAR). No `CreatedAt` — only `UpdatedAt`. Not a full BaseEntity. Uses `SYSUTCDATETIME()`.

| Column | Type | Notes |
|---|---|---|
| `PreferenceId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `UserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `FamilyId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Families.FamilyId |
| `AttendanceAlerts` | `BIT` | NOT NULL, DEFAULT 1 |
| `FeedbackAlerts` | `BIT` | NOT NULL, DEFAULT 1 |
| `TaskVerificationAlerts` | `BIT` | NOT NULL, DEFAULT 1 |
| `RewardAlerts` | `BIT` | NOT NULL, DEFAULT 1 |
| `CalendarAlerts` | `BIT` | NOT NULL, DEFAULT 1 |
| `WeeklyDigest` | `BIT` | NOT NULL, DEFAULT 1 |
| `QuietHoursEnabled` | `BIT` | NOT NULL, DEFAULT 1 |
| `QuietHoursStartTime` | `TIME` | NOT NULL, DEFAULT `'22:00:00'` |
| `QuietHoursEndTime` | `TIME` | NOT NULL, DEFAULT `'07:00:00'` |
| `MorningDigestTime` | `TIME` | NOT NULL, DEFAULT `'07:00:00'` |
| `EveningDigestTime` | `TIME` | NOT NULL, DEFAULT `'20:00:00'` |
| `UpdatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |

**Index:** `UX_NotificationPreferences_UserId` — UNIQUE on UserId

#### Notifications
- **Scripts:** `033_CreateNotifications.sql` · `034_CreateNotificationIndexes.sql` (Phase 17)
- **Note:** PK is `NotificationId`. Column is `RecipientUserId` (not `UserId`). `DeepLinkPath` (not `DeepLink`). Has `FcmMessageId`, `IsBatched`, `BatchGroup`, `ScheduledFor` — all missing from previous docs. Minimal table: no `IsDeleted`, `UpdatedAt`. Auto-purges rows > 90 days old.

| Column | Type | Notes |
|---|---|---|
| `NotificationId` | `UNIQUEIDENTIFIER` | PK, DEFAULT NEWID() |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL, FK → Families.FamilyId |
| `RecipientUserId` | `UNIQUEIDENTIFIER` | NOT NULL, FK → Users.UserId |
| `Title` | `NVARCHAR(200)` | NOT NULL |
| `Body` | `NVARCHAR(1000)` | NOT NULL |
| `Priority` | `INT` | NOT NULL, DEFAULT 2 (Normal) — NotificationPriority enum |
| `Channel` | `INT` | NOT NULL, DEFAULT 1 (Push) — NotificationChannel enum |
| `ReferenceType` | `NVARCHAR(50)` | NULL — context reference type string |
| `ReferenceId` | `UNIQUEIDENTIFIER` | NULL — FK to context record |
| `DeepLinkPath` | `NVARCHAR(300)` | NULL — in-app navigation path |
| `IsRead` | `BIT` | NOT NULL, DEFAULT 0 |
| `ReadAt` | `DATETIME2` | NULL |
| `IsSent` | `BIT` | NOT NULL, DEFAULT 0 — set to 1 after FCM delivery |
| `SentAt` | `DATETIME2` | NULL until delivered |
| `FcmMessageId` | `NVARCHAR(200)` | NULL — FCM response ID; sentinel `"suppressed"` when no FCM token |
| `IsBatched` | `BIT` | NOT NULL, DEFAULT 0 |
| `BatchGroup` | `NVARCHAR(50)` | NULL — batching group key |
| `ScheduledFor` | `DATETIME2` | NULL — deferred delivery time; NULL = immediate |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT SYSUTCDATETIME() |

**Index:** `IX_Notifications_RecipientUserId_IsRead_IsSent` — (RecipientUserId, IsRead, IsSent)

**Auto-purge:** `NotificationDeliveryWorker` calls `PurgeOlderThanAsync(90 days)` on every poll tick.

**NotificationChannel enum (confirmed from `NotificationChannel.cs`):**

| Int | Name |
|---|---|
| 1 | Push |
| 2 | SMS |
| 3 | Email |
| 4 | InApp |

**NotificationPriority enum (confirmed from `NotificationPriority.cs`):**

| Int | Name |
|---|---|
| 1 | Low |
| 2 | Normal |
| 3 | High |
| 4 | Urgent |

---

### 10.4 Business Rules

**Confirmed (Phase 16 + cross-phase references):**

1. **One preferences row per user.** `NotificationPreferences` has a unique `UserId` index.
   **Auto-created on first GET or PUT** via `GetOrCreatePreferencesAsync`. Requires an active
   family membership (FamilyId sourced from primary active membership). No membership → 403.

2. **Quiet hours.** When `QuietHoursStart`/`QuietHoursEnd` are set on a user's preferences,
   `ReminderDeliveryWorker` defers non-urgent calendar reminders until quiet hours end.

3. **MedicineReminder bypasses quiet hours.** `EventType = MedicineReminder` is exempt
   from quiet-hours deferral — delivered at the exact `ScheduledFor` time.

4. **WeeklyDigest flag.** `NotificationPreferences.WeeklyDigest = false` suppresses
   weekly digest push for that user. Enforced in `NotificationService` (wired in Phase 18).

5. **Per-family notification rules (Phase 20).** At notification creation, `NotificationService`
   resolves family-level overrides from `NotificationRules` table:
   - `IsEnabled = false` → notification suppressed for that family.
   - `PriorityOverride` → overrides default priority.
   - `DeliveryDelayMinutes` → delays queued delivery by the specified minutes.

6. **Notification campaigns (Phase 19).** SuperAdmin broadcast creates `Notifications`
   rows via `NotificationService`. Delivery handled by `NotificationDeliveryWorker`.

7. **UrgentEscalation feedback (Phase 11).** Delivered inline via FCM — bypasses the
   `Notifications` table and delivery worker queue entirely.

**Confirmed from Phase 17 source (NotificationDeliveryWorker):**
- **Poll interval: 5 minutes.** Worker ticks every 5 minutes.
- **Retry behavior:** Failed FCM sends leave `IsSent=0`. Row is picked up again on next poll — **no retry limit** (unlike ReminderDeliveryWorker's 3-attempt limit).
- **No FCM token:** Notification marked `IsSent=true` with sentinel `FcmMessageId = "suppressed"` — silently discarded. No error logged.
- **Notification retention: 90 days.** `PurgeOlderThanAsync(90 days)` runs on every worker tick before processing.

**[VERIFY] — still absent:**
- Batching logic (`IsBatched`, `BatchGroup`) — exact batching window and grouping strategy not confirmed.
- `MorningDigestWorker` and `EveningDigestWorker` — implementation not confirmed from source (no files found matching these names in the codebase).

---

### 10.5 Flow Summaries

#### Flow 1 — Update Notification Preferences

```
Trigger       : User opens notification settings screen and toggles preferences
→ API call    : PUT /api/v1/users/{userId}/notification-preferences
                { AttendanceAlerts, FeedbackAlerts, TaskVerificationAlerts, RewardAlerts,
                  CalendarAlerts, WeeklyDigest, QuietHoursEnabled,
                  QuietHoursStartTime, QuietHoursEndTime,
                  MorningDigestTime, EveningDigestTime }
→ Validation  : Own profile only → 403; TimeOnly format (HH:mm:ss)
→ DB operation: If no row exists: INSERT NotificationPreferences (auto-create).
                UPDATE NotificationPreferences SET all fields WHERE UserId=@userId.
→ Response    : 200 ApiResponse<NotificationPreferenceDto>
→ Side effect : Future ReminderDeliveryWorker ticks respect updated quiet hours.
```

#### Flow 2 — Notification Creation via NotificationService

```
Trigger       : Any module calls INotificationService (task approval, feedback, redemption, etc.)
→ NotificationService resolves:
    1. Per-family NotificationRules for FamilyId + ReferenceType → IsEnabled check.
    2. PriorityOverride and DeliveryDelayMinutes applied.
→ DB operation: INSERT Notifications (FamilyId?, RecipientUserId, Title, Body, Channel,
                Priority, ReferenceType?, ReferenceId?, DeepLinkPath?,
                IsBatched, BatchGroup?, ScheduledFor?, IsSent=0, IsRead=0).
→ Side effect : NotificationDeliveryWorker picks up row on next poll and sends FCM push.
```

#### Flow 3 — Notification Delivery (Background Worker)

```
Trigger       : NotificationDeliveryWorker tick — every 5 minutes
→ Purge       : DELETE Notifications WHERE CreatedAt < GETUTCDATE() - 90 days.
→ DB query    : ListDueForImmediateDeliveryAsync(GETUTCDATE())
                → SELECT WHERE IsSent=0 AND (ScheduledFor IS NULL OR ScheduledFor ≤ now)
→ For each queued notification:
    1. If recipient has no FcmToken:
       UPDATE SET IsSent=1, FcmMessageId='suppressed' — silently skipped, no error.
    2. Send FCM push (HTTP v1, deep-link data payload from DeepLinkPath).
    3. On success: UPDATE Notifications SET IsSent=1, SentAt=GETUTCDATE().
    4. On failure: notification stays IsSent=0 — retried on next 5-minute tick (no retry limit).
→ Side effect : Push delivered to device; deep-link navigates user to relevant screen.
```

---

### 10.6 Flutter Integration

**Status: Flutter app not yet built.** Planned screens from DevPlan spec — not confirmed implemented.

**Planned screens ([VERIFY] against implementation when built):**

| Screen | Notes |
|---|---|
| Notification settings | `lib/features/notifications/screens/notification_settings_screen.dart` |
| Notification history / inbox | Planned — but no API endpoints exist yet (Phase 17 not exposed) |

**Critical integration notes:**
- `UpdatePreferencesRequest` sends **all** fields on every update — including `TimeOnly` values (`QuietHoursStartTime` etc.). Flutter must serialize `TimeOnly` as `HH:mm:ss` format.
- `QuietHoursEnabled` is a separate bool — quiet hours only active when both enabled AND start/end are set.
- There is **no notification history API** — the `Notifications` table is internal only. Flutter should not expect a `/notifications` list endpoint until one is added.
- FCM deep-link handling: `DeepLinkPath` in push payload — GoRouter must handle incoming deep links by parsing this path.

**Confirmed constraints:**
- Folder: `lib/features/notifications/`
- Demo mode: notification settings can show defaults from `NotificationPreferenceDto`.

---

### 10.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| `Users.FcmToken` (Phase 02) | Device token per user | Target for all FCM pushes |
| `IPushNotificationService` / FCM (Phase 02/16) | HTTP v1 credentials flow | Push delivery for all notification types |
| `EventReminders` (Phase 15) | Due reminder rows | `ReminderDeliveryWorker` queries these (Section 9) |
| `NotificationRules` (Phase 20 — Section 11) | Per-family delivery rules | Applied by `NotificationService` at creation time |
| `FeatureFlags` (Phase 19) | `MaintenanceMode` flag | Notification delivery blocked during maintenance for non-admin |
| All event-emitting modules | Call `INotificationService` | Task approval (S6), feedback (S7), redemption (S8), calendar reminders (S9), campaigns (S11) all route through this service |

---

## 11. Admin Configuration & Reports

### 11.1 Module Purpose

Covers three distinct admin surfaces, all implemented in the final phases:

- **Phase 18 — Reports & Weekly Digest** (`ReportsController`): family-facing aggregate
  reports — weekly digest, child weekly summary, attendance heatmap. No new DB tables.
- **Phase 19 — Super Admin Panel** (`AdminController`): platform-level family management,
  subscription control, plan management, analytics, feature flags, notification campaigns,
  and `MaintenanceModeMiddleware`.
- **Phase 20 — Family Admin Configuration** (`FamilyAdminController`): module visibility
  control per family, per-family notification rules, custom attendance statuses, and
  `FamilyModuleVisibilityFilter` applied globally.

Controllers: `ReportsController` · `AdminController` · `FamilyAdminController`
Phase 20 also adds `GET /attendance/statuses` to `AttendanceController`.

---

### 11.2 Key APIs

---

#### — Phase 18: Reports & Digest —

---

#### GET /api/v1/families/{familyId}/reports/weekly-digest

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `weekStartDate` | `date` | Must be a Monday; defaults to most recent Monday if omitted |

**Response DTO — `ApiResponse<WeeklyDigestDto>`:**

| Field | Type | Notes |
|---|---|---|
| Per-child attendance rate | `decimal` | Aggregated from `AttendanceRecords` for the week |
| Per-child task rate | `decimal` | Aggregated from `TaskCompletions` for the week |
| Weekly feedback count | `int` | Count of `TeacherFeedback` rows for the week |
| Upcoming 7-day events | `EventDto[]` | From `CalendarEvents` |
| `FamilyScoreTrend` | `string` | `Up` / `Down` / `Flat` — current week vs prior week performance |

**Business rules:**
- `weekStartDate` must be a Monday → 400 if not.
- Data aggregated from existing Phase 04–17 tables — no new report table.
- `FamilyScoreTrend` derived by comparing combined attendance/task performance
  across two consecutive weeks. Implementation inference (no weekly history table exists).

---

#### GET /api/v1/families/{familyId}/children/{childId}/reports/weekly

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent only |

**Request query params:** `weekStartDate` (Monday; defaults to current week).

**Response DTO — `ApiResponse<ChildWeeklyReportDto>`:**

| Field | Type | Notes |
|---|---|---|
| `AttendanceRate` | `decimal` | — |
| `TaskRate` | `decimal` | — |
| Feedback counts by type | `Dictionary<FeedbackType, int>` | — |
| Latest parent remark | `string?` | [VERIFY] source field |
| Pillar scores (×5) | `int[]` | Current values from `ChildProfiles` |

---

#### GET /api/v1/families/{familyId}/children/{childId}/reports/attendance-summary

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent only |

**Request query params:**

| Param | Type | Notes |
|---|---|---|
| `fromDate` | `date` | Optional; defaults to current Monday |
| `toDate` | `date` | Optional; defaults to current Sunday |

**Response DTO — `ApiResponse<AttendanceSummaryDto>`:**

| Field | Type | Notes |
|---|---|---|
| `TotalSessions` | `int` | — |
| `PresentCount` | `int` | — |
| `AbsentCount` | `int` | — |
| `LateCount` | `int` | — |
| `LeftEarlyCount` | `int` | — |
| `AttendanceRatePct` | `decimal` | — |
| `Heatmap` | `HeatmapDayDto[]` | Day-by-day array |
| `Heatmap[].Date` | `date` | — |
| `Heatmap[].Status` | `int` | Most severe status for that day: Absent > Late > LeftEarly > Present |

---

#### — Phase 19: Super Admin Panel —

All Phase 19 endpoints require **`SuperAdmin` role** (policy applied at `AdminController` level).

---

#### GET /api/v1/admin/dashboard

**Response DTO — `ApiResponse<AdminDashboardDto>`:**
Platform-level KPIs — [VERIFY] exact fields (total families, active users, total children, etc.).

---

#### GET /api/v1/admin/families

**Request query params:** `page`, `pageSize`; [VERIFY] search/filter params.

**Response DTO — `ApiResponse<PaginatedList<AdminFamilySummaryDto>>`.**

---

#### GET /api/v1/admin/families/{familyId}

**Response DTO — `ApiResponse<AdminFamilyDetailDto>`:** Full family detail including
subscription, member count, plan — [VERIFY] exact fields.

---

#### PUT /api/v1/admin/families/{familyId}/subscription

**Request DTO — `UpdateFamilySubscriptionRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `PlanId` | `int` | NO | Target plan to switch to |
| `ExtendTrialDays` | `int` | NO | Days to extend the trial |
| `[VERIFY]` | — | — | Other fields |

**Business rules:**
- When `ExtendTrialDays` provided: `Subscription.TrialEndDate += ExtendTrialDays`
  and `Subscription.Status` forced to `Trial`.
- Plan change: updates `Subscriptions.PlanId`.

---

#### DELETE /api/v1/admin/families/{familyId}

**Business rules:**
- Soft-deletes the `Families` row.
- Sets `FamilyMembers.IsActive = false` for all members of the family.
  Blocked members cannot authenticate.
- [VERIFY] whether `Families.IsActive` also set to 0 (vs IsDeleted).

**Error cases:** 403, 404.

---

#### GET /api/v1/admin/plans

**Response DTO — `ApiResponse<List<AdminPlanDto>>`:** All plans from `Plans` table.

---

#### PUT /api/v1/admin/plans/{planId}

**Request DTO — `UpdatePlanRequest`:** [VERIFY] fields (price, child limit, etc.).

**Business rules:** Updates existing plan row only. No plan creation endpoint.

---

#### GET /api/v1/admin/analytics/overview

**Response DTO — `ApiResponse<AnalyticsOverviewDto>`:**

| Field | Type | Notes |
|---|---|---|
| Total users | `int` | COUNT from `Users` |
| Total children | `int` | COUNT from `ChildProfiles` |
| Total teachers | `int` | COUNT from `TeacherProfiles` |
| Total tasks | `int` | COUNT from `TaskItems` |
| Total completions | `int` | COUNT from `TaskCompletions` |
| Total feedback | `int` | COUNT from `TeacherFeedback` |
| Total notifications sent | `int` | COUNT from `Notifications` |
| `[VERIFY]` | — | Other count fields |

**Business rules:** Count queries only — no charting, no time-series analytics.

---

#### GET /api/v1/admin/feature-flags

**Response DTO — `ApiResponse<List<FeatureFlagDto>>`:** All feature flags.

---

#### PUT /api/v1/admin/feature-flags/{flag}

**Request DTO — `FeatureFlagDto`:** [VERIFY] — `{ Key, Value }` or `{ IsEnabled }`.

**Business rules:**
- Feature flags stored as key/value string records.
- `MaintenanceMode` flag: when enabled, `MaintenanceModeMiddleware` returns 503 for all
  non-admin, non-auth traffic.
- `MinimumAppVersion` flag: string-type value — [VERIFY] enforcement mechanism.
- Other flags: [VERIFY].

---

#### POST /api/v1/admin/notifications/campaign

**Request DTO:** [VERIFY] — campaign target (role, plan), title, body.

**Business rules:**
- Queries recipient user IDs by family-member role and/or plan code.
- Creates `Notifications` rows via `INotificationService` for each recipient.
- `NotificationDeliveryWorker` handles actual FCM dispatch.

---

#### — Phase 20: Family Admin Configuration —

All Phase 20 configuration endpoints require **`FamilyAdmin` role**.

---

#### GET /api/v1/families/{familyId}/admin/panel

**Response DTO — `ApiResponse<FamilyAdminPanelDto>`:**
Summary of all family admin configuration — [VERIFY] exact shape.

---

#### GET /api/v1/families/{familyId}/admin/module-visibility

**Response DTO — `ApiResponse<List<ModuleVisibilityDto>>`:**
Module visibility settings for the family (family-specific rows + defaults).

---

#### PUT /api/v1/families/{familyId}/admin/module-visibility

**Request DTO — `UpdateModuleVisibilityRequest`:** [VERIFY] fields.

**Business rules:**
- FamilyAdmin cannot change visibility settings for `SuperAdmin` or any role above
  FamilyAdmin in the role hierarchy.
- Writes an `AuditLogs` row for every visibility change.
- Family-specific override stored in `ModuleVisibilityConfig` with `FamilyId` set.
  Default rows (`FamilyId = NULL`) are not modified.

---

#### GET /api/v1/families/{familyId}/admin/notification-rules

**Response DTO — `ApiResponse<List<NotificationRuleDto>>`:**
Per-family notification rules. Missing default rules materialized on first read.

**Business rules:**
- If a family has no row for a default rule key, `FamilyAdminService` creates it
  on first GET using documented defaults.
- Default rule keys: `Attendance`, `Feedback`, `Task`, `Reward`, `Calendar`, `WeeklyDigest`.

---

#### PUT /api/v1/families/{familyId}/admin/notification-rules/{ruleId}

**Request DTO — `UpdateNotificationRuleRequest`:**

| Field | Type | Required | Constraint |
|---|---|---|---|
| `IsEnabled` | `bool` | NO | Enables/disables this notification type family-wide |
| `PriorityOverride` | `int?` | NO | Overrides default `NotificationPriority` |
| `DeliveryDelayMinutes` | `int?` | NO | Delays delivery by N minutes |

**Business rules:**
- Changes applied by `NotificationService` at notification creation time.
- Writes `AuditLogs` row for the update.

---

#### GET /api/v1/families/{familyId}/admin/attendance-statuses

**Response DTO — `ApiResponse<List<CustomAttendanceStatusDto>>`:**
Returns the 4 default statuses (virtual, not in DB) plus up to 5 custom family statuses.

**Role gate:** FamilyAdmin only.

---

#### POST /api/v1/families/{familyId}/admin/attendance-statuses

**Request DTO:** [VERIFY] — custom status name and display properties.

**Business rules:**
- Hard limit: **5 custom statuses per family** → 422 if exceeded.
- Default statuses (`Present`, `Absent`, `Late`, `LeftEarly`) are virtual — not stored in
  `CustomAttendanceStatuses` and cannot be deleted.

**Error cases:** 422 (cap exceeded), 400, 403.

---

#### DELETE /api/v1/families/{familyId}/admin/attendance-statuses/{statusId}

**Business rules:**
- Soft-deletes `CustomAttendanceStatuses` row.
- Cannot delete the 4 default statuses (they are not in the table) — [VERIFY] whether
  attempting to delete a default status returns 404 or 403.

---

#### GET /api/v1/families/{familyId}/attendance/statuses

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any active family member (read-only status list) |
| Controller | `AttendanceController` (added Phase 20) |

**Response DTO — `ApiResponse<List<CustomAttendanceStatusDto>>`:**
Same as the admin GET — exposes status config for the attendance marking flow.

---

### 11.3 DB Tables

#### FeatureFlags
- **Script:** `035_CreateFeatureFlags.sql` + `036_SeedFeatureFlags.sql` (Phase 19)

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK — [VERIFY] or INT IDENTITY |
| `Key` | `NVARCHAR` | Unique key (e.g. `MaintenanceMode`, `MinimumAppVersion`) |
| `Value` | `NVARCHAR` | String value — booleans stored as `"true"`/`"false"` |
| `[VERIFY]` | — | IsEnabled column may be separate from Value; confirm from script |
| `CreatedAt` | `DATETIME2` | — |
| `UpdatedAt` | `DATETIME2` | — |

- **Seeded flags (Phase 19):** `MaintenanceMode` (default false), `MinimumAppVersion`,
  and [VERIFY] others.

#### ModuleVisibilityConfig
- **Script:** `037_CreateModuleVisibilityConfig.sql` + `040_SeedDefaultModuleVisibility.sql` (Phase 20)

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | NULL for seeded defaults; FK → Families.Id for family overrides |
| `ModuleName` | `NVARCHAR` | e.g. `Attendance`, `Tasks`, `Rewards`, `Feedback`, `Calendar`, `Reports`, `Notifications` |
| `IsVisible` | `BIT` | Whether the module is visible for this family |
| `[VERIFY]` | — | Role-level visibility columns (per-role toggles?) |
| `CreatedAt` | `DATETIME2` | — |
| `UpdatedAt` | `DATETIME2` | — |

- **Seeded defaults:** `040_SeedDefaultModuleVisibility.sql` seeds `FamilyId = NULL` rows
  for all documented modules.
- **Override pattern:** Family-specific rows (`FamilyId` set) take precedence over seed rows.

#### NotificationRules
- **Script:** `038_CreateNotificationRules.sql` (Phase 20)

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | FK → Families.Id |
| `RuleKey` | `NVARCHAR` | `Attendance` / `Feedback` / `Task` / `Reward` / `Calendar` / `WeeklyDigest` |
| `IsEnabled` | `BIT` | Enables/disables this notification type for the family |
| `PriorityOverride` | `INT?` | Overrides default `NotificationPriority`; nullable |
| `DeliveryDelayMinutes` | `INT?` | Delays delivery; nullable |
| `CreatedAt` | `DATETIME2` | — |
| `UpdatedAt` | `DATETIME2` | — |

- **Materialized on demand:** Missing rows created on first FamilyAdmin GET for that family.

#### CustomAttendanceStatuses
- **Script:** `039_CreateCustomAttendanceStatuses.sql` (Phase 20)

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | FK → Families.Id |
| `StatusName` | `NVARCHAR` | Custom status label |
| `[VERIFY]` | — | Display properties (color code, icon, etc.) |
| `CreatedAt` | `DATETIME2` | — |
| `UpdatedAt` | `DATETIME2` | — |
| `IsDeleted` | `BIT` | — |
| `DeletedAt` | `DATETIME2` | — |

- **Hard limit:** 5 custom rows per family (`FamilyId`) → 422 when exceeded.
- **Default statuses** (`Present`, `Absent`, `Late`, `LeftEarly`) are not stored here —
  returned virtually by `FamilyAdminService`.

---

### 11.4 Business Rules

**Phase 18 — Reports:**

1. **weekStartDate must be a Monday.** Any other day-of-week → 400.

2. **Attendance heatmap severity order:** When multiple records exist on the same date,
   the most severe status wins: `Absent` > `Late` > `LeftEarly` > `Present`.

3. **FamilyScoreTrend** is an implementation inference — derived by comparing current
   week vs prior week combined performance. No dedicated history table exists; calculated
   from source-of-truth tables at request time.

4. **No new DB tables in Phase 18.** All report data aggregated from existing tables.

**Phase 19 — Super Admin:**

5. **SuperAdmin policy:** Applied at `AdminController` class level. Every Phase 19
   endpoint requires `Role = SuperAdmin` → 403 for any other role.

6. **Family block:** `DELETE /admin/families/{familyId}` sets `Families.IsActive = false`
   and `FamilyMembers.IsActive = false` for all members. Blocked members cannot log in.

7. **Trial extension:** `ExtendTrialDays` in subscription update extends `TrialEndDate`
   and forces `Subscription.Status = Trial`.

8. **Feature flag — MaintenanceMode:** When enabled, `MaintenanceModeMiddleware` returns
   `503 Service Unavailable` for all non-admin, non-auth traffic. Bypassed routes:
   `/api/v1/admin/*` and `/api/v1/auth/*`.

9. **Feature flag — MinimumAppVersion:** String-type value. [VERIFY] enforcement mechanism.

10. **Notification campaign:** Creates `Notifications` rows via `INotificationService`.
    Delivery handled asynchronously by `NotificationDeliveryWorker`.

**Phase 20 — Family Admin:**

11. **Module visibility enforcement:** `FamilyModuleVisibilityFilter` is applied globally.
    Maps controller name → module name and blocks requests for hidden modules.
    `SuperAdmin` and `FamilyAdmin` always bypass the filter.
    Modules covered: `Families`, `Children`, `Attendance`, `CommentTemplates`, `Tasks`,
    `Rewards`, `Feedback`, `Calendar`, `Reports`, `Notifications`, `FamilyAdmin`.

12. **Visibility permission ceiling:** FamilyAdmin cannot change visibility for
    `SuperAdmin` or any role above FamilyAdmin in the `UserRole` enum hierarchy.

13. **Notification rules — on-demand creation:** Missing `NotificationRules` rows for
    default keys are created on the first FamilyAdmin GET for that family, ensuring
    all subsequent PUT calls operate on real `RuleId` values.

14. **Notification rules — applied at creation time:** `NotificationService` resolves
    per-family rules for `FamilyId + ReferenceType`. `IsEnabled = false` suppresses
    the notification. `PriorityOverride` and `DeliveryDelayMinutes` applied before
    batching defaults.

15. **Custom attendance status cap:** Hard limit of **5 custom statuses per family** → 422.

16. **Default attendance statuses are immutable:** `Present`, `Absent`, `Late`, `LeftEarly`
    are returned virtually and cannot be deleted (they are not stored in
    `CustomAttendanceStatuses`).

17. **All Phase 20 config mutations write to `AuditLogs`:** Module visibility updates,
    notification rule updates, custom status creation, and custom status deletion each
    produce an `AuditLogs` row.

---

### 11.5 Flow Summaries

#### Flow 1 — SuperAdmin Blocks a Family

```
Trigger       : SuperAdmin blocks a family for policy violation
→ API call    : DELETE /api/v1/admin/families/{familyId}
→ Validation  : Role = SuperAdmin (policy gate at controller level)
→ DB operation: UPDATE Families SET IsActive=false (or IsDeleted=1 — [VERIFY]);
                UPDATE FamilyMembers SET IsActive=false WHERE FamilyId=@familyId.
→ Response    : 204 No Content
→ Side effect : All family members blocked from authenticating.
```

#### Flow 2 — SuperAdmin Enables Maintenance Mode

```
Trigger       : SuperAdmin enables maintenance mode before a deployment
→ API call    : PUT /api/v1/admin/feature-flags/MaintenanceMode { Value: "true" }
→ Validation  : Role = SuperAdmin
→ DB operation: UPDATE FeatureFlags SET Value='true' WHERE Key='MaintenanceMode'.
→ Response    : 200
→ Side effect : MaintenanceModeMiddleware reads flag on each request and returns
                503 for non-admin/non-auth traffic until flag is set back to false.
```

#### Flow 3 — FamilyAdmin Hides a Module

```
Trigger       : FamilyAdmin disables the Rewards module for their family
→ API call    : PUT /families/{familyId}/admin/module-visibility
                { ModuleName: "Rewards", IsVisible: false }
→ Validation  : Role = FamilyAdmin; target role not above FamilyAdmin
→ DB operation: UPSERT ModuleVisibilityConfig (FamilyId=@familyId, ModuleName='Rewards',
                IsVisible=false);
                INSERT AuditLogs (EntityType='ModuleVisibilityConfig', ...).
→ Response    : 200 ApiResponse<List<ModuleVisibilityDto>>
→ Side effect : FamilyModuleVisibilityFilter blocks Rewards endpoints for non-admin
                roles in this family on subsequent requests.
```

#### Flow 4 — Parent Views Weekly Digest

```
Trigger       : Parent opens weekly summary screen
→ API call    : GET /families/{familyId}/reports/weekly-digest?weekStartDate=2026-05-25
→ Validation  : Role = Parent or FamilyAdmin; weekStartDate is a Monday → 400 if not
→ DB operation: Aggregate queries across AttendanceRecords, TaskCompletions,
                TeacherFeedback, CalendarEvents for the week. No writes.
→ Response    : 200 ApiResponse<WeeklyDigestDto>
→ Side effect : None.
```

#### Flow 5 — Weekly Digest Push (Background Worker)

```
Trigger       : WeeklyDigestWorker fires Sunday 19:00 UTC
→ Query       : SELECT active Parent + FamilyAdmin FamilyMembers across all families
→ For each recipient:
    Check NotificationPreferences.WeeklyDigest — skip if false.
    Generate digest data via IReportService.GenerateWeeklyDigestAsync.
    INSERT Notifications row via INotificationService.
→ Side effect : NotificationDeliveryWorker picks up rows and sends FCM pushes.
```

---

### 11.6 Flutter Integration

**[VERIFY]** — No Flutter screen names or MockDataService methods confirmed.
Read `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` to populate:

- Screen files for: SuperAdmin panel, family admin panel, module visibility toggles,
  notification rule config, custom attendance statuses, weekly digest, child report,
  attendance summary heatmap
- Route names from `RouteNames` constants
- `MockDataService` method signatures for demo-mode admin and report data
- `StateNotifier` names for admin and reports state

**Known constraints (CLAUDE.md — standards):**
- Folder: `lib/features/admin/screens/`, `/providers/`, `/repositories/`
- SuperAdmin screens gated by `Role = 1`; FamilyAdmin screens gated by `Role = 2`.
- Demo mode must show non-blank admin panels and populated report charts.

---

### 11.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| All Phase 01–17 tables | Read-only count queries | Analytics overview (Phase 19) aggregates across all core tables |
| `AuditLogs` table (Phase 06) | Shared audit store | Phase 20 config mutations write here |
| `INotificationService` (Phase 17) | Notification creation | Campaign delivery (Phase 19) and weekly digest (Phase 18) |
| `NotificationDeliveryWorker` (Phase 17) | Background delivery | Processes campaign and digest `Notifications` rows |
| `FamilyModuleVisibilityFilter` (Phase 20) | Request-level enforcement | Applied globally — blocks hidden modules per family |
| `MaintenanceModeMiddleware` (Phase 19) | Request-level guard | Applied after authentication middleware in `Program.cs` |
| `FeatureFlags` table (Phase 19) | `MaintenanceMode`, `MinimumAppVersion` | Read by middleware on every request |
| `NotificationRules` table (Phase 20) | Per-family delivery config | Applied by `NotificationService` at creation time |
| `WeeklyDigestWorker` (Phase 18) | Hosted service | Fires Sunday 19:00 UTC; registered in Phase 20 final integration |

---

## 12. Level 2 — Document Vault

### 12.1 Module Purpose

**Level 2 — Build Priority 1. Available from Basic plan upward.**

Secure, centralised storage for every important family document — medical records, legal
papers, identity documents, school records, financial papers, insurance policies — with
intelligent expiry tracking, full-text search, version history, secure sharing, and
offline access. The Emergency Folder is always accessible without login or internet.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

- Controller: `DocumentVaultController`
- Flutter feature folder: `lib/features/vault/`
- Flutter screen prefix: `DV-`
- SuperAdmin cannot view individual family documents — platform admin access is
  absolutely excluded from family document content. (Note: DV-01 Vault Home lists
  Super Admin as accessible — this is for aggregate/structural view only, not document
  content. Individual document access is prohibited absolutely.)
- Build priority: Level 2 Priority 1 — ship before Medical Records, Safety, Finance, Reports.

**API endpoint paths are [VERIFY]** — the product document defines screens and business
rules but not exact REST paths. Read `FamilyFirst_L1_TechSpec.docx` or the Level 2 tech
spec when available to confirm exact endpoint paths and request/response DTOs.

---

### 12.2 Key APIs

**Screen-confirmed API surface** (paths [VERIFY] — derived from DV screen definitions):

---

#### GET /api/v1/families/{familyId}/vault/documents [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request query params (confirmed from DV-02 / DV-05):**

| Param | Notes |
|---|---|
| `category` | Filter by one of the 8 categories |
| `memberId` | Filter by family member |
| `search` | Full-text search on document name and auto-generated tags |
| `expiryStatus` | e.g. `expiring-soon` — documents expiring within 30 days |
| `dateFrom` / `dateTo` | Filter by upload date range |
| `sortBy` | `date` / `expiry` / `name` |
| `page`, `pageSize` | Standard pagination |

**Response DTO — `ApiResponse<PaginatedList<DocumentDto>>`:** [VERIFY] exact shape.

**Document card fields (confirmed from DV-02):**

| Field | Notes |
|---|---|
| DocumentId | — |
| DocumentName | — |
| Category | One of 8 category values |
| MemberId / MemberName | — |
| UploadedByUserId | — |
| UploadDate | — |
| ExpiryDate | Nullable |
| ExpiryStatus | Green (>90d) / Amber (30–90d) / Red (<30d) |
| ThumbnailUrl | — |
| Tags | Auto-generated and user-defined |
| IsEmergencyPriority | Whether tagged for Emergency Folder |

---

#### POST /api/v1/families/{familyId}/vault/documents [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Flow (confirmed from DV-03 Upload Flow):**
Client obtains a presigned upload URL first (see upload-url endpoint), uploads file
directly to storage, then calls this endpoint with the returned file reference and metadata.

**Request DTO — [VERIFY] exact fields. Confirmed fields from DV-03:**

| Field | Type | Required | Notes |
|---|---|---|---|
| `DocumentName` | `string` | YES | Auto-suggested, editable |
| `MemberId` | `Guid` | YES | Family member this document belongs to |
| `Category` | `string` | YES | Must be one of 8 valid categories |
| `FileUrl` | `string` | YES | S3 URL returned from presigned upload |
| `ExpiryDate` | `date` | NO | Strongly prompted for Insurance and Identity categories |
| `Tags` | `string[]` | NO | User-defined tags |
| `Visibility` | `string/int` | YES | Role-based preset — [VERIFY] allowed values |
| `IsEmergencyPriority` | `bool` | NO | Adds to Emergency Folder |

---

#### GET /api/v1/families/{familyId}/vault/documents/{documentId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES (or emergency link token) |
| Role gate | Per visibility rules — see Section 12.4 Rule 3 |

**Response DTO — `ApiResponse<DocumentDetailDto>`** (confirmed from DV-04):
Includes all metadata, version history, linked reminders, and presigned download URL.

---

#### PUT /api/v1/families/{familyId}/vault/documents/{documentId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Document uploader OR FamilyAdmin |

**Business rules:**
- Replace document: uploads new version, archives old version with original upload date
  and version number.
- Edit metadata only: updates name, tags, expiry, visibility without replacing file.

---

#### DELETE /api/v1/families/{familyId}/vault/documents/{documentId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Document owner OR FamilyAdmin |

**Business rules:**
- Requires explicit confirmation — client must send confirmation token (type `DELETE`
  in UI confirmation field).
- Deleted documents enter a **30-day recovery window**. After 30 days: permanently purged.
- On subscription cancellation: 30-day export window, then data purged per DPDP Act 2023.

---

#### POST /api/v1/families/{familyId}/vault/documents/upload-url [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO:** [VERIFY] — likely `{ FileName, ContentType, Category }`.

**Response DTO:** `{ UploadUrl (presigned), FileUrl (final S3 reference), ExpiresAt }` — [VERIFY].

**Business rules:** Presigned URL TTL [VERIFY]. S3 storage backend confirmed.

---

#### GET /api/v1/families/{familyId}/vault/emergency [VERIFY path]

| Field | Value |
|---|---|
| Auth required | NO (if Emergency Access toggle is ON) or YES (if toggle is OFF) |
| Role gate | Parent (configure). Any trusted contact with valid emergency link. |

**Response DTO (confirmed from DV-07):**
- Emergency cards per family member: Name, Photo, Blood Group, Known Allergies,
  Current Medications, Insurance Policy Number, Emergency Contact.
- Quick Documents: up to 5 documents tagged as `IsEmergencyPriority = true`.

**Business rules:**
- Emergency access mode configurable by FamilyAdmin: `Login required` / `PIN only` /
  `No login` (full emergency use).
- Emergency link expires after **72 hours** by default. Admin can extend or revoke at any time.
- Emergency link is **view-only** — no write access.
- Auto-updates when member health profile is updated; FamilyAdmin notified on changes.

---

#### GET /api/v1/families/{familyId}/vault/expiry [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Response (confirmed from DV-06 Expiry Dashboard):**
Documents expiring within the next 90 days, sorted by urgency (soonest first).

---

#### POST /api/v1/families/{familyId}/vault/documents/{documentId}/share [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin (sharing must be explicitly permitted) |

**Request DTO (confirmed from DV-08 Secure Share Modal):** [VERIFY] — `{ ExpiryHours, AllowDownload }`.

**Response:** Secure share link (time-limited, read-only).

**Business rules:**
- Default share link TTL: **72 hours**. Configurable.
- Read-only. No download unless explicitly permitted by FamilyAdmin.
- No FamilyFirst account required to view via share link.

---

### 12.3 DB Tables

**[VERIFY] — DB schema not in product document. Read Level 2 tech spec to populate.**

Expected tables (confirmed as required from business rules; schema [VERIFY]):

| Table | What it stores |
|---|---|
| `VaultDocuments` | Document metadata, file URL, member link, category, expiry, visibility, version state |
| `VaultDocumentVersions` | Archived versions when a document is replaced |
| `VaultFolders` or category config | [VERIFY] — categories are fixed 8 values; whether stored in DB or as enum |
| `VaultShareLinks` | Time-limited secure share links with expiry and revocation state |
| `VaultTags` or JSON column | Auto-generated and user-defined tags per document |

**Mandatory columns on `VaultDocuments` (confirmed from rules):**

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | FK → Families.Id — row-level security |
| `MemberId` | `UNIQUEIDENTIFIER` | FK → FamilyMembers.Id |
| `UploadedByUserId` | `UNIQUEIDENTIFIER` | FK → Users.Id |
| `DocumentName` | `NVARCHAR` | — |
| `Category` | `NVARCHAR` / `INT` | One of 8 categories |
| `FileUrl` | `NVARCHAR` | S3 object URL |
| `ExpiryDate` | `DATETIME2` | Nullable — drives expiry reminders |
| `IsEmergencyPriority` | `BIT` | Max 5 per family |
| `Visibility` | `INT` / `NVARCHAR` | Role-based access preset |
| `IsDeleted` | `BIT` | Soft delete with 30-day recovery |
| `DeletedAt` | `DATETIME2` | — |
| `PermanentDeleteAt` | `DATETIME2` | 30 days after `DeletedAt` |
| Standard BaseEntity columns | — | CreatedAt, UpdatedAt |

---

### 12.4 Business Rules

**Document Categories — exactly 8 (confirmed):**

| Category | Document Types | Expiry Tracking |
|---|---|---|
| Medical | Prescriptions, test reports, discharge summaries, vaccination records, scan reports, specialist letters, surgical notes | Prescription end dates, follow-up appointment dates |
| Identity | Passports, Aadhaar, PAN, Driving Licence, Birth Certificates, Marriage Certificate | Passport and driving licence expiry — **Mandatory** |
| School | Report cards, fee receipts, transfer certificates, hall tickets, scholarship letters, school ID cards | Fee receipt cycle dates, TC validity |
| Financial | Payslips, bank statements, IT returns, loan documents, investment certificates, FD receipts | FD maturity dates, loan tenure tracking |
| Insurance | Health, vehicle, home, life insurance policies | Policy expiry and premium due dates — **Mandatory**. 90-day advance alert |
| Legal | Property documents, wills, agreements, court documents, power of attorney | Agreement end dates, lease renewal dates |
| Certificates | Award certificates, activity completions, sports achievements, professional certifications | Optional — professional cert renewals |
| Other | Travel documents, rental agreements, utility bills, warranty cards, club memberships | Rental expiry, warranty, membership renewals |

**Visibility Rules (confirmed):**

| Scenario | Rule |
|---|---|
| Default | Visible to: uploader + FamilyAdmin + members explicitly assigned as viewers |
| Medical document | Parent (all children), Child (own only — not other children's or parents' medical docs). Elder: NOT visible by default |
| Identity document | FamilyAdmin + Parent only. Child: NOT visible. Teacher: NEVER visible |
| Secure share link | Time-limited (72hrs default). Read-only. No download unless FamilyAdmin explicitly permits. No FamilyFirst account required |
| Emergency folder | Configurable: Login required / PIN only / No login. Set by FamilyAdmin |
| Teacher access | ZERO access by default. FamilyAdmin can grant exception per document |
| SuperAdmin access | **Cannot view individual family documents under any circumstance. Absolute.** |

**Expiry Reminder Schedule (confirmed):**

| Category | Reminder Schedule | Escalation |
|---|---|---|
| Insurance (all types) | 90d, 30d, 14d, 3d before expiry | 7d before: Morning Digest + separate push. Day of: urgent alert |
| Passport / Driving Licence | 90d, 30d, 7d before expiry | Weekly alert until resolved if no action taken |
| FD / Investment maturity | 30d, 7d, 3d before maturity | Maturity day: informational — not urgent |
| Loan agreement | 60d, 30d before end | Informational — no escalation |
| Prescription / Medication | Course end date reminder, follow-up appointment prompt | No escalation — informational only |
| Other (configured) | Admin-configurable lead times | Admin-configurable escalation |

**Version, Archive, and Deletion Rules (confirmed):**

1. **Version history on replace:** When a document is replaced, the old version is archived
   with its original upload date and version number. FamilyAdmin can restore any archived version.

2. **Archived versions quota:** Count toward storage quota but are not shown in main vault view.
   Accessible via "Version History" on Document Detail (DV-04).

3. **Deletion confirmation:** Requires typing `DELETE` in UI confirmation field.

4. **30-day recovery window:** Deleted documents can be recovered for 30 days.
   After 30 days: permanently purged.

5. **Subscription cancellation:** Family has 30 days to export all documents.
   After 30 days: data purged per privacy policy and DPDP Act 2023.

**Document Search Behavior (confirmed from spec):**

| Search Input | System Behavior |
|---|---|
| Member name (e.g. "Arjun") | All documents tagged to Arjun, sorted by upload date |
| Category name (e.g. "Insurance") | All insurance documents across all members |
| Keyword (e.g. "prescription") | Full-text search on document names and auto-generated tags |
| Date range (e.g. "January 2025") | Documents uploaded or dated in that period |
| Tag (e.g. "Antibiotic") | Documents with that user-defined or auto-generated tag |
| Expiry status (e.g. "expiring soon") | Documents with expiry in next 30 days |

**Plan Gating:** Document Vault available from **Basic plan upward** (not Free Trial).

**Emergency Folder Constraints:**
- Maximum **5 documents** tagged as Emergency Priority per family.
- Emergency cards auto-update when member health profile is updated.

---

### 12.5 Flow Summaries

#### Flow 1 — Upload Document (Online)

```
Trigger       : Parent taps upload from DV-01, DV-02, or Home Dashboard shortcut
→ UI step 1   : Source selection — Camera / File Picker / QR scan (DV-03 Step 1)
→ UI step 2   : Preview and confirm or retake (DV-03 Step 2)
→ UI step 3   : Auto-tag review — system suggests Category, Member, Expiry (DV-03 Step 3)
→ UI step 4   : Details entry — DocumentName, Member, Category, ExpiryDate, Tags (DV-03 Step 4)
→ API call 1  : POST /vault/documents/upload-url { FileName, ContentType, Category }
→ Response 1  : { UploadUrl (presigned S3), FileUrl }
→ Action      : Client uploads file directly to S3 via UploadUrl
→ API call 2  : POST /vault/documents { DocumentName, MemberId, Category, FileUrl,
                ExpiryDate, Tags, Visibility, IsEmergencyPriority }
→ Validation  : Role gate; category valid; IsEmergencyPriority count ≤ 5
→ DB operation: INSERT VaultDocuments.
→ Response    : 201 ApiResponse<DocumentDto>
→ Side effect : Document card appears in vault. Expiry reminder scheduled if ExpiryDate set.
```

#### Flow 2 — Upload Document (Offline Queue)

```
Trigger       : Parent attempts upload with no network connection
→ Action      : Document saved to local device queue with amber badge on vault icon.
→ Notification: "Document saved. Will upload when connected."
→ On reconnect: Client replays queued upload automatically (no user action).
→ Side effect : No data loss. No user frustration.
```

#### Flow 3 — Emergency Folder Access (No Login)

```
Trigger       : Emergency responder (doctor, nurse) taps shared emergency link / QR code
→ Check       : Emergency Access toggle is ON for this family (set by FamilyAdmin)
→ Action      : Emergency folder opens — no FamilyFirst account required
→ Shows       : Emergency cards per member (Blood Group, Allergies, Medications,
                Insurance Policy Number, Emergency Contact) + up to 5 Priority documents
→ Constraint  : View-only. No write access. Share link expires after 72 hours.
```

#### Flow 4 — Replace Document (New Version)

```
Trigger       : Parent replaces an expiring Insurance policy with the renewed document
→ API call    : PUT /vault/documents/{documentId} with new file reference
→ DB operation: Archive old VaultDocuments row (version snapshot);
                INSERT new VaultDocuments row (or update with new FileUrl + VersionNumber++).
→ Side effect : Old version accessible via Version History. Expiry reminder reset to new date.
```

#### Flow 5 — Expiry Reminder Delivery (Background)

```
Trigger       : Background worker evaluates document expiry dates
→ For Insurance: 90d, 30d, 14d, 3d before expiry — push + Morning Digest inclusion
→ For Passport:  90d, 30d, 7d before expiry — push. If no action: weekly repeat.
→ 7d before Insurance expiry: Morning Digest entry + dedicated urgent push.
→ Day of Insurance expiry: urgent alert.
→ Side effect : Push delivered via INotificationService / FCM. Respects quiet hours
                except urgent-level alerts.
```

---

### 12.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| DV-01 | Vault Home | Parent, FamilyAdmin |
| DV-02 | Document Category View | Parent, FamilyAdmin |
| DV-03 | Document Upload Flow | Parent, FamilyAdmin |
| DV-04 | Document Detail View | Parent, FamilyAdmin + shared link recipients |
| DV-05 | Document Search | Parent, FamilyAdmin |
| DV-06 | Expiry Dashboard | Parent, FamilyAdmin |
| DV-07 | Emergency Folder | Parent (configure); any trusted contact via emergency link |
| DV-08 | Secure Share Modal | Parent, FamilyAdmin |

**Confirmed behavior:**
- Feature folder: `lib/features/vault/`
- Screen prefix: `DV-`
- DV-01 shows: 8 category tiles with document count + expiry badge, recent uploads
  (last 5 as horizontal scroll), expiry alerts strip (collapsible, 90-day window),
  persistent Emergency Folder shortcut card at top.
- Offline indicator: amber banner when no network. Uploads queue locally and sync on reconnect.
- Empty state DV-01: warm illustration + "Your Document Vault is ready. Start by uploading
  your family's most important document." Single upload CTA.
- Time targets: 2s to orient on DV-01; 5s to find any category; 60s camera upload; 30s file upload.
- Demo mode: must show a populated document list with at least one document per category.

**[VERIFY]:**
- Exact route names from `RouteNames` constants
- `MockDataService` method signatures
- `StateNotifier` name for vault state
- Offline cache library (Hive / Isar / SQLite — not confirmed)
- Emergency folder offline sync strategy (pre-download on vault load vs on Emergency card access)

---

### 12.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| JWT / Auth (Section 2) | `FamilyId`, `Role` claims | All vault APIs family-scoped; SuperAdmin excluded |
| AWS S3 (Phase 09) | Presigned upload URLs, file storage | Documents stored in S3; same pattern as task photo uploads |
| `FamilyMembers` table (Section 3) | Active membership + role | Visibility rules enforced per role |
| `INotificationService` (Section 10) | Push delivery | Expiry reminders, urgent alerts |
| `NotificationPreferences` (Section 10) | Quiet hours | Non-urgent expiry reminders respect quiet hours |
| Module visibility config (Section 11) | `DocumentVault` module enabled flag | `FamilyModuleVisibilityFilter` can disable vault per family |
| Background worker | Expiry evaluation | [VERIFY] — dedicated VaultExpiryWorker or reuse existing worker |
| Health profile data (Section 13) | Blood group, allergies, medications | Emergency cards auto-update from Medical Records |

---

### 12.8 Offline Behavior

**What is cached locally (confirmed):**

1. **All vault content is browsable offline** using last-synced cache. Full vault is
   available when there is no network — not just recently opened documents.

2. **Emergency folder is fully cached and viewable without internet.** This is the one
   screen in the app that must **never** require connectivity. It is the highest-priority
   cached content.

3. **Upload queue:** When a document upload is attempted offline, it is queued locally
   and retried automatically when the connection is restored. No data loss.

**Offline indicators (confirmed from DV-01 / DV-03):**
- Amber banner shown on DV-01 when offline.
- Vault upload failure: amber badge on vault icon, user notified "Document saved. Will
  upload when connected."

**Cache invalidation and sync (confirmed):**
- Emergency cards auto-update when a member's health profile is updated.
- FamilyAdmin notified when Emergency Folder content changes.

**[VERIFY] — Implementation details:**

| Behavior | Notes |
|---|---|
| Cache storage library | [VERIFY] — Hive / Isar / SQLite / device file system |
| Cache invalidation trigger | [VERIFY] — on app foreground? Periodic? On vault open? |
| Storage quota / device limit | [VERIFY] — no limit stated in spec |
| Encryption of local cache | [VERIFY] — documents are sensitive; confirm whether local cache is encrypted |
| Conflict resolution | [VERIFY] — server delete vs locally cached document handling |
| Offline document viewer | [VERIFY] — embedded PDF/image viewer confirmed; offline-capable library not specified |

---

## 13. Level 2 — Medical & Health Records

### 13.1 Module Purpose

**Level 2 — Build Priority 2. Plan gating: Family plan and above.**

Per-member digital health profile — blood type, allergies, vaccinations, prescriptions,
hospital visits — always accessible, always up to date. The Medical Emergency Card can
be shared in 3 taps and is usable at a hospital without a FamilyFirst account or internet
connection.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

- Controller: `MedicalController`
- Flutter feature folder: `lib/features/medical/`
- Flutter screen prefix: `MR-`
- SuperAdmin: **ZERO access** to individual family medical records. Absolute.
- Build priority: Level 2 Priority 2 — after Document Vault, before Safety and Finance.

**API endpoint paths are [VERIFY]** — product doc defines screens and rules, not exact
REST paths. Read Level 2 tech spec when available to confirm.

---

### 13.2 Key APIs

**Screen-confirmed API surface** (paths [VERIFY]):

---

#### GET /api/v1/families/{familyId}/health-profiles [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin — all members; Child — own profile only |

**Response (confirmed from MR-01 Health Home):**
Member list with health status summary — [VERIFY] exact DTO shape.

---

#### GET /api/v1/families/{familyId}/health-profiles/{memberId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent (all children); Child (own — read-only, restricted fields); Elder (summary card only); Teacher: ZERO by default |

**Response DTO — `ApiResponse<HealthProfileDto>`** (confirmed from MR-02):

| Field | Type | Notes |
|---|---|---|
| `BloodGroup` | `string` | A+/A-/B+/B-/AB+/AB-/O+/O- — Required; never hidden |
| `KnownAllergies` | `object[]` | Free text + category tags: Food / Medication / Environmental |
| `ChronicConditions` | `string[]` | Asthma / Diabetes / Epilepsy / Heart condition / Other |
| `CurrentMedications` | `MedicationDto[]` | Active only — Name, Dosage, Frequency, Doctor, Start, End |
| `PrimaryDoctor` | `DoctorDto` | Name + phone |
| `EmergencyContact` | `ContactDto` | Name + relationship + phone |
| `HeightWeight` | `object[]` | Optional, date-stamped; not shown on emergency card |
| `OrganDonor` | `bool` | Adults only; shown on emergency card if `true` |
| `VaccinationStatus` | `VaccinationDto[]` | Each vaccine: status + date + document link |
| `ActivePrescriptions` | `PrescriptionDto[]` | Active medications with linked document |
| `[VERIFY]` | — | Other fields |

**Child role restrictions (confirmed):**
Child sees: own blood group, allergies, active medications, vaccination status.
Child does NOT see: prescription documents, test reports, past hospital visits.
Child cannot edit any health data.

**Elder role restriction (confirmed):**
Summary only — "Arjun is healthy" / "upcoming vaccination." No detailed data unless
FamilyAdmin explicitly grants access.

---

#### PUT /api/v1/families/{familyId}/health-profiles/{memberId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO** (confirmed from MR-03 Add/Edit Health Record + spec): All fields from
`HealthProfileDto` — BloodGroup, KnownAllergies, ChronicConditions, PrimaryDoctor,
EmergencyContact, HeightWeight, OrganDonor. [VERIFY] whether single-field patch or full PUT.

---

#### POST /api/v1/families/{familyId}/health-profiles/{memberId}/prescriptions [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `AddPrescriptionRequest`** (confirmed from spec):

| Field | Type | Required | Notes |
|---|---|---|---|
| `MedicationName` | `string` | YES | — |
| `Dosage` | `string` | YES | e.g. "500mg" |
| `Frequency` | `string` | YES | e.g. "Twice daily" |
| `PrescribingDoctor` | `string` | YES | — |
| `StartDate` | `date` | YES | — |
| `EndDate` | `date` | NO | After end date: auto-archived |
| `LinkedDocumentId` | `Guid?` | NO | Link to scanned prescription in Document Vault |
| `IsRecurring` | `bool` | NO | If true: daily Calendar reminder auto-created |

**Business rules (confirmed):**
- If `IsRecurring = true`: a daily reminder is auto-created in Family Calendar — no extra setup.
- Document auto-link: if a scanned prescription exists in Document Vault for this member
  matching the prescription date, it is automatically linked.
- After `EndDate`: prescription auto-archived — removed from active profile, visible in
  Health Timeline only.

---

#### GET /api/v1/families/{familyId}/health-profiles/{memberId}/timeline [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent only (confirmed from MR-07) |

**Response (confirmed from MR-02 Health Timeline section):**
Chronological feed — every health event: hospital visit, prescription, test report,
vaccination, doctor note, allergy update — newest first.
Standard pagination — [VERIFY] filters (date range, event type).

---

#### GET /api/v1/families/{familyId}/health-profiles/{memberId}/vaccinations [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent; Child (own vaccination status only) |

**Response DTO (confirmed from MR-06 + spec):**

| Field | Type | Notes |
|---|---|---|
| `VaccineName` | `string` | — |
| `Status` | `string` | `Given` / `Due` / `Overdue` / `NotApplicable` |
| `Date` | `date` | Given date or due date |
| `LinkedDocumentId` | `Guid?` | Optional document link |

**Vaccination status behavior (confirmed):**
- `Given` — Green. Date recorded. Document link optional.
- `Due` (upcoming) — Amber. Reminder sent **14 days before** due date.
- `Overdue` — Red. Alert sent to parent. Prominent on health profile.
- `NotApplicable` — Grey. Configured by FamilyAdmin for age-inappropriate vaccines.

---

#### GET /api/v1/families/{familyId}/health-profiles/{memberId}/emergency-card [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES (generate); NO (if link is active and no-login access enabled) |
| Role gate | Parent, FamilyAdmin (generate); anyone with valid share link (view) |

**Response DTO — Emergency Card content (confirmed from MR-05):**

| Field | Notes |
|---|---|
| Member Name and Photo | — |
| Age | — |
| Blood Group | Large, prominent |
| Known Allergies | Red banner if any allergies present |
| Current Medications | Name + dosage |
| Primary Doctor | Name + contact |
| Emergency Contact | Name + phone |
| Family Insurance Policy Number | — |
| `OrganDonor` | Shown only if `true` |

**Business rules:**
- Card rendered in under 1 second from cached profile data.
- **Minimum required before sharing:** Blood Group and Allergies must be entered.
  Sharing blocked until minimum data is present; parent shown: "Health profile is incomplete."
- Card auto-updates when health profile changes.
- Card language: English, Hindi, or regional language — configured by FamilyAdmin.
- Fully cached offline — accessible in airplane mode.

---

#### POST /api/v1/families/{familyId}/health-profiles/{memberId}/emergency-card/share [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO:** [VERIFY] — `{ ExpiryHours (default 72), Language }`.

**Response:** Secure share link, QR code data, shareable image URL — [VERIFY].

**Sharing options (confirmed from MR-05):**
- Share as image (WhatsApp/messaging)
- Generate QR code (doctor scans at OPD — no app or login required)
- Secure time-limited link (72 hours default)
- Print as PDF (via share sheet)

**Business rules:**
- Share link expires after **72 hours** by default.
- Expired link message: "This emergency card link has expired. Contact the family for a new one."
- FamilyAdmin can regenerate or extend at any time.
- Link is **view-only** — no write access, no FamilyFirst account needed.
- Auto-updates: if the shared link is still valid and health profile changes, the recipient
  always sees current data.

---

### 13.3 DB Tables

**[VERIFY] — Schema not in product document. Read Level 2 tech spec to populate.**

Expected tables (confirmed as required; names and columns [VERIFY]):

| Table | What it stores |
|---|---|
| `HealthProfiles` | Per-member core health data: blood group, allergies, conditions, emergency contact, doctor, organ donor |
| `Prescriptions` | Prescription records per member — active and archived |
| `Vaccinations` | Vaccination records per member with status tracking |
| `HealthRecords` | Chronological health events (hospital visits, test reports, doctor notes, etc.) |
| `EmergencyCardLinks` | Time-limited secure share links with expiry and revocation |
| `MedicationReminders` | [VERIFY] or handled via CalendarEvents for recurring medications |

**Confirmed columns on `HealthProfiles`:**

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | FK → Families.Id |
| `FamilyMemberId` | `UNIQUEIDENTIFIER` | FK → FamilyMembers.Id — one profile per member |
| `BloodGroup` | `NVARCHAR` | Required — A+/A-/B+/B-/AB+/AB-/O+/O- |
| `KnownAllergiesJson` | `NVARCHAR` | JSON — free text + Food/Medication/Environmental tags |
| `ChronicConditionsJson` | `NVARCHAR` | JSON — multi-select list |
| `PrimaryDoctorName` | `NVARCHAR` | — |
| `PrimaryDoctorPhone` | `NVARCHAR` | — |
| `EmergencyContactName` | `NVARCHAR` | Defaults to FamilyAdmin |
| `EmergencyContactRelationship` | `NVARCHAR` | — |
| `EmergencyContactPhone` | `NVARCHAR` | — |
| `OrganDonor` | `BIT` | Adults only |
| `[VERIFY]` | — | Height/Weight stored separately (date-stamped series) |
| Standard BaseEntity | — | CreatedAt, UpdatedAt, IsDeleted, DeletedAt |

---

### 13.4 Business Rules

**Health Profile — Core Field Rules (confirmed):**

1. **Blood Group is required.** Options: `A+`, `A-`, `B+`, `B-`, `AB+`, `AB-`, `O+`, `O-`.
   Shown prominently on emergency card. Never hidden.

2. **Allergies display.** When allergies are present, shown with amber warning indicator
   everywhere they appear. Allergy field empty: gentle warning prompts parent to add.

3. **Organ Donor toggle** is for adults only. Shown on emergency card if `true`.

4. **Height/Weight** is optional, date-stamped for trend tracking. **Not shown** on emergency card.

**Prescription Rules (confirmed):**

5. **Auto-archive:** Prescriptions with an `EndDate` are automatically archived after
   the end date. Archived prescriptions are visible in Health Timeline only, not in
   active profile.

6. **Recurring medication → Calendar reminder:** If `IsRecurring = true`, a daily
   reminder is auto-created in Family Calendar. One tap — no extra setup for the parent.

7. **Document auto-link:** If a scanned prescription exists in Document Vault for this
   member with a matching date, it is automatically linked to the prescription entry.

**Vaccination Rules (confirmed):**

8. **Due reminder:** Push sent to parent **14 days before** a vaccination's due date.

9. **Overdue alert:** Red status + alert sent to parent when a vaccination is past its
   due date.

10. **Not applicable:** FamilyAdmin can mark a vaccine as `NotApplicable` (grey) for
    age-inappropriate vaccines.

**Privacy Rules (confirmed — absolute):**

11. **Parent:** Full access — create, edit, delete all records; generate and share
    emergency cards.

12. **Child:** Own blood group, allergies, active medications, vaccination status only.
    Cannot view prescription documents, test reports, or past hospital visits.
    Cannot edit any health data.

13. **Elder:** Summary view only — "healthy" / "upcoming vaccination." No detailed
    data unless FamilyAdmin explicitly grants.

14. **Teacher:** ZERO access by default. FamilyAdmin can grant exception (e.g. allergy
    info to school nurse via emergency link only).

15. **SuperAdmin: ZERO access.** Absolute. Non-negotiable.

**Emergency Card Rules (confirmed):**

16. **Minimum data required before sharing:** Blood Group and Allergies must be entered.
    Sharing blocked otherwise.

17. **Auto-updates:** Card content updates automatically when health profile changes.
    A recipient with an active share link always sees current data.

18. **72-hour share link expiry** (default). FamilyAdmin can extend or revoke.

19. **Shareable in 3 taps.** Must work offline. Never behind a login wall when accessed
    via share link. (CLAUDE.md confirmed business rule.)

20. **QR code sharing:** Doctor scans at OPD — no FamilyFirst app or account required.

---

### 13.5 Flow Summaries

#### Flow 1 — Add/Update Health Profile

```
Trigger       : Parent opens MR-03 to add or edit health data for a child
→ API call    : PUT /families/{familyId}/health-profiles/{memberId}
                { BloodGroup, KnownAllergies, ChronicConditions, PrimaryDoctor,
                  EmergencyContact }
→ Validation  : Role gate (Parent/FamilyAdmin); BloodGroup from allowed values
→ DB operation: UPDATE HealthProfiles SET ... WHERE FamilyMemberId=@memberId.
→ Response    : 200 ApiResponse<HealthProfileDto>
→ Side effect : Emergency card auto-updated. If linked share links are active,
                recipients see updated data immediately.
```

#### Flow 2 — Add Prescription with Recurring Reminder

```
Trigger       : Parent adds a new medication prescription
→ API call    : POST /families/{familyId}/health-profiles/{memberId}/prescriptions
                { MedicationName, Dosage, Frequency, PrescribingDoctor, StartDate,
                  EndDate, IsRecurring=true }
→ Validation  : Role gate (Parent/FamilyAdmin)
→ DB operation: INSERT Prescriptions.
                If IsRecurring=true: INSERT CalendarEvents (daily reminder,
                EventType=MedicineReminder, StartDateTime=StartDate).
→ Response    : 201 ApiResponse<PrescriptionDto>
→ Side effect : Daily Calendar reminder created. Document Vault scanned for
                matching prescription document to auto-link.
```

#### Flow 3 — Generate and Share Emergency Card

```
Trigger       : Parent taps "Generate Emergency Card" from MR-02 or Emergency Folder
→ Validation  : BloodGroup and KnownAllergies must be present → blocked with prompt if absent
→ API call 1  : GET /health-profiles/{memberId}/emergency-card
→ Response 1  : Card rendered from cached profile — < 1 second
→ Review      : Parent reviews card for accuracy
→ API call 2  : POST /health-profiles/{memberId}/emergency-card/share
                { ExpiryHours=72, Language="en" }
→ Response 2  : { ShareLink (72hr), QrCodeData, ShareableImageUrl }
→ Sharing     : Parent sends via WhatsApp image / QR code / link / PDF print
→ Side effect : EmergencyCardLinks row created with expiry. Doctor/nurse opens
                link — no login, no app required. View-only.
```

#### Flow 4 — Vaccination Due Reminder

```
Trigger       : Background worker evaluates vaccination due dates daily
→ For each vaccination with Status=Due AND DueDate = today + 14 days:
    INSERT Notifications row for parent via INotificationService.
→ For each vaccination with Status=Overdue (DueDate < today AND Status != Given):
    UPDATE vaccination Status=Overdue.
    INSERT urgent Notifications row for parent.
→ Side effect : Push sent via NotificationDeliveryWorker. Health profile badge updated.
```

---

### 13.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| MR-01 | Health Home (Member List) | Parent, FamilyAdmin |
| MR-02 | Member Health Profile | Parent, Child (own — read-only), Elder (limited) |
| MR-03 | Add/Edit Health Record | Parent, FamilyAdmin |
| MR-04 | Prescription Detail | Parent |
| MR-05 | Medical Emergency Card | Parent (generate/share), anyone (via share link) |
| MR-06 | Vaccination Tracker | Parent |
| MR-07 | Health Timeline | Parent |

**Confirmed UX behavior:**
- Feature folder: `lib/features/medical/`
- Screen prefix: `MR-`
- MR-02 Health Summary Card always visible at top — most critical info first.
- Allergy field shown with amber warning wherever it appears.
- Empty health data: warm prompt, never cold empty state.
  Allergy field empty: "Allergy information is important for emergency care. Add it now?"
- MR-05 Emergency Card: generated in under 1 second. Shareable in under 5 seconds.
- Demo mode: must show a populated health profile with at least blood group and one allergy.

**[VERIFY]:**
- Exact route names from `RouteNames` constants
- `MockDataService` method signatures
- `StateNotifier` name for medical/health state
- Offline cache strategy for emergency card (pre-cached vs on-demand)
- PDF generation library for emergency card printing

---

### 13.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| JWT / Auth (Section 2) | `FamilyId`, `Role` claims | All medical APIs family-scoped; SuperAdmin excluded |
| `FamilyMembers` table (Section 3) | Active membership + role | Privacy rules enforced per role |
| `Document Vault` (Section 12) | `LinkedDocumentId` linkage | Prescriptions auto-link to scanned documents |
| `CalendarEvents` / `ICalendarService` (Section 9) | Event creation | Recurring medication reminders create daily Calendar events (EventType=MedicineReminder) |
| `INotificationService` (Section 10) | Push delivery | Vaccination reminders (14d before), overdue alerts |
| `NotificationPreferences` (Section 10) | Quiet hours | Vaccination reminders respect quiet hours; MedicineReminder bypasses |
| Module visibility config (Section 11) | `MedicalRecords` module flag | FamilyModuleVisibilityFilter can disable module per family |
| AWS S3 [VERIFY] | Storage for health documents | Documents uploaded from MR screens stored via Document Vault flow |

---

### 13.8 Emergency Card Behavior

**Offline access (confirmed):**
- Emergency card is **fully cached offline.** Parent can access in airplane mode.
- Card content rendered in under 1 second from cached profile data.

**Share link behavior (confirmed):**
- Default TTL: **72 hours.**
- Link is **view-only.** No write access from share link.
- **No FamilyFirst account required** to view via share link.
- **No app required** — opens in any mobile browser.
- QR code for doctor to scan at OPD — same no-login, no-app access.
- Expired link message: "This emergency card link has expired. Contact the family for a new one."
- FamilyAdmin can regenerate or extend at any time.
- Auto-updates: active links always show current health data.

**PIN vs no-login access (confirmed):**
- Emergency card via share link: **No login required** when accessed via valid share link.
- This is the core design principle — an emergency card behind a login wall defeats its purpose.
- FamilyAdmin configures access for the Emergency Folder in Document Vault (Login / PIN / No login),
  but the Medical Emergency Card share link is always no-login by design.

**Minimum data before sharing (confirmed):**
- Blood Group and Known Allergies must be entered.
- Sharing blocked with prompt: "Health profile is incomplete. Add Blood Group and Allergies
  before sharing the emergency card."

**Card language (confirmed):**
- Generated in English, Hindi, or regional language.
- Language configured by FamilyAdmin.

**[VERIFY]:**
- Emergency card link storage mechanism (DB table name, columns)
- Whether card link is invalidated when health profile data changes (or stays valid with live data)
- RevocationAt / ExtendedExpiryAt columns on share link table
- Whether QR code and image are generated server-side or client-side

---

## 14. Level 2 — Safety, Location & Emergency

### 14.1 Module Purpose

**Level 2 — Build Priority 3. Plan gating: Family plan and above.**

Child safety through smart location awareness — configured safe zones with
arrival/departure/late alerts, battery-balanced 15-minute location updates, and a
one-tap SOS that reaches all parents instantly, bypassing quiet hours and device
silent mode. Designed as a quiet guardian, not a surveillance system.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

- Controller: `SafetyController`
- Flutter feature folder: `lib/features/safety/`
- Flutter screen prefix: `SL-`
- SuperAdmin: **ZERO visibility** into any family's location data. Absolute.
- Child receives SOS-only interface — not a live tracking panel.
- Adult members must **explicitly opt in** to location sharing. Parent cannot enable for an
  adult without consent.
- Data retention: **30 days.** Location history older than 30 days auto-purged.
- Data residency: India-located servers. DPDP Act 2023 compliant.

**API endpoint paths are [VERIFY]** — product doc defines screens and rules, not exact REST paths.

---

### 14.2 Key APIs

---

#### GET /api/v1/families/{familyId}/safety/map [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Response (confirmed from SL-02 Family Map View):**

| Field | Notes |
|---|---|
| Member pins | Each: MemberId, Name, Photo, LastKnownLat, LastKnownLng, LastUpdatedAt, BatteryLevel, CurrentLocationName (reverse-geocoded), IsInsideZone, ZoneType |
| Safe zone overlays | Each zone: center coords, radius, type, color code |
| Stale indicator | `IsStale = true` when `LastUpdatedAt < GETUTCDATE() - 60min` |

**Business rules:**
- Only members who have opted in to location sharing are returned.
- Adult members with sharing disabled are not shown on the map.
- Stale location (>60 min): pin shown with "Location outdated" label.

---

#### POST /api/v1/families/{familyId}/safety/location [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Any member (device reporting own location) |

**Request DTO (confirmed from location update logic):**

| Field | Type | Notes |
|---|---|---|
| `Latitude` | `decimal` | — |
| `Longitude` | `decimal` | — |
| `BatteryLevel` | `int` | Percentage 0–100 |
| `Timestamp` | `datetime` | UTC |

**Business rules (confirmed):**
- Location updated every **15 minutes** by client (battery-balanced, passive).
- **Immediate** update dispatched by client on zone boundary crossing — not waiting
  for the 15-minute cycle.
- Battery <15%: client extends update interval to 30 minutes.
- Battery <10%: client pauses location updates. Last-known retained with timestamp.
- Network failure: location cached on device, synced on reconnect.

---

#### GET /api/v1/families/{familyId}/safety/zones [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Response DTO — `ApiResponse<List<SafeZoneDto>>`** (confirmed from SL-03):
All configured safe zones for the family with member assignments and alert settings.

---

#### POST /api/v1/families/{familyId}/safety/zones [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO — `CreateSafeZoneRequest`** (confirmed from SL-04):

| Field | Type | Required | Constraint |
|---|---|---|---|
| `ZoneName` | `string` | YES | Max 40 characters |
| `ZoneType` | `string` | YES | `Home` / `School` / `Tuition` / `RelativesHouse` / `Workplace` / `PlaceOfWorship` / `Other` |
| `Latitude` | `decimal` | YES | Zone center |
| `Longitude` | `decimal` | YES | Zone center |
| `RadiusMetres` | `int` | YES | 50–500m; default 150m |
| `AppliedToMemberIds` | `Guid[]` | YES | Default: all children |
| `AlertOnArrival` | `bool` | YES | Default ON |
| `AlertOnDeparture` | `bool` | YES | Default ON for School; OFF for Home |
| `LateAlertEnabled` | `bool` | YES | — |
| `LateAlertTime` | `time` | Conditional | Required when `LateAlertEnabled = true` |
| `OverrideQuietHours` | `bool` | YES | Default ON for School and Home zones |

**Business rules:**
- `ZoneName` max 40 chars → 400.
- `RadiusMetres` 50–500 → 400 outside range.
- `LateAlertTime` required when `LateAlertEnabled = true`.
- Overlapping zones: allowed but warning returned in response.
- Workplace zones: adults must have opted in to location sharing.
- Zone saved → shown on family map immediately as colored circle overlay.
- Zone creation target: under 90 seconds (UX requirement).

**Error cases:**

| Condition | Status |
|---|---|
| `ZoneName` > 40 chars | 400 |
| `RadiusMetres` outside 50–500 | 400 |
| `LateAlertTime` missing when `LateAlertEnabled = true` | 400 |
| Invalid `ZoneType` value | 400 |

---

#### PUT /api/v1/families/{familyId}/safety/zones/{zoneId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO:** Same constraints as create. [VERIFY] which fields are optional on update.

---

#### DELETE /api/v1/families/{familyId}/safety/zones/{zoneId} [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Business rules:** Soft-delete or hard-delete — [VERIFY]. Zone removed from family map.

---

#### GET /api/v1/families/{familyId}/safety/alerts [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Response (confirmed from SL-05 Location Alert History):**
Paginated list of all location alerts — arrival, departure, late, SOS, battery, stale.
Standard pagination. [VERIFY] filters (date range, alert type, member).

---

#### POST /api/v1/families/{familyId}/safety/sos [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Child (own device only) |

**Request DTO:**

| Field | Type | Notes |
|---|---|---|
| `Latitude` | `decimal` | Current GPS location at SOS trigger |
| `Longitude` | `decimal` | — |
| `Timestamp` | `datetime` | UTC |

**Response:** 201 — [VERIFY] exact shape.

**Business rules (confirmed from SOS flow):**
- Child holds SOS button for **2 seconds** to activate (prevents accidental trigger).
- 2-second **cancel window** after activation before alert dispatches.
- On dispatch: push to **all parents + emergency contact** — bypasses quiet hours,
  bypasses device silent mode. Marked URGENT.
- Notification content: child name, GPS location, timestamp, one-tap call button.
- Child screen after dispatch: "Your parents have been notified. Stay safe." Cancel option.
- Parent map: child pin shows red SOS indicator with precise location.

---

#### PUT /api/v1/families/{familyId}/safety/alerts/{alertId}/resolve [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO:** [VERIFY] — `{ ResolutionNote? }`.

**Business rules:**
- Marks SOS alert as resolved. Archived in alert history.
- Parent must call/confirm child is safe before marking resolved (UX requirement — not API enforcement).

---

#### GET /api/v1/families/{familyId}/safety/settings [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin |

**Response (confirmed from SL-08 Location Settings):**
Per-family location sharing configuration — which members have sharing enabled, consent state. [VERIFY] exact shape.

---

#### PUT /api/v1/families/{familyId}/safety/settings [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin |

**Request DTO:** [VERIFY] — per-member sharing enable/disable, caregiver view config.

**Business rules:**
- Adult member sharing: FamilyAdmin **cannot** enable location for an adult without
  that adult's explicit consent.
- Caregiver/Driver sharing: logistics view only (current assignment) — no full history.

---

### 14.3 DB Tables

**[VERIFY] — Schema not in product document.**

Expected tables (confirmed as required; names and columns [VERIFY]):

| Table | What it stores |
|---|---|
| `SafeZones` | Zone definitions: name, type, coordinates, radius, member assignments, alert toggles |
| `LocationHistory` | Per-member location records — **retained 30 days, then auto-purged** |
| `LocationAlerts` | All alert events: arrival, departure, late, SOS, battery, stale |
| `SOSEvents` | SOS activations with dispatch timestamp, resolution state, GPS location |
| `LocationSharingConsent` | Per-member consent records for location sharing (adults) |

**Confirmed constraints on `SafeZones`:**

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `FamilyId` | `UNIQUEIDENTIFIER` | FK → Families.Id |
| `ZoneName` | `NVARCHAR(40)` | Max 40 chars |
| `ZoneType` | `NVARCHAR` / `INT` | 7 valid types |
| `CenterLatitude` | `DECIMAL` | — |
| `CenterLongitude` | `DECIMAL` | — |
| `RadiusMetres` | `INT` | 50–500 |
| `AlertOnArrival` | `BIT` | — |
| `AlertOnDeparture` | `BIT` | — |
| `LateAlertEnabled` | `BIT` | — |
| `LateAlertTime` | `TIME` | Nullable — required when LateAlertEnabled=true |
| `OverrideQuietHours` | `BIT` | — |
| `AppliedMemberIdsJson` | `NVARCHAR` | JSON array of FamilyMemberId |
| Standard BaseEntity | — | CreatedAt, UpdatedAt, IsDeleted, DeletedAt |

**Data retention rule (confirmed):**
`LocationHistory` rows older than **30 days** are auto-purged by a background job.
No permanent location archive — enforced by DPDP Act 2023 requirement.

---

### 14.4 Business Rules

**Safe Zone Types and Default Alert Behavior (confirmed):**

| Zone Type | Alert on Arrival | Alert on Departure | Late Alert |
|---|---|---|---|
| Home | NO (too frequent) | YES (school morning) | NO |
| School | YES | YES | YES — configurable time |
| Tuition / Class | YES | YES | NO (default) |
| Relative's House | YES | NO | NO |
| Masjid / Temple | YES | NO | NO |
| Workplace (adult) | Optional (adult opt-in) | Optional | NO |
| Other (custom) | Fully configurable | Fully configurable | Fully configurable |

**Location Update Cadence (confirmed):**

1. **15-minute passive updates** — battery-balanced background updates.
2. **Immediate on zone boundary crossing** — does not wait for 15-minute cycle.
3. **Battery <15%:** update interval extends to **30 minutes**. Battery warning sent to parent.
4. **Battery <10%:** location updates pause. Last-known location retained with timestamp.
5. **Network failure:** location cached on device, synced on reconnect. Parent always
   sees last-known location with timestamp — never a blank pin.

**Alert Types and Urgency (confirmed):**

| Alert Type | Trigger | Urgency |
|---|---|---|
| Zone arrival | Member enters zone boundary | Informational |
| Zone departure | Member leaves zone boundary | Informational |
| Late alert | Not arrived at zone by set time | Elevated — bypasses batching |
| SOS | Child holds button 2 seconds | **URGENT — bypasses quiet hours AND silent mode** |
| Battery warning | Device battery < 15% | Informational — gentle |
| Location stale | No update for > 60 minutes | Amber — worth checking |
| Location sharing paused | App backgrounded / location disabled | Informational |

**SOS Rules (confirmed):**

6. **2-second hold** to activate SOS — prevents accidental trigger.
7. **2-second cancel window** after activation before alert dispatches.
8. SOS push sent to **all parents + emergency contact** — bypasses quiet hours and
   device silent mode.
9. Child screen after dispatch: calm message — "Your parents have been notified. Stay safe."
10. Parent resolves SOS by marking alert as resolved after confirming child's safety.

**Privacy Rules (confirmed — non-negotiable):**

11. **Adults must explicitly opt in.** Parent cannot enable location sharing for an adult
    family member without that adult's consent.
12. **Caregiver / Driver:** logistics view only (current assignment). No full location history.
13. **SuperAdmin: ZERO visibility** into any family's location data. Absolute.
14. **Data retention: 30 days.** Location history auto-purged after 30 days.
15. **No third-party sharing — ever.** Location data never shared with advertisers or partners.
16. **Data residency:** India-located servers. DPDP Act 2023 compliant.

**Child-Facing Design Rules (confirmed):**

17. Child sees SOS button only — not a live tracking interface.
18. Child sees their own location history (same as parents — transparency, no secrets).
19. "Family can see my location" badge visible to child on home screen.
20. Framing: "your family knows you are safe" — not "we are watching you."

---

### 14.5 Flow Summaries

#### Flow 1 — Create Safe Zone

```
Trigger       : Parent sets up School zone with late alert
→ API call    : POST /families/{familyId}/safety/zones
                { ZoneName="Delhi Public School", ZoneType="School",
                  Latitude, Longitude, RadiusMetres=200,
                  AppliedToMemberIds=[childId], AlertOnArrival=true,
                  AlertOnDeparture=true, LateAlertEnabled=true,
                  LateAlertTime="08:30", OverrideQuietHours=true }
→ Validation  : ZoneName ≤ 40 chars; RadiusMetres 50–500;
                LateAlertTime present when LateAlertEnabled=true
→ DB operation: INSERT SafeZones.
→ Response    : 201 ApiResponse<SafeZoneDto>
→ Side effect : Zone appears on family map immediately as blue circle.
                Next zone crossing triggers arrival/departure push.
```

#### Flow 2 — Child SOS Activation

```
Trigger       : Child holds SOS button for 2 seconds on their home screen
→ UI          : 2-second confirmation animation plays.
                Child can cancel within this window — no alert dispatched.
→ API call    : POST /families/{familyId}/safety/sos
                { Latitude, Longitude, Timestamp }
→ Validation  : Role = Child; childProfileId from JWT
→ DB operation: INSERT SOSEvents (ChildProfileId, GPS, DispatchedAt);
                INSERT LocationAlerts (Type=SOS, Priority=URGENT).
→ Response    : 201
→ Side effect : URGENT push to all parents + emergency contact — bypasses
                quiet hours and device silent mode.
                Child sees: "Your parents have been notified. Stay safe."
                Parent map: child pin shows red SOS indicator.
```

#### Flow 3 — Zone Arrival Alert

```
Trigger       : Child's device crosses School zone boundary (inbound)
→ Client      : Device detects geofence event → immediate location update POST
→ Server      : Zone boundary check — child is now inside School zone
→ DB operation: INSERT LocationAlerts (Type=ZoneArrival, ZoneId, MemberId, Timestamp);
                UPDATE LocationHistory (latest position).
→ Side effect : Push to configured parents — "Arjun arrived at Delhi Public School — 8:14 AM."
                Informational — respects quiet hours (unless OverrideQuietHours=true).
```

#### Flow 4 — Late Alert

```
Trigger       : Background worker checks configured LateAlertTime for School zone
→ At 08:30:   Worker checks — has child arrived at School zone today?
→ If NOT arrived:
    INSERT LocationAlerts (Type=LateAlert, ZoneId, MemberId).
    Dispatch elevated push to all parents — bypasses batching.
→ Side effect : "Arjun has not arrived at Delhi Public School — expected by 8:30 AM."
                Parent can check map for last-known location.
```

#### Flow 5 — Parent Resolves SOS

```
Trigger       : Parent confirms child is safe after SOS
→ API call    : PUT /families/{familyId}/safety/alerts/{alertId}/resolve
→ DB operation: UPDATE SOSEvents SET ResolvedAt=GETUTCDATE(), ResolvedByUserId.
                UPDATE LocationAlerts SET IsResolved=true.
→ Response    : 200
→ Side effect : Alert archived in SL-05 Location Alert History. SOS indicator
                removed from parent map.
```

---

### 14.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| SL-01 | Family Safety Home | Parent |
| SL-02 | Family Map View | Parent, FamilyAdmin |
| SL-03 | Safe Zone Manager | Parent, FamilyAdmin |
| SL-04 | Add/Edit Safe Zone | Parent, FamilyAdmin |
| SL-05 | Location Alert History | Parent |
| SL-06 | SOS Alert Screen (Parent) | Parent — push notification deep-link |
| SL-07 | Emergency Button (Child) | Child — always accessible on home screen |
| SL-08 | Location Settings | FamilyAdmin |

**Confirmed UX behavior:**
- Feature folder: `lib/features/safety/`
- Screen prefix: `SL-`
- SL-02 map: member avatars as pins, safe zone colored circle overlays (Home=green,
  School=blue, Tuition=purple, Other=grey). Tap pin: name, location, timestamp, battery.
- Battery warning: pin shows battery icon when child device <15%.
- Stale location (>1 hour): pin turns grey with "Location outdated" label.
- SL-04: visual radius circle on map adjusts in real time as slider moves.
- SL-07: SOS button is a floating button — always accessible on child home screen.
  Subtle when not needed. Unmissable in an emergency.
- Child home screen: small "Family can see my location" badge.
- Demo mode: must show a populated map with at least one member location and one safe zone.

**[VERIFY]:**
- Route names from `RouteNames` constants
- `MockDataService` method signatures
- `StateNotifier` names for safety and location state
- Background location service library (Flutter — confirm which package)
- Geofencing library for zone boundary detection
- Google Maps / map package used

---

### 14.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| JWT / Auth (Section 2) | `FamilyId`, `Role`, `ChildProfileId` claims | All safety APIs family-scoped; child SOS uses childProfileId |
| `FamilyMembers` table (Section 3) | Member roles and consent state | Adults require opt-in; role determines visibility |
| `INotificationService` (Section 10) | Push delivery | Zone alerts (informational), late alerts (elevated), SOS (urgent) |
| `NotificationPreferences` (Section 10) | Quiet-hours config | Informational zone alerts respect quiet hours; SOS bypasses |
| Medical Emergency Contact (Section 13) | Emergency contact phone | SOS push also goes to emergency contact |
| Background location service (Flutter) | Passive 15-min GPS + geofence | Client-side location updates; zone boundary detection |
| Google Places API [VERIFY] | Reverse geocoding | Convert GPS coords to location name on parent map |
| Module visibility config (Section 11) | `Safety` module flag | FamilyModuleVisibilityFilter can disable module per family |
| Background worker (server) | Late alert evaluation, data purge | [VERIFY] — SafetyWorker for late alerts + 30-day retention purge |

---

## 15. Level 2 — Family Finance & SMS Ledger

### 15.1 Module Purpose

**Level 2 — Build Priority 5. Plan gating: Premium plan only.**

SMS-based family financial visibility — transaction capture from bank SMS, smart
categorisation (14 Indian-context categories), commitment detection, budget tracking,
and a question-a-transaction flow that feels like a conversation, not an interrogation.
The Family CFO (designated by FamilyAdmin) sees the family's financial picture; every
other adult sees only what their privacy tier permits.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

- Controller: `FinanceController`
- Flutter feature folder: `lib/features/finance/`
- Flutter screen prefix: `FF-`
- **Adult consent is mandatory before any SMS data is read.** Non-negotiable.
  Privacy tier is configurable per member but cannot be set below documented minimums.
- Build priority: Level 2 Priority 5 — built last, after all other Level 2 modules.

**API endpoint paths are [VERIFY]** — product doc defines screens and rules, not exact REST paths.

---

### 15.2 Key APIs

---

#### GET /api/v1/families/{familyId}/finance/dashboard [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO only |

**Response — `ApiResponse<FinanceDashboardDto>`** (confirmed from FF-01):

| Section | Content |
|---|---|
| Family Health Score | Monthly spend vs budget gauge (Green/Amber/Red); MTD: total spend, total income, net savings, savings rate % |
| Member Spend Cards | One per consented member: avatar, name, today's spend, this month's spend, status dot — content filtered by privacy tier |
| Today's Transaction Feed | Chronological parsed transactions across all members; each: member avatar, time, merchant, amount, category icon |
| Alerts Panel | Overspend warnings, large transactions, SIP/EMI missed, low balance — colour-coded by severity |
| Commitments Preview | Next 3 upcoming commitments (EMI, LIC premium, school fees) |

**Business rules:**
- Member cards show only what each member's privacy tier permits (see Section 15.8).
- Tier 2 members: spend shown by category, not merchant on cards.
- Tier 3 members: monthly total only on cards.

---

#### GET /api/v1/families/{familyId}/finance/transactions [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Request query params:** `memberId`, `category`, `fromDate`, `toDate`, `page`, `pageSize` — [VERIFY].

**Response — `ApiResponse<PaginatedList<TransactionDto>>`** (confirmed from FF-03).

**Privacy tier applied per member transaction** — see Section 15.8.

---

#### GET /api/v1/families/{familyId}/finance/members/{memberId}/transactions [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO; own member (own transactions only) |

**Response filtered by privacy tier for that member.**

---

#### POST /api/v1/families/{familyId}/finance/transactions/{transactionId}/question [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Request DTO (confirmed from FF-04 Transaction Questioning Flow):**

| Field | Type | Required | Notes |
|---|---|---|---|
| `QuestionType` | `string` | YES | `FamilyExpense` / `PersonalUnderstood` / `NeedToKnowMore` / `PossibleError` |
| `ContextNote` | `string` | NO | CFO's optional message to member |

**Business rules (confirmed — ethical design):**
- Message sent to member via **WhatsApp/SMS** — not an app push notification.
- Language is always curious, never accusatory.
- Member replies naturally via WhatsApp; reply is tagged to the transaction in CFO dashboard.
- CFO resolves with status: `Resolved` / `FamilyExpense` / `Personal` / `UnderReview`.
- Every question must feel like a conversation — not an interrogation.

---

#### GET /api/v1/families/{familyId}/finance/budget [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Response (confirmed from FF-05 Budget Manager):** Category-wise budget vs actual spend — [VERIFY] exact shape.

---

#### GET /api/v1/families/{familyId}/finance/categories [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Response (confirmed from FF-06 Category Breakdown):** Spend breakdown by category for the period — [VERIFY] shape.

---

#### GET /api/v1/families/{familyId}/finance/commitments [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Response (confirmed from FF-09 Commitments Tracker):**
Detected recurring commitments — EMIs, SIPs, LIC, school fees, OTT subscriptions, chit funds.
Each: commitment name, amount, due date, status (upcoming / missed / paid).

---

#### POST /api/v1/families/{familyId}/finance/consent/invite [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO |

**Request DTO (confirmed from onboarding flow):**

| Field | Type | Required | Notes |
|---|---|---|---|
| `MemberId` | `Guid` | YES | Family member to invite |
| `PrivacyTier` | `int` | YES | 1 / 2 / 3 — see Section 15.8 |

**Business rules:**
- Member receives SMS invite with mobile web consent page (no app required).
- CFO pre-configures the privacy tier; member sees it during consent.
- Adult earning members default to Tier 2 or 3.

---

#### POST /api/v1/families/{familyId}/finance/consent/accept [VERIFY path]

| Field | Value |
|---|---|
| Auth required | NO (accessible from mobile web consent page) |

**Request DTO:** [VERIFY] — consent token, IP, consent version.

**Business rules (confirmed from FF-07 consent flow):**
- **Consent record stored:** timestamp, IP, consent version — DPDP Act 2023 compliant.
- Monthly reminder SMS sent: "You are sharing finance data with [CFO]. Reply STOP anytime."
- After acceptance: companion FamilyLedger service installs in background.
  Foreground notification: "FamilyLedger running — tap to manage."
- First transaction parsed and appears in CFO dashboard within **60 seconds**.

---

#### POST /api/v1/families/{familyId}/finance/consent/decline [VERIFY path]

**Business rules:**
- CFO notified with neutral message: "Member declined finance sharing."
- No follow-up pressure. Decline is honored immediately.

---

#### DELETE /api/v1/families/{familyId}/finance/consent/{memberId} [VERIFY path]

**Opt-out (confirmed):**
- Member texts STOP to system number OR navigates Settings > Finance > Stop Sharing.
- Service stops **immediately.** No residual data retained. CFO notified.
- DPDP Act 2023 compliant.

---

#### GET /api/v1/families/{familyId}/finance/settings [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin, Family CFO |

**Response (confirmed from FF-08 Finance Settings & Privacy Tiers):**
Per-member privacy tier assignments, consent status, CFO designation — [VERIFY] shape.

---

#### PUT /api/v1/families/{familyId}/finance/settings [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin |

**Request DTO:** [VERIFY] — CFO designation, per-member privacy tier changes.

**Business rules:**
- Privacy tier **cannot be set below documented minimums** (see Section 15.8).
- Changing a member's tier requires re-consent if tier is being decreased — [VERIFY].

---

### 15.3 DB Tables

**[VERIFY] — Schema not in product document. Read Level 2 tech spec to populate.**

Expected tables (confirmed as required; names and columns [VERIFY]):

| Table | What it stores |
|---|---|
| `FinanceConsents` | Per-member consent records — tier, timestamp, IP, consent version, opt-out state |
| `Transactions` | Parsed SMS transactions — member, merchant, amount, category, timestamp, question state |
| `TransactionQuestions` | CFO questions + member replies tagged to transactions |
| `Budgets` | Per-category monthly budget targets set by CFO |
| `Commitments` | Detected recurring commitments with due dates and status |
| `FinanceSettings` | Per-family CFO designation, module enabled state |

**Confirmed data constraints on `Transactions`:**

| Column | Notes |
|---|---|
| `FamilyId` | FK → Families.Id |
| `MemberId` | FK → FamilyMembers.Id |
| `MerchantName` | Hashed for Tier 2 members in CFO view |
| `Amount` | DECIMAL(18,2) |
| `Category` | One of 14 confirmed categories |
| `PrivacyTierAtCapture` | Tier at time of capture — used for rendering decisions |
| `IsCommitment` | Detected as recurring commitment |
| `QuestionStatus` | Resolved / FamilyExpense / Personal / UnderReview / None |
| `ParsedAt` | UTC timestamp |

---

### 15.4 Business Rules

**Privacy Tier Model — Non-Negotiable Minimums (confirmed):**

See Section 15.8 for full tier definitions.

1. Privacy tier model is **not optional** — it is the ethical foundation.
   Tiers cannot be configured below the minimums defined in Section 15.8.

2. **Adult consent is mandatory** before any SMS data is read. Platform must not
   capture SMS data from any member without explicit, recorded consent.

3. **Opt-out is always available and immediate.** Text STOP or tap Settings > Finance
   > Stop Sharing. Service stops at once. No residual data. No follow-up pressure.

**Transaction Categorisation (14 confirmed Indian-context categories):**

| Category | Key Merchants / Detection |
|---|---|
| Groceries & Kirana | DMart, BigBazaar, Blinkit, Zepto, JioMart, local kirana UPIs |
| Food & Dining | Zomato, Swiggy, restaurant UPIs, tiffin services |
| Utilities | BESCOM, MSEDCL, IGL, MGL, Jio, Airtel broadband, DTH |
| Mobile Recharge | Jio, Airtel, Vi, BSNL recharge UPIs |
| Education & School | School fee UPIs, coaching, BYJU's, Unacademy, book shops |
| Medical & Health | Apollo, 1mg, PharmEasy, diagnostic labs, doctor UPIs |
| Travel & Transport | IRCTC, Ola, Uber, Rapido, fuel stations, MakeMyTrip |
| Shopping | Amazon, Flipkart, Myntra, Meesho, Nykaa |
| Insurance & LIC | LIC, HDFC Life, SBI Life, NACH debits to insurers |
| Loan EMI | NACH debits, 'EMI' keyword, bank loan accounts |
| Domestic Help (Cash) | ATM withdrawal on last day of month, fixed amount (heuristic) |
| Entertainment | Netflix, Hotstar, Amazon Prime, PVR, BookMyShow |
| Donations & Religion | Temple/mosque donations, NGO UPIs, pooja materials |
| Chit Fund / Investment | Fixed monthly debit to same non-bank UPI over 20–40 months |

**Commitment Detection Rules (confirmed):**

4. **Home Loan EMI:** NACH debit on 1st–5th of month, same amount, 'EMI' keyword in SMS.
   Alert to CFO if not received by 7th of month.

5. **SIP / Mutual Fund:** NACH debit on 5th/10th to AMC names (CAMS, KARVY, HDFC MF,
   SBI MF). Alert to CFO if missed.

6. **LIC / Insurance:** NACH debit to LIC or insurance company sender IDs. Quarterly or
   annual pattern. Reminder sent **7 days before** due date.

7. **School Fees:** Large debit to school account in April, July, October, January.
   Quarterly pattern auto-detected. Budget alert sent in advance.

8. **OTT Subscriptions:** Monthly/annual recurring debit to Netflix, Hotstar, Amazon Prime.
   Renewal alert **3 days before**.

9. **Chit Fund:** Fixed monthly debit to same beneficiary over 20–40 months.
   Prompt to CFO: "Chit Fund pattern detected — please confirm."

**Transaction Questioning Ethics (confirmed):**

10. Every question must feel like a **conversation, not an interrogation.** Language is
    always curious, never accusatory.
11. Questions sent via **WhatsApp/SMS** — not app push — for natural, low-pressure reply.
12. Member replies via WhatsApp; reply tagged to transaction in CFO dashboard.

**Plan Gating (confirmed):**

13. Finance module available on **Premium plan only** (₹299/month).

---

### 15.5 Flow Summaries

#### Flow 1 — Member Onboarding & Consent

```
Trigger       : Family CFO invites an adult member to join finance tracking
→ CFO action  : POST /finance/consent/invite { MemberId, PrivacyTier=2 }
→ System      : SMS sent to member with mobile web consent link (no app needed)
→ Member      : Opens link — reads 5-step consent flow (FF-07):
                  1. Who is inviting (CFO name/photo)
                  2. What data shared (per selected tier — plain language)
                  3. Who sees it (only CFO — named)
                  4. How to opt out (text STOP — immediate)
                  5. Accept or Decline (equally prominent buttons)
→ On Accept   : POST /finance/consent/accept { token, IP, consentVersion }
                Consent record stored (timestamp, IP, version). DPDP compliant.
                FamilyLedger service installs in background.
                Monthly reminder SMS scheduled.
→ First SMS   : Next bank transaction SMS captured → parsed → appears in CFO
                dashboard within 60 seconds.
```

#### Flow 2 — Transaction Capture and Categorisation

```
Trigger       : Member's bank sends transaction SMS (e.g. "Debited Rs.850 at Zomato")
→ FamilyLedger: Captures SMS on member device (consent required).
→ Parse       : NLP/regex parsing — extract merchant, amount, timestamp.
                Category auto-detected (Zomato → Food & Dining).
→ API call    : POST /finance/transactions (internal — device SDK to server)
                { MemberId, Merchant, Amount, Category, Timestamp }
→ Privacy filter: Privacy tier applied at storage — Tier 2 hashes merchant name.
→ DB          : INSERT Transactions. Commitment pattern check triggered.
→ Side effect : Transaction appears in CFO dashboard within 60 seconds.
                If commitment pattern detected → INSERT Commitments with due date.
```

#### Flow 3 — CFO Questions a Transaction

```
Trigger       : CFO taps flag icon on a Rs.3,200 transaction at unknown merchant
→ UI          : FF-04 opens — CFO selects "Need to know more", types context note
→ API call    : POST /finance/transactions/{txId}/question
                { QuestionType: "NeedToKnowMore", ContextNote: "Was this school trip?" }
→ System      : WhatsApp/SMS sent to member:
                "Papa wants to understand the Rs.3,200 on 15th Jan — was this the school trip?"
→ Member      : Replies via WhatsApp naturally.
→ System      : Reply tagged to transaction in CFO dashboard.
→ CFO         : Marks as Resolved / FamilyExpense / Personal / UnderReview.
```

#### Flow 4 — Commitment Alert (LIC Premium)

```
Trigger       : Background worker evaluates commitment due dates
→ 7 days before LIC premium due:
    Push to CFO: "LIC premium of Rs.12,000 due on [date]. Ensure account has balance."
→ On due date: Check if NACH debit received. If not:
    Elevated alert: "LIC premium NACH debit not received. Verify with bank."
→ On detection: Transaction auto-matched to commitment → Commitments status = Paid.
```

#### Flow 5 — Member Opts Out

```
Trigger       : Adult member texts STOP to FamilyLedger number
→ System      : FamilyLedger service stops immediately on member device.
                No new SMS data captured.
→ API call    : DELETE /finance/consent/{memberId} (triggered by STOP)
→ DB          : FinanceConsents.IsActive = false. No residual data retained.
→ CFO notified: "Member has stopped finance sharing." Neutral — no follow-up prompt.
→ Side effect : Member's transaction data removed from CFO dashboard — [VERIFY]
                whether historical data retained or purged on opt-out.
```

---

### 15.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| FF-01 | Finance Home Dashboard | Family CFO |
| FF-02 | Member Finance Detail | Family CFO (per privacy tier) |
| FF-03 | Transaction Feed | Family CFO |
| FF-04 | Question a Transaction | Family CFO |
| FF-05 | Budget Manager | Family CFO |
| FF-06 | Category Breakdown | Family CFO |
| FF-07 | Finance Consent Onboarding | All adult members (consent flow — mobile web) |
| FF-08 | Finance Settings & Privacy Tiers | FamilyAdmin |
| FF-09 | Commitments Tracker | Family CFO |

**Confirmed UX behavior:**
- Feature folder: `lib/features/finance/`
- Screen prefix: `FF-`
- FF-01 Family Health Score: visual gauge (Green/Amber/Red). Target: loads in under 2 seconds.
- FF-01 Member cards: horizontal scroll, privacy-tier filtered content per member.
- FF-03 Transaction Feed: swipe left for quick actions (Mark Family Expense / Question / Approve).
- FF-07 Consent flow: mobile web page — no app required for consent. Two equally prominent
  Accept/Decline buttons. No dark patterns.
- Empty state FF-01: "Finance module not yet configured" — onboarding wizard prompt.
- Demo mode: must show populated dashboard with mock transactions and member spend cards.

**[VERIFY]:**
- Route names from `RouteNames` constants
- `MockDataService` method signatures
- `StateNotifier` name for finance state
- SMS capture mechanism (FamilyLedger companion service — Android-only likely)
- WhatsApp integration for transaction questioning

---

### 15.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| JWT / Auth (Section 2) | `FamilyId`, `Role` claims | All finance APIs family-scoped |
| `FamilyMembers` table (Section 3) | Member roles and consent state | Privacy tier applied per member |
| `INotificationService` (Section 10) | Push delivery | Commitment alerts, overspend warnings |
| SMS capture service (FamilyLedger) | Android SMS listener with consent | Core data ingestion — consent required first |
| WhatsApp / SMS gateway | Transaction questioning | Questions and opt-out sent via WhatsApp/SMS |
| NLP / regex parser | SMS text → structured transaction | Merchant detection, amount extraction, category assignment |
| Module visibility config (Section 11) | `Finance` module flag | FamilyModuleVisibilityFilter; Premium plan gate enforced here |
| DPDP Act 2023 compliance | Consent records, opt-out, data residency | Legal requirement — consent before data capture |

---

### 15.8 Privacy Tier Rules

**Privacy tier model is the ethical foundation of the Finance module.
Tiers cannot be configured below the minimums documented here.**

**Tier Definitions (confirmed):**

| Tier | For | What CFO Sees |
|---|---|---|
| Tier 1 — Full Visibility | Dependent children, non-earning members on family accounts | All transactions: merchant name, amount, category, timestamp. Full visibility. |
| Tier 2 — Category Only | Adult earning members (spouse, adult children) | Category + amount only. Merchant name is **hashed** (private). Personal categories (Entertainment, Personal Care) **blurred**. Transactions above **Rs. 5,000** surface to CFO regardless. |
| Tier 3 — Aggregate Only | Financially independent members (married daughter, son with own family) | **Monthly total only.** No line items. Alert only if member-defined threshold is breached. |

**Default Tier Assignments (confirmed):**
- Adult earning members → Tier 2 or Tier 3 by default.
- Dependent children / non-earning members → Tier 1.
- CFO pre-configures tier; member sees their assigned tier during consent flow.

**Consent Flow Requirements (confirmed):**

1. **Explicit consent required before any SMS data is read.** No exceptions.
2. Consent must explain in plain language: what data is shared, who sees it, the exact tier.
3. Consent must name who will see the data: "Only [CFO name] will see your data."
4. Opt-out must be clearly available at consent time and at any time thereafter.
5. Consent record must store: timestamp, IP address, consent version.
6. Monthly reminder SMS must be sent: "You are sharing finance data with [CFO]. Reply STOP anytime."
7. **Opt-out is immediate.** Service stops the moment STOP is received. No residual data.
8. Consent is **DPDP Act 2023 compliant.**

**Privacy Tier Boundaries — Non-Configurable Minimums:**
- Tier 2 merchant hashing: cannot be disabled for adult earning members.
- Tier 2 personal category blurring (Entertainment, Personal Care): cannot be disabled.
- Tier 3 aggregate-only: cannot be overridden to show line items.
- CFO cannot access raw data for a Tier 3 member beyond monthly total + threshold alerts.

---

## 16. Level 2 — Reports & Insights

### 16.1 Module Purpose

**Level 2 — Build Priority 4. Plan gating: [VERIFY] — likely Basic plan and above.**

Automated weekly and monthly intelligence across every FamilyFirst module — transforming
raw data into beautiful, magazine-quality, narrative reports. Reports are only valuable
when enough data exists to surface patterns; Level 1 generates the data, Level 2 turns
it into intelligence.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

**Level 1 foundation:** Phase 18 (documented in Section 11) implemented basic aggregate
report endpoints (`weekly-digest`, `child-weekly`, `attendance-summary`).
Level 2 extends this with monthly reports, finance/health/document integration, narrative
language, magazine-quality rendering, PDF export, shareable images, and a 12-month archive.

- Controller: `ReportsController` (extended from Phase 18)
- Flutter feature folder: `lib/features/reports/`
- Flutter screen prefix: `RP-`
- Primary users: Parent (weekly/monthly digest), FamilyAdmin (family summary),
  Child (personal score history), Elder (simplified update), Family CFO (finance report).

**API endpoint paths are [VERIFY]** — product doc defines screens and rules, not exact REST paths.

---

### 16.2 Key APIs

---

#### GET /api/v1/families/{familyId}/reports/weekly-digest [extended from Phase 18]

*(Phase 18 foundation — Section 11. Level 2 enriches the response with health, document,
and finance data when those modules are enabled.)*

**Level 2 additions to `WeeklyDigestDto`** (confirmed from RP-02 screen):

| Section | Content Added in Level 2 |
|---|---|
| Health & Documents | Medications due for renewal; vaccinations overdue; documents expiring in next 30 days |
| Finance Snapshot | Week total spend; top 2 categories; any alerts triggered — one sentence (shown only when Finance module enabled) |
| What's Coming Next Week | Now includes: document expiry dates, health appointments, fee due dates |

**Design rules (confirmed):**
- Missing module data: section gracefully omitted — no error shown.
- Narrative language: "Arjun had his best attendance week yet" — not raw percentages only.
- First week message: "Your first week report is ready. It will get richer as FamilyFirst learns your family."
- Shareable as image (WhatsApp family group).
- Archived — accessible from Reports tab for up to 12 months.

---

#### GET /api/v1/families/{familyId}/reports/monthly [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request query params:** `year`, `month` (default: previous calendar month) — [VERIFY].

**Response DTO — `ApiResponse<MonthlyFamilyReportDto>`** (confirmed from spec):

| Section | Content |
|---|---|
| Monthly totals | Across all modules |
| Child-by-child trends | Performance vs previous month |
| Attendance heatmap | Per child |
| Feedback volume & resolution | Count and resolution rate |
| Documents expiring next month | List with renewal actions |
| Health reminders due | Vaccinations, medications, follow-ups |
| Finance summary | Category breakdown (if Finance enabled) |

**Design rules (confirmed):**
- Downloadable as **PDF** (clean export).
- Delivered on **1st of each month** via push notification.
- Narrative-first — numbers support the story, they do not lead it.
- Actionable — each alert section has a direct action button (Renew, Set Reminder, etc.).

---

#### GET /api/v1/families/{familyId}/children/{childId}/reports/monthly [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent only |

**Response DTO — `ApiResponse<ChildMonthlySummaryDto>`** (confirmed from spec):

| Field | Notes |
|---|---|
| Attendance history | Sessions attended/missed |
| Homework completion trend | [VERIFY] source — likely from TaskCompletions |
| Teacher observation count | Count of TeacherFeedback records |
| Reward earned vs redeemed | CoinTransactions Earn vs Spent |
| Pillar score radar chart | Evolution over **3 months** — not just current |

**Design rule (confirmed):** Written in warm, narrative language — not just numbers.
"Arjun earned 3 new rewards this month and his Academic pillar reached its highest level."

---

#### GET /api/v1/families/{familyId}/reports/finance [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Family CFO only |

**Request query params:** `year`, `month` — [VERIFY].

**Response DTO — `ApiResponse<FinanceMonthlyReportDto>`** (confirmed from spec):

| Field | Notes |
|---|---|
| Total family income | Parsed from transactions |
| Total spend | — |
| Savings rate | (Income − Spend) / Income % |
| Category breakdown | Per category, current month |
| Member spend comparison | Privacy tier applied per member |
| Commitments fulfilled vs missed | From Commitments table |
| Largest transactions | Flagged for CFO attention |
| Net savings trend | **6-month trend line** |

---

#### GET /api/v1/families/{familyId}/reports/documents/expiry [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Response** (confirmed from spec):
All documents expiring in next 90 days, sorted by urgency.
Each item: one-tap to view document, one-tap to open upload for renewal.
Delivered in monthly report and included in weekly digest.

---

#### GET /api/v1/families/{familyId}/reports/health/reminders [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent |

**Response** (confirmed from spec):
- Vaccinations due or overdue
- Medications ending this month
- Follow-up appointments due
- Doctor visits recommended

Delivered in monthly report and visible on health profile screens (MR-02).

---

#### GET /api/v1/families/{familyId}/children/{childId}/reports/attendance-summary [from Phase 18]

*(Phase 18 foundation — Section 11. Level 2 adds PDF export and parent-teacher meeting format.)*

**Level 2 addition:** Exportable for parent-teacher meetings.

---

#### POST /api/v1/families/{familyId}/reports/export [VERIFY path]

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | Parent, FamilyAdmin |

**Request DTO:** [VERIFY] — `{ ReportType, Period, Format: "PDF" | "Image" }`.

**Business rules (confirmed):**
- Monthly report: exports as **clean PDF**.
- Weekly digest: shareable as **image** for WhatsApp/messaging.
- Attendance summary: exportable for parent-teacher meetings.

---

### 16.3 DB Tables

**[VERIFY] — Report content is aggregated from existing module tables at query time.**
No dedicated report storage tables are expected for most report types.

**Confirmed from Level 1 Phase 18:** No new DB tables were created for reports —
all data aggregated from source-of-truth tables.

**Level 2 additions [VERIFY]:**

| Table | Notes |
|---|---|
| `ReportArchive` or `WeeklyDigests` | [VERIFY] — whether Level 2 stores generated digests for 12-month archive or regenerates on demand |
| `ReportExports` | [VERIFY] — tracking PDF/image export jobs |

**Source tables read per report type (confirmed):**

| Report | Source Tables |
|---|---|
| Weekly Digest | AttendanceRecords, TaskCompletions, TeacherFeedback, CalendarEvents, VaultDocuments, Prescriptions, Vaccinations, Transactions (if Finance enabled) |
| Monthly Family Report | All above + CoinTransactions, RewardRedemptions, HealthProfiles |
| Child Monthly Summary | AttendanceRecords, TaskCompletions, TeacherFeedback, CoinTransactions, ChildProfiles (pillar scores) |
| Finance Monthly Report | Transactions, Commitments, FinanceConsents |
| Document Expiry Report | VaultDocuments (ExpiryDate filter) |
| Health Reminder Summary | Vaccinations, Prescriptions, HealthProfiles |
| Attendance Summary | AttendanceSessions, AttendanceRecords |

---

### 16.4 Business Rules

**Report Delivery Schedule (confirmed):**

1. **Weekly Digest:** Generated **Sunday 6 PM UTC** by `WeeklyDigestWorker`. Push delivered
   at **Sunday 7 PM**. "Your Family Weekly Digest is ready." Tapping opens RP-02.

2. **Monthly Family Report:** Delivered on **1st of each month** via push notification.

3. **Document Expiry Report:** Delivered monthly AND included in weekly digest when
   documents are expiring within 30 days.

4. **Health Reminder Summary:** Delivered in monthly report and surfaced on MR-02 health
   profile screens when reminders are due.

**Report Design Principles (confirmed — non-negotiable):**

5. **Magazine quality.** Every report must look like something a parent wants to read —
   not a spreadsheet.

6. **Narrative first.** Numbers support the story. "Arjun had his best attendance month yet
   — 19 out of 20 sessions." Not just "95% attendance rate."

7. **Visual over tabular.** Radar charts, trend lines, progress bars, heatmaps.
   Tables used only when comparison requires them.

8. **Actionable always.** Every alert section has a clear next action:
   - Document expiry → 'Renew' button
   - Missed vaccination → 'Set Reminder' button
   - Commitment missed → alert with bank contact shortcut

9. **Shareable.** Weekly digest → shareable image for WhatsApp family group.
   Monthly report → clean PDF export.

10. **Archived.** All reports stored and accessible from Reports tab.
    12 months of weekly digests accessible — the family's institutional memory.

**Module-missing graceful degradation (confirmed):**

11. If a module is not enabled or has no data, its section is gracefully omitted from
    the digest. No error state shown. Report still generated from available data.

**Pillar score radar chart (confirmed):**

12. Child Monthly Summary includes pillar score radar chart evolution over **3 months**
    — not just current values. Shows improvement or regression trends.

**Finance report privacy (confirmed):**

13. Finance Monthly Report (RP-05) applies per-member privacy tiers when showing member
    spend comparisons — same Tier 1/2/3 rules as the live Finance module (Section 15.8).

**Archive (confirmed):**

14. Previous weekly digests stored and accessible from Reports tab.
    Family can scroll back through **12 months** of weekly digests.

---

### 16.5 Flow Summaries

#### Flow 1 — Weekly Digest Generation and Delivery

```
Trigger       : WeeklyDigestWorker fires Sunday 6:00 PM UTC
→ Data query  : Aggregate across all enabled modules for the past 7 days:
                  AttendanceRecords, TaskCompletions, TeacherFeedback,
                  CalendarEvents (next 7d preview), VaultDocuments (expiry),
                  Vaccinations/Prescriptions (health), Transactions (if Finance ON)
→ Render      : Narrative digest generated — warm language, visual components.
                Missing module data: section omitted gracefully.
→ Archive     : INSERT WeeklyDigest record (or regenerate-on-demand — [VERIFY])
→ Push        : 7:00 PM UTC — INotificationService delivers push to all active
                Parent + FamilyAdmin family members who have WeeklyDigest pref ON.
→ Side effect : RP-02 accessible from Reports tab and push deep-link.
                Shareable as image via RP-08.
```

#### Flow 2 — Parent Views Child Monthly Summary

```
Trigger       : Parent taps RP-04 from Reports Home on 3rd of the month
→ API call    : GET /families/{familyId}/children/{childId}/reports/monthly
                ?year=2026&month=4
→ DB queries  : AttendanceRecords (attendance history), TaskCompletions (homework trend),
                TeacherFeedback (observation count), CoinTransactions (earn/spend),
                ChildProfiles (current + 3-month pillar score history)
→ Response    : ChildMonthlySummaryDto — narrative language, pillar radar chart data
→ Side effect : None.
```

#### Flow 3 — Export Monthly Report as PDF

```
Trigger       : Parent taps Export on RP-03 Monthly Family Report
→ API call    : POST /families/{familyId}/reports/export
                { ReportType: "Monthly", Period: "2026-04", Format: "PDF" }
→ Processing  : Report rendered as PDF — clean layout, narrative content.
                [VERIFY] whether synchronous or background job.
→ Response    : Download URL or file stream
→ Side effect : PDF shareable via device share sheet.
                Useful for parent-teacher meetings (Attendance Summary).
```

#### Flow 4 — Document Expiry Alert in Weekly Digest

```
Trigger       : WeeklyDigestWorker runs — finds VaultDocuments with ExpiryDate ≤ 30 days
→ Included in digest: Document expiry section with list of expiring documents.
→ Each item: document name, expiry date, 'Renew' button (deep-link to DV-03 upload).
→ Also: 90-day expiry standalone report available via GET /reports/documents/expiry.
```

---

### 16.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| RP-01 | Reports Home | Parent, FamilyAdmin |
| RP-02 | Weekly Digest View | Parent |
| RP-03 | Monthly Family Report | Parent, FamilyAdmin |
| RP-04 | Child Monthly Summary | Parent |
| RP-05 | Finance Report | Family CFO |
| RP-06 | Document Expiry Report | Parent, FamilyAdmin |
| RP-07 | Health Reminder Summary | Parent |
| RP-08 | Report Export / Share | Parent, FamilyAdmin |

**Confirmed UX behavior:**
- Feature folder: `lib/features/reports/`
- Screen prefix: `RP-`
- RP-02: Magazine layout, warm illustrations, no tables, no heavy data.
  Entry via Sunday 7 PM push or Reports tab.
  2-minute read target. One-tap to share as image.
- RP-02 Children's Highlights: per child — attendance rate, task rate, best moment,
  one area to watch (lowest pillar score). Visual, not tabular.
- RP-02 Finance Snapshot: one sentence only when Finance module enabled.
- RP-04: Pillar score radar chart with 3-month evolution overlay.
- RP-08: Share sheet for image (WhatsApp) or PDF export.
- Demo mode: must show a fully populated weekly digest with all sections visible.
  First-week state: "Your first week report is ready. It will get richer as FamilyFirst learns your family."

**[VERIFY]:**
- Route names from `RouteNames` constants
- `MockDataService` method signatures
- `StateNotifier` name for reports state
- Chart library used (radar chart, heatmap, trend lines)
- PDF generation library
- Whether digests are cached locally for offline access

---

### 16.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| All Level 1 module tables (Sections 5–11) | AttendanceRecords, TaskCompletions, TeacherFeedback, CoinTransactions, CalendarEvents | Core data sources for weekly and monthly reports |
| Document Vault (Section 12) | VaultDocuments with ExpiryDate | Document expiry report and weekly digest section |
| Medical Records (Section 13) | Vaccinations, Prescriptions, HealthProfiles | Health reminder summary and digest section |
| Finance (Section 15) | Transactions, Commitments | Finance monthly report and weekly digest snapshot |
| `INotificationService` (Section 10) | Push delivery | Weekly digest and monthly report push delivery |
| `NotificationPreferences.WeeklyDigest` (Section 10) | User preference flag | Suppress digest push if user has opted out |
| `WeeklyDigestWorker` (Phase 18 / Section 11) | Background aggregation | Sunday 6 PM generation trigger |
| Module visibility config (Section 11) | Report module flag | FamilyModuleVisibilityFilter — reports module can be disabled per family |

---

## 17. Level 2 — Advanced Admin Configuration

### 17.1 Module Purpose

**Level 2 — Build Priority 6: Alongside each module (not a separate workstream).**
Each AC screen ships with the module it configures.

Deep control panel for FamilyAdmin and SuperAdmin — storage backend selection,
per-category document routing, notification intelligence, alert thresholds, safe zone
rule defaults, finance privacy configuration, report automation, emergency access rules,
and escalation paths.

**Source confirmed:** `FamilyFirst_Level2_ProductDocument.docx` (read 2026-05-29)

- No dedicated controller — configuration screens are extensions of the module controllers
  they configure, plus the existing `AdminController` (Phase 19) and `FamilyAdminController`
  (Phase 20). [VERIFY] whether a dedicated Level 2 `AdvancedAdminController` is introduced.
- Flutter feature folder: `lib/features/admin/` (extension of Level 1 admin folder)
- Flutter screen prefix: `AC-`
- Primary users: SuperAdmin (platform-wide), FamilyAdmin (family-specific).

**API endpoint paths are [VERIFY]** — product doc defines configuration areas and screens,
not exact REST paths.

---

### 17.2 Key APIs

**Configuration areas confirmed from spec (paths [VERIFY]):**

---

#### Storage Provider Configuration [AC-01 / AC-02]

**GET + PUT /api/v1/families/{familyId}/admin/storage [VERIFY path]**

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | FamilyAdmin (own family); SuperAdmin (platform defaults) |

**Configurable settings (confirmed from AC-01 screen definition):**

| Setting | Options | Notes |
|---|---|---|
| `StorageMode` | `AppManaged` / `GoogleDrive` / `Hybrid` | Default: AppManaged |
| Google Drive OAuth | OAuth2 flow | Triggered when GoogleDrive or Hybrid selected |
| Hybrid routing rules | Per-category category→provider mapping | Default: Medical + Identity → app; others → Google Drive |
| `StorageQuotaAlertThreshold` | `75%` / `90%` / `95%` | Push sent to FamilyAdmin when crossed |
| `OfflineCacheSizeMb` | `500` / `1000` / `2000` | Cached by last-accessed priority |

**Business rules (confirmed):**
- Google Drive mode: OAuth2 flow creates a dedicated "FamilyFirst" folder in Drive.
- Hybrid routing: per-category, FamilyAdmin configures which categories go to which provider.
- Migrate existing documents: background job when switching providers. Family can continue
  using app during migration. Admin notified on completion.
- Storage provider disconnected (OAuth expired): uploads queued locally.
  Admin notification: "Google Drive connection expired. Reconnect to resume uploads."

**Storage quota limits by plan (confirmed):**

| Plan | Storage Quota |
|---|---|
| Free Trial | 100 MB |
| Basic | 500 MB |
| Family | 2 GB |
| Premium | 10 GB |

---

#### Document Category Configuration [AC-03]

**GET + PUT /api/v1/families/{familyId}/admin/document-categories [VERIFY path]**

| Setting | Notes |
|---|---|
| Add / rename / reorder / enable / disable categories | Family-level customisation |
| Expiry tracking rules per category | Which categories require expiry date |
| Default visibility per category | Role-based visibility preset per category |

---

#### Notification Intelligence Configuration [AC-04]

**GET + PUT /api/v1/families/{familyId}/admin/notification-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Recipient list per event type | Who receives which notification |
| Timing per event type | Delivery delay or advance notice |
| Channel per event type | Push / SMS |
| Quiet hours window | Already in `NotificationPreferences` (Section 10); this is family-level override |
| Urgency bypass rules | Which event types bypass quiet hours |
| Batching preferences | How notifications are grouped |

---

#### Alert Thresholds [AC-04 extended]

| Alert | Configurable Threshold | Default |
|---|---|---|
| Finance: large transaction | Amount above which transaction surfaces to CFO | Rs. 5,000 |
| Location: late arrival tolerance | Minutes past LateAlertTime before alert fires | [VERIFY] |
| Document expiry lead times | Per category — days before expiry to start reminders | See Section 12.4 |

---

#### Safe Zone Rules Configuration [AC-05]

**GET + PUT /api/v1/families/{familyId}/admin/safety-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Default radius by zone type | School default, Home default, etc. |
| Late alert default times | Per zone type |
| Which members are location-tracked by default | Children: yes by default; adults: opt-in only |

---

#### Finance Privacy Configuration [AC-06]

**GET + PUT /api/v1/families/{familyId}/admin/finance-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Default privacy tier per family link type | Tier 2 for adult earning members by default |
| Which categories are always private | Cannot be overridden below minimum |
| CFO designation | Which family member is the Family CFO |
| Adult consent reminder frequency | How often monthly reminders are sent |

---

#### Report Automation Configuration [AC-07]

**GET + PUT /api/v1/families/{familyId}/admin/report-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Weekly digest day/time | Default: Sunday 7 PM |
| Monthly report cut-off date | Default: 1st of month |
| Modules included in digest | Toggle per-module inclusion |
| Auto-share to WhatsApp | Opt-in — WhatsApp Business API required (Level 2b) |

---

#### Emergency Access Configuration [DV-07 admin settings]

**GET + PUT /api/v1/families/{familyId}/admin/emergency-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Emergency folder contents | Which documents are Emergency Priority (max 5) |
| Emergency link expiry duration | Default 72h; max 7 days |
| Access mode | Login required / PIN only / No login |
| Emergency contacts list | Who receives SOS + emergency alerts |

---

#### Escalation Settings

**GET + PUT /api/v1/families/{familyId}/admin/escalation-config [VERIFY path]**

| Setting | Notes |
|---|---|
| Primary non-response window | Minutes before escalation fires |
| Backup contact | Who receives escalation if primary does not respond |

---

#### Module Visibility Per Role [Extension of Phase 20]

**Already documented in Section 11.** Level 2 adds toggles for all Level 2 modules
(DocumentVault, MedicalRecords, Safety, Finance, Reports) per role.

---

#### SuperAdmin Analytics Dashboard [AC-08]

**GET /api/v1/admin/analytics/level2 [VERIFY path]**

| Field | Value |
|---|---|
| Auth required | YES |
| Role gate | SuperAdmin only |

**Content (confirmed from AC-08):** Platform-level analytics — [VERIFY] exact metrics.
SuperAdmin has **zero access** to individual family documents, medical, location, or
financial data. Analytics are aggregate counts only.

---

#### SuperAdmin Notification Campaign Manager Level 2 [AC-09]

Extension of Phase 19 campaign endpoint. Level 2 adds campaign targeting by Level 2
module adoption — [VERIFY] exact additions.

---

### 17.3 DB Tables

**[VERIFY] — Schema not in product document.**

Expected tables (confirmed as required from configuration areas):

| Table | Notes |
|---|---|
| `StorageConfigs` | Per-family storage mode, provider config, OAuth tokens, routing rules |
| `DocumentCategoryConfigs` | Per-family category customisation |
| `NotificationConfigs` | Per-family notification intelligence overrides (extends `NotificationRules` from Phase 20) |
| `AlertThresholds` | Per-family configurable thresholds (finance, location, document) |
| `SafetyConfigs` | Per-family safe zone defaults and location tracking rules |
| `EmergencyConfigs` | Per-family emergency access mode, link expiry, escalation |
| `ReportConfigs` | Per-family report automation settings |

---

### 17.4 Business Rules

**Complete Level 2 Business Rules (confirmed from spec section 15):**

1. **Document storage quota by plan:**
   Free Trial: 100 MB · Basic: 500 MB · Family: 2 GB · Premium: 10 GB.
   Storage used visible to FamilyAdmin at all times.

2. **Document deletion window:** Deleted documents recoverable within **30 days** by
   FamilyAdmin. After 30 days: permanently purged. Uploader warned before permanent deletion.

3. **Medical data — absolute protection:** Medical records are **never** used for
   analytics, advertising, or third-party purposes. Non-negotiable. Stated in plain
   language in privacy policy.

4. **Emergency card minimum data:** Emergency card **cannot be shared** if both Blood
   Group AND Allergy status are empty. At least one must be entered.

5. **Location data retention: 30 days maximum.** Auto-purged. FamilyAdmin **cannot
   override** this. Platform-level commitment.

6. **Finance consent — adult members must consent independently.** FamilyAdmin, SuperAdmin,
   or Family CFO **cannot consent on their behalf.** DPDP Act 2023 requirement.

7. **Finance opt-out: 60 seconds.** System must cease data capture within 60 seconds of
   receiving STOP. No grace period. Legal compliance.

8. **SMS parsing scope — strict whitelist.** Only bank and payment application SMS
   messages parsed. Personal SMS, OTP messages, and promotional messages are **never**
   read or stored. Strict sender ID whitelist enforced.

9. **Secure share link expiry:** Default 72 hours. FamilyAdmin can extend to **7 days
   maximum**. No permanent public links. Emergency links auto-revoke when content expires.

10. **Report archival:**
    - Weekly digests: archived for **12 months**.
    - Monthly reports: archived **permanently** (until account deletion).

11. **Subscription feature gating:**
    | Module | Minimum Plan |
    |---|---|
    | Document Vault | Basic |
    | Medical Records | Family |
    | Safety / Location | Family |
    | Finance | Premium only |
    | Reports & Insights | [VERIFY] |

12. **SuperAdmin data isolation — database-layer enforcement.** SuperAdmin has zero access
    to individual family documents, medical records, location history, or financial data.
    Enforced at the **database layer**, not just the UI.

13. **Feedback lock after 24 hours.** Consistent with Level 1. Also applies to observations
    linked to health concerns or urgent escalations. Accountability trail is permanent.

14. **Expiry alert suppression.** When a document is renewed and a new version uploaded,
    all previous expiry alerts for that document are automatically suppressed.

**Edge Cases — Confirmed Production Behaviors:**

15. **Blurred/unreadable document:** Upload accepted; OCR confidence below threshold →
    warning shown "Document may be unclear — verify readability." Not auto-tagged; manual
    tag required.

16. **Duplicate document detected:** Same file hash OR same member + category + date →
    "A similar document already exists. Replace it or keep both?" Previous version archived
    if replaced.

17. **Insurance without expiry date:** Amber badge on document; gentle prompt to add expiry.

18. **Emergency card link — live data:** Shared link always shows **current** health data,
    not a snapshot at share time. Recipients see updates automatically.

19. **GPS disabled during school hours:** Parent notified informatively, not with alarm.
    "Location unavailable for Arjun — last seen at School Gate at 8:14 AM." 'Call Arjun'
    quick-action shown.

20. **Accidental SOS:** 2-second cancel window. If dispatched accidentally, parent marks
    "Resolved — False Alarm." No penalty, no shame.

21. **Overlapping safe zones:** Warning shown during setup. Admin can adjust radius or
    disable one zone's departure alert.

22. **Finance: no bank SMS (iPhone / rural bank):** Manual entry fallback prompted. CFO
    sees amber indicator on member card.

23. **Finance: duplicate transaction SMS:** Deduplication — same transaction ID within
    60 seconds, same amount + bank + member → second SMS discarded silently.

24. **Finance: consent revoked mid-month:** Capture stops within 60 seconds. Data captured
    before opt-out is retained (member consented when captured). No retroactive deletion
    unless member separately requests data erasure.

25. **Finance: salary misidentified:** Auto-tagged as Income, excluded from expense
    calculations. CFO notified to re-tag if incorrect.

26. **Wrong member on medical record:** FamilyAdmin can move records between member
    profiles. Audit log maintained. Original timestamp preserved.

27. **Subscription cancelled:** Read-only access for **30 days**. Export prompts shown
    prominently. After 30 days: data purged per privacy policy. No silent deletion.

28. **Report with incomplete module data:** Sections with no data gracefully omitted —
    not shown as empty boxes. Report still sent.

---

### 17.5 Flow Summaries

#### Flow 1 — Configure Google Drive Storage

```
Trigger       : FamilyAdmin opens AC-01 Storage Provider Config
→ API call    : PUT /families/{familyId}/admin/storage
                { StorageMode: "GoogleDrive" }
→ UI          : "Connect Google Drive" button → triggers OAuth2 flow
→ OAuth       : Admin authenticates with Google. FamilyFirst folder created in Drive.
→ DB operation: UPDATE StorageConfigs SET StorageMode=GoogleDrive,
                OAuthToken=<encrypted_token>, DriveFolder=<folder_id>.
→ Response    : 200 — Google account shown, folder location confirmed.
→ Test upload : System offers a test upload (dummy document) to verify configuration.
→ Side effect : All future uploads for configured categories route to Google Drive.
                Existing documents optionally migrated via background job.
```

#### Flow 2 — Configure Hybrid Storage Routing

```
Trigger       : FamilyAdmin switches to Hybrid mode to keep Medical + Identity on app
→ API call    : PUT /families/{familyId}/admin/storage
                { StorageMode: "Hybrid",
                  HybridRouting: [
                    { Category: "Medical", Provider: "App" },
                    { Category: "Identity", Provider: "App" },
                    { Category: "Other", Provider: "GoogleDrive" }
                  ] }
→ DB          : UPDATE StorageConfigs with per-category routing map.
→ Side effect : Future uploads route per configuration. Migration background job
                offered for existing documents.
```

#### Flow 3 — FamilyAdmin Disables Finance for Child Role

```
Trigger       : FamilyAdmin opens AC-06 or Module Visibility settings
→ API call    : PUT /families/{familyId}/admin/module-visibility
                { ModuleName: "Finance", Role: "Child", IsVisible: false }
→ DB          : UPDATE ModuleVisibilityConfig. INSERT AuditLogs.
→ Side effect : FamilyModuleVisibilityFilter blocks Finance routes for Child
                role in this family on all subsequent requests.
```

#### Flow 4 — Configure Emergency Access (No-Login Mode)

```
Trigger       : FamilyAdmin enables no-login emergency access for Emergency Folder
→ API call    : PUT /families/{familyId}/admin/emergency-config
                { AccessMode: "NoLogin", EmergencyLinkExpiryHours: 72 }
→ DB          : UPDATE EmergencyConfigs.
→ Side effect : DV-07 Emergency Folder and MR-05 Emergency Card links now
                accessible without FamilyFirst account or login.
```

---

### 17.6 Flutter Integration

**Screens confirmed from `FamilyFirst_Level2_ProductDocument.docx`:**

| Screen ID | Screen Name | Who Can Access |
|---|---|---|
| AC-01 | Storage Provider Config | FamilyAdmin, SuperAdmin |
| AC-02 | Google Drive Integration | FamilyAdmin |
| AC-03 | Document Category Config | FamilyAdmin, SuperAdmin |
| AC-04 | Notification Intelligence Config | FamilyAdmin, SuperAdmin |
| AC-05 | Safe Zone Rules Config | FamilyAdmin |
| AC-06 | Finance Privacy Config | FamilyAdmin |
| AC-07 | Report Automation Config | FamilyAdmin |
| AC-08 | Super Admin Analytics Dashboard | SuperAdmin |
| AC-09 | Notification Campaign Manager (L2) | SuperAdmin |

**Confirmed UX behavior:**
- Feature folder: `lib/features/admin/` (extended from Level 1)
- Screen prefix: `AC-`
- AC-01: Visual radius slider for offline cache size. Storage usage gauge.
  Migration progress indicator. Test upload verification.
- Each config screen: safe defaults pre-filled. Every setting has clear purpose and impact.
- "Powerful but calm control room" design — no overwhelming options.

**[VERIFY]:**
- Route names from `RouteNames` constants
- `MockDataService` method signatures for admin demo data
- `StateNotifier` names for advanced admin state

---

### 17.7 Dependencies

| Dependency | What is needed | Why |
|---|---|---|
| All Level 2 module controllers | Config changes affect module behavior | Storage config routes uploads; visibility config blocks routes |
| `FamilyModuleVisibilityFilter` (Section 11) | Level 2 module toggle enforcement | Applied globally — includes L2 modules |
| `AuditLogs` table (Section 5) | Audit trail | All Level 2 admin config mutations write AuditLogs |
| Google OAuth2 service | Access token for Drive API | Storage provider Google Drive mode |
| AWS S3 (Phase 09) | Default app-managed storage | App-Managed mode stores documents in S3 |
| `INotificationService` (Section 10) | Storage quota alerts, config-change notifications | Storage threshold alerts delivered via notification pipeline |
| `AdminController` (Phase 19) | SuperAdmin analytics | AC-08 extends Phase 19 analytics endpoint |
| `FamilyAdminController` (Phase 20) | Family-level config | AC-05, AC-06, AC-07 extend Phase 20 family admin endpoints |

---

## 18. Role & Permission Reference

**[VERIFY] — Section 18 not yet written.**
Read `FamilyFirst_L1_TechSpec.docx` and `FamilyFirst_Level2_ProductDocument.docx`
to populate Sections 18.1–18.4.

Confirmed starting data exists in:
- CLAUDE.md (role definitions, data scope rules)
- Section 17.4 of this file (Level 2 role permission matrix by module)
- Individual module sections (role gates per endpoint)

Target structure (from Brain_Update.md):
- **18.1 Role Definitions** — Int value, who, daily time, emotional goal
- **18.2 Role-wise Data Scope Rules** — what each role can see
- **18.3 API Endpoint Authorization Matrix** — which endpoints each role can call
- **18.4 Row-Level Security Rules** — FamilyId scoping, IsDeleted filter

---

## 19. Database Standards & Shared Patterns

### 19.1 Naming Conventions

**Engine:** SQL Server 2022. All scripts are raw `.sql` files — no EF migrations, no
auto-migrations, no `SELECT *`.

**Table naming:**

| Object | Rule | Example |
|---|---|---|
| Table | PascalCase singular, no prefix | `Users`, `AttendanceSessions`, `TaskCompletions` |
| Column | PascalCase | `FamilyId`, `CreatedAt`, `IsDeleted` |
| Primary Key column | Always `Id` | `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()` |
| Foreign Key column | `<Entity>Id` | `FamilyId`, `ChildProfileId`, `UserId` |
| Script file | `NNN_Action.sql` — 3-digit zero-padded prefix | `001_CreateUsers.sql` → `040_SeedDefaultModuleVisibility.sql` |
| Index | `IX_<Table>_<Col1>[_<Col2>]` | `IX_AttendanceSessions_FamilyId_SessionDate` |
| Unique index | `UX_<Table>_<Col1>[_<Col2>]` — [VERIFY] whether UX or IX prefix used for uniques |

**Primary key exception:**
- `Plans` table uses `INT IDENTITY` — the only table without a GUID PK.
  All other tables: `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()`.

**Naming anti-patterns — never use:**
- `tbl` prefix (Coolzo convention — does not apply to FamilyFirst)
- `usp` prefix for stored procedures
- `col` or `fld` column prefix
- Snake_case anywhere in DB objects

---

### 19.2 Mandatory Audit Columns

Every business table carries these four columns. No exceptions.

```sql
CreatedAt    DATETIME2    NOT NULL  DEFAULT GETUTCDATE()
UpdatedAt    DATETIME2    NOT NULL  DEFAULT GETUTCDATE()
IsDeleted    BIT          NOT NULL  DEFAULT 0
DeletedAt    DATETIME2    NULL
```

**Rules:**
- All timestamps are **UTC only** — `GETUTCDATE()` everywhere. Never `GETDATE()`.
- `UpdatedAt` must be set by application code on every UPDATE — SQL default only sets
  the initial value at INSERT.
- `CoinTransactions` is the **only** table that omits the soft-delete columns
  (`IsDeleted`, `DeletedAt`) — it is append-only and records are never deleted.

---

### 19.3 Soft Delete Pattern

**All deletes are soft deletes.** Hard (permanent) delete requires explicit approval only.

**Soft delete operation:**
```sql
UPDATE <Table>
SET    IsDeleted = 1,
       DeletedAt = GETUTCDATE(),
       UpdatedAt = GETUTCDATE()
WHERE  Id = @id
```

**Enforcement rules:**
- Every repository query must include `WHERE IsDeleted = 0`.
- No query may return soft-deleted records to any API caller.
- Soft-deleted records remain in the database for audit purposes.

**Exceptions and special cases:**
- `CoinTransactions`: append-only — no soft delete. Records are never modified or deleted.
- Document Vault (Level 2): deleted documents enter a **30-day recovery window**.
  A background job sets a `PermanentDeleteAt` column; records are hard-deleted only
  after the recovery window expires.
- Location history (Level 2): auto-purged after **30 days** by a background worker —
  this is a platform-level privacy commitment enforced at the application layer, not
  via the standard soft-delete pattern.

---

### 19.4 BaseEntity Definition

**C# base class** in `FamilyFirst.Domain/Entities/Base/BaseEntity.cs`:

```csharp
public abstract class BaseEntity
{
    public Guid       Id         { get; set; } = Guid.NewGuid();
    public DateTime   CreatedAt  { get; set; }
    public DateTime   UpdatedAt  { get; set; }
    public bool       IsDeleted  { get; set; }
    public DateTime?  DeletedAt  { get; set; }
}
```

**Rules:**
- Every domain entity derives from `BaseEntity`.
- No entity may omit these fields.
- EF entity configurations set `HasKey(e => e.Id)` and `IsRequired()` on timestamp columns.
- `Plans` entity uses `int` PK and does not inherit `BaseEntity` (or has a custom base) — [VERIFY].

---

### 19.5 Common Query Patterns

#### Pagination

**All list endpoints support pagination.** No list endpoint returns an unbounded result set.

**Request parameters:**
```
page      int    Page number — 1-indexed
pageSize  int    Records per page
```

**Response wrapper — `PaginatedList<T>`:**
```csharp
public class PaginatedList<T>
{
    public List<T> Items          { get; set; }
    public int     TotalCount     { get; set; }
    public int     TotalPages     { get; set; }
    public bool    HasNextPage    { get; set; }
    public bool    HasPreviousPage { get; set; }
}
```

Returned inside the standard `ApiResponse<PaginatedList<T>>` envelope.

#### Soft-Delete Filter

Applied in every repository query. Never omitted.

```sql
WHERE IsDeleted = 0
```

In EF Core, this is enforced via a global query filter on `BaseEntity`-derived DbSets:
```csharp
modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
```
[VERIFY] whether global query filter is used or manual `.Where(e => !e.IsDeleted)` is applied per query.

#### Family Scope Filter (Row-Level Security)

Applied in every repository that operates on family-scoped data.

```sql
WHERE FamilyId = @currentFamilyId
  AND IsDeleted = 0
```

**The `@currentFamilyId` is resolved from the JWT claim `FamilyId`** at the service layer.
No cross-family data access is permitted at the repository layer under any circumstances.

#### Explicit Column List

`SELECT *` is never used. Every query lists columns explicitly:
```sql
SELECT Id, FamilyId, ChildProfileId, Status, SubmittedAt
FROM   TaskCompletions
WHERE  FamilyId = @familyId
  AND  IsDeleted = 0
```

---

### 19.6 Transaction Patterns

#### Coin Mutation Pattern

**All coin balance changes route through `ICoinService`.** No service or controller
may write to `ChildProfile.CoinBalance` directly. Every mutation also inserts an
append-only `CoinTransactions` row.

**Earn (task approval):**
```
ICoinService.EarnCoinsForTaskCompletionAsync(childProfileId, taskCompletionId, coinValue)
  → UPDATE ChildProfiles SET CoinBalance += coinValue, TotalCoinsEarned += coinValue
     (with RowVersion optimistic concurrency check)
  → INSERT CoinTransactions (TransactionType=Earn, ReferenceType=TaskCompletion,
     ReferenceId=completionId, Amount=coinValue, BalanceAfter=newBalance)
  → Evaluate level thresholds from TotalCoinsEarned
  → Increment matching pillar score (capped at 20)
  → Check streak freeze milestone
```

**Spend (reward redemption approval):**
```
ICoinService.SpendCoinsForRewardRedemptionAsync(childProfileId, redemptionId, coinsSpent)
  → Re-validate CoinBalance >= coinsSpent → 422 if insufficient
  → UPDATE ChildProfiles SET CoinBalance -= coinsSpent
     (with RowVersion optimistic concurrency check)
  → INSERT CoinTransactions (TransactionType=Spent, ReferenceType=RewardRedemption,
     ReferenceId=redemptionId, Amount=coinsSpent, BalanceAfter=newBalance)
```

**Deduct (parent manual):**
```
ICoinService.DeductCoinsAsync(childProfileId, amount, note, createdByUserId)
  → Validate CoinBalance >= amount → 422 if insufficient
  → UPDATE ChildProfiles SET CoinBalance -= amount (with RowVersion check)
  → INSERT CoinTransactions (TransactionType=Deduction, Note=note, Amount=amount)
```

#### Optimistic Concurrency

`ChildProfiles` has a `RowVersion` column (added `023_AlterChildProfiles_RowVersion.sql`,
Phase 10). Every coin balance mutation includes a `RowVersion` check via EF Core's
`IsRowVersion()` configuration.

`DbUpdateConcurrencyException` is caught at the service layer and translated to
**409 Conflict** response. Clients must retry on 409.

#### Reward Redemption Idempotency

Duplicate pending redemptions for the same child/reward pair are blocked at the
**database layer** via a filtered unique index:

```sql
CREATE UNIQUE INDEX IX_RewardRedemptions_Pending
ON RewardRedemptions (ChildProfileId, RewardId)
WHERE Status = 0  -- Pending
```

(Status INT value for Pending — [VERIFY] exact value.)

Attempting to create a second pending redemption for the same `(ChildProfileId, RewardId)`
returns **409 Conflict** before any application code runs.

#### Approval Atomicity

Reward redemption approval writes three records in a single database transaction via
`RewardRedemptionRepository.ApplyApprovalAsync`:

```
BEGIN TRANSACTION
  UPDATE RewardRedemptions SET Status=Approved, ReviewedAt, ReviewedByUserId
  UPDATE ChildProfiles SET CoinBalance -= CoinsSpent (RowVersion check)
  INSERT CoinTransactions (TransactionType=Spent, ...)
  UPDATE Rewards SET TimesRedeemedTotal += 1
COMMIT
```

On `DbUpdateConcurrencyException`: transaction rolls back → **409 Conflict**.
No partial writes are possible.

---

### 19.7 SQL Script Inventory — Level 1

All 40 Level 1 scripts, in execution order:

| Script | Phase | What it Creates |
|---|---|---|
| `001_CreateUsers.sql` | 01 | `Users` table |
| `002_CreateRefreshTokens.sql` | 01 | `RefreshTokens` table |
| `003_CreatePlans.sql` | 01 | `Plans` table |
| `004_CreateFamilies.sql` | 01 | `Families` table |
| `005_CreateSubscriptions.sql` | 01 | `Subscriptions` table |
| `006_CreateFamilyMembers.sql` | 01 | `FamilyMembers` table |
| `007_SeedPlans.sql` | 01 | Seeds 4 plan rows |
| `008_SeedCommentTemplates.sql` | 01 | Creates + seeds `CommentTemplates` |
| `009_AlterUsers_AddIndexes.sql` | 02 | Indexes on `Users` |
| `010_CreateFamilyMemberIndexes.sql` | 03 | Indexes on `FamilyMembers` |
| `011_AlterFamilies_JoinCode.sql` | 03 | Join code index on `Families` |
| `012_CreateChildProfiles.sql` | 04 | `ChildProfiles` table |
| `013_CreateTeacherProfiles.sql` | 04 | `TeacherProfiles` table |
| `014_CreateTeacherChildAssignments.sql` | 04 | `TeacherChildAssignments` table |
| `015_CreateAttendanceSessions.sql` | 05 | `AttendanceSessions` table |
| `016_CreateAttendanceSessionIndexes.sql` | 05 | Indexes on `AttendanceSessions` |
| `017_CreateAttendanceRecords.sql` | 06 | `AttendanceRecords` table |
| `018_CreateAuditLogs.sql` | 06 | `AuditLogs` table |
| `019_CreateTaskItems.sql` | 08 | `TaskItems` table |
| `020_CreateTaskItemIndexes.sql` | 08 | Indexes on `TaskItems` |
| `021_CreateTaskCompletions.sql` | 09 | `TaskCompletions` table + unique index |
| `022_CreateCoinTransactions.sql` | 10 | `CoinTransactions` table |
| `023_AlterChildProfiles_RowVersion.sql` | 10 | Adds `RowVersion` to `ChildProfiles` |
| `024_CreateTeacherFeedback.sql` | 11 | `TeacherFeedback` table |
| `025_CreateFeedbackIndexes.sql` | 11 | Indexes on `TeacherFeedback` |
| `026_CreateRewards.sql` | 13 | `Rewards` table |
| `027_SeedSystemRewards.sql` | 13 | Seeds 10 system reward rows |
| `028_CreateRewardRedemptions.sql` | 14 | `RewardRedemptions` table + filtered unique index |
| `029_CreateCalendarEvents.sql` | 15 | `CalendarEvents` table |
| `030_CreateEventReminders.sql` | 15 | `EventReminders` table |
| `031_CreateCalendarIndexes.sql` | 15 | Indexes on `CalendarEvents` |
| `032_CreateNotificationPreferences.sql` | 16 | `NotificationPreferences` table |
| `033_[VERIFY].sql` | 17 | **Phase 17 gap — script name not confirmed** |
| `034_[VERIFY].sql` | 17 | **Phase 17 gap — script name not confirmed** |
| `035_CreateFeatureFlags.sql` | 19 | `FeatureFlags` table |
| `036_SeedFeatureFlags.sql` | 19 | Seeds default feature flag rows |
| `037_CreateModuleVisibilityConfig.sql` | 20 | `ModuleVisibilityConfig` table |
| `038_CreateNotificationRules.sql` | 20 | `NotificationRules` table |
| `039_CreateCustomAttendanceStatuses.sql` | 20 | `CustomAttendanceStatuses` table |
| `040_SeedDefaultModuleVisibility.sql` | 20 | Seeds default visibility rows |

**Execution rules:**
- Scripts must run in order `001 → 040`.
- FK dependencies are resolvable by sequential execution.
- All scripts use `IF NOT EXISTS` guards — idempotent where possible.
- Manual execution only — the application never runs migrations at startup.
- Scripts 033–034 are unconfirmed (Phase 17 raw notes absent).
  Read `FamilyFirst_L1_Codex_DevPlan.docx` to recover Phase 17 script names.

---

## 20. Flutter App Architecture

**Source confirmed:** `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` (read 2026-05-29)

Single Flutter app — iOS + Android — all 6 roles inside one binary.
20 development phases · 42 Level 1 screens · 5 user-facing roles · 103 API endpoints.

---

### 20.1 Project Structure

```
lib/
  core/
    config/
      app_config.dart               ← isDemo (const), apiBaseUrl, appVersion, environment enum
      app_config_prod.dart          ← production overrides
    theme/
      app_theme.dart                ← full Flutter ThemeData
      app_colors.dart               ← color constants
      app_text_styles.dart          ← typography scale
    router/
      app_router.dart               ← GoRouter — all 42 screen routes declared
      route_names.dart              ← all route string constants
    state/
      auth_notifier.dart            ← global auth state machine (Riverpod)
      auth_state.dart               ← AuthState sealed class
    models/
      user_model.dart
      role_enum.dart                ← UserRole enum matching backend values
    network/
      api_client.dart               ← Dio singleton
      demo_interceptor.dart         ← intercepts all calls when isDemo=true
      auth_interceptor.dart         ← adds Bearer token header
      token_interceptor.dart        ← auto-refresh on 401 (Phase 02)
      retry_interceptor.dart        ← 3 retries, 1s/2s/4s backoff (Phase 19)
    storage/
      secure_storage_service.dart   ← flutter_secure_storage: accessToken, refreshToken
    mock/
      mock_data_service.dart        ← all module mock method implementations
    connectivity/
      connectivity_service.dart     ← connectivity_plus stream
      offline_banner_widget.dart    ← amber non-blocking banner
    cache/
      hive_cache_service.dart       ← Hive stale-while-revalidate cache
    local/
      offline_queue_service.dart    ← sqflite queue for offline attendance marking
    master_api_reference.dart       ← all 103 endpoints mapped (screen → endpoint → DTO)

  features/
    auth/
      screens/      splash_screen.dart, phone_login_screen.dart, otp_verify_screen.dart,
                    child_login_screen.dart, elder_login_screen.dart, demo_login_screen.dart
      widgets/      pin_pad_widget.dart
      repositories/ auth_repository.dart (demo + live)

    parent/
      screens/      parent_home_screen.dart, child_detail_screen.dart, feedback_inbox_screen.dart,
                    feedback_detail_screen.dart, verification_queue_screen.dart,
                    reward_shop_screen.dart, family_goals_screen.dart,
                    parent_profile_screen.dart, parent_settings_screen.dart
      widgets/      child_summary_card.dart, alert_strip_widget.dart, events_preview_widget.dart,
                    child_radar_chart.dart, week_mini_calendar.dart, feedback_card_widget.dart,
                    photo_review_sheet.dart, task_status_list.dart, verification_card.dart
      providers/    dashboard_provider.dart, child_detail_provider.dart, reward_shop_provider.dart
      repositories/ dashboard_repository.dart, child_repository.dart, reward_repository.dart

    family/
      screens/      family_setup_wizard.dart, family_members_screen.dart,
                    add_member_screen.dart, join_code_screen.dart
      providers/    family_provider.dart
      repositories/ family_repository.dart

    family_admin/
      screens/      family_admin_panel_screen.dart, module_visibility_screen.dart,
                    notification_rules_screen.dart

    teacher/
      screens/      teacher_home_screen.dart, attendance_marking_screen.dart,
                    create_session_screen.dart, feedback_submission_screen.dart,
                    feedback_history_screen.dart, teacher_profile_screen.dart,
                    teacher_settings_screen.dart
      widgets/      attendance_child_row.dart, comment_template_sheet.dart,
                    feedback_type_picker.dart, weekly_summary_form.dart
      providers/    attendance_provider.dart, feedback_provider.dart
      repositories/ attendance_repository.dart, feedback_repository.dart

    tasks/
      screens/      routine_builder_screen.dart, add_task_screen.dart
      widgets/      task_template_picker.dart, time_block_column.dart, task_chip.dart
      providers/    task_provider.dart
      repositories/ task_repository.dart

    child/
      screens/      child_home_screen.dart, task_detail_screen.dart, coins_rewards_screen.dart,
                    my_scores_screen.dart, child_family_screen.dart, child_settings_screen.dart
      widgets/      progress_ring_widget.dart, time_block_section.dart, task_list_item.dart,
                    countdown_timer_widget.dart, reward_card_widget.dart,
                    coin_animation_overlay.dart, badge_grid_widget.dart
      providers/    child_day_provider.dart, coins_provider.dart, scores_provider.dart
      repositories/ task_completion_repository.dart

    elder/
      screens/      elder_home_screen.dart, elder_send_appreciation_screen.dart,
                    elder_settings_screen.dart
      widgets/      grandchild_card_widget.dart
      providers/    elder_provider.dart

    calendar/
      screens/      family_calendar_screen.dart, create_event_screen.dart,
                    event_detail_screen.dart
      widgets/      calendar_event_tile.dart
      providers/    calendar_provider.dart
      repositories/ calendar_repository.dart

    notifications/
      screens/      notification_history_screen.dart, notification_preferences_screen.dart
      widgets/      notification_tile.dart
      providers/    notification_provider.dart
      repositories/ notification_repository.dart

    reports/
      screens/      scores_reports_screen.dart, weekly_digest_screen.dart,
                    attendance_summary_screen.dart
      widgets/      score_radar_widget.dart, attendance_heatmap_widget.dart,
                    score_trend_chart.dart
      providers/    reports_provider.dart
      repositories/ reports_repository.dart

    admin/
      screens/      admin_dashboard_screen.dart, family_management_screen.dart,
                    plans_manager_screen.dart, task_templates_screen.dart,
                    reward_catalog_screen.dart, notification_campaign_screen.dart,
                    app_config_screen.dart, analytics_screen.dart,
                    support_tickets_screen.dart, content_manager_screen.dart
      providers/    admin_provider.dart, analytics_provider.dart
      repositories/ admin_repository.dart

    settings/
      screens/      subscription_screen.dart

  shared/
    widgets/
      app_nav_shell.dart            ← role-adaptive bottom nav bar
      ff_button.dart                ← primary/secondary button
      ff_card.dart                  ← 16px radius, 2dp elevation
      ff_avatar.dart                ← member avatar with role border
      ff_badge.dart                 ← notification/count badge
      ff_status_pill.dart           ← coloured status indicator
      ff_empty_state.dart           ← empty state with illustration
      ff_shimmer_loader.dart        ← loading skeleton (Phase 19)
      ff_error_state.dart           ← error + retry button (Phase 19)

  l10n/
    app_en.arb                      ← base English strings
    app_hi.arb                      ← Hindi stubs (English fallback)
    app_ta.arb, app_te.arb, app_mr.arb  ← Tamil, Telugu, Marathi stubs
```

---

### 20.2 State Management

**Library:** Riverpod (flutter_riverpod)

**Global state — `AuthNotifier` (`lib/core/state/auth_notifier.dart`):**

Holds current authenticated user across the entire app. Initialized from SecureStorage
on splash. Never rebuilt except on login/logout/role-switch.

| Field | Type | Notes |
|---|---|---|
| `role` | `UserRole` | Current authenticated role (enum int) |
| `userId` | `Guid` | — |
| `familyId` | `Guid?` | Null until family is created/joined |
| `familyMemberId` | `Guid?` | — |
| `childProfileId` | `Guid?` | Child role only |
| `planCode` | `String?` | Current subscription plan |
| `isAuthenticated` | `bool` | — |

**Actions on `AuthNotifier`:**
`sendOtp`, `verifyOtp`, `verifyPin`, `refreshToken`, `logout`

**Feature-level StateNotifiers (one per module screen group):**

| Provider | Module | Location |
|---|---|---|
| `DashboardProvider` | Parent home | `lib/features/parent/providers/` |
| `ChildDetailProvider` | Child profile view | `lib/features/parent/providers/` |
| `RewardShopProvider` | Reward browsing | `lib/features/parent/providers/` |
| `FamilyProvider` | Family management | `lib/features/family/providers/` |
| `AttendanceProvider` | Teacher attendance | `lib/features/teacher/providers/` |
| `FeedbackProvider` | Teacher feedback | `lib/features/teacher/providers/` |
| `TaskProvider` | Task CRUD | `lib/features/tasks/providers/` |
| `ChildDayProvider` | Child home / MyDay | `lib/features/child/providers/` |
| `CoinsProvider` | Coins & rewards | `lib/features/child/providers/` |
| `ScoresProvider` | Pillar scores | `lib/features/child/providers/` |
| `ElderProvider` | Elder home | `lib/features/elder/providers/` |
| `CalendarProvider` | Family calendar | `lib/features/calendar/providers/` |
| `NotificationProvider` | Notification history & prefs | `lib/features/notifications/providers/` |
| `ReportsProvider` | Reports & digest | `lib/features/reports/providers/` |
| `AdminProvider` | Admin panel | `lib/features/admin/providers/` |
| `AnalyticsProvider` | Admin analytics | `lib/features/admin/providers/` |

**Rules:**
- No `setState` for API data — always Riverpod StateNotifier.
- `const` constructors used wherever possible.
- All interactive widgets check `context.read<AuthNotifier>()` for current role before
  rendering action buttons.

---

### 20.3 Navigation

**Library:** GoRouter (`app_router.dart` + `route_names.dart`)

All 42 Level 1 screen routes declared in `app_router.dart` at Phase 01.
No `MaterialPageRoute.push` anywhere in the codebase.

**Role-based redirect rules (confirmed):**

| Condition | Redirect |
|---|---|
| `null` auth state (no token) | `/splash` |
| SuperAdmin | `/admin/dashboard` |
| FamilyAdmin | `/parent/home` (same shell, admin features unlocked) — [VERIFY] |
| Parent | `/parent/home` |
| Teacher | `/teacher/home` |
| Child | `/child/home` |
| Elder | `/elder/home` |

**Bottom navigation per role (confirmed):**

| Role | Nav Items |
|---|---|
| Parent | Home · Children · Calendar · Vault · Profile |
| Teacher | Sessions · Feedback · Profile |
| Child | MyDay · Coins · Scores · Family · Settings |
| Elder | Home · Events · Settings |
| FamilyAdmin | [VERIFY] — likely Parent nav + Admin tab |
| SuperAdmin | [VERIFY] — admin-specific navigation |

**Deep link handling (confirmed from Phase 17):**
- `FirebaseMessaging.onMessageOpenedApp` → GoRouter deep link
- `FirebaseMessaging.getInitialMessage` → app-launch-from-notification deep link
- Calendar reminder pushes carry deep-link data payload (backend Phase 16)

---

### 20.4 Demo vs Live Mode

**`AppConfig.isDemo` is a `const` bool.**
Changing it requires a rebuild. There is no runtime toggle in production.

**Demo login screen (`demo_login_screen.dart`):**
6 role cards shown on app launch. Tapping any card sets that role in `AuthState`
and navigates to the role's home screen. Zero network calls.

**Demo users (confirmed):**

| Role | Demo Name | Notes |
|---|---|---|
| Parent | Amina Sharma | Family with 2 children |
| Teacher | Mr. Ahmed | Assigned to both demo children |
| Child | Arjun (age 12) | 340 demo coins, 8-day streak |
| Elder | Dadi | Grandparent view |
| FamilyAdmin | [VERIFY] | — |
| SuperAdmin | [VERIFY] | — |

**Demo credentials:**
- Phone number: any 10-digit number accepted
- OTP code: always `123456`
- PIN: always `1234`
- Join code: `DEMO01`

**MockDataService (`lib/core/mock/mock_data_service.dart`):**
All module mock method implementations in one file. Returns meaningful data —
not null/empty. Every screen must show real-looking data in demo mode.
No blank screens permitted.

**DemoInterceptor (`lib/core/network/demo_interceptor.dart`):**
Intercepts all Dio calls when `AppConfig.isDemo = true`. Returns mock responses
from MockDataService. No network requests made in demo mode.

**Repository pattern (two implementations per feature):**

```dart
abstract class IDashboardRepository {
  Future<FamilyDashboardDto> getDashboard(String familyId);
}

class DemoDashboardRepository implements IDashboardRepository {
  // reads from MockDataService — no Dio
}

class LiveDashboardRepository implements IDashboardRepository {
  // calls ApiClient (Dio) — real API
}
```

Repository implementation selected at startup via `AppConfig.isDemo`.
No conditional logic inside UI widgets or providers.

---

### 20.5 API Client

**Library:** Dio (`lib/core/network/api_client.dart` — singleton)

**Configuration:**
- `BaseOptions.baseUrl` = `AppConfig.apiBaseUrl` (e.g. `https://api.familyfirst.app/api/v1/`)
- Standard headers: `Content-Type: application/json`, `Accept: application/json`
- Timeout: [VERIFY] — connect + receive timeouts

**Interceptor stack (in order):**

1. **`DemoInterceptor`** — When `AppConfig.isDemo = true`, intercepts every request
   before it reaches the network. Returns `MockDataService` response. No real HTTP call.

2. **`TokenInterceptor`** — Adds `Authorization: Bearer <accessToken>` header from
   `SecureStorageService`. On 401 response:
   - Calls `POST /auth/refresh-token` with stored `refreshToken`
   - Stores new `accessToken` and `refreshToken` in `SecureStorage`
   - Retries the original request with the new token
   - If refresh also fails: clears tokens, resets `AuthState`, navigates to `/splash`
   - User **never sees a 401** — refresh is completely transparent

3. **`RetryInterceptor`** (Phase 19) — On network failure or 5xx:
   - Retries up to **3 times** with **exponential backoff: 1s → 2s → 4s**
   - After 3 failures: surfaces error to UI via StateNotifier

**SecureStorageService (`lib/core/storage/secure_storage_service.dart`):**
- Package: `flutter_secure_storage`
- Stores: `accessToken`, `refreshToken`
- Operations: `save`, `read`, `delete`

**Parallel API calls:**
`Future.wait([...])` used where a screen needs multiple endpoints simultaneously.
Example: child detail screen fires 4 parallel calls on load.

---

### 20.6 Design System

**Colors (confirmed from CLAUDE.md + Phase 01):**

| Role | Hex | Usage |
|---|---|---|
| Primary (Navy) | `#1A2E4A` | AppBar, primary buttons, headings |
| Accent (Gold) | `#C8922A` | FAB, highlights, badges, coin icons |
| Success (Green) | `#2D6A4F` | Present status, approved states |
| Alert (Red) | `#C1121F` | Absent status, error states, urgent alerts |
| Background (Cream) | `#F8F4EE` | App background, card background |

**Typography (confirmed):**

| Use | Font | Weight |
|---|---|---|
| Headings | Poppins | Bold |
| Body text | Nunito | Regular |
| Numbers & data | Space Grotesk | Medium |

**Spacing and sizing (confirmed):**

| Constant | Value |
|---|---|
| Card border radius | 16px |
| Card elevation | 2dp (soft shadow) |
| Standard padding | 16px |
| Generous padding | 24px |
| Minimum touch target | 48×48px (all interactive elements) |

**Shared widgets (confirmed from Phase 01):**

| Widget | File | Purpose |
|---|---|---|
| `FFButton` | `ff_button.dart` | Primary and secondary action buttons |
| `FFCard` | `ff_card.dart` | 16px radius, 2dp elevation — all content cards |
| `FFAvatar` | `ff_avatar.dart` | Member photo with role-colour border |
| `FFBadge` | `ff_badge.dart` | Notification counts, status indicators |
| `FFStatusPill` | `ff_status_pill.dart` | Coloured pill for status values |
| `FFEmptyState` | `ff_empty_state.dart` | Warm illustration + prompt for empty screens |
| `FFShimmerLoader` | `ff_shimmer_loader.dart` | Screen-layout-matched loading skeleton |
| `FFErrorState` | `ff_error_state.dart` | Error icon + message + Retry button |
| `AppNavShell` | `app_nav_shell.dart` | Role-adaptive bottom nav bar (3–5 items) |

**Chart library: `fl_chart`**

| Chart | Where Used |
|---|---|
| `RadarChart` (pentagon) | 5-pillar score display — parent child detail + child scores screen |
| `LineChart` | Score trends (30-day), admin analytics DAU/WAU/MAU |
| `BarChart` | Admin feature-usage breakdown |
| Heatmap grid (custom) | Admin analytics (7×24 grid), attendance summary |

**Calendar widget:** `table_calendar` — month/week/day toggle with custom cell builder
for colored event dots.

**Image handling:**
- `cached_network_image` — placeholder shimmer + error fallback avatar
- `image_picker` — camera or gallery for task photo uploads
- `flutter_image_compress` — max 1 MB before S3 presigned URL upload

**Offline libraries:**
- `Hive` (hive_flutter) — stale-while-revalidate cache for all screen data
- `sqflite` — offline queue for attendance marking (syncs on reconnect)
- `connectivity_plus` — stream-based connectivity monitoring

**Push notifications:** `firebase_messaging`
- Token obtained on launch → `PUT /users/{id}/fcm-token`
- `onMessage` → in-app local notification
- `onMessageOpenedApp` → GoRouter deep-link navigation
- `getInitialMessage` → handle notification that launched cold app

**Localization:**
- Base: `l10n/app_en.arb` (English)
- Stubs with English fallback: Hindi (`app_hi.arb`), Tamil, Telugu, Marathi

---

### 20.7 Screen-to-API Master Reference

Full mapping lives in `lib/core/master_api_reference.dart` (all 103 endpoints).
Key screen-to-API mappings per module (confirmed from phase API tables):

**Authentication (Phase 02):**

| Screen | API Call |
|---|---|
| `phone_login_screen` | `POST /auth/send-otp` |
| `otp_verify_screen` | `POST /auth/verify-otp` |
| `child_login_screen` | `POST /auth/verify-pin` |
| `elder_login_screen` | `POST /auth/verify-pin` |
| `splash_screen` | `GET /auth/me` |
| `token_interceptor` | `POST /auth/refresh-token` |
| logout flow | `POST /auth/revoke-token` |

**Parent Home / Dashboard (Phase 03):**

| Screen | API Call |
|---|---|
| `parent_home_screen` | `GET /families/{id}/dashboard` |

**Family Management (Phase 04):**

| Screen | API Call |
|---|---|
| `family_setup_wizard` | `POST /families` (step 1), `POST /families/{id}/members` (each child), `GET /families/{id}/join-code` (step 5) |
| `family_members_screen` | `GET /families/{id}/members` |
| `join_code_screen` | `POST /families/join` |
| `parent_profile_screen` | `GET /users/{id}`, `PUT /users/{id}`, `PUT /users/{id}/fcm-token` |

**Attendance (Phase 06):**

| Screen | API Call |
|---|---|
| `teacher_home_screen` | `GET /attendance/sessions?date=today` |
| `attendance_marking_screen` | `GET /sessions/{id}`, `POST /sessions/{id}/submit` |
| `create_session_screen` | `POST /attendance/sessions` |

**Tasks (Phase 08–09):**

| Screen | API Call |
|---|---|
| `routine_builder_screen` | `GET /families/{id}/tasks`, `POST /families/{id}/tasks` |
| `child_home_screen` | `GET /tasks?date=today` |
| `task_detail_screen` | `POST /tasks/{id}/completions`, `POST /completions/upload-url` |
| `verification_queue_screen` | `GET /tasks/verification-queue`, `PUT /completions/{id}/review` |

**Coins & Rewards (Phase 10, 13–14):**

| Screen | API Call |
|---|---|
| `coins_rewards_screen` | `GET /families/{id}/rewards`, `POST /rewards/{id}/redeem`, `GET /children/{id}/coin-history` |
| `reward_shop_screen` (parent) | `GET /rewards/redemptions`, `PUT /redemptions/{id}` |
| `my_scores_screen` | `GET /children/{id}/score-history` |

**Teacher Feedback (Phase 07 Flutter = Phases 11–12 backend):**

| Screen | API Call |
|---|---|
| `feedback_submission_screen` | `POST /families/{id}/feedback` |
| `feedback_history_screen` | `GET /families/{id}/feedback` |
| `feedback_inbox_screen` (parent) | `GET /families/{id}/feedback`, `POST /feedback/{id}/acknowledge` |

**Calendar (Phase 11 Flutter = Phase 15 backend):**

| Screen | API Call |
|---|---|
| `family_calendar_screen` | `GET /calendar/events`, `GET /calendar/upcoming` |
| `create_event_screen` | `POST /calendar/events` |
| `event_detail_screen` | `GET /calendar/events/{id}`, `PUT /calendar/events/{id}`, `DELETE /calendar/events/{id}` |

**Notifications (Phase 14 Flutter):**

| Screen | API Call |
|---|---|
| `notification_history_screen` | `GET /users/{id}/notifications` — [VERIFY Phase 17] |
| `notification_preferences_screen` | `GET /users/{id}/notification-preferences`, `PUT /users/{id}/notification-preferences` |

**Reports (Phase 15 Flutter):**

| Screen | API Call |
|---|---|
| `weekly_digest_screen` | `GET /reports/weekly-digest?weekStartDate=YYYY-MM-DD` |
| `scores_reports_screen` | `GET /children/{id}/score-history` |
| `attendance_summary_screen` | `GET /children/{id}/reports/attendance-summary` |

**Admin (Phase 13, 18 Flutter):**

| Screen | API Call |
|---|---|
| `admin_dashboard_screen` | `GET /admin/dashboard` |
| `family_management_screen` | `GET /admin/families`, `DELETE /admin/families/{id}` |
| `analytics_screen` | `GET /admin/analytics/overview?fromDate&toDate` |
| `module_visibility_screen` | `GET /families/{id}/admin/module-visibility`, `PUT /families/{id}/admin/module-visibility` |

**Elder (Phase 12 Flutter):**

| Screen | API Call |
|---|---|
| `elder_home_screen` | `GET /families/{id}/dashboard`, `GET /calendar/upcoming`, `GET /feedback?type=Appreciation` |
| `elder_send_appreciation_screen` | `POST /families/{id}/feedback` (type=Appreciation) |

---

## 21. Known Drift & Resolved Issues

This section records only issues with **HIGH recurrence risk** or that caused repeated
failures. One-off bugs and minor fixes are not documented here.

---

### Drift Entry 001 — Phase 17 Never Documented

- **Module:** Notification Engine (Section 10)
- **Drift Type:** Stale docs
- **What drifted:** Phase 17 ("Notification Engine: Push, Batching & History") was
  implemented as part of the Level 1 backend but its raw implementation notes were
  **never added** to ProjectOverview.md. The file jumps directly from Phase 16 to Phase 18.
  The `Notifications` table schema, notification history API endpoints, `NotificationDeliveryWorker`
  poll interval and retry logic, `MorningDigestWorker` and `EveningDigestWorker` schedules,
  and SQL scripts 033–034 are all unconfirmed.
- **How resolved:** PARTIAL. Section 10 documents all Phase 16 details and cross-phase
  inferences (Notifications table exists, delivery worker exists, INotificationService created).
  All Phase 17 specifics remain `[VERIFY]`. Full resolution requires reading
  `FamilyFirst_L1_Codex_DevPlan.docx` to recover Phase 17 implementation notes and
  adding them to Section 10 and Section 19.7.
- **Recurrence risk:** HIGH — any developer working on notifications, reading Sections 10
  or 19, or trying to run SQL scripts in order will immediately encounter this gap.
- **Date discovered:** 2026-05-29

---

### Drift Entry 002 — Coin Mutation Path Changed in Phase 10

- **Module:** Task & Routine System (Section 6) + Rewards & Coins (Section 8)
- **Drift Type:** DB contract drift / stale docs
- **What drifted:** Phase 09 implemented task completion approval by directly mutating
  `ChildProfile.CoinBalance` and `TotalCoinsEarned` in the `TaskService`. Phase 10
  refactored this — all coin mutations now route exclusively through `ICoinService`,
  which also writes an append-only `CoinTransactions` ledger entry. Any developer reading
  Phase 09 notes in isolation would implement the wrong pattern (direct ChildProfile mutation)
  if they added a new coin-earning event.
- **How resolved:** RESOLVED. Sections 6 and 8 both document the current (post-Phase 10)
  coin mutation path explicitly. Section 6.2 approval flow notes: "Phase 10 update: coin
  deduction now writes a `CoinTransactions` ledger entry." Section 19.6 documents all three
  coin mutation methods (`EarnCoinsForTaskCompletionAsync`, `SpendCoinsForRewardRedemptionAsync`,
  `DeductCoinsAsync`) as the canonical patterns. The raw Phase 09 execution note is preserved
  below for audit but must not be used as a code reference.
- **Recurrence risk:** HIGH — the most likely source of future regressions if a new
  coin-earning event (e.g. streak milestone, level-up bonus) is added without reading Section 19.6.
- **Date resolved:** Phase 10 implementation (2026-04-23). Documentation updated: 2026-05-29.

---

### Drift Entry 003 — SQL Scripts 033–034 Names Unconfirmed

- **Module:** Notification Engine (Section 10) / Database Standards (Section 19)
- **Drift Type:** Stale docs
- **What drifted:** The Level 1 script inventory runs 001–040. Scripts 033 and 034
  correspond to Phase 17. Because Phase 17 notes were never recorded, the script names
  and what tables they create are unknown. Any developer executing scripts sequentially
  will either skip 033–034 (missing tables) or be unable to determine what to run.
- **How resolved:** PARTIAL. Section 19.7 explicitly marks `033_[VERIFY].sql` and
  `034_[VERIFY].sql` with a note to read `FamilyFirst_L1_Codex_DevPlan.docx`.
  Full resolution requires recovering the Phase 17 script names and content.
- **Recurrence risk:** HIGH — directly blocks correct database setup from scratch.
- **Date discovered:** 2026-05-29

---

### Drift Entry 004 — IsPhotoRequired vs RequiresPhotoProof Field Name

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** DB contract drift (potential)
- **What drifted:** CLAUDE.md business rule 4 refers to the task photo field as
  `RequiresPhotoProof`. Phase 08 implementation notes name it `IsPhotoRequired` in the
  `TaskItem` entity field list and in the `CreateTaskRequest` DTO. If the Flutter
  `MockDataService` or any future API consumer uses `RequiresPhotoProof` as the field
  name, submissions will silently fail validation (missing required photo not detected).
- **How resolved:** PARTIAL. Section 6.3 documents both names with a `[VERIFY]` marker:
  "Note: CLAUDE.md refers to this as `RequiresPhotoProof` — [VERIFY] exact column name."
  Full resolution requires reading `001_CreateUsers.sql` or the `TaskItem` entity source
  to confirm the exact column name, then updating CLAUDE.md if needed.
- **Recurrence risk:** HIGH — any Flutter developer implementing the task photo upload
  flow may use the wrong field name, causing silent failures.
- **Date discovered:** 2026-05-29

---

### Drift Entry 005 — FamilyDashboardDto Extended Without Propagation

- **Module:** Family Dashboard (Section 4)
- **Drift Type:** Stale docs
- **What drifted:** `FamilyDashboardDto` was created in Phase 03 with "aggregate member
  counts and family score/streak fields only." Phase 12 added `UnacknowledgedFeedbackCount`.
  No other phase notes mention dashboard DTO updates, but it is likely that later phases
  (attendance, tasks, calendar, rewards) also extended the dashboard — Phase 03 notes
  explicitly say "task/attendance data remains out of scope until later phases."
  The current documentation in Section 4 only confirms the Phase 03 + Phase 12 fields;
  the full `FamilyDashboardDto` shape is unknown.
- **How resolved:** PARTIAL. Section 4 documents confirmed fields with `[VERIFY]` markers
  on probable additions. Full resolution requires reading `FamilyDashboardDto.cs` source
  or the tech spec to list all confirmed fields.
- **Recurrence risk:** HIGH — any Flutter developer building the parent home screen will
  render an incomplete dashboard if they only reference Section 4.
- **Date discovered:** 2026-05-29

---

### Drift Entry 006 — ProjectOverview.md Had No Structured Sections 2–21

- **Module:** All modules
- **Drift Type:** Stale docs
- **What drifted:** Before this brain update session (2026-05-29), ProjectOverview.md
  contained only raw phase execution logs (what files were created, what build output was
  produced). It had no stable contract-level documentation for any API, DB table, business
  rule, flow, or Flutter screen. Any developer starting a new session would have had to
  read all raw phase notes (749 lines) to reconstruct the project state — with no guarantee
  of correctness since phase notes contain implementation-time inferences, not validated contracts.
- **How resolved:** RESOLVED. This brain update session added structured Sections 1–21
  covering all Level 1 and Level 2 modules. Raw phase execution notes are preserved below
  the structured sections for audit and are explicitly marked read-only.
- **Recurrence risk:** HIGH without a maintenance discipline. Future sessions must update
  the relevant section after any structural change — not just append raw execution notes.
- **Date resolved:** 2026-05-29

---

### Drift Entry 007 — Flutter Architecture Entirely Undocumented

- **Module:** Flutter App (Section 20)
- **Drift Type:** Stale docs
- **What drifted:** Before this session, no Flutter implementation details were recorded
  in ProjectOverview.md. Section 1.5 carried only a `[VERIFY]` placeholder referencing
  CLAUDE.md standards. Every module section (2–11) had `[VERIFY]` for all Flutter screen
  names, MockDataService method names, and StateNotifier names. This made it impossible
  to build or review the Flutter app from documentation alone.
- **How resolved:** RESOLVED. Section 20 now documents the complete Flutter architecture:
  full project folder structure with all confirmed file paths, all 16 StateNotifiers,
  GoRouter redirect rules, demo mode credentials and behavior, complete Dio interceptor
  stack, design system constants, all packages with phase attribution, and the
  screen-to-API master reference per module.
- **Recurrence risk:** HIGH without a maintenance discipline. Flutter screen additions
  must be reflected in Section 20.7 (Screen-to-API Reference) and the relevant module's
  Section X.6 (Flutter Integration).
- **Date resolved:** 2026-05-29

---

### Drift Entry 008 — Level 2 Modules Had No Documentation

- **Module:** Sections 12–17 (all Level 2 modules)
- **Drift Type:** Missing fallback / stale docs
- **What drifted:** Before this session, Sections 12–17 did not exist in ProjectOverview.md.
  All Level 2 product requirements, screen definitions, business rules, and privacy rules
  (Document Vault, Medical Records, Safety, Finance, Reports, Advanced Admin) were locked
  inside the source `.docx` file. Any session starting on Level 2 work would have had to
  read the full 1,965-line product document to understand the product.
- **How resolved:** RESOLVED. Sections 12–17 now document all confirmed Level 2 content
  from `FamilyFirst_Level2_ProductDocument.docx`: all 40 Level 2 screens, all module-specific
  business rules, privacy tier model, offline behavior, emergency card rules, consent flow,
  safe zone architecture, report design principles, and storage configuration.
  API paths and DB schemas remain `[VERIFY]` until a Level 2 tech spec is available.
- **Recurrence risk:** HIGH without a Level 2 tech spec. The current Level 2 sections
  are stable at product-rule level but not at API/DB contract level.
- **Date resolved:** 2026-05-29

---

### Drift Entry 061 — NotificationPreferenceDto: Missing FamilyId, QuietHoursEnabled; Types Are TimeOnly Not String

- **Module:** Notification Engine (Section 10)
- **Drift Type:** Stale docs / request drift
- **What drifted:** Section 10.2 documented `NotificationPreferenceDto` with `[VERIFY]` for most fields. Missing: `PreferenceId (Guid)`, `FamilyId (Guid)`, `QuietHoursEnabled (bool)`. Time fields documented as `string/time [VERIFY]` — actual type is `TimeOnly` in both DTO and DB (`TIME` SQL type). Documented as having `UserId` PK — actual PK field is `PreferenceId`.
- **How resolved:** RESOLVED. Full 15-field `NotificationPreferenceDto` documented in 10.2 and 10.3.
- **Recurrence risk:** HIGH — Flutter serializing `string` for `QuietHoursStartTime` instead of `TimeOnly` format (`HH:mm:ss`) would fail deserialization.
- **Date resolved:** 2026-05-29

---

### Drift Entry 062 — UpdatePreferencesRequest: All Fields Required; TimeOnly Not String

- **Module:** Notification Engine (Section 10)
- **Drift Type:** Request / response drift
- **What drifted:** Section 10.2 documented `UpdatePreferencesRequest` with all fields as optional and types as `string [VERIFY]`. Actual DTO has all 11 fields required (with defaults) and `TimeOnly` type for time fields. No partial-update semantics.
- **How resolved:** RESOLVED. Section 10.2 updated with full field list, types, and defaults.
- **Recurrence risk:** MEDIUM — missing fields would use defaults silently; TimeOnly vs string type matters for serialization.
- **Date resolved:** 2026-05-29

---

### Drift Entry 063 — Phase 17 Notification History Endpoints Not Implemented

- **Module:** Notification Engine (Section 10), cross-references Drift Entry 001
- **Drift Type:** Stale docs / missing entry
- **What drifted:** Section 10.2 listed "likely endpoints" for notification history as `[VERIFY]` inferences. Confirmed from source: no notification history endpoints exist in any controller. `NotificationsController` only has GET/PUT for notification preferences. The `Notifications` table, `NotificationService`, and `NotificationDto` exist but are internal only. `MarkAllReadResultDto` is defined but no endpoint uses it.
- **How resolved:** RESOLVED. Section 10.2 now documents this as "Status: NOT IMPLEMENTED" with explanation.
- **Recurrence risk:** HIGH — Flutter developer expecting a `/notifications` inbox endpoint would find nothing.
- **Date resolved:** 2026-05-29

---

### Drift Entry 064 — NotificationPreferences Table: PK Is PreferenceId; Has FamilyId; TIME Types; No CreatedAt

- **Module:** Notification Engine (Section 10)
- **Drift Type:** DB contract drift
- **What drifted:** Section 10.3 documented `NotificationPreferences` with PK `Id`, no `FamilyId`, `[VERIFY]` for all column types. Actual: PK is `PreferenceId`; has `FamilyId NOT NULL`; time columns are `TIME` SQL type; table has NO `CreatedAt` column (only `UpdatedAt`). Unique index is `UX_NotificationPreferences_UserId`.
- **How resolved:** RESOLVED. Section 10.3 fully rewritten.
- **Recurrence risk:** HIGH — missing `FamilyId NOT NULL` means any INSERT without it fails. Wrong time type breaks queries.
- **Date resolved:** 2026-05-29

---

### Drift Entry 065 — Notifications Table: Scripts Were 033/034 (Not Unknown); Missing Columns Confirmed

- **Module:** Notification Engine (Section 10), closes Drift Entries 001 and 003
- **Drift Type:** Stale docs
- **What drifted:** Drift Entries 001 and 003 stated Phase 17 scripts 033–034 were unknown. Scripts exist at `033_CreateNotifications.sql` and `034_CreateNotificationIndexes.sql`. Notifications table had many undocumented columns: `FcmMessageId`, `IsBatched`, `BatchGroup`, `ScheduledFor`. Column `DeepLinkPath` (not `DeepLink`). PK is `NotificationId`. No `IsDeleted`/`UpdatedAt`. `NotificationDeliveryWorker` poll interval confirmed as 5 minutes; purge threshold is 90 days; no FCM token → silently suppressed.
- **How resolved:** RESOLVED. Section 10.3 Notifications table fully documented. Section 10.4 and 10.5 updated. Drift Entries 001 and 003 are **partially resolved** — the scripts and Notifications table are now documented. `MorningDigestWorker` and `EveningDigestWorker` still not found in source; batching logic still not confirmed.
- **Recurrence risk:** HIGH — missing `FcmMessageId` suppression sentinel and purge behavior are important for monitoring/debugging. `IsBatched`/`BatchGroup` needed for any batching feature implementation.
- **Date resolved:** 2026-05-29

---

### Drift Entry 066 — NotificationPreference Auto-Create on GET (Not 404)

- **Module:** Notification Engine (Section 10)
- **Drift Type:** Stale docs
- **What drifted:** Section 10.2 documented GET as possibly returning 404 if no preferences row exists with `[VERIFY]` on auto-create. Confirmed: `GetOrCreatePreferencesAsync` is called on both GET and PUT — auto-inserts with defaults if no row exists. Requires active family membership; no membership → 403 (not 404).
- **How resolved:** RESOLVED. Section 10.2 business rules and error cases updated.
- **Recurrence risk:** LOW — Flutter would get a preferences object (not 404), but missing FamilyId constraint awareness could cause unexpected 403.
- **Date resolved:** 2026-05-29

---

### Drift Entry 055 — CreateEventRequest: EventTitle Not Title; RemindBeforeMinutes Not ReminderMinutes; Missing Fields

- **Module:** Family Calendar (Section 9)
- **Drift Type:** Request / response drift
- **What drifted:** Section 9.2 documented `CreateEventRequest` with field names `Title` and `ReminderMinutes` — both wrong. Actual DTO uses `EventTitle` and `RemindBeforeMinutes`. Additionally, `IsAllDay (bool)`, `ColorHex (string)`, and `Channel (NotificationChannel)` per reminder were entirely missing from the docs. The `ColorHex` field has `#RRGGBB` format validation.
- **How resolved:** RESOLVED. Section 9.2 `CreateEventRequest` fully rewritten with all confirmed fields.
- **Recurrence risk:** HIGH — `EventTitle`/`Title` name mismatch causes silent field drop; missing `Channel` causes validation failure on reminder creation.
- **Date resolved:** 2026-05-29

---

### Drift Entry 056 — VisibilityScope Is String With 5 Values (Not INT)

- **Module:** Family Calendar (Section 9)
- **Drift Type:** DB contract drift / request drift
- **What drifted:** Section 9.2 and 9.3 documented `VisibilityScope` as `INT` with `[VERIFY]`. Actual DB column is `NVARCHAR(50)` with DEFAULT `'Family'` and 5 allowed values: `Family|Parent|Child|Elder|Caregiver`. Confirmed from SQL script CHECK constraint and validator.
- **How resolved:** RESOLVED. Sections 9.2, 9.3, and 9.4 updated with confirmed string type and 5 values.
- **Recurrence risk:** HIGH — Flutter sending an int would fail validation; missing Elder/Caregiver/Parent visibility scopes would limit feature.
- **Date resolved:** 2026-05-29

---

### Drift Entry 057 — GET /calendar/events Is Not Paginated; Params Are fromDate/toDate

- **Module:** Family Calendar (Section 9)
- **Drift Type:** Request / response drift
- **What drifted:** Section 9.2 documented `GET /calendar/events` response as `PaginatedList<EventDto>` with `page`/`pageSize` params plus `from`/`to` filters. Actual controller has no `page`/`pageSize` params. Actual param names are `fromDate` and `toDate` (DateTime?, not DateOnly). Response is `IReadOnlyCollection<EventDto>` — not paginated. Same for `GET /upcoming` — `List<EventDto>` → `IReadOnlyCollection<EventDto>`.
- **How resolved:** RESOLVED. Both endpoint docs updated.
- **Recurrence risk:** HIGH — Flutter pagination logic would fail; wrong param names silently ignored.
- **Date resolved:** 2026-05-29

---

### Drift Entry 058 — EventDto and EventReminderDto Had Missing Fields; EventType Enum Incomplete

- **Module:** Family Calendar (Section 9)
- **Drift Type:** Stale docs
- **What drifted:** `EventDto` was not documented in Section 9.2 at all. `EventReminderDto` was missing `ReminderId` and `Channel`. `EventType` enum only documented `MedicineReminder` and `Birthday` — all 8 values were not listed. CalendarEvents table was missing `IsAllDay`, `ColorHex`, `IsActive`. EventReminders table was missing `FamilyId` and `Channel` columns, and PK was wrong (`Id` vs `ReminderId`).
- **How resolved:** RESOLVED. Full shapes documented in 9.2 and 9.3. All 8 EventType values documented.
- **Recurrence risk:** HIGH — Flutter developer building event display or reminder creation would miss critical fields.
- **Date resolved:** 2026-05-29

---

### Drift Entry 059 — DELETE /calendar/events Returns ApiResponse<bool> (Not 204)

- **Module:** Family Calendar (Section 9)
- **Drift Type:** Request / response drift
- **What drifted:** Section 9.5 Flow 4 documented `DELETE` response as `204 No Content`. Actual controller returns `Ok(ApiResponse<bool>.Success(deleted, "Calendar event deleted."))` — 200 with a bool body.
- **How resolved:** RESOLVED. Section 9.2 and Flow 4 updated.
- **Recurrence risk:** LOW — difference between 200 and 204 rarely breaks clients, but type mismatch causes deserialization errors.
- **Date resolved:** 2026-05-29

---

### Drift Entry 060 — Calendar Visibility Rules Were Incomplete (Teacher/Elder Scoping Undocumented)

- **Module:** Family Calendar (Section 9)
- **Drift Type:** Stale docs
- **What drifted:** Section 9.4 Rule 9 documented only Child visibility with a `[VERIFY]` marker. Teacher and Elder role visibility rules were entirely absent. Teacher can see events they created OR where VisibilityScope is `Family`, `Child`, or `Caregiver`. Elder sees only `Family` or `Elder` scoped events. Also: `IsActive = false` events are hidden from all roles.
- **How resolved:** RESOLVED. Section 9.4 Rule 9 updated with full per-role visibility rules confirmed from `CanViewEvent()` in CalendarService.
- **Recurrence risk:** MEDIUM — Teacher and Elder role calendar screens would show wrong events if filtering is done client-side.
- **Date resolved:** 2026-05-29

---

### Drift Entry 048 — CoinTransactionDto TransactionType and ReferenceType Are Strings (Not INT Enums)

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Request / response drift
- **What drifted:** Section 8.2 documented `CoinTransactionDto.TransactionType` as `int` mapping to enum "Earn/Spent/Deduction". Actual DTO has `string` type with values `"Earned"`, `"Spent"`, `"Deducted"`. Same for `ReferenceType` — documented as `[VERIFY] exact values`; actual values are `"TaskCompletion"`, `"RewardRedemption"`, `"ManualDeduction"`. Also: `Amount` is negative for `"Spent"` type.
- **How resolved:** RESOLVED. Sections 8.2 and 8.3 updated with confirmed string values.
- **Recurrence risk:** HIGH — Flutter developer expecting int enum would break on display/comparison logic.
- **Date resolved:** 2026-05-29

---

### Drift Entry 049 — ReviewRedemptionRequest Uses RedemptionStatus Enum (Not String)

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Request / response drift
- **What drifted:** Section 8.2 documented `ReviewRedemptionRequest { Status: string "Approved"|"Rejected" }`. Actual DTO uses `RedemptionStatus` enum (`Approved=2`, `Rejected=3`). Same pattern as the task review DTO fix in Drift Entry 036. Also: review only processes `Pending` (=1) redemptions → 409 for non-Pending.
- **How resolved:** RESOLVED. Section 8.2 updated; flows updated with enum values.
- **Recurrence risk:** HIGH — Flutter sending `{ "status": "Approved" }` would get 400 validation error.
- **Date resolved:** 2026-05-29

---

### Drift Entry 050 — RedemptionStatus Has 4 Values; Fulfilled Was Missing

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Stale docs
- **What drifted:** Section 8.3 listed only Pending/Approved/Rejected for `RedemptionStatus`. Actual enum has a 4th value: `Fulfilled = 4`. Currently no endpoint sets this status — it is reserved for future use. Also: the `GET /rewards/redemptions` `status` filter accepts all 4 values including Fulfilled.
- **How resolved:** RESOLVED. Section 8.3 RedemptionStatus table updated with all 4 values.
- **Recurrence risk:** LOW — no current code path uses Fulfilled, but filtering by it returns empty results silently.
- **Date resolved:** 2026-05-29

---

### Drift Entry 051 — No Push Notification Sent to Parent on Child Redemption Request

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Stale docs
- **What drifted:** Section 8.4 Rule 13 had "[VERIFY] whether push is sent to parent on new redemption request." Flow 1 had "[VERIFY] push to parent". Confirmed from `RedeemAsync` in `RewardService.cs` — no `SendPush` call exists. Parent is not notified; they must check the dashboard or redemptions list manually.
- **How resolved:** RESOLVED. Rule 13 and Flow 1 updated to confirm no push sent.
- **Recurrence risk:** LOW — but Flutter should not display "Parent notified" on redemption submission.
- **Date resolved:** 2026-05-29

---

### Drift Entry 052 — GET /rewards/redemptions Is Not Paginated; Has childId/status Filters

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Request / response drift
- **What drifted:** Section 8.2 documented `GET /rewards/redemptions` response as `PaginatedList<RedemptionDto>` with `page`/`pageSize` params. Actual controller returns `IReadOnlyCollection<RedemptionDto>` — not paginated. Confirmed query params are `childId (Guid?)` and `status (RedemptionStatus?)`.
- **How resolved:** RESOLVED. Section 8.2 updated.
- **Recurrence risk:** HIGH — Flutter building against pagination shape would get deserialization errors.
- **Date resolved:** 2026-05-29

---

### Drift Entry 053 — RedeemRequest Has ChildProfileId Field (Not "Likely No Body")

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** Request / response drift
- **What drifted:** Section 8.2 documented `RedeemRequest` as "[VERIFY] likely no body or minimal body since rewardId is in route". Actual `RedeemRequest` has `{ ChildProfileId: Guid }` — required to identify which child is redeeming (validated against JWT `childProfileId` claim).
- **How resolved:** RESOLVED. Section 8.2 updated.
- **Recurrence risk:** HIGH — Flutter sending no body would result in validation error.
- **Date resolved:** 2026-05-29

---

### Drift Entry 054 — CoinTransactions/Rewards/RewardRedemptions PKs Were Wrong

- **Module:** Rewards & Coins (Section 8)
- **Drift Type:** DB contract drift
- **What drifted:** All three tables documented with `Id` as PK. Actual PKs: `TransactionId` (CoinTransactions), `RewardId` (Rewards), `RedemptionId` (RewardRedemptions). Also: `CoinTransactions.TransactionType` is `NVARCHAR(30)` string; `Rewards` has `IX_Rewards_FamilyId_IsEnabled` index; `RewardRedemptions` has 2 indexes including the filtered unique index for Pending.
- **How resolved:** RESOLVED. All three tables fully rewritten in Section 8.3.
- **Recurrence risk:** HIGH — wrong PK column names cause EF mapping errors.
- **Date resolved:** 2026-05-29

---

### Drift Entry 043 — FeedbackType Enum: "Concern" Doesn't Exist; HomeworkIssue and WeeklySummary Were Missing

- **Module:** Teacher Feedback (Section 7)
- **Drift Type:** Stale docs
- **What drifted:** Section 7.3 listed "Concern" as a FeedbackType value — this enum member does not exist. Actual 4th value is `HomeworkIssue = 4`. The 6th value `WeeklySummary = 6` was entirely missing from the documentation. The `FeedbackSummaryDto` was documented as `{ TotalCount, CountByType: Dictionary }` — the actual DTO has 8 flat count fields per type (no dictionary).
- **How resolved:** RESOLVED. All 6 `FeedbackType` enum values documented in 7.3. `FeedbackSummaryDto` full shape documented in 7.2.
- **Recurrence risk:** HIGH — Flutter developer building feedback type UI would show wrong options and miss HomeworkIssue and WeeklySummary entirely.
- **Date resolved:** 2026-05-29

---

### Drift Entry 044 — FeedbackSeverity Enum: Values Are Low/Medium/Urgent (Not High/Critical)

- **Module:** Teacher Feedback (Section 7)
- **Drift Type:** Stale docs
- **What drifted:** Section 7.3 documented FeedbackSeverity as "[VERIFY] — Low/Medium/High/Critical or similar". Actual enum: Low=1, Medium=2, Urgent=3. No High or Critical values exist.
- **How resolved:** RESOLVED. Section 7.3 FeedbackSeverity table updated with confirmed values.
- **Recurrence risk:** MEDIUM — Flutter developer building severity picker would show wrong options.
- **Date resolved:** 2026-05-29

---

### Drift Entry 045 — TeacherFeedback PK Is FeedbackId; ResolutionStatus Is String Not INT

- **Module:** Teacher Feedback (Section 7)
- **Drift Type:** DB contract drift
- **What drifted:** Section 7.3 documented PK as `Id` and `ResolutionStatus` as `INT` mapping to enum with "Pending (default)". Actual SQL: PK is `FeedbackId`; `ResolutionStatus` is `NVARCHAR(20)` with DEFAULT `'Open'` and allowed values `Open|Acknowledged|Resolved`. There is no `Pending` value — default is `Open`.
- **How resolved:** RESOLVED. Section 7.3 TeacherFeedback fully rewritten.
- **Recurrence risk:** HIGH — any code comparing `ResolutionStatus` to an int would fail; default `'Open'` vs `'Pending'` causes UI badge mismatch.
- **Date resolved:** 2026-05-29

---

### Drift Entry 046 — UpdateFeedbackRequest Only Has Message and Severity

- **Module:** Teacher Feedback (Section 7)
- **Drift Type:** Request / response drift
- **What drifted:** Section 7.2 documented `UpdateFeedbackRequest` as "[VERIFY] — same fields as submit (Message, Subject, Severity, WeeklySummaryJson)". Actual DTO has only `{ Message, Severity? }`. Subject and WeeklySummaryJson are **not updatable** after submission. A Flutter developer sending Subject in the update would have it silently ignored.
- **How resolved:** RESOLVED. Section 7.2 UpdateFeedbackRequest and Flow 2 updated.
- **Recurrence risk:** MEDIUM — Subject data silently lost if Flutter sends it on update.
- **Date resolved:** 2026-05-29

---

### Drift Entry 047 — WeeklySummaryJson Rates Are int (Not decimal); Triggered by WeeklySummary Type

- **Module:** Teacher Feedback (Section 7)
- **Drift Type:** Request / response drift / stale docs
- **What drifted:** Section 7.2 documented `WeeklySummaryJson` as having `attendanceRate: decimal` and `homeworkRate: decimal`. Actual validator (`WeeklySummaryPayload`) uses `int` with range 0–100. Also, the trigger condition was documented as "FeedbackType is weekly summary — [VERIFY] which type triggers this" — it is `FeedbackType.WeeklySummary` (=6).
- **How resolved:** RESOLVED. Type corrected to `int`. WeeklySummary type confirmed in 7.2 and 7.4.
- **Recurrence risk:** LOW — `int` vs `decimal` is unlikely to break JSON parsing, but the type documentation should be accurate.
- **Date resolved:** 2026-05-29

---

### Drift Entry 036 — ReviewTaskCompletionRequest Uses Status Enum, Not Action String

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Request / response drift
- **What drifted:** Section 6.2 documented `ReviewTaskCompletionRequest { Action: "Approve"|"Flag", ReviewNote }`. Actual DTO is `{ Status: TaskStatus (Approved=4 | Flagged=5), ReviewNote? }`. A Flutter developer sending `{ "action": "Approve" }` would get a 400 validation error with no data change.
- **How resolved:** RESOLVED. Section 6.2 review endpoint and both flow summaries corrected.
- **Recurrence risk:** HIGH — any Flutter developer reading old docs would build wrong request.
- **Date resolved:** 2026-05-29

---

### Drift Entry 037 — Child Submission Creates SubmittedForReview, Not Pending

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Request / response drift / stale docs
- **What drifted:** Section 6.2 and 6.5 documented child submission as setting `Status = Pending`. Actual `TaskService` sets `Status = SubmittedForReview` (int 3). The verification queue also returns `SubmittedForReview` completions, not `Pending`. This means `Pending` (1) is the initial row state before any child interaction — not the submitted state. The review endpoint rejects anything not in `SubmittedForReview` with a 409 Conflict.
- **How resolved:** RESOLVED. Sections 6.2, 6.3, 6.4, 6.5 all updated.
- **Recurrence risk:** HIGH — this is the most likely source of a "can't review task" bug for any Flutter developer.
- **Date resolved:** 2026-05-29

---

### Drift Entry 038 — TaskTimeBlock 4th Value Is Night, Not Afternoon

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Stale docs
- **What drifted:** Section 6.3 `TaskTimeBlock` enum listed `Afternoon` as a value. Actual `TaskTimeBlock.cs` enum has `Night = 4` (not `Afternoon`). The complete enum: Morning=1, School=2, Evening=3, Night=4.
- **How resolved:** RESOLVED. Sections 6.2 and 6.3 updated with confirmed enum values.
- **Recurrence risk:** MEDIUM — Flutter developer building time-block UI would show wrong option.
- **Date resolved:** 2026-05-29

---

### Drift Entry 039 — TaskStatus Has 6 Values (Not 3)

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Stale docs
- **What drifted:** Section 6.3 listed only `Pending`, `Approved`, `Flagged` as TaskStatus values. Actual enum has 6 values: Pending=1, InProgress=2, SubmittedForReview=3, Approved=4, Flagged=5, Missed=6. `InProgress` and `Missed` were entirely undocumented.
- **How resolved:** RESOLVED. Section 6.3 TaskStatus table updated with all 6 values.
- **Recurrence risk:** MEDIUM — Flutter status display and filter logic would miss InProgress and Missed states.
- **Date resolved:** 2026-05-29

---

### Drift Entry 040 — TaskCompletionUploadUrlDto Has ObjectKey, Not PhotoUrl

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Request / response drift
- **What drifted:** Section 6.2 documented upload-url response as `{ UploadUrl, PhotoUrl }`. Actual `TaskCompletionUploadUrlDto` has `{ TaskId, UploadUrl, ObjectKey, ExpiresAtUtc }`. The field the client must carry forward is `ObjectKey` (not `PhotoUrl`). Sending the presigned URL itself as `PhotoUrl` would store the wrong value.
- **How resolved:** RESOLVED. Section 6.2 upload-url response updated.
- **Recurrence risk:** HIGH — stores wrong S3 URL if Flutter uses old docs.
- **Date resolved:** 2026-05-29

---

### Drift Entry 041 — Review/Queue/ApproveAll Role Gate Is Parent Only (Not FamilyAdmin)

- **Module:** Task & Routine System (Section 6)
- **Drift Type:** Request / response drift
- **What drifted:** Section 6.2 documented `PUT /review` role gate as "Parent only — [VERIFY] FamilyAdmin". Section 6.2 `GET /verification-queue` and `POST /approve-all` documented as "Parent, FamilyAdmin". All three use `EnsureParentAsync` — FamilyAdmin → 403 on all three.
- **How resolved:** RESOLVED. All three endpoints updated to "Parent only".
- **Recurrence risk:** HIGH — FamilyAdmin using review/approve features gets unexpected 403.
- **Date resolved:** 2026-05-29

---

### Drift Entry 042 — TaskItems PK Is TaskId; IsPhotoRequired Confirmed (Resolves Drift Entry 004)

- **Module:** Task & Routine System (Section 6), cross-reference Drift Entry 004
- **Drift Type:** DB contract drift / stale docs
- **What drifted:** Section 6.3 TaskItems documented PK as `Id`. Actual SQL (`019_CreateTaskItems.sql`) uses `TaskId`. Also resolves Drift Entry 004: the column name is `IsPhotoRequired` (not `RequiresPhotoProof`). `ActiveFromDate`/`ActiveToDate` are `DATE` type (not `DATETIME2`). `RecurringDays` is NOT NULL with default `[1,2,3,4,5,6,7]`. 4 DB CHECK constraints added.
- **How resolved:** RESOLVED. Section 6.3 TaskItems fully rewritten. Drift Entry 004 is now resolved.
- **Recurrence risk:** HIGH — `IsPhotoRequired` column name mismatch causes silent query failures.
- **Date resolved:** 2026-05-29

---

### Drift Entry 028 — CreateSessionRequest Missing SubjectName and BatchName

- **Module:** Attendance System (Section 5)
- **Drift Type:** Request / response drift
- **What drifted:** `CreateSessionRequest` documented only `{ SessionName, ScheduledDate, StartTime, EndTime, IsRecurring, RecurringDays }`. Missing `SubjectName` (required, NOT NULL) and `BatchName` (optional). `SubjectName` is a NOT NULL column in `AttendanceSessions` — any INSERT without it fails at the DB level.
- **How resolved:** RESOLVED. Both fields added to 5.2 and 5.3 documentation.
- **Recurrence risk:** HIGH — `SubjectName` omission causes INSERT failure.
- **Date resolved:** 2026-05-29

---

### Drift Entry 029 — Edit Attendance Endpoint URL Missing sessionId Segment

- **Module:** Attendance System (Section 5)
- **Drift Type:** Request / response drift
- **What drifted:** Section 5.2 documented `PUT /api/v1/families/{familyId}/attendance/records/{recordId}`. Actual route (confirmed from `AttendanceController.cs`) is `PUT /api/v1/families/{familyId}/attendance/sessions/{sessionId}/records/{recordId}` — `{sessionId}` segment was missing. Flutter calling the wrong URL would get 404.
- **How resolved:** RESOLVED. Endpoint corrected in 5.2 and both flow summaries updated.
- **Recurrence risk:** HIGH — wrong URL causes 404 on every attendance edit.
- **Date resolved:** 2026-05-29

---

### Drift Entry 030 — GET /attendance/sessions Returns List Not PaginatedList

- **Module:** Attendance System (Section 5)
- **Drift Type:** Request / response drift
- **What drifted:** Section 5.2 documented response as `PaginatedList<AttendanceSessionDto>` with `page`/`pageSize` query params. Actual implementation (confirmed from `AttendanceController.cs`) returns `IReadOnlyCollection<AttendanceSessionDto>` — no pagination, no `page`/`pageSize` params.
- **How resolved:** RESOLVED. Response type and query params corrected in 5.2.
- **Recurrence risk:** HIGH — Flutter building against pagination shape would get deserialization errors.
- **Date resolved:** 2026-05-29

---

### Drift Entry 031 — Child Attendance Role Gate Excludes FamilyAdmin

- **Module:** Attendance System (Section 5)
- **Drift Type:** Request / response drift
- **What drifted:** Section 5.2 documented `GET /children/{childId}/attendance` role gate as "Parent, FamilyAdmin (any child); Child (own)". Actual service (`ListChildAttendanceAsync`) throws `ForbiddenAccessException` for any role that is not Parent or Child. FamilyAdmin → 403. Also: query params documented as `page`/`pageSize`; actual params are `fromDate`/`toDate` (DateOnly). Response is not paginated.
- **How resolved:** RESOLVED. Role gate, query params, and response type corrected in 5.2.
- **Recurrence risk:** HIGH — FamilyAdmin trying to view child attendance gets unexpected 403. Wrong query params return empty results.
- **Date resolved:** 2026-05-29

---

### Drift Entry 032 — AttendanceRecordDto Missing Fields; MarkedAt vs SubmittedAt

- **Module:** Attendance System (Section 5)
- **Drift Type:** Request / response drift / DB contract drift
- **What drifted:** `AttendanceRecordDto` was not documented in Section 5.2 at all. The `AttendanceRecords` table documented a `SubmittedAt` column and a `CreatedByTeacherProfileId` column — neither exists in the actual SQL. Actual columns are `MarkedAt`, `MarkedByUserId`, `EditedAt`, `EditedByUserId`, and `CommentTemplateId`. The `EditAttendanceRequest` DTO was missing `CommentTemplateId` (Guid?).
- **How resolved:** RESOLVED. `AttendanceRecordDto` full shape documented in 5.2. `AttendanceRecords` DB table fully rewritten in 5.3. `EditAttendanceRequest` updated with `CommentTemplateId`.
- **Recurrence risk:** HIGH — `MarkedAt`/`SubmittedAt` name mismatch and missing `CommentTemplateId` cause silent data gaps in Flutter.
- **Date resolved:** 2026-05-29

---

### Drift Entry 033 — AuditLogs PK Is BIGINT IDENTITY (not GUID)

- **Module:** Attendance System (Section 5), Admin Configuration (Section 11)
- **Drift Type:** DB contract drift
- **What drifted:** AuditLogs table was documented with `Id UNIQUEIDENTIFIER` PK. Actual SQL (`018_CreateAuditLogs.sql`) uses `AuditId BIGINT IDENTITY(1,1)` — exception to the GUID PK rule. Also missing from docs: `Action`, `FamilyId`, `IpAddress`, `UserAgent` columns. Column was `ChangedByUserId` in docs; actual is `UserId`. `EntityId` is `NVARCHAR(100)` not `UNIQUEIDENTIFIER`.
- **How resolved:** RESOLVED. AuditLogs table fully rewritten in 5.3.
- **Recurrence risk:** HIGH — BIGINT PK changes ORM mapping; `NVARCHAR EntityId` means GUID must be `.ToString()` not a typed FK.
- **Date resolved:** 2026-05-29

---

### Drift Entry 034 — CommentTemplates Not a Full BaseEntity; TemplateId PK Confirmed

- **Module:** Attendance System (Section 5)
- **Drift Type:** DB contract drift / stale docs
- **What drifted:** Section 5.3 had `[VERIFY]` on whether CommentTemplates has full BaseEntity columns. Confirmed: no `UpdatedAt`, `IsDeleted`, `DeletedAt`. `TemplateId` (not `Id`) is the PK. `TemplateText` max 500 chars. `IsActive` used for filtering. 12 system templates seeded (4 per category).
- **How resolved:** RESOLVED. CommentTemplates fully documented in 5.3 with seed data table.
- **Recurrence risk:** LOW — now documented. Risk: code assuming soft-delete on CommentTemplates would behave incorrectly.
- **Date resolved:** 2026-05-29

---

### Drift Entry 035 — FCM Push Re-Sent on Correction/Override (Conditional)

- **Module:** Attendance System (Section 5)
- **Drift Type:** Stale docs
- **What drifted:** Flow 3 and Flow 4 both had `[VERIFY]` on whether FCM push is re-sent when a record is corrected or overridden. Actual service (`EditAttendanceRecordAsync`): FCM IS re-sent, but only when `oldStatus != newStatus AND new status is Absent or Late`. Changing to Present or LeftEarly does not re-trigger a push.
- **How resolved:** RESOLVED. Both flow summaries updated with the exact conditional.
- **Recurrence risk:** LOW — now documented. Important for Flutter: UI must not assume a push was sent on status changes to Present.
- **Date resolved:** 2026-05-29

---

### Drift Entry 026 — FamilyDashboardDto Unconfirmed Fields and Wrong Scope Assumption

- **Module:** Family Dashboard (Section 4)
- **Drift Type:** Stale docs
- **What drifted:** Section 4 documented `FamilyDashboardDto` with only 2 confirmed fields
  (`MemberCount`, `FamilyScore`) and a long list of speculative unconfirmed fields
  (child summaries, task counts, calendar events, reward redemptions). It also incorrectly
  marked `FamilyStreakDays` as `[VERIFY]`, listed unconfirmed tables as reads, and said
  "real-time or cached — [VERIFY]".
  The actual DTO has exactly 12 fields, none of which include child summaries or task data.
  The scope boundary was clearly stated in Codex DevPlan Phase 03: "actual task/attendance
  data in dashboard (Phase 05+)" — meaning it was deferred and never implemented.
- **How resolved:** RESOLVED. `FamilyDashboardDto` fully confirmed from source
  (`FamilyDashboardDto.cs`). All 12 fields documented. Unconfirmed speculative fields removed.
  DB reads confirmed to exactly 3 tables. Real-time confirmed (no caching).
- **Recurrence risk:** HIGH — a Flutter developer reading the old docs would build a dashboard
  screen expecting child card data and task counts that the API does not return.
- **Date resolved:** 2026-05-29

---

### Drift Entry 027 — ParentCount Includes FamilyAdmin Role

- **Module:** Family Dashboard (Section 4)
- **Drift Type:** Stale docs / request drift
- **What drifted:** The `ParentCount` field in `FamilyDashboardDto` was not documented at all
  previously. The implementation counts members where `Role == Parent OR Role == FamilyAdmin`
  as the parent count. A Flutter developer might expect `ParentCount` to reflect only the
  Parent role, missing that FamilyAdmin is included.
- **How resolved:** RESOLVED. Section 4.2 and 4.4 now explicitly document this.
- **Recurrence risk:** LOW — now documented. Risk if a developer creates a separate
  FamilyAdmin count and double-counts.
- **Date resolved:** 2026-05-29

---

### Drift Entry 016 — coin-history Endpoint Missing from Section 3

- **Module:** Family & User Management (Section 3)
- **Drift Type:** Stale docs / missing entry
- **What drifted:** `GET /families/{familyId}/children/{childId}/coin-history` was documented
  in TechSpec Section 4.3 (Children) but was entirely absent from Section 3 in ProjectOverview.md.
  A Flutter developer building the child coin history screen would not find this endpoint.
- **How resolved:** RESOLVED. Endpoint added to Section 3.2 with paginated response shape.
- **Recurrence risk:** LOW — now documented.
- **Date resolved:** 2026-05-29

---

### Drift Entry 017 — CreateFamilyRequest Missing City Field

- **Module:** Family & User Management (Section 3)
- **Drift Type:** Request / response drift
- **What drifted:** Section 3.2 documented `CreateFamilyRequest { FamilyName }` only.
  TechSpec confirms `{ FamilyName, City }`. Same gap exists in `UpdateFamilyRequest`.
  If Flutter sends only `FamilyName`, the `City` field is silently omitted — family would
  have no city stored.
- **How resolved:** RESOLVED. Both `CreateFamilyRequest` and `UpdateFamilyRequest` updated
  to include `City` field.
- **Recurrence risk:** LOW — now documented.
- **Date resolved:** 2026-05-29

---

### Drift Entry 018 — JoinFamilyRequest and AddMemberRequest Missing LinkType + Using FullName not DisplayName

- **Module:** Family & User Management (Section 3)
- **Drift Type:** Request / response drift
- **What drifted:** Section 3.2 documented:
  - `JoinFamilyRequest { JoinCode, Role, DisplayName }` — missing `FullName`, missing `LinkType`
  - `AddMemberRequest { PhoneNumber, Role, DisplayName }` — missing `FullName`, missing `LinkType`
  TechSpec confirms both use `FullName` (not `DisplayName`) and both require `LinkType`.
  `LinkType` is a required enum with 13 allowed values. Missing it would cause 400 errors.
- **How resolved:** RESOLVED. Both request DTOs updated in Section 3.2.
- **Recurrence risk:** HIGH — `LinkType` is required. A Flutter developer using the old docs
  would get 400 errors on every member add or join attempt.
- **Date resolved:** 2026-05-29

---

### Drift Entry 019 — Plans Table: Missing Columns and Wrong Column Names

- **Module:** Family & User Management (Section 3), Admin Configuration (Section 11)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 Plans table was missing 7 columns and had one wrong column name:
  - Missing: `PlanCode` (NVARCHAR — `free_trial|basic|family|premium`), `MaxTeachers` INT,
    `HasElderMode` BIT, `HasWeeklyDigest` BIT, `HasAdvancedReports` BIT, `StorageQuotaMb` INT
  - Wrong: column was documented as `MonthlyPrice` — TechSpec confirms `PriceMonthly`
  - Wrong: Premium MaxChildren documented as `NULL` (unlimited) — TechSpec confirms `99`
  - `PlanCode` is especially critical: it is the value stored in the JWT `planCode` claim
- **How resolved:** RESOLVED. Section 3.3 Plans table fully rewritten from TechSpec.
- **Recurrence risk:** HIGH — `PlanCode` is used in JWT and API comparisons. `MaxTeachers`
  affects teacher plan enforcement. Any developer writing plan-limit logic from the old docs
  would use wrong column names.
- **Date resolved:** 2026-05-29

---

### Drift Entry 020 — Families Table: Missing Columns

- **Module:** Family & User Management (Section 3)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 Families table was missing 8 columns: `City`, `PlanId`,
  `SubscriptionId`, `FamilyAdminUserId`, `FamilyScoreUpdatedAt`, `CurrentStreakDays`,
  `BestStreakDays`, `TimezoneId`. PK documented as `Id` — TechSpec confirms `FamilyId`.
- **How resolved:** RESOLVED. Section 3.3 Families table fully rewritten from TechSpec.
- **Recurrence risk:** MEDIUM — missing columns cause silent data gaps. `FamilyAdminUserId`
  is used for sole-admin checks. `TimezoneId` is used for calendar/scheduling features.
- **Date resolved:** 2026-05-29

---

### Drift Entry 021 — Subscriptions.Status Is NVARCHAR Not INT

- **Module:** Family & User Management (Section 3), Admin Configuration (Section 11)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 documented `Status` as `INT` (enum storage pattern).
  TechSpec confirms `NVARCHAR(20)` with string values `Active|Trial|Expired|Cancelled`.
  Any developer writing queries like `WHERE Status = 1` would get no results.
- **How resolved:** RESOLVED. Subscriptions table updated in Section 3.3.
- **Recurrence risk:** HIGH — incorrect type causes silent query failures.
- **Date resolved:** 2026-05-29

---

### Drift Entry 022 — FamilyMembers Table: Missing LinkType, JoinedAt, InvitedByUserId

- **Module:** Family & User Management (Section 3)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 FamilyMembers table was missing `LinkType` (NOT NULL),
  `JoinedAt` (NOT NULL), and `InvitedByUserId` (NULL FK → Users). `LinkType` is especially
  critical — it is a required field on all member-add and join requests.
  PK documented as `Id` — TechSpec confirms `FamilyMemberId`.
- **How resolved:** RESOLVED. Section 3.3 FamilyMembers table fully rewritten from TechSpec.
- **Recurrence risk:** HIGH — any INSERT that omits `LinkType` will fail at DB level.
- **Date resolved:** 2026-05-29

---

### Drift Entry 023 — ChildProfiles Table: Missing Columns and Wrong DateOfBirth Type

- **Module:** Family & User Management (Section 3)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 ChildProfiles had multiple gaps:
  - Wrong type: `DateOfBirth` documented as `DATETIME2` — TechSpec confirms `DATE`
  - Missing: `GradeLevel` NVARCHAR(50), `SchoolName` NVARCHAR(200), `LevelCode` INT,
    `BestStreakDays` INT, `ScoreUpdatedAt` DATETIME2, `AgeYears` computed column
  - Missing: All 5 pillar score column names (`StudyScore`, `CleanlinessScore`,
    `DisciplineScore`, `ScreenControlScore`, `ResponsibilityScore`)
  - Wrong column: documented `DisplayName` — ChildProfiles has no DisplayName column.
    Display name lives in `FamilyMembers.DisplayName`.
  - PK documented as `Id` — TechSpec confirms `ChildProfileId`
- **How resolved:** RESOLVED. Section 3.3 ChildProfiles fully rewritten from TechSpec.
- **Recurrence risk:** HIGH — `DateOfBirth` type, missing pillar columns, and wrong DisplayName
  source would all cause incorrect queries and Flutter binding failures.
- **Date resolved:** 2026-05-29

---

### Drift Entry 024 — TeacherProfiles Missing SubjectName Column

- **Module:** Family & User Management (Section 3), Attendance System (Section 5)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 TeacherProfiles was missing `SubjectName NVARCHAR(200) NOT NULL`.
  This is a critical NOT NULL column — any INSERT into TeacherProfiles without SubjectName
  would fail. Also: PK documented as `Id` — TechSpec confirms `TeacherProfileId`.
  TeacherType values confirmed: School|Tuition|Arabic|Music|Other (resolves [VERIFY]).
- **How resolved:** RESOLVED. Section 3.3 TeacherProfiles fully rewritten from TechSpec.
- **Recurrence risk:** HIGH — missing NOT NULL column causes INSERT failures.
- **Date resolved:** 2026-05-29

---

### Drift Entry 025 — TeacherChildAssignments Missing FamilyId Column

- **Module:** Family & User Management (Section 3), Attendance System (Section 5)
- **Drift Type:** DB contract drift
- **What drifted:** Section 3.3 TeacherChildAssignments was missing `FamilyId NOT NULL FK → Families`.
  PK documented as `Id` — TechSpec confirms `AssignmentId`. Column `AssignedAt` confirmed
  (not just `CreatedAt`).
- **How resolved:** RESOLVED. Section 3.3 TeacherChildAssignments fully rewritten from TechSpec.
- **Recurrence risk:** MEDIUM — FamilyId is used for row-level family scoping on teacher assignment queries.
- **Date resolved:** 2026-05-29

---

### Drift Entry 009 — OTP Storage: Redis (spec) vs In-Memory (implementation)

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** Request / response drift (infrastructure)
- **What drifted:** TechSpec specifies Redis with 5-minute TTL for OTP storage.
  Phase 02 implementation uses an in-process `OtpService` (in-memory dictionary) due to
  no Redis connection available in the build environment.
  In-memory storage is lost on process restart — OTPs cannot survive app restarts or
  multi-instance deployments. This is a production risk.
- **How resolved:** UNRESOLVED. Both approaches share the same 5-minute TTL behavior in
  development. Production confirmation needed: if running multi-instance or with restarts,
  Redis must be wired. Read `OtpService.cs` to confirm current impl and update accordingly.
- **Recurrence risk:** HIGH in multi-instance / restart scenarios.
- **Date discovered:** 2026-05-29

---

### Drift Entry 010 — Users Table PK Column: `UserId` vs `Id`

- **Module:** Authentication & Session (Section 2), Family & User Management (Section 3)
- **Drift Type:** DB contract drift
- **What drifted:** BaseEntity convention uses `Id` as the C# property name. ProjectOverview.md
  previously documented the DB column as `Id` with a note "UserId in spec". The actual SQL
  script (`001_CreateUsers.sql`) confirms the DB column is `UserId` — matching the TechSpec.
  The C# `UserConfiguration.cs` maps `Id` (C# property) → `UserId` (DB column) via EF config.
- **How resolved:** RESOLVED. Section 2.3 Users table now documents `UserId` as the DB column.
  The FK in `002_CreateRefreshTokens.sql` (`FK_RefreshTokens_Users_UserId`) confirms this.
  C# code uses `Id` (BaseEntity); DB column is `UserId`.
- **Recurrence risk:** LOW — now correctly documented. Risk when adding FK from new tables:
  reference `dbo.Users (UserId)`, not `dbo.Users (Id)`.
- **Date resolved:** 2026-05-29

---

### Drift Entry 011 — PIN Hash Algorithm: bcrypt (spec) vs PBKDF2 (implementation)

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** DB contract drift
- **What drifted:** TechSpec specifies bcrypt for PIN hashing. Phase 02 implementation uses
  PBKDF2/SHA256 with a custom format: `v1.{base64(16-byte-salt)}.{base64(32-byte-hash)}`,
  100,000 iterations. Hash stored in `Users.PinHash`.
- **How resolved:** RESOLVED. Source confirmed from `AuthService.cs` — `HashPin()` and `VerifyPin()`
  use `Rfc2898DeriveBytes.Pbkdf2` with `HashAlgorithmName.SHA256`. Section 2.3 and 2.4 now
  document the PBKDF2 format. Spec reference to bcrypt is superseded by implementation.
- **Recurrence risk:** LOW — resolved and documented. Risk: if a future developer writes a
  separate PIN verification path and uses bcrypt, hashes will not match.
- **Date resolved:** 2026-05-29

---

### Drift Entry 012 — Auth Failure Status Codes: 400 (docs) vs 401 (implementation)

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** Request / response drift
- **What drifted:** ProjectOverview.md documented OTP mismatch, OTP expired, wrong PIN, PIN not
  set, and UserId not found as `400 Bad Request`. The actual implementation throws
  `UnauthorizedAccessException` for all these cases, which `ExceptionHandlingMiddleware` maps
  to `401 Unauthorized`. A Flutter developer relying on the documented 400 status would write
  incorrect error-handling logic.
- **How resolved:** RESOLVED. Sections 2.2 error case tables updated:
  - `POST /auth/verify-otp`: OTP invalid or expired → 401
  - `POST /auth/verify-pin`: UserId not found / PIN not set / wrong PIN → 401 (same message:
    "PIN is invalid." — deliberately vague to prevent user enumeration)
- **Recurrence risk:** HIGH — any new auth endpoint must use `UnauthorizedAccessException`
  for auth failures (not `ValidationException` which maps to 400/422).
- **Date resolved:** 2026-05-29

---

### Drift Entry 013 — `GET /auth/me` Does a DB Read (not claims-only)

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** Stale docs
- **What drifted:** Section 2.2 and 2.5 (Flow 7) documented `GET /auth/me` as a claims-only
  operation — "No DB read required." The actual `GetCurrentUserAsync` in `AuthService.cs`
  calls `_userRepository.GetByIdAsync(userId)` to populate `Name` and `PhoneNumber` from the
  DB row, falling back to JWT claims only when the row is not found.
- **How resolved:** RESOLVED. Section 2.2 business rules and Flow 7 updated to reflect the
  DB read. The behaviour is: DB read for Name + PhoneNumber; JWT claims for all other fields.
- **Recurrence risk:** LOW — low blast radius, but any caching strategy for this endpoint
  must account for the DB read, not assume it's stateless.
- **Date resolved:** 2026-05-29

---

### Drift Entry 014 — `revoke-token`: No Ownership Check + Token-Not-Found Returns 200

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** Stale docs / missing fallback
- **What drifted:** ProjectOverview.md documented two behaviors that do not exist in the
  implementation:
  1. "Token ownership validated — only the token owner (matched via `UserId` claim) may revoke."
     → `RevokeTokenAsync` does NOT check ownership. Any authenticated user can submit any
     token hash and it will be revoked.
  2. "Token not found → 404" → The service returns `true` (idempotent 200 OK) when token
     is not found. No `NotFoundException` is thrown.
- **How resolved:** RESOLVED. Section 2.2 business rules and error cases updated. Section 2.5
  Flow 4 updated to reflect the actual behavior.
- **Recurrence risk:** MEDIUM — the missing ownership check is a security consideration.
  If token revocation scoping becomes a requirement, it must be added explicitly to the service.
- **Date resolved:** 2026-05-29

---

### Drift Entry 015 — `verify-pin` Flow Used PhoneNumber Instead of UserId

- **Module:** Authentication & Session (Section 2)
- **Drift Type:** Request / response drift
- **What drifted:** Section 2.5 Flow 6 documented `POST /auth/verify-pin { PhoneNumber, Pin }`.
  The actual `VerifyPinRequest` DTO uses `UserId (Guid)` — not `PhoneNumber`. The Flutter child
  login flow is: join code → `GET /families/children` (name list) → child picks name → client
  gets `UserId` → calls `POST /auth/verify-pin { UserId, Pin }`.
- **How resolved:** RESOLVED. Section 2.2 verify-pin request DTO confirmed from source.
  Section 2.5 Flow 6 updated. Section 2.2 verify-pin business rules updated.
- **Recurrence risk:** HIGH — if a Flutter developer implements PIN login using PhoneNumber,
  the call will silently fail validation (PhoneNumber is not a field on `VerifyPinRequest`).
- **Date resolved:** 2026-05-29

---

### Maintenance Rules

To prevent new drift from accumulating:

1. **After every backend phase:** Update the relevant module section in ProjectOverview.md.
   Raw execution logs may be appended to the raw section below for audit but must NOT
   be used as the primary reference.

2. **After every Flutter phase:** Update Section 20 (project structure, screen-to-API
   map) and the relevant module's Section X.6 (Flutter Integration).

3. **After any DB schema change:** Update the relevant module's Section X.3 (DB Tables)
   AND Section 19.7 (Script Inventory).

4. **After any business rule change:** Update the relevant module's Section X.4 (Business
   Rules) with the specific condition, limit, and error code.

5. **After any Phase 17 recovery work:** Update Section 10, Section 19.7 (scripts 033-034),
   and close Drift Entries 001 and 003.

<!-- ================================================================
     SECTIONS 2–21 RESTRUCTURE COMPLETE (2026-05-29)
     Raw phase execution notes preserved below for audit only.
     DO NOT use raw notes as primary reference — use Sections 2–21 above.
     DO NOT edit the raw content below.
     ================================================================ -->

## Project Documentation - FamilyFirst Level 1 Codex Development Plan

Affected module section: Project Documentation / Level 1 Backend Development Plan
What changed: Read and summarized `Source/FamilyFirst_L1_Codex_DevPlan.docx`.
Files impacted: `Source/FamilyFirst_L1_Codex_DevPlan.docx` was read; `Source/ProjectOverview.txt` updated for task memory; `Source/ModuleIndex.txt` updated only to add navigation for the newly initialized overview section.
Why changed: User requested that the FamilyFirst Level 1 Codex development plan be read.
Date: 2026-04-11
Canonical status: Level 1 plan read. The document defines a backend-only .NET 8 / ASP.NET Core Web API / SQL Server 2022 Clean Architecture build with manual SQL scripts, JWT + refresh auth, MSG91 OTP, FCM push, AWS S3 photo verification, Domain/Application/Infrastructure/API layering, 20 implementation phases, 40 SQL scripts, and 103 API endpoints. No application source code was changed.

Current plan summary:
- Phase 01: Database Foundation & Solution Scaffold
- Phase 02: Authentication: OTP, JWT, Refresh Tokens
- Phase 03: Family & User Management
- Phase 04: Child & Teacher Profiles
- Phase 05: Attendance Sessions: Create & Schedule
- Phase 06: Attendance Marking, Submission & Parent Notification
- Phase 07: Comment Templates
- Phase 08: Task & Routine Foundation (CRUD)
- Phase 09: Task Completion, Photo Verification & Coin Award
- Phase 10: Coins, CoinTransactions & Streak Engine
- Phase 11: Teacher Feedback Submission
- Phase 12: Feedback Acknowledgement & Parent Response Loop
- Phase 13: Rewards Catalog (System & Family)
- Phase 14: Reward Redemption Lifecycle
- Phase 15: Family Calendar: Events & CRUD
- Phase 16: Event Reminders & Notification Scheduling
- Phase 17: Notification Engine: Push, Batching & History
- Phase 18: Reports & Weekly Digest
- Phase 19: Super Admin Panel & Analytics
- Phase 20: Family Admin Configuration & Final Integration

Execution notes captured from the document:
- Implement one phase at a time.
- Execute the phase SQL scripts immediately after generation.
- Run `dotnet build` after each phase and require 0 errors.
- Do not implement out-of-scope items for a phase.
- Do not create Flutter, Angular, or frontend code for Level 1.
- Raw `.sql` scripts are required; EF migrations are not used.

Phase 00 status check:
- `Source/FamilyFirst_L1_Codex_DevPlan.docx` defines Phase 00 as pre-execution setup only.
- Current workspace already contains the required setup artifacts: `Backend/FamilyFirst.sln`, four backend projects (`Backend/FamilyFirst.API`, `Backend/FamilyFirst.Application`, `Backend/FamilyFirst.Domain`, `Backend/FamilyFirst.Infrastructure`), and `Backend/FamilyFirst.API/appsettings.json`.
- Because Phases 01-06 are already implemented in the recorded project state, no Phase 00 code changes were applied in this task to avoid modifying completed later-phase work outside scope.

## Platform Foundation - Phase 01 Database Foundation & Solution Scaffold

Affected module section: Platform Foundation / Phase 01 Database Foundation & Solution Scaffold
What changed: Implemented Phase 01 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the Phase 01-relevant architecture, enum, and table definitions from `FamilyFirst_L1_TechSpec.docx`.
Files impacted: `FamilyFirst.sln`; `src/FamilyFirst.Domain/FamilyFirst.Domain.csproj`; `src/FamilyFirst.Application/FamilyFirst.Application.csproj`; `src/FamilyFirst.Infrastructure/FamilyFirst.Infrastructure.csproj`; `src/FamilyFirst.API/FamilyFirst.API.csproj`; `src/FamilyFirst.Domain/Entities/Base/BaseEntity.cs`; `src/FamilyFirst.Domain/Enums/UserRole.cs`; `src/FamilyFirst.Domain/Enums/AttendanceStatus.cs`; `src/FamilyFirst.Domain/Enums/FeedbackType.cs`; `src/FamilyFirst.Domain/Enums/FeedbackSeverity.cs`; `src/FamilyFirst.Domain/Enums/TaskStatus.cs`; `src/FamilyFirst.Domain/Enums/TaskTimeBlock.cs`; `src/FamilyFirst.Domain/Enums/EventType.cs`; `src/FamilyFirst.Domain/Enums/RedemptionStatus.cs`; `src/FamilyFirst.Domain/Enums/NotificationChannel.cs`; `src/FamilyFirst.Domain/Enums/NotificationPriority.cs`; `src/FamilyFirst.Domain/Enums/SubscriptionPlan.cs`; `src/FamilyFirst.Application/Common/Models/Result.cs`; `src/FamilyFirst.Application/Common/Models/PaginatedList.cs`; `src/FamilyFirst.Application/Common/Models/ApiResponse.cs`; `src/FamilyFirst.Application/Common/Exceptions/ValidationException.cs`; `src/FamilyFirst.Application/Common/Exceptions/NotFoundException.cs`; `src/FamilyFirst.Application/Common/Exceptions/ForbiddenAccessException.cs`; `src/FamilyFirst.Application/Common/Exceptions/ConflictException.cs`; `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `src/FamilyFirst.Infrastructure/DependencyInjection.cs`; `src/FamilyFirst.API/Program.cs`; `src/FamilyFirst.API/appsettings.json`; `src/FamilyFirst.API/Middleware/ExceptionHandlingMiddleware.cs`; `src/FamilyFirst.API/Middleware/RequestLoggingMiddleware.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/001_CreateUsers.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/002_CreateRefreshTokens.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/003_CreatePlans.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/004_CreateFamilies.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/005_CreateSubscriptions.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/006_CreateFamilyMembers.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/007_SeedPlans.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/008_SeedCommentTemplates.sql`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested Phase 01 implementation with .NET 8, SQL Server raw scripts, Clean Architecture, no frontend, no EF migrations, and no later-phase work.
Date: 2026-04-11
Canonical status: Phase 01 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors. SQL scripts 001-008 are present in order and execution-ready for SQL Server, but were not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification to keep only source and SQL artifacts.

Phase 01 implementation notes:
- Created the four-project Clean Architecture scaffold: API, Application, Domain, and Infrastructure.
- Added BaseEntity with Guid Id, CreatedAt, UpdatedAt, IsDeleted, and DeletedAt.
- Added the shared enum files from the tech spec enum list. Note: the Codex plan says 10 enum files while the tech spec explicitly lists 11; implementation followed the tech spec list and included `SubscriptionPlan.cs`.
- Added common Application response/result/pagination models and the four custom exception classes required by Phase 01.
- Added `FamilyFirstDbContext` with timestamp handling for future BaseEntity-derived entities and DI registration through `AddInfrastructure`.
- Added API startup pipeline with controllers registered, exception middleware, request logging middleware, HTTPS redirection, and no Phase 02+ controllers or endpoints.
- Added full Phase 01 `appsettings.json` structure with connection/JWT/OTP/FCM/AWS/App configuration placeholders.
- Added SQL scripts for Users, RefreshTokens, Plans, Families, Subscriptions, FamilyMembers, plan seed data, and system comment template seed data.
- Included `CommentTemplates` table creation inside `008_SeedCommentTemplates.sql` because Phase 07 states the table exists from Phase 01 while Phase 01 provides only a comment-template seed script slot.

## Authentication & Session - Phase 02 OTP JWT Refresh Tokens

Affected module section: Authentication & Session / Phase 02 OTP, JWT, Refresh Tokens
What changed: Implemented Phase 02 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the auth endpoint, validation, JWT claim, and User/RefreshToken table slices from `FamilyFirst_L1_TechSpec.docx`.
Files impacted: `src/FamilyFirst.Domain/Entities/User.cs`; `src/FamilyFirst.Domain/Entities/RefreshToken.cs`; `src/FamilyFirst.Application/DTOs/Auth/SendOtpRequest.cs`; `src/FamilyFirst.Application/DTOs/Auth/SendOtpResponse.cs`; `src/FamilyFirst.Application/DTOs/Auth/VerifyOtpRequest.cs`; `src/FamilyFirst.Application/DTOs/Auth/AuthResponse.cs`; `src/FamilyFirst.Application/DTOs/Auth/RefreshTokenRequest.cs`; `src/FamilyFirst.Application/DTOs/Auth/RevokeTokenRequest.cs`; `src/FamilyFirst.Application/DTOs/Auth/SetPinRequest.cs`; `src/FamilyFirst.Application/DTOs/Auth/VerifyPinRequest.cs`; `src/FamilyFirst.Application/Services/Interfaces/IAuthService.cs`; `src/FamilyFirst.Application/Services/Interfaces/IOtpService.cs`; `src/FamilyFirst.Application/Services/Implementations/AuthService.cs`; `src/FamilyFirst.Application/Validators/SendOtpRequestValidator.cs`; `src/FamilyFirst.Application/Validators/VerifyOtpRequestValidator.cs`; `src/FamilyFirst.Application/Validators/TokenRequestValidators.cs`; `src/FamilyFirst.Application/Validators/PinRequestValidators.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/UserConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/UserRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/RefreshTokenRepository.cs`; `src/FamilyFirst.Infrastructure/Services/JwtTokenService.cs`; `src/FamilyFirst.Infrastructure/Services/OtpService.cs`; `src/FamilyFirst.API/Controllers/v1/AuthController.cs`; `src/FamilyFirst.API/Middleware/RateLimitingMiddleware.cs`; `src/FamilyFirst.API/Filters/ValidationFilter.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/009_AlterUsers_AddIndexes.sql`; `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `src/FamilyFirst.Infrastructure/DependencyInjection.cs`; `src/FamilyFirst.API/Program.cs`; `src/FamilyFirst.API/Middleware/ExceptionHandlingMiddleware.cs`; `src/FamilyFirst.Application/FamilyFirst.Application.csproj`; `src/FamilyFirst.Infrastructure/FamilyFirst.Infrastructure.csproj`; `src/FamilyFirst.API/FamilyFirst.API.csproj`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested Phase 02 implementation with .NET 8, SQL Server raw scripts, Clean Architecture, no frontend, no EF migrations, and no later-phase work.
Date: 2026-04-11
Canonical status: Phase 02 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors after one scoped fix replacing ASP.NET-specific `FindFirstValue` usage in Application with plain `ClaimsPrincipal.FindFirst`. SQL script `009_AlterUsers_AddIndexes.sql` is present after scripts 001-008 and execution-ready for SQL Server, but was not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification to keep only source and SQL artifacts.

Phase 02 implementation notes:
- Added User and RefreshToken domain entities and EF configurations mapped to the Phase 01 SQL table names/columns.
- Added Auth DTOs in eight Phase 02 files; `AuthResponse.cs` also contains the related `UserDto` and `CurrentUserDto` response shapes to keep file creation within the phase file count.
- Added `IAuthService`, `IOtpService`, token/repository abstractions, `AuthService`, and repository implementations for User and RefreshToken.
- Added `OtpService` with in-memory 5-minute OTP token storage and MSG91 HTTP delivery path; when MSG91 config placeholders remain unset, it logs the OTP instead of attempting external delivery.
- Added `JwtTokenService` with access token generation, refresh token generation, and SHA-256 refresh-token hashing.
- Added refresh token rotation on use, revoke handling, OTP verify user creation, PIN set/verify with PBKDF2 hashing, and `/auth/me` claim echoing.
- Added the seven Phase 02 auth endpoints: send OTP, verify OTP, refresh token, revoke token, set PIN, verify PIN, and me.
- Added FluentValidation validators in four files and a global `ValidationFilter`.
- Added `RateLimitingMiddleware` for `/api/v1/auth/send-otp`, enforcing 3 OTP requests per hour per phone number.
- Added JWT bearer authentication and authorization wiring in `Program.cs`.
- Family context claims, teacher assigned child claims, and child/teacher profile-backed claims remain absent until Phase 03/Phase 04 create their backing tables and entities, matching Phase 02 out-of-scope boundaries.

## Family & User Management - Phase 03 Family and User Management

Affected module section: Family & User Management / Phase 03 Family & User Management
What changed: Implemented Phase 03 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the Family/User API, validation, JWT family context, and Plan/Family/Subscription/FamilyMember table slices from `FamilyFirst_L1_TechSpec.docx`. Applied compatible parts of `New API Format.txt` and `New SQL Format.txt` while preserving the approved FamilyFirst schema names and phase file list.
Files impacted: `src/FamilyFirst.Domain/Entities/Family.cs`; `src/FamilyFirst.Domain/Entities/FamilyMember.cs`; `src/FamilyFirst.Domain/Entities/Subscription.cs`; `src/FamilyFirst.Domain/Entities/Plan.cs`; `src/FamilyFirst.Application/DTOs/Family/**`; `src/FamilyFirst.Application/DTOs/User/**`; `src/FamilyFirst.Application/Services/Interfaces/IFamilyService.cs`; `src/FamilyFirst.Application/Services/Interfaces/IUserService.cs`; `src/FamilyFirst.Application/Services/Implementations/FamilyService.cs`; `src/FamilyFirst.Application/Services/Implementations/UserService.cs`; `src/FamilyFirst.Application/Validators/FamilyRequestValidators.cs`; `src/FamilyFirst.Application/Validators/JoinFamilyRequestValidator.cs`; `src/FamilyFirst.Application/Validators/MemberRequestValidators.cs`; `src/FamilyFirst.Application/Validators/UserRequestValidators.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/FamilyConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/FamilyMemberConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/PlanConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/SubscriptionConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/FamilyRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/FamilyMemberRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/SubscriptionRepository.cs`; `src/FamilyFirst.API/Controllers/v1/FamiliesController.cs`; `src/FamilyFirst.API/Controllers/v1/UsersController.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/010_CreateFamilyMemberIndexes.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/011_AlterFamilies_JoinCode.sql`; Phase 03 wiring changes in `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`, `src/FamilyFirst.Infrastructure/DependencyInjection.cs`, `src/FamilyFirst.Application/Services/Interfaces/IAuthService.cs`, `src/FamilyFirst.Application/Services/Implementations/AuthService.cs`, and `src/FamilyFirst.Infrastructure/Services/JwtTokenService.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested Phase 03 implementation with .NET 8, SQL Server raw scripts, Clean Architecture, no frontend, no EF migrations, strict phase-only scope, and the new API/SQL format guides.
Date: 2026-04-11
Canonical status: Phase 03 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors. SQL scripts 010-011 are present after scripts 001-009 and execution-ready for SQL Server, but were not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification.

Phase 03 implementation notes:
- Added Family, FamilyMember, Subscription, and Plan domain entities and matching EF configurations.
- Added eight Family DTO files and three User DTO files for the Phase 03 endpoints.
- Added FamilyService and UserService with repository-only data access and service-level authorization checks.
- Added family create, get, update, join code get/regenerate, join family, member list/add/update/remove, dashboard, user get/update, and FCM-token update endpoints.
- POST `/api/v1/families` creates a family, FreeTrial subscription, and FamilyAdmin membership.
- Duplicate family ownership, duplicate membership, SuperAdmin assignment, sole FamilyAdmin removal, and child plan limits are enforced.
- Dashboard returns Phase 03-safe aggregate member counts and family score/streak fields only; task/attendance data remains out of scope until later phases.
- Added `010_CreateFamilyMemberIndexes.sql` and `011_AlterFamilies_JoinCode.sql` as idempotent raw SQL scripts. These preserve FamilyFirst's existing `Families`/`FamilyMembers` table and index names from the plan, even though `New SQL Format.txt` has generic Coolzo `tbl` naming guidance.
- Extended Phase 02 token generation so future auth/refresh/PIN tokens include available `familyId`, `familyMemberId`, `planCode`, and membership role after Phase 03 membership data exists. Phase 04 child/teacher profile claims remain out of scope.

## Profiles - Phase 04 Child & Teacher Profiles

Affected module section: Profiles / Phase 04 Child & Teacher Profiles
What changed: Implemented Phase 04 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the ChildProfile, TeacherProfile, TeacherChildAssignment, child API, teacher assignment API, JWT profile-claim, and validation slices from `FamilyFirst_L1_TechSpec.docx`. Applied compatible parts of `New API Format.txt` and `New SQL Format.txt` while preserving approved FamilyFirst schema names and phase boundaries.
Files impacted: `src/FamilyFirst.Domain/Entities/ChildProfile.cs`; `src/FamilyFirst.Domain/Entities/TeacherProfile.cs`; `src/FamilyFirst.Domain/Entities/TeacherChildAssignment.cs`; `src/FamilyFirst.Application/DTOs/Family/ChildSummaryDto.cs`; `src/FamilyFirst.Application/DTOs/Family/ChildDetailDto.cs`; `src/FamilyFirst.Application/DTOs/Family/UpdateChildRequest.cs`; `src/FamilyFirst.Application/DTOs/Family/ScoreHistoryDto.cs`; `src/FamilyFirst.Application/DTOs/Family/DeductCoinsRequest.cs`; `src/FamilyFirst.Application/Services/Interfaces/IChildService.cs`; `src/FamilyFirst.Application/Services/Interfaces/ITeacherService.cs`; `src/FamilyFirst.Application/Services/Implementations/ChildService.cs`; `src/FamilyFirst.Application/Services/Implementations/TeacherService.cs`; `src/FamilyFirst.Application/Validators/ProfileValueValidators.cs`; `src/FamilyFirst.Application/Validators/UpdateChildRequestValidator.cs`; `src/FamilyFirst.Application/Validators/DeductCoinsRequestValidator.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/ChildProfileConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/TeacherProfileConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/TeacherChildAssignmentConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/ChildProfileRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/TeacherProfileRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/TeacherChildAssignmentRepository.cs`; `src/FamilyFirst.API/Controllers/v1/ChildrenController.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/012_CreateChildProfiles.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/013_CreateTeacherProfiles.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/014_CreateTeacherChildAssignments.sql`; Phase 04 wiring changes in `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`, `src/FamilyFirst.Infrastructure/DependencyInjection.cs`, `src/FamilyFirst.Application/Services/Implementations/FamilyService.cs`, and `src/FamilyFirst.Application/Services/Implementations/AuthService.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested Phase 04 implementation with .NET 8, SQL Server raw scripts, Clean Architecture, no frontend, no EF migrations, strict phase-only scope, and the new API/SQL format guides.
Date: 2026-04-11
Canonical status: Phase 04 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors. SQL scripts 012-014 are present after scripts 001-011 and are statically ordered/readied for SQL Server execution, but were not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification.

Phase 04 implementation notes:
- Added ChildProfile, TeacherProfile, and TeacherChildAssignment domain entities, DbSets, EF configurations, repositories, and raw SQL scripts 012-014.
- Added the five Phase 04 Family DTO files for child summary, detail, update, score-history, and coin-deduction responses/requests.
- Added ChildService and TeacherService with service-level authorization checks for Parent, FamilyAdmin, and Child-own profile access through the `childProfileId` JWT claim.
- Added ChildrenController with the seven Phase 04 endpoints: child list, child detail, child update, score history, coin deduction stub, teacher assign, and teacher unassign.
- Extended FamilyService only where Phase 04 requires it: Child and Teacher profiles are created automatically when a FamilyMember is added, joins, or is updated into Child/Teacher role. No separate profile-create endpoints from later phases were added.
- Extended AuthService token context so Child tokens include `childProfileId`, Teacher tokens include `teacherProfileId`, and Teacher tokens include active `assignedChildIds`. JwtTokenService already had the Phase 02/03 claim emission hooks and did not need a Phase 04 rewrite.
- Teacher assignment uniqueness is enforced in service with 409 conflict and in SQL/EF through unique `(TeacherProfileId, ChildProfileId)` filtering where `IsActive = 1`.
- Avatar code validation rejects values outside `avatar_01` through `avatar_10`; child date-of-birth validation enforces 3-17 years when provided.
- Score history remains Phase 04-safe and returns the current score snapshot only; no task, attendance, feedback, or later analytics data was introduced.
- Coin deduction updates the ChildProfile coin balance only and returns `IsRecorded = false`; no later-phase coin ledger/table recording was introduced.
- SQL scripts preserve FamilyFirst table names (`ChildProfiles`, `TeacherProfiles`, `TeacherChildAssignments`) instead of generic Coolzo `tbl` prefixes because the approved plan and existing scripts already establish the FamilyFirst schema naming.

## Attendance System - Phase 05 Attendance Sessions Create & Schedule

Affected module section: Attendance System / Phase 05 Attendance Sessions: Create & Schedule
What changed: Implemented Phase 05 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the AttendanceSession table, attendance session API, recurring-day storage, and validation slices from `FamilyFirst_L1_TechSpec.docx`. Applied compatible parts of `New API Format.txt` and `New SQL Format.txt` while preserving approved FamilyFirst schema names and phase boundaries.
Files impacted: `src/FamilyFirst.Domain/Entities/AttendanceSession.cs`; `src/FamilyFirst.Application/DTOs/Attendance/CreateSessionRequest.cs`; `src/FamilyFirst.Application/DTOs/Attendance/AttendanceSessionDto.cs`; `src/FamilyFirst.Application/Services/Interfaces/IAttendanceService.cs`; `src/FamilyFirst.Application/Services/Implementations/AttendanceService.cs`; `src/FamilyFirst.Application/Validators/CreateSessionRequestValidator.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/AttendanceSessionConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/AttendanceSessionRepository.cs`; `src/FamilyFirst.API/Controllers/v1/AttendanceController.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/015_CreateAttendanceSessions.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/016_CreateAttendanceSessionIndexes.sql`; Phase 05 wiring changes in `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs` and `src/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested Phase 05 implementation with .NET 8, SQL Server raw scripts, Clean Architecture, no frontend, no EF migrations, strict phase-only scope, and the new API/SQL format guides.
Date: 2026-04-11
Canonical status: Phase 05 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors after one scoped validator fix for the `RecurringDays` array length check. SQL scripts 015-016 are present after scripts 001-014 and are statically ordered/readied for SQL Server execution, but were not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification.

Phase 05 implementation notes:
- Added AttendanceSession domain entity, DbSet, EF configuration, repository implementation, and raw SQL scripts 015-016.
- Added Attendance DTOs for creating sessions and returning session details; recurring days are accepted as a JSON request array and stored as JSON text in `AttendanceSessions.RecurringDays`.
- Added AttendanceService with session create, list-by-date, and detail retrieval only.
- Added AttendanceController with the three Phase 05 endpoints: create session, list sessions by date, and get session detail.
- Teacher session creation is bound to the current member's active TeacherProfile in the requested family, enforcing the assigned-family rule. FamilyAdmin is accepted by the role gate only when the current member also has an active TeacherProfile because the Phase 05 request DTO does not include a TeacherProfileId target field.
- Teacher listing returns only the teacher's own sessions. Parent/FamilyAdmin listing returns sessions whose teacher is assigned to the family's children through existing `TeacherChildAssignments`.
- ScheduledDate validation enforces max 7 days past and max 30 days future; EndTime must be after StartTime when provided; RecurringDays must be unique integers 1 through 7 when IsRecurring is true.
- No AttendanceRecord entity, attendance submit endpoint, record-edit endpoint, comment-template session logic, parent notifications, or Phase 06/07 behavior was introduced.
- SQL scripts preserve FamilyFirst table names (`AttendanceSessions`) instead of generic Coolzo `tbl` prefixes because the approved plan and existing scripts already establish the FamilyFirst schema naming.

## Attendance System - Phase 06 Attendance Marking Submission & Parent Notification

Affected module section: Attendance System / Phase 06 Attendance Marking, Submission & Parent Notification
What changed: Implemented Phase 06 only from `FamilyFirst_L1_Codex_DevPlan.docx`, aligned with the AttendanceRecords, AuditLogs, attendance marking API, teacher edit-window, FamilyAdmin override/audit, and FCM parent-alert slices from `FamilyFirst_L1_TechSpec.docx`. Applied compatible parts of `New API Format.txt` and `New SQL Format.txt` while preserving approved FamilyFirst schema names and phase boundaries.
Files impacted: `src/FamilyFirst.Domain/Entities/AttendanceRecord.cs`; `src/FamilyFirst.Domain/Entities/AuditLog.cs`; `src/FamilyFirst.Application/DTOs/Attendance/SubmitAttendanceRequest.cs`; `src/FamilyFirst.Application/DTOs/Attendance/AttendanceRecordDto.cs`; `src/FamilyFirst.Application/DTOs/Attendance/EditAttendanceRequest.cs`; `src/FamilyFirst.Application/DTOs/Attendance/AttendanceSummaryDto.cs`; `src/FamilyFirst.Application/Services/Interfaces/IAttendanceService.cs`; `src/FamilyFirst.Application/Services/Implementations/AttendanceService.cs`; `src/FamilyFirst.Application/Validators/SubmitAttendanceRequestValidator.cs`; `src/FamilyFirst.Application/Validators/EditAttendanceRequestValidator.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/AttendanceRecordConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Configurations/AuditLogConfiguration.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/AttendanceRecordRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/AuditLogRepository.cs`; `src/FamilyFirst.Infrastructure/Data/Repositories/Implementations/AttendanceSessionRepository.cs`; `src/FamilyFirst.Infrastructure/Services/FcmPushNotificationService.cs`; `src/FamilyFirst.API/Controllers/v1/AttendanceController.cs`; `src/FamilyFirst.Infrastructure/Data/Scripts/017_CreateAttendanceRecords.sql`; `src/FamilyFirst.Infrastructure/Data/Scripts/018_CreateAuditLogs.sql`; Phase 06 wiring changes in `src/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs` and `src/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested proceeding with Phase 06 under strict phase-only implementation constraints and requested following `New API Format.txt` and `New SQL Format.txt`.
Date: 2026-04-11
Canonical status: Phase 06 complete and build-ready. `dotnet build FamilyFirst.sln` completed with 0 warnings and 0 errors. SQL scripts 017-018 are present after scripts 001-016 and are statically ordered/readied for SQL Server execution, but were not executed because no SQL Server/sqlcmd connection is available in this environment. Generated build output directories were removed after verification.

Phase 06 implementation notes:
- Added AttendanceRecord and AuditLog domain entities, DbSets, EF configurations, repositories, and raw SQL scripts 017-018.
- Extended IAttendanceService and AttendanceService with bulk attendance submission, attendance record editing, child attendance history, and session record listing only.
- Added Attendance DTOs for bulk submission, edit request, record response, and summary response; the summary DTO is present for the Phase 06 contract surface but no later analytics or weekly summary flow was implemented.
- Added SubmitAttendanceRequestValidator and EditAttendanceRequestValidator for valid AttendanceStatus values, unique submitted child IDs, and 500-character teacher comments.
- Extended AttendanceController with the four Phase 06 endpoints: submit attendance, edit attendance record, list child attendance history, and list session records.
- Bulk submission creates one AttendanceRecord for every active child assigned to the submitting teacher, defaults omitted assigned children to Present, rejects unassigned submitted child IDs, marks the session submitted, and blocks repeat submission with 409 conflict.
- Teacher edits are allowed only for the teacher's own submitted session while the SubmittedAt UTC timestamp is less than one hour old. FamilyAdmin edits are allowed outside the teacher edit window and create AuditLog rows with OldValues/NewValues JSON.
- Parent push alerts are scoped to Absent and Late attendance statuses through a minimal FCM push service using the existing user FcmToken field and appsettings Fcm configuration. No Notifications table, notification history endpoint, CommentTemplates CRUD, or weekly summary behavior from later phases was introduced.
- SQL scripts preserve FamilyFirst table names (`AttendanceRecords`, `AuditLogs`) instead of generic Coolzo `tbl` prefixes because the approved plan and existing scripts already establish the FamilyFirst schema naming.

## Attendance System - Phase 07 Comment Templates

Affected module section: Attendance System / Phase 07 Comment Templates
What changed: Implemented Phase 07 comment template management from `Source/FamilyFirst_L1_Codex_DevPlan.docx`, using the existing `CommentTemplates` table created in Phase 01 and adding family-scoped CRUD, merged list retrieval, validators, repository/configuration wiring, and controller endpoints only.
Files impacted: `Backend/FamilyFirst.Domain/Entities/CommentTemplate.cs`; `Backend/FamilyFirst.Application/DTOs/Attendance/CommentTemplateDto.cs`; `Backend/FamilyFirst.Application/DTOs/Attendance/CreateCommentTemplateRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Attendance/UpdateCommentTemplateRequest.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ICommentTemplateService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/CommentTemplateService.cs`; `Backend/FamilyFirst.Application/Validators/CreateCommentTemplateRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/UpdateCommentTemplateRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/CommentTemplateConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/CommentTemplateRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/CommentTemplatesController.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Application/Common/Exceptions/ValidationException.cs`; `Backend/FamilyFirst.API/Middleware/ExceptionHandlingMiddleware.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 07 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 07 implemented in source only. No SQL script was added because the plan states the `CommentTemplates` table already exists from Phase 01. No build, runtime execution, test run, or SQL execution was performed in this task because the user explicitly prohibited testing/execution/validation/debugging.

Phase 07 implementation notes:
- Added `CommentTemplate` as the domain model matching the actual Phase 01 table shape already present in `008_SeedCommentTemplates.sql`, including `TemplateId`, `FamilyId`, `TemplateText`, `Category`, `IsSystem`, `IsActive`, `SortOrder`, and `CreatedAt`.
- Added three Attendance DTO files for template responses and create/update requests, plus shared category constants for `Attendance`, `Feedback`, and `Homework`.
- Added `ICommentTemplateService` and `ICommentTemplateRepository`, then implemented `CommentTemplateService` and `CommentTemplateRepository`.
- Added `CommentTemplateConfiguration` and `DbSet<CommentTemplate>` wiring so EF maps to the existing `CommentTemplates` table with active-template query filtering.
- Added `/api/v1/families/{familyId}/comment-templates` GET/POST/PUT/DELETE endpoints in `CommentTemplatesController`.
- GET returns merged system + family templates, supports optional category filtering, and sorts by `SortOrder` then `TemplateText`.
- POST/PUT enforce category validity and `TemplateText` length; POST/PUT/DELETE require `FamilyAdmin`; GET requires `Teacher` or `FamilyAdmin`.
- System templates are exposed read-only. Update/delete attempts against `IsSystem = 1` templates return forbidden.
- Family template count is capped at 20 per category. To support the phase rule that this returns HTTP 422 without changing unrelated earlier-phase validation behavior, `ValidationException` now carries an explicit status code and the exception middleware respects it.
- No new SQL scripts, attendance submission changes, feedback module changes, subject-specific template rules, or later-phase features were introduced.

## Task & Routine System - Phase 08 Task & Routine Foundation CRUD

Affected module section: Task & Routine System / Phase 08 Task & Routine Foundation (CRUD)
What changed: Implemented Phase 08 task foundation from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `TaskItems` schema/rule slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding task CRUD, date-based family task listing, SuperAdmin task-template catalog endpoints, validators, repository/configuration wiring, and SQL scripts `019`-`020` only.
Files impacted: `Backend/FamilyFirst.Domain/Entities/TaskItem.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskMetadata.cs`; `Backend/FamilyFirst.Application/DTOs/Task/CreateTaskRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Task/UpdateTaskRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskItemDto.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskTemplateDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ITaskService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/TaskService.cs`; `Backend/FamilyFirst.Application/Validators/TaskRequestValidators.cs`; `Backend/FamilyFirst.Application/Validators/TaskTemplateRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/TaskItemConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/TaskItemRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/TasksController.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/019_CreateTaskItems.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/020_CreateTaskItemIndexes.sql`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 08 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 08 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 08 implementation notes:
- Added `TaskItem` as the Phase 08 domain model with the tech-spec `TaskItems` fields for family tasks: `TaskId`, `FamilyId`, `ChildProfileId`, `CreatedByUserId`, `TaskName`, `Instructions`, `IconCode`, `TimeBlock`, `DurationMinutes`, `CoinValue`, `IsPhotoRequired`, `PillarTag`, `IsRecurring`, `RecurringDays`, `ActiveFromDate`, `ActiveToDate`, `IsActive`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, and `DeletedAt`.
- Added the Task DTO folder and five DTO files for create/update task requests, task responses, task-template responses, and task-template creation. `TaskMetadata.cs` centralizes the Phase 08 pillar-tag and recurrence validation constants.
- Added `ITaskService` and `ITaskItemRepository`, then implemented `TaskService` and `TaskItemRepository`.
- Added `TaskItemConfiguration` and `DbSet<TaskItem>` wiring so EF maps to the new `TaskItems` table and applies the active task query filter.
- Added `/api/v1/families/{familyId}/tasks` GET/POST/PUT/DELETE and `/api/v1/admin/task-templates` GET/POST in `TasksController`.
- GET family tasks filters by date against `RecurringDays` and `ActiveFromDate`/`ActiveToDate`. Child users are restricted to their `childProfileId` JWT context plus family-wide tasks with `ChildProfileId = NULL`. Parent users can view all family tasks or a specific child's task view.
- POST/PUT task validation enforces Phase 08 rules: `TaskName` length, `TimeBlock != School`, `CoinValue` 5-200, `DurationMinutes` 5-120, valid recurring day arrays, valid optional pillar tags, and `ActiveFromDate` range checks.
- Soft delete marks the task inactive and deleted only; no task completion, photo upload, verification queue, or Phase 09 reward logic was introduced.
- The plan/API contract required `/admin/task-templates` GET/POST but neither `ProjectOverview` nor the tech spec defined a separate Phase 08 table for templates. To keep the implementation inside Phase 08 and avoid inventing a new module/table, system templates are stored in `TaskItems` with `IsSystemTemplate = 1`, `FamilyId = NULL`, and `ChildProfileId = NULL`. Two metadata columns, `TemplateCategory` and `AgeGroup`, were added to support the documented admin filters `?category&ageGroup`. This storage shape is an explicit implementation inference from the Phase 08 API contract plus the product document's "master task templates by category and age group" requirement.
- SQL script `019_CreateTaskItems.sql` creates `TaskItems` with the task fields above plus the template-support metadata required by the documented admin endpoints. SQL script `020_CreateTaskItemIndexes.sql` adds the family task lookup index required by the phase.
- No TaskCompletion entity, no photo-upload flow, no verification queue, no coin awarding, and no Phase 09+ behavior was introduced.

## Task & Routine System - Phase 09 Task Completion Photo Verification & Coin Award

Affected module section: Task & Routine System / Phase 09 Task Completion, Photo Verification & Coin Award
What changed: Implemented Phase 09 task completion flow from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `TaskCompletions` schema/API/rule slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding task completion submission, review, queue, batch approval, S3 presigned upload URL generation, validators, repository/configuration wiring, and SQL script `021` only.
Files impacted: `Backend/FamilyFirst.Domain/Entities/TaskCompletion.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskCompletionDto.cs`; `Backend/FamilyFirst.Application/DTOs/Task/SubmitTaskCompletionRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Task/ReviewTaskCompletionRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskCompletionUploadUrlRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Task/TaskCompletionUploadUrlDto.cs`; `Backend/FamilyFirst.Application/Validators/TaskCompletionRequestValidators.cs`; `Backend/FamilyFirst.Application/Validators/ReviewTaskCompletionRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/TaskCompletionConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/TaskCompletionRepository.cs`; `Backend/FamilyFirst.Infrastructure/Services/S3StorageService.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/021_CreateTaskCompletions.sql`; Phase 09 extensions in `Backend/FamilyFirst.Application/Services/Interfaces/ITaskService.cs`, `Backend/FamilyFirst.Application/Services/Implementations/TaskService.cs`, `Backend/FamilyFirst.API/Controllers/v1/TasksController.cs`, `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`, `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`, and `Backend/FamilyFirst.Infrastructure/FamilyFirst.Infrastructure.csproj`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 09 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 09 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 09 implementation notes:
- Added `TaskCompletion` as the Phase 09 domain model with the tech-spec `TaskCompletions` fields: `CompletionId`, `TaskId`, `ChildProfileId`, `FamilyId`, `ScheduledDate`, `Status`, `PhotoUrl`, `SubmittedAt`, `ReviewedByUserId`, `ReviewedAt`, `ReviewNote`, `CoinsAwarded`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, and `DeletedAt`.
- Added five Phase 09 task DTO files for completion responses, child submission, parent review, upload-url request, and upload-url response. `TaskCompletionDto.cs` also contains `BatchApproveResultDto` to keep the DTO surface within the requested phase file count.
- Added two validator files for completion submission, upload-url request validation, and parent review validation. Flagged reviews enforce `ReviewNote` min 5 / max 500 characters.
- Added `TaskCompletionConfiguration`, `DbSet<TaskCompletion>`, `ITaskCompletionRepository`, and `TaskCompletionRepository`. The repository includes the required verification-queue query and a transaction-backed update path for approval plus `ChildProfile` coin-balance updates.
- Extended `ITaskService` and `TaskService` with: completion listing, child submission, parent review, verification queue listing, batch approval, and S3 upload-url generation.
- Added all six Phase 09 endpoints to `TasksController`: `GET /tasks/completions`, `POST /tasks/{taskId}/completions`, `PUT /tasks/completions/{completionId}/review`, `GET /tasks/verification-queue`, `POST /tasks/verification-queue/approve-all`, and `POST /tasks/completions/upload-url`.
- Child submission creates one completion per `(TaskId, ChildProfileId, ScheduledDate)` and returns 409 on duplicate. Submission is limited to the logged-in child's own `childProfileId` plus family-wide tasks with `ChildProfileId = NULL`. If `TaskItem.IsPhotoRequired = true`, `PhotoUrl` is required.
- Parent review is restricted to `Parent` role exactly, matching the Phase 09 role table. Approve sets `Status = Approved`, snapshots `CoinsAwarded = TaskItem.CoinValue`, and increments `ChildProfile.CoinBalance` and `ChildProfile.TotalCoinsEarned` directly. Flag sets `Status = Flagged`, stores `ReviewNote`, and awards zero coins. The Phase 10 coin-ledger/table work remains out of scope.
- Parent notification on child submission is delivered through the existing `IPushNotificationService`/FCM path using family members with `Role = Parent`. Child push is sent on approve and flag using the child profile's linked user `FcmToken`.
- Added `S3StorageService` plus the infrastructure package reference `AWSSDK.S3`. Presigned upload URLs follow the documented key format `family/{familyId}/tasks/{taskId}/{GUID}.jpg` with 15-minute expiry, using the configured AWS bucket and region from `appsettings.json`.
- SQL script `021_CreateTaskCompletions.sql` creates `TaskCompletions` plus the required unique index `IX_TaskCompletions_Task_Child_Date`. No Phase 10 coin ledger, no streak engine, and no Phase 17 missed-task cron logic was introduced.

## Rewards & Streak Engine - Phase 10 Coins, CoinTransactions & Streak Engine

Affected module section: Rewards & Streak Engine / Phase 10 Coins, CoinTransactions & Streak Engine
What changed: Implemented the Phase 10 coin-ledger module from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `CoinTransactions` schema/API/rule slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding append-only coin transactions, optimistic-concurrency support on `ChildProfiles`, a dedicated coin service/repository path, child coin-history and streak-freeze APIs, and Phase 09 task-approval integration through the new ledger service.
Files impacted: `Backend/FamilyFirst.Domain/Entities/CoinTransaction.cs`; `Backend/FamilyFirst.Domain/Entities/ChildProfile.cs`; `Backend/FamilyFirst.Application/DTOs/Task/CoinTransactionDto.cs`; `Backend/FamilyFirst.Application/DTOs/Task/DeductCoinsRequest.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ICoinService.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IChildService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/CoinService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/ChildService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/TaskService.cs`; `Backend/FamilyFirst.Application/Validators/DeductCoinsRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/CoinTransactionConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/ChildProfileConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/CoinTransactionRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/022_CreateCoinTransactions.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/023_AlterChildProfiles_RowVersion.sql`; `Backend/FamilyFirst.API/Controllers/v1/ChildrenController.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 10 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 10 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 10 implementation notes:
- Added `CoinTransaction` as the Phase 10 ledger model with the tech-spec fields `TransactionId`, `ChildProfileId`, `FamilyId`, `TransactionType`, `Amount`, `BalanceAfter`, `ReferenceType`, `ReferenceId`, `Note`, `CreatedByUserId`, and `CreatedAt`.
- Added the Phase 10 task DTOs `CoinTransactionDto` and `DeductCoinsRequest`, then switched the active child coin-deduction validator/service/controller path to the new task-side request contract. Coin deduction requires a reason and validates `Note` at the documented 5-500 character range before the service writes the ledger entry.
- Added `ICoinService`, `CoinService`, `ICoinTransactionRepository`, and `CoinTransactionRepository`. Coin mutations now flow through a dedicated append-only ledger path that updates `ChildProfiles` and inserts `CoinTransactions` in one repository transaction. `DbUpdateConcurrencyException` is translated into a Phase 10 conflict response message for optimistic-concurrency retries.
- Added `GET /api/v1/families/{familyId}/children/{childId}/coin-history`, `POST /api/v1/families/{familyId}/children/{childId}/coin-deduction`, and `POST /api/v1/families/{familyId}/children/{childId}/streak/use-freeze` in `ChildrenController`, with `ChildService` delegating the Phase 10 logic to `ICoinService`.
- Added `RowVersion` to `ChildProfile`, mapped it with `IsRowVersion()` in EF, exposed `DbSet<CoinTransaction>` in `FamilyFirstDbContext`, and registered the new service/repository in infrastructure DI.
- Updated `TaskService` parent approval and batch-approval flows so Phase 09 no longer mutates `ChildProfile.CoinBalance` or `TotalCoinsEarned` directly. The task completion is first marked approved with `CoinsAwarded = TaskItem.CoinValue`, then the balance/ledger write is performed through `ICoinService` with `ReferenceType = TaskCompletion` and `ReferenceId = CompletionId`.
- `CoinService` applies the documented level thresholds (`0-499 => 1`, `500-1499 => 2`, `1500-2999 => 3`, `3000-4999 => 4`, `5000+ => 5`) from `TotalCoinsEarned`, increments the matching pillar score on approved task awards, and caps each pillar score at `20`.
- Streak freeze usage is limited to the logged-in child's own profile and decrements `StreakFreezesAvailable` only when at least one freeze is available. The earn-coins path awards a new freeze on each 10-day streak milestone up to the documented max of `2`.
- SQL script `022_CreateCoinTransactions.sql` creates `CoinTransactions` and the required child/date lookup index. SQL script `023_AlterChildProfiles_RowVersion.sql` adds the `RowVersion` column for concurrency control.
- Implementation inference recorded from the current schema: the Phase 10 plan introduces the 50% daily approval threshold and freeze behavior but does not add a dedicated daily streak-state table or last-evaluated date column. To stay inside the documented schema and current architecture, streak advancement is evaluated from the existing `TaskItems` and same-day `TaskCompletions` data when approvals are processed through `ICoinService`, rather than introducing new persistence outside the phase spec.

## Teacher Feedback - Phase 11 Teacher Feedback Submission

Affected module section: Teacher Feedback / Phase 11 Teacher Feedback Submission
What changed: Implemented the Phase 11 teacher-feedback submission module from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `TeacherFeedback` schema/API slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding feedback submission/list/detail/update/delete/summary APIs, feedback DTOs, validators, repository/configuration wiring, parent push notifications, and SQL scripts `024`-`025`.
Files impacted: `Backend/FamilyFirst.Domain/Entities/TeacherFeedback.cs`; `Backend/FamilyFirst.Application/DTOs/Feedback/FeedbackDto.cs`; `Backend/FamilyFirst.Application/DTOs/Feedback/SubmitFeedbackRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Feedback/UpdateFeedbackRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Feedback/FeedbackSummaryDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IFeedbackService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/FeedbackService.cs`; `Backend/FamilyFirst.Application/Validators/SubmitFeedbackRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/UpdateFeedbackRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/TeacherFeedbackConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/FeedbackRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/FeedbackController.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/024_CreateTeacherFeedback.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/025_CreateFeedbackIndexes.sql`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 11 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 11 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 11 implementation notes:
- Added `TeacherFeedback` as the Phase 11 domain model with the documented fields `FeedbackId`, `TeacherProfileId`, `ChildProfileId`, `FamilyId`, `SessionId`, `FeedbackType`, `Severity`, `Subject`, `Message`, `CommentTemplateId`, `WeeklySummaryJson`, acknowledgement fields, `ResolutionStatus`, computed `IsEditable`, and base soft-delete/timestamp fields.
- Added the four feedback DTO files required by the phase: submission request, update request, feedback response, and child feedback summary. The list endpoint returns `PaginatedList<FeedbackDto>` and the summary endpoint returns count-by-type totals for the requested child and period.
- Added `IFeedbackService`, `IFeedbackRepository`, `FeedbackService`, and `FeedbackRepository`, then wired the new module into `FamilyFirstDbContext` and infrastructure DI.
- Added `POST /api/v1/families/{familyId}/feedback`, `GET /api/v1/families/{familyId}/feedback`, `GET /api/v1/families/{familyId}/feedback/{feedbackId}`, `PUT /api/v1/families/{familyId}/feedback/{feedbackId}`, `DELETE /api/v1/families/{familyId}/feedback/{feedbackId}`, and `GET /api/v1/families/{familyId}/children/{childId}/feedback-summary` in `FeedbackController`.
- Submit validation enforces `Message` length `5-2000`, optional `Subject` max `300`, valid optional `SessionId`/`CommentTemplateId`, severity rules for `Complaint` and `UrgentEscalation`, and `WeeklySummaryJson` structure with the required fields `attendanceRate`, `homeworkRate`, `standoutMoment`, and `focusArea`.
- Teacher submission is restricted to children in the teacher's active `TeacherChildAssignments`. Parent listing is family-wide. Teacher listing/detail/edit/delete is restricted to that teacher's own feedback only.
- The 24-hour edit/delete rule is enforced in service using both the computed `IsEditable` projection and the `CreatedAt` timestamp window. Delete is implemented as soft delete through the existing base-entity fields.
- Parent notifications are sent inline through the existing `IPushNotificationService`. `UrgentEscalation` uses a dedicated urgent push title/body path, satisfying the phase rule that urgent escalations bypass later batching/quiet-hours handling.
- SQL script `024_CreateTeacherFeedback.sql` creates `TeacherFeedback` with the computed `IsEditable` column and the documented foreign keys/check constraints. SQL script `025_CreateFeedbackIndexes.sql` adds the required `FamilyId + ChildProfileId + FeedbackType` lookup index.
- Implementation inference recorded from a schema gap: the Phase 11 plan allows `Elder` users to submit `Appreciation`, but the documented `TeacherFeedback` table stores only `TeacherProfileId` and has no separate elder-author column. To stay inside the phase schema, elder appreciation submissions resolve to a `TeacherProfile` row linked to that elder's `FamilyMember`; if no such row exists yet, Phase 11 creates one on demand with `TeacherType = Other` so the feedback can be stored without altering the table design.

## Teacher Feedback - Phase 12 Feedback Acknowledgement & Parent Response Loop

Affected module section: Teacher Feedback / Phase 12 Feedback Acknowledgement & Parent Response Loop
What changed: Implemented the Phase 12 acknowledgement flow from `Source/FamilyFirst_L1_Codex_DevPlan.docx`, adding the acknowledgement request DTO/validator, the feedback acknowledge service/controller path, teacher FCM acknowledgement notifications, and dashboard unacknowledged-feedback count support without adding any new database scripts.
Files impacted: `Backend/FamilyFirst.Application/DTOs/Feedback/AcknowledgeRequest.cs`; `Backend/FamilyFirst.Application/Validators/AcknowledgeRequestValidator.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IFeedbackService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/FeedbackService.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/FeedbackRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/FeedbackController.cs`; `Backend/FamilyFirst.Application/DTOs/Family/FamilyDashboardDto.cs`; `Backend/FamilyFirst.Application/Services/Implementations/FamilyService.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 12 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 12 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 12 implementation notes:
- Added `AcknowledgeRequest` with optional `ParentResponseText`, plus `AcknowledgeRequestValidator` enforcing the documented max length of `1000`.
- Extended `IFeedbackService` and `FeedbackService` with `AcknowledgeFeedbackAsync`, and added `POST /api/v1/families/{familyId}/feedback/{feedbackId}/acknowledge` in `FeedbackController`.
- Acknowledgement is restricted to `Parent` and `FamilyAdmin` roles. The feedback must belong to the requested family; otherwise the existing family-scoped not-found path is used.
- The acknowledgement flow sets `IsAcknowledged = true`, `AcknowledgedAt = UTC now`, `AcknowledgedByUserId = current user`, `ParentResponseText`, and `ResolutionStatus = Acknowledged` exactly as required by the phase.
- A second acknowledgement is idempotent: the endpoint returns the existing `FeedbackDto` with no error and does not re-send the teacher notification.
- On first acknowledgement, `FeedbackService` sends an immediate push notification to the feedback author through the existing `IPushNotificationService`, using the linked `TeacherProfile.User` or `TeacherProfile.FamilyMember.User` FCM token.
- Extended `IFeedbackRepository`/`FeedbackRepository` with an unacknowledged-feedback count query and updated `FamilyDashboardDto` plus `FamilyService.GetDashboardAsync` to include `UnacknowledgedFeedbackCount` for the Phase 12 dashboard requirement.
- No new SQL scripts, no new feedback table columns, no resolution states beyond `Acknowledged`, and no escalation-tracking logic were introduced in this phase.

## Rewards Catalog - Phase 13 Rewards Catalog (System & Family)

Affected module section: Rewards Catalog / Phase 13 Rewards Catalog (System & Family)
What changed: Implemented the Phase 13 rewards-catalog module from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `Rewards` schema/API slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding the reward domain model, reward DTOs, service/repository/configuration wiring, admin catalog endpoints, family reward management endpoints, and SQL scripts `026`-`027`.
Files impacted: `Backend/FamilyFirst.Domain/Entities/Reward.cs`; `Backend/FamilyFirst.Application/DTOs/Reward/CreateRewardRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Reward/UpdateRewardRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Reward/RewardDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IRewardService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/RewardService.cs`; `Backend/FamilyFirst.Application/Validators/CreateRewardRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/UpdateRewardRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/RewardConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/RewardRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/RewardsController.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/026_CreateRewards.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/027_SeedSystemRewards.sql`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 13 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 13 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 13 implementation notes:
- Added `Reward` as the Phase 13 domain model with the documented fields `RewardId`, `FamilyId`, `MasterRewardId`, `RewardName`, `Description`, `IconCode`, `Category`, `CoinCost`, `IsSystem`, `IsEnabled`, and `TimesRedeemedTotal`, using the existing base-entity timestamp/soft-delete pattern.
- Added the three reward DTO files required by the phase: `CreateRewardRequest`, `UpdateRewardRequest`, and `RewardDto`.
- Added `IRewardService`, `IRewardRepository`, `RewardService`, and `RewardRepository`, then wired the module into `FamilyFirstDbContext` and infrastructure DI.
- Added `GET /api/v1/admin/rewards/catalog`, `POST /api/v1/admin/rewards/catalog`, `PUT /api/v1/admin/rewards/catalog/{rewardId}`, `GET /api/v1/families/{familyId}/rewards`, `POST /api/v1/families/{familyId}/rewards`, and `PUT /api/v1/families/{familyId}/rewards/{rewardId}` in `RewardsController`.
- SuperAdmin catalog operations are restricted by role-claim checks, while family create/update operations require `Parent` or `FamilyAdmin`. Child access is limited to `GET /families/{familyId}/rewards`.
- Reward validation enforces `RewardName` length, optional `Description` max `500`, optional `IconCode` max `50`, allowed categories `ScreenTime|FoodTreat|Outing|Purchase|FamilyActivity`, and `CoinCost` range `10-9999`.
- Family enabling of a system reward is implemented by cloning the selected system row into a family-scoped copy with `FamilyId` set and `MasterRewardId` pointing to the system reward, matching the phase rule that system rewards stay read-only from family scope.
- SQL script `026_CreateRewards.sql` creates `Rewards`, its category/cost constraints, the self-reference for `MasterRewardId`, and the family enabled-index. SQL script `027_SeedSystemRewards.sql` seeds 10 system rewards across the 5 required categories.
- The seeded defaults are implementation-defined because Phase 13 specifies only category coverage, not exact names. The seed set added in this phase is: `Extra 15 Minutes Screen Time`, `Choose Movie Night`, `Ice Cream Treat`, `Favorite Snack Pick`, `Park Visit Choice`, `Mini Outing Pick`, `Small Toy Purchase`, `Book of Choice`, `Choose Family Game Night`, and `Pick Weekend Activity`.
- Implementation inference recorded from an API gap: the plan requires family users to enable system rewards but does not define a separate family-visible system catalog endpoint. To keep the implementation inside the documented endpoint set, `GET /families/{familyId}/rewards` returns family reward rows plus system reward templates that do not yet have a family copy for `Parent`/`FamilyAdmin`, while `Child` role receives enabled family reward rows only.

## Rewards Redemption - Phase 14 Reward Redemption Lifecycle

Affected module section: Rewards Redemption / Phase 14 Reward Redemption Lifecycle
What changed: Implemented the Phase 14 reward-redemption flow from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `RewardRedemptions` schema/API slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding the redemption entity, redemption DTOs, review validator, repository/configuration wiring, redemption endpoints, and SQL script `028`.
Files impacted: `Backend/FamilyFirst.Domain/Entities/RewardRedemption.cs`; `Backend/FamilyFirst.Application/DTOs/Reward/RedemptionDto.cs`; `Backend/FamilyFirst.Application/DTOs/Reward/ReviewRedemptionRequest.cs`; `Backend/FamilyFirst.Application/Validators/ReviewRedemptionRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/RewardRedemptionConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/RewardRedemptionRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/028_CreateRewardRedemptions.sql`; `Backend/FamilyFirst.Application/Services/Interfaces/IRewardService.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ICoinService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/RewardService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/CoinService.cs`; `Backend/FamilyFirst.API/Controllers/v1/RewardsController.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 14 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 14 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 14 implementation notes:
- Added `RewardRedemption` as the Phase 14 domain model with the documented fields `RedemptionId`, `RewardId`, `ChildProfileId`, `FamilyId`, `CoinsSpent`, `Status`, `RequestedAt`, `ReviewedByUserId`, `ReviewedAt`, and `ParentNote`, using the existing base-entity timestamps/soft-delete fields for consistency with the current architecture.
- Added `RedemptionDto` and `ReviewRedemptionRequest`, and colocated `RedeemRequest` in `RedemptionDto.cs` to keep the request/response surface within the phase file count while still matching the documented redeem endpoint contract.
- Added `ReviewRedemptionRequestValidator` enforcing `Approved|Rejected` only and parent-note max length `500`.
- Added `IRewardRedemptionRepository` and `RewardRedemptionRepository`, then wired `RewardRedemptionConfiguration` and `DbSet<RewardRedemption>` into the infrastructure layer.
- Extended `IRewardService`/`RewardService` with `RedeemAsync`, `ListRedemptionsAsync`, and `ReviewRedemptionAsync`, and extended `RewardsController` with `POST /api/v1/families/{familyId}/rewards/{rewardId}/redeem`, `GET /api/v1/families/{familyId}/rewards/redemptions`, and `PUT /api/v1/families/{familyId}/rewards/redemptions/{redemptionId}`.
- Child redemption is restricted to the logged-in child's own `childProfileId`. Redeem validates enabled family reward access, current coin balance, and duplicate pending redemptions for the same `(ChildProfileId, RewardId)` pair.
- Approval re-validates balance through the new `ICoinService.SpendCoinsForRewardRedemptionAsync` path, increments `Reward.TimesRedeemedTotal`, and creates a `CoinTransaction` with `TransactionType = Spent` and `ReferenceType = RewardRedemption`.
- Approval-side reward, redemption, child-balance, and coin-transaction writes are applied together through `RewardRedemptionRepository.ApplyApprovalAsync` in one database transaction. Concurrency conflicts on the child profile row-version surface as a conflict response from the coin service.
- Approval sends a child push notification for the approved reward; rejection leaves coin balance unchanged, persists `ParentNote`, and sends a rejection push to the child.
- SQL script `028_CreateRewardRedemptions.sql` creates `RewardRedemptions`, adds the family/status lookup index, and adds a filtered unique pending-redemption index to enforce the no-duplicate-pending rule at the database level.
- Implementation inference recorded from the current catalog shape: Phase 14 redemption requests target the family-scoped reward row returned by Phase 13, including family-enabled system copies. This keeps `CoinsSpent` snapped from the family's effective configured coin cost rather than the system master reward.

## Family Calendar - Phase 15 Family Calendar Events & CRUD

Affected module section: Family Calendar / Phase 15 Family Calendar Events & CRUD
What changed: Implemented the Phase 15 family-calendar module from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `CalendarEvents`, `EventReminders`, and calendar API slices from `Source/FamilyFirst_L1_TechSpec.docx`, adding the calendar/event entities, DTOs, validators, service/repository/configuration stack, controller endpoints, and SQL scripts `029`-`031`.
Files impacted: `Backend/FamilyFirst.Domain/Entities/CalendarEvent.cs`; `Backend/FamilyFirst.Domain/Entities/EventReminder.cs`; `Backend/FamilyFirst.Application/DTOs/Calendar/EventDto.cs`; `Backend/FamilyFirst.Application/DTOs/Calendar/EventReminderDto.cs`; `Backend/FamilyFirst.Application/DTOs/Calendar/CreateEventRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Calendar/UpdateEventRequest.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ICalendarService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/CalendarService.cs`; `Backend/FamilyFirst.Application/Validators/CreateEventRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/UpdateEventRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/CalendarEventConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/EventReminderConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/CalendarEventRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/EventReminderRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/CalendarController.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/029_CreateCalendarEvents.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/030_CreateEventReminders.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/031_CreateCalendarIndexes.sql`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 15 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 15 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 15 implementation notes:
- Added `CalendarEvent` and `EventReminder` as the Phase 15 domain models with the documented fields, using the current base-entity timestamp/soft-delete pattern so the new calendar tables align with the existing architecture.
- Added the four calendar DTO files required by the phase: `EventDto`, `EventReminderDto`, `CreateEventRequest`, and `UpdateEventRequest`. `EventReminderRequest` is colocated with `EventReminderDto` to keep the DTO file count inside the documented Phase 15 scope.
- Added `ICalendarService`, `ICalendarEventRepository`, `IEventReminderRepository`, `CalendarService`, `CalendarEventRepository`, and `EventReminderRepository`, then wired the module into `FamilyFirstDbContext` and infrastructure DI.
- Added `GET /api/v1/families/{familyId}/calendar/events`, `POST /api/v1/families/{familyId}/calendar/events`, `GET /api/v1/families/{familyId}/calendar/events/{eventId}`, `PUT /api/v1/families/{familyId}/calendar/events/{eventId}`, `DELETE /api/v1/families/{familyId}/calendar/events/{eventId}`, and `GET /api/v1/families/{familyId}/calendar/upcoming` in `CalendarController`.
- Create permissions are restricted to `Parent`, `FamilyAdmin`, and `Teacher`. Update/delete permissions are restricted to the event creator or a `FamilyAdmin`, matching the phase rule.
- Validation enforces title length `2-300`, description max `1000`, location max `300`, `StartDateTime` not more than one year in the past, `EndDateTime >= StartDateTime`, `RecurrenceRule` max `200` with RRULE-style structure when recurring, max `5` reminders per event, and reminder minutes limited to `5, 10, 15, 30, 60, 120, 480, 1440, 4320`.
- Reminder rows are created from the event's `StartDateTime` and stored as `EventReminder` rows with `IsSent = false`. Update replaces active reminder rows for the event, and delete soft-deletes the event plus its active reminder rows together.
- SQL script `029_CreateCalendarEvents.sql` creates `CalendarEvents` with the documented fields and scope/date constraints. SQL script `030_CreateEventReminders.sql` creates `EventReminders` with the scheduled-for column. SQL script `031_CreateCalendarIndexes.sql` adds the documented family/start-date index.
- Implementation inference recorded from a contract gap: the phase describes child views as "own tasks/classes/birthdays", but the documented `EventType` enum does not include a task-specific calendar type. The child role filter is therefore implemented from the documented visibility and child-link fields: `VisibilityScope` must be `Family` or `Child`, and any `LinkedChildProfileId` must match the current child profile. This avoids inventing an undocumented event type while keeping child-visible events tied to the documented model.

## Event Reminders - Phase 16 Event Reminders & Notification Scheduling

Affected module section: Event Reminders / Phase 16 Event Reminders & Notification Scheduling
What changed: Implemented the Phase 16 reminder-delivery and notification-preference slice from `Source/FamilyFirst_L1_Codex_DevPlan.docx` and the `NotificationPreferences` schema from `Source/FamilyFirst_L1_TechSpec.docx`, adding notification-preference persistence, reminder and birthday hosted workers, the notification-preferences API endpoints, and the FCM HTTP v1 push service update.
Files impacted: `Backend/FamilyFirst.Domain/Entities/NotificationPreference.cs`; `Backend/FamilyFirst.Application/DTOs/Notification/NotificationPreferenceDto.cs`; `Backend/FamilyFirst.Application/DTOs/Notification/UpdatePreferencesRequest.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/INotificationPreferenceService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/NotificationPreferenceService.cs`; `Backend/FamilyFirst.Application/Validators/UpdatePreferencesRequestValidator.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/NotificationPreferenceConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/NotificationPreferenceRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/BackgroundServices/ReminderDeliveryWorker.cs`; `Backend/FamilyFirst.Infrastructure/Data/BackgroundServices/BirthdayEventGeneratorWorker.cs`; `Backend/FamilyFirst.API/Controllers/v1/NotificationsController.cs`; `Backend/FamilyFirst.Infrastructure/Services/FcmPushNotificationService.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/032_CreateNotificationPreferences.sql`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/EventReminderRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/CalendarEventRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/ChildProfileRepository.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IAttendanceService.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IChildService.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/ICalendarService.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Infrastructure/FamilyFirst.Infrastructure.csproj`; `Backend/FamilyFirst.API/Program.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 16 only, under strict phase-only development constraints and without testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 16 implemented in source only. No build, test run, SQL execution, runtime validation, or debugging was performed because the task scope explicitly prohibited them.

Phase 16 implementation notes:
- Added `NotificationPreference` persistence with the documented alert toggles, quiet-hours fields, digest times, and `GET`/`PUT /api/v1/users/{userId}/notification-preferences`.
- Added `ReminderDeliveryWorker` as a hosted background service that polls due `EventReminders` every 5 minutes, evaluates quiet hours, retries failed FCM sends up to 3 times with 1-minute backoff, and marks delivered reminders as sent.
- Added `BirthdayEventGeneratorWorker` as a hosted background service that runs on the daily UTC boundary and creates birthday `CalendarEvent` rows 7 days ahead when a matching birthday event does not already exist.
- Extended the existing calendar and child-profile repositories with the Phase 16 query paths required for due-reminder polling and birthday-event generation instead of introducing Phase 17 notification-table behavior early.
- Updated `FcmPushNotificationService` from the legacy server-key call shape to Firebase HTTP v1 credentials flow, and added deep-link data payload support for calendar reminder pushes.
- SQL script `032_CreateNotificationPreferences.sql` creates `NotificationPreferences` with the documented defaults and the unique `UserId` index.
- Implementation inference recorded from a Phase 16 storage gap: `EventReminders` are event-scoped and Phase 17's recipient-level `Notifications` table does not exist yet, so reminder delivery targets the event creator's FCM token and notification-preference row in Phase 16. This keeps quiet-hours scheduling and sent-state coherent without inventing per-recipient reminder records before Phase 17.
- Implementation inference recorded from a priority gap: the calendar schema has no priority field in Phase 16, so "urgent reminders bypass quiet hours" is applied to `EventType = MedicineReminder`, which is the only documented event type that clearly requires exact-time delivery in the current schema.

## Reports & Insights - Phase 18 Reports & Weekly Digest

Affected module section: Reports & Insights / Phase 18 Reports & Weekly Digest
What changed: Implemented Phase 18 reports with report DTOs, `IReportService`, `ReportService`, `ReportsController`, `WeeklyDigestWorker`, Phase 18 DI/hosted-service wiring, and weekly-digest notification preference handling only.
Files impacted: `Backend/FamilyFirst.Application/DTOs/Reports/WeeklyDigestDto.cs`; `Backend/FamilyFirst.Application/DTOs/Reports/ChildWeeklyReportDto.cs`; `Backend/FamilyFirst.Application/DTOs/Reports/AttendanceSummaryDto.cs`; `Backend/FamilyFirst.Application/DTOs/Reports/FeedbackSummaryDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IReportService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/ReportService.cs`; `Backend/FamilyFirst.API/Controllers/v1/ReportsController.cs`; `Backend/FamilyFirst.Infrastructure/Data/BackgroundServices/WeeklyDigestWorker.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.API/Program.cs`; `Backend/FamilyFirst.Application/Services/Implementations/NotificationService.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 18 only, under strict phase-only development constraints with no testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 18 source implementation completed. No build, no SQL execution, no tests, no runtime validation, and no debugging were performed because the task scope explicitly forbids them.

Phase 18 implementation notes:
- Added the `Reports` DTO folder with the four planned files: `WeeklyDigestDto`, `ChildWeeklyReportDto`, `AttendanceSummaryDto`, and `FeedbackSummaryDto`. Nested digest child/event, pillar-score, and attendance-heatmap records were kept inside those same four files to preserve the phase file list.
- Added `IReportService` and `ReportService` with the three Phase 18 aggregation surfaces: weekly digest, child weekly report, and attendance summary. A worker-only `GenerateWeeklyDigestAsync` method was also added so the scheduled digest can reuse the same aggregation logic without depending on a user JWT context.
- Added `ReportsController` endpoints for `GET /api/v1/families/{familyId}/reports/weekly-digest`, `GET /api/v1/families/{familyId}/children/{childId}/reports/weekly`, and `GET /api/v1/families/{familyId}/children/{childId}/reports/attendance-summary`.
- Weekly digest authorization is limited to `Parent` and `FamilyAdmin`. Child weekly report and attendance summary are limited to `Parent`, matching the phase plan.
- `weekStartDate` defaults to the most recent Monday when omitted and must be a Monday when provided. Attendance summary accepts `fromDate` / `toDate`; when both are omitted it defaults to the current Monday-Sunday range.
- Weekly digest aggregates existing Phase 04-17 data only: per-child attendance rate, per-child task rate, weekly feedback count, and upcoming 7-day calendar events. No new report table or SQL script was introduced because the phase plan explicitly says no DB script is required.
- Child weekly report includes attendance rate, task rate, feedback counts by type, latest parent remark, and the current five pillar scores from `ChildProfile`.
- Attendance summary returns total sessions, present/absent/late/left-early counts, attendance rate percentage, and a day-by-day heatmap array. When multiple attendance records exist on the same date, the heatmap uses the most severe status for that day in the order `Absent`, `Late`, `LeftEarly`, `Present`.
- Added `WeeklyDigestWorker` as a hosted background service that schedules the digest for Sunday 19:00 UTC and creates per-recipient weekly digest notifications for active `Parent` and `FamilyAdmin` family members by reusing the existing notification pipeline.
- Wired the previously unused `NotificationPreference.WeeklyDigest` flag into `NotificationService` so Phase 18 digest pushes respect user weekly-digest preferences.
- Because the current schema has no dedicated family-digest history table and Phase 18 explicitly disallows new DB scripts, historical weekly digest retrieval is generated from existing source-of-truth tables for the requested `weekStartDate`, while digest delivery history is persisted through the existing per-user `Notifications` table.
- `WeeklyDigestDto.FamilyScoreTrend` is implemented as `Up`, `Down`, or `Flat` by comparing the current week's combined family attendance/task performance against the prior week. This is an explicit implementation inference required because the current schema has a current `FamilyScore` but no weekly family-score history table.

## Admin Configuration - Phase 19 Super Admin Panel & Analytics

Affected module section: Admin Configuration / Phase 19 Super Admin Panel & Analytics
What changed: Implemented Phase 19 admin DTOs, `IAdminService`, `AdminService`, `IAdminRepository`, `AdminRepository`, `AdminController`, `FeatureFlag` entity/configuration, SQL scripts `035`-`036`, SuperAdmin authorization policy wiring, and maintenance-mode middleware only.
Files impacted: `Backend/FamilyFirst.Domain/Entities/FeatureFlag.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/AdminDashboardDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/AdminFamilySummaryDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/AdminFamilyDetailDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/UpdateFamilySubscriptionRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/AdminPlanDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/UpdatePlanRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/AnalyticsOverviewDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/FeatureFlagDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IAdminService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/AdminService.cs`; `Backend/FamilyFirst.Application/Validators/UpdateFamilySubscriptionRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/AdminRequestValidators.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/FeatureFlagConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/AdminRepository.cs`; `Backend/FamilyFirst.API/Controllers/v1/AdminController.cs`; `Backend/FamilyFirst.API/Middleware/MaintenanceModeMiddleware.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/035_CreateFeatureFlags.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/036_SeedFeatureFlags.sql`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.API/Program.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 19 only, under strict phase-only development constraints with no testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 19 source implementation completed. No build, no SQL execution, no tests, no runtime validation, and no debugging were performed because the task scope explicitly forbids them.

Phase 19 implementation notes:
- Added the `Admin` DTO folder with eight files to cover the documented Phase 19 surfaces: KPI dashboard, family search/detail, subscription update, plan list/update, analytics overview, feature flag list/update, and notification campaign request/result contracts.
- Added `IAdminService`/`AdminService` and `IAdminRepository`/`AdminRepository` for the full Phase 19 API surface:
  - `GET /api/v1/admin/dashboard`
  - `GET /api/v1/admin/families`
  - `GET /api/v1/admin/families/{familyId}`
  - `PUT /api/v1/admin/families/{familyId}/subscription`
  - `DELETE /api/v1/admin/families/{familyId}`
  - `GET /api/v1/admin/plans`
  - `PUT /api/v1/admin/plans/{planId}`
  - `GET /api/v1/admin/analytics/overview`
  - `GET /api/v1/admin/feature-flags`
  - `PUT /api/v1/admin/feature-flags/{flag}`
  - `POST /api/v1/admin/notifications/campaign`
- Added the `FeatureFlags` table through raw SQL scripts `035_CreateFeatureFlags.sql` and `036_SeedFeatureFlags.sql`, plus the matching `FeatureFlag` entity, EF configuration, and `DbSet`.
- Added a `SuperAdmin` authorization policy in `Program.cs` and applied it at controller level on `AdminController`.
- Added `MaintenanceModeMiddleware` and inserted it after authentication so it can inspect the authenticated role claim before returning `503` for non-admin traffic when the `MaintenanceMode` feature flag is enabled.
- Block family behavior is implemented as `Family.IsActive = false` plus deactivation of that family's `FamilyMembers.IsActive` rows. This is the closest source-backed implementation available for the plan rule that blocked family members cannot log in, without inventing a new auth storage model or cross-phase account-ban table.
- Subscription management supports plan changes and trial extensions. When `ExtendTrialDays` is provided, `TrialEndDate` is extended and `Subscription.Status` is forced to `Trial`, matching the phase rule.
- Plan management updates the existing `Plans` table only; no plan-create endpoint or new billing table was introduced because Phase 19 documents only list/update behavior.
- Feature flags are implemented as string-backed key/value records so the same table can support boolean flags like `MaintenanceMode` and scalar values like `MinimumAppVersion`. This is an explicit implementation inference from the phase rule that mentions both toggle-style and version-style flags while requiring a single key-value store table.
- Notification campaigns are implemented by querying recipient user IDs by family-member role and family plan code, then creating `Notifications` rows through the existing notification service so the already-implemented delivery worker handles sending.
- Analytics overview is implemented as platform count queries across existing core tables (`Users`, `ChildProfiles`, `TeacherProfiles`, `TaskItems`, `TaskCompletions`, `TeacherFeedback`, `Notifications`), matching the plan's "count queries" wording without introducing out-of-scope charting or time-series analytics.
- To allow maintenance-mode administration access, `MaintenanceModeMiddleware` bypasses `/api/v1/admin` routes and `/api/v1/auth` routes. The `/api/v1/auth` bypass is an explicit implementation inference so SuperAdmin sign-in is still possible while maintenance mode is active.

## Admin Configuration - Phase 20 Family Admin Configuration & Final Integration

Affected module section: Admin Configuration / Phase 20 Family Admin Configuration & Final Integration
What changed: Implemented Phase 20 family-admin DTOs, `IFamilyAdminService`, `FamilyAdminService`, Phase 20 validators, `IFamilyAdminConfigRepository`, `FamilyAdminConfigRepository`, `FamilyAdminController`, module-visibility enforcement filter, SQL scripts `037`-`040`, family admin attendance-status exposure, notification-rule integration, final hosted-service registration, and Phase 20 DI wiring only.
Files impacted: `Backend/FamilyFirst.Domain/Entities/ModuleVisibilityConfig.cs`; `Backend/FamilyFirst.Domain/Entities/NotificationRule.cs`; `Backend/FamilyFirst.Domain/Entities/CustomAttendanceStatus.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/FamilyAdminPanelDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/ModuleVisibilityDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/UpdateModuleVisibilityRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/NotificationRuleDto.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/UpdateNotificationRuleRequest.cs`; `Backend/FamilyFirst.Application/DTOs/Admin/CustomAttendanceStatusDto.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/IFamilyAdminService.cs`; `Backend/FamilyFirst.Application/Services/Interfaces/INotificationService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/FamilyAdminService.cs`; `Backend/FamilyFirst.Application/Services/Implementations/NotificationService.cs`; `Backend/FamilyFirst.Application/Validators/UpdateModuleVisibilityRequestValidator.cs`; `Backend/FamilyFirst.Application/Validators/FamilyAdminConfigRequestValidators.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/ModuleVisibilityConfigConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/NotificationRuleConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Configurations/CustomAttendanceStatusConfiguration.cs`; `Backend/FamilyFirst.Infrastructure/Data/Repositories/Implementations/FamilyAdminConfigRepository.cs`; `Backend/FamilyFirst.Infrastructure/Data/FamilyFirstDbContext.cs`; `Backend/FamilyFirst.Infrastructure/DependencyInjection.cs`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/037_CreateModuleVisibilityConfig.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/038_CreateNotificationRules.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/039_CreateCustomAttendanceStatuses.sql`; `Backend/FamilyFirst.Infrastructure/Data/Scripts/040_SeedDefaultModuleVisibility.sql`; `Backend/FamilyFirst.API/Controllers/v1/FamilyAdminController.cs`; `Backend/FamilyFirst.API/Controllers/v1/AttendanceController.cs`; `Backend/FamilyFirst.API/Filters/FamilyModuleVisibilityFilter.cs`; `Backend/FamilyFirst.API/Program.cs`; `Source/ProjectOverview.txt`; `Source/ModuleIndex.txt`.
Why changed: User requested execution of Phase 20 only, under strict phase-only development constraints with no testing, execution, validation, or debugging.
Date: 2026-04-23
Canonical status: Phase 20 source implementation completed. No build, no SQL execution, no tests, no runtime validation, and no debugging were performed because the task scope explicitly forbids them.

Phase 20 implementation notes:
- Added the planned Phase 20 `Admin` DTO files for the family-admin panel, module visibility, notification rules, and custom attendance status management, keeping the phase contracts inside the documented DTO folder already introduced by Phase 19.
- Added `IFamilyAdminService`/`FamilyAdminService` and `IFamilyAdminConfigRepository`/`FamilyAdminConfigRepository` for the full Phase 20 API surface:
  - `GET /api/v1/families/{familyId}/admin/panel`
  - `GET /api/v1/families/{familyId}/admin/module-visibility`
  - `PUT /api/v1/families/{familyId}/admin/module-visibility`
  - `GET /api/v1/families/{familyId}/admin/notification-rules`
  - `PUT /api/v1/families/{familyId}/admin/notification-rules/{ruleId}`
  - `GET /api/v1/families/{familyId}/admin/attendance-statuses`
  - `POST /api/v1/families/{familyId}/admin/attendance-statuses`
  - `DELETE /api/v1/families/{familyId}/admin/attendance-statuses/{statusId}`
- Added SQL scripts `037_CreateModuleVisibilityConfig.sql`, `038_CreateNotificationRules.sql`, `039_CreateCustomAttendanceStatuses.sql`, and `040_SeedDefaultModuleVisibility.sql`, plus the matching EF entities/configurations and `DbSet` registrations.
- `ModuleVisibilityConfig` is implemented as a combined template-and-override store. Script `040` seeds the default visibility matrix with `FamilyId = NULL`, and family-specific overrides are stored in the same table with a concrete `FamilyId`. This is an explicit implementation inference required because the phase plan calls for seeded defaults plus per-family visibility storage while introducing only one visibility table.
- Added `FamilyModuleVisibilityFilter` as a central enforcement layer for family-scoped API modules. It reads `familyId` from route values, maps controller names to module names, allows `SuperAdmin` and `FamilyAdmin` to pass through, skips non-family routes, and blocks hidden modules for the current role using family-specific rows first and seeded default rows second.
- The controller-to-module mapping currently covers `Families`, `Children`, `Attendance`, `CommentTemplates`, `Tasks`, `Rewards`, `Feedback`, `Calendar`, `Reports`, `Notifications`, and `FamilyAdmin`, which aligns Phase 20 visibility control with the existing phase-built API surface without changing unrelated authorization paths.
- `FamilyAdminService` enforces the documented permission ceiling by rejecting module-visibility changes for `SuperAdmin` or any role above `FamilyAdmin` using a role-level map derived from the existing `UserRole` enum.
- All Phase 20 family-admin configuration mutations write to the existing `AuditLogs` table through `IAuditLogRepository`, covering module visibility updates, notification rule updates, attendance-status creation, and attendance-status deletion.
- Notification rules are implemented as per-family records keyed by `RuleKey`. Missing default rules are materialized on first family-admin read for the default keys `Attendance`, `Feedback`, `Task`, `Reward`, `Calendar`, and `WeeklyDigest`, so later update calls operate on persisted `RuleId` values instead of invented virtual IDs.
- `NotificationService` now resolves per-family notification-rule overrides by `FamilyId` plus `ReferenceType`, then applies `IsEnabled`, `PriorityOverride`, and `DeliveryDelayMinutes` centrally during notification creation. Delivery metadata is applied first and rule overrides second so the configured delay/priority override is not overwritten by batching defaults.
- Custom attendance statuses are implemented as family-level configuration records with a hard limit of 5 custom rows per family. The four default statuses `Present`, `Absent`, `Late`, and `LeftEarly` are returned virtually and cannot be deleted because they are not persisted in `CustomAttendanceStatuses`.
- Added `GET /api/v1/families/{familyId}/attendance/statuses` to `AttendanceController` so attendance status configuration is available from the attendance module as required by the phase done criteria. This endpoint permits any active family member, while create/delete remains limited to `FamilyAdmin`.
- Existing `AttendanceRecord.Status` remains enum-backed to the original default `AttendanceStatus` values. Phase 20 did not introduce an `AttendanceRecords` schema change, so custom statuses are exposed as configurable family metadata and attendance-module lookup data only. This boundary is an explicit implementation inference to stay inside the documented phase scope without inventing a cross-table status migration.
- Final integration wiring in `Program.cs` now registers all background services already present in `Infrastructure/Data/BackgroundServices`: `ReminderDeliveryWorker`, `BirthdayEventGeneratorWorker`, `NotificationDeliveryWorker`, `MorningDigestWorker`, `EveningDigestWorker`, and `WeeklyDigestWorker`.
