# FamilyFirst — Full Project Audit & Phase-wise Codex Development Plan

**Generated:** 2026-11-20
**Scope:** Level 1 and Level 2 combined: Backend (`API/`), Mobile PWA (`Mobile/`), SQL scripts
**Sources:** `CLAUDE.md`, `API/Docs/Flow/ModuleIndex.md`, `API/Docs/Flow/ProjectOverview.md` (§12–§23), `API/Docs/Task/*`, and a read-only source audit of `API/` and `Mobile/src`
**Decisions confirmed by the founder:**
- Offline: only the Emergency Folder and Emergency Card. No full-app offline.
- Subscriptions: stay hidden behind `subscriptionEnabled=false`. Parked.
- Every phase has a copy-paste Codex prompt.

> **How to use this file**
> 1. Run phases **in order**. Each phase is one Codex run. Do not combine phases.
> 2. Copy the phase's **Codex Prompt** block into Codex as-is.
> 3. Don't start the next phase until every box in **Done Criteria** is ticked.
> 4. After each phase, Codex must update `ProjectOverview.md` as listed under **Docs to update**.
> 5. Anything marked `[VERIFY]` was not confirmable from source. Check it before you build it.

---

## PART A — AUDIT FINDINGS

### A.1 Module status summary

| # | Module | Backend | Mobile | DB | Overall | Main gap |
|---|---|---|---|---|---|---|
| 1 | Authentication | ✅ | ✅ | ✅ | ⚠️ | OTP is stored in memory; OTP is logged in plain text when no key is configured |
| 2 | Family & User | ✅ | ⚠️ | ✅ | ⚠️ | GetMasters cleanup; no visual check yet |
| 3 | Family Dashboard | ✅ | ⚠️ | ✅ | ⚠️ | Broken link `/parent/child/:id` |
| 4 | Attendance | ✅ | ⚠️ | ✅ | ⚠️ | Demo lookup arrays; no visual check yet |
| 5 | Tasks & Routines | ✅ | ⚠️ | ✅ | ⚠️ | Screens don't use the live TaskType/TaskStatus lists yet |
| 6 | Teacher Feedback | ⚠️ | ⚠️ | ✅ | ⚠️ | 500 error from an EF query on an ignored property |
| 7 | Rewards & Coins | ⚠️ | ⚠️ | ✅ | ⚠️ | 500 error from an EF query on an ignored property; 2 hardcoded strings |
| 8 | Family Calendar | ✅ | ⚠️ | ✅ | ⚠️ | GetMasters cleanup |
| 9 | Notifications | ❌ (workers fail) | ⚠️ | ✅ | ⚠️ | `PurgeOlderThanAsync` 500 error breaks 3 workers; FCM never sends |
| 10 | Admin Configuration | ⚠️ | ⚠️ | ✅ | ⚠️ | 500 errors from EF queries; Analytics, Support and Content screens use hardcoded data |
| 11 | Document Vault (L2) | ⚠️ | ⚠️ | ⚠️ | ⚠️ | No permission check; mapping bug; no offline; share link requires login |
| 12 | Medical (L2) | ❌ standards | ⚠️ | ⚠️ | ⚠️ | None of the 4 cross-cutting services; Emergency Card share bug; no offline |
| 13 | Safety (L2) | ❌ standards | ⚠️ | ⚠️ | ⚠️ | None of the 4 cross-cutting services; hardcoded lookup lists |
| 14 | Finance (L2) | ❌ standards | ⚠️ | ⚠️ | ⚠️ | None of the 4 cross-cutting services; deferred workers |
| 15 | Reports (L2) | ❌ standards | ⚠️ | ⚠️ | ⚠️ | PDF export throws; hardcoded child id `'c1'` |
| 16 | Advanced Admin (L2) | ⚠️ | ⚠️ | ⚠️ | ⚠️ | AC-03, AC-05, AC-07, Escalation and AC-09 deferred |

Legend: ✅ done · ⚠️ partial · ❌ broken or not started. **No module is yet COMPLETE.** Per CLAUDE.md §5a, none has been visually verified.

### A.2 Stale documentation (fix in docs, not code)

| # | Item | Truth (source-verified) |
|---|---|---|
| D1 | ProjectOverview §19.8 is marked "BLOCKING — backend not updated" | **Stale.** `FamilyFirst.Domain/Entities/Base/BaseEntity.cs` already has `long InternalId`, `Guid Id`, CompanyId/SiteId/CreatedBy/DateCreated/UpdatedBy/LastUpdated/DeletedBy/DateDeleted/IsDeleted. All 45 EF configurations use `ToTable("tbl…")` + `HasKey(InternalId)` via `Configurations/BaseEntityConfiguration.cs`. All 128 FK properties are `long`. Items 19.8.1–19.8.6 are done; 19.8.7 (move to stored procedures) is parked. |
| D2 | `Mobile/LIVE_API_INTEGRATION_CHECKLIST.md` | Lists made-up endpoints (`/api/parent/dashboard`, `/api/teacher/classes`…) that don't match `src/core/api/MasterApiReference.ts`. Rewrite in P13. |
| D3 | `API/Docs/Task/AllUIScreens.md` marks modules "Done (Unverified)" | Correct label. None can move to Done until the P13 visual check. |
| D4 | Script 087 `uspGetMasterDataSpNameByCode` (planned in Flow_Change_DB_GapAnalysis §5.3) | Never created, and no C# code calls it. Mark **NOT REQUIRED**. Scripts run 001–104 with only 087 missing. |

### A.3 Backend findings

**CRITICAL: runtime 500 errors.** EF LINQ uses ignored alias properties (§23.2 pattern: use `DateCreated`/`LastUpdated`/`DateDeleted`). All paths are under `API/FamilyFirst.Infrastructure/Data/Repositories/Implementations/`:

| File | Line(s) | Use |
|---|---|---|
| AdminRepository.cs | 36, 48, 226 | CountAsync predicate, OrderBy, `.Select` (`UpdatedAt ?? CreatedAt`) |
| ChildProfileRepository.cs | 32 | query |
| CoinTransactionRepository.cs | 23 | query |
| FamilyAdminConfigRepository.cs | 214–215, 267–268 | query |
| FeedbackRepository.cs | 55, 69–70 | query |
| MedicalRepository.cs | 172 | ThenByDescending |
| NotificationRepository.cs | 51, 66, **119** | 119 = `PurgeOlderThanAsync`, called by Morning, Evening and NotificationDelivery workers, so they fail every run |
| VaultDocumentRepository.cs | 52, 55, 69 | date filters + default sort |

These lines are **safe** and must not be touched: AdminRepository 78/110, EventReminderRepository 51/72/82, FamilyAdminConfigRepository 182, FinanceRepository 99/148/234, Morning/EveningDigestWorker 60. They run in memory or set values on tracked entities.

**Other backend defects**

| # | Finding | Location |
|---|---|---|
| B1 | `DocumentId { get => …; set { } }` is not ignored, so EF likely maps a missing column | `FamilyFirst.Domain/Entities/…/VaultExpiryReminderLog.cs:9` vs `044_CreateVaultExpiryReminderLogs.sql` |
| B2 | `ExportReportAsync` throws `NotImplementedException`, so it returns a 500. QuestPDF is not referenced. | `FamilyFirst.Application/Services/Implementations/ReportService.cs:791`, `ReportsController.cs:120` |
| B3 | `tblReportExport` has no entity, configuration or DbSet | `Data/Scripts/058_CreateReportExports.sql` |
| B4 | Finance, Medical, Safety and Report services inject **none** of IApiLogService, IPermissionService, IErrorCodeService, IMasterDataResolver | Finance: 10 hardcoded strings + 6 bare `ForbiddenAccessException()`. Medical: 5 + 6. Safety: 2 + 6. Report: 9 + 0. |
| B5 | `DocumentVaultService` does role checks by hand; no `IPermissionService` | `DocumentVaultService.cs` |
| B6 | Leftover hardcoded exception strings | `CoinService.cs:214, 298`; `FamilyAdminService.cs` (1) |
| B7 | `FamilyFirstModule` has values 1–10 only; `MasterDataCodes` has 0–19 only | `FamilyFirst.Domain/Enums/` |
| B8 | Module and role-permission seeds cover modules 1–10 only | `080_SeedModules.sql`, `082_SeedRolePermissions.sql` |
| B9 | Level 2 C# enums exist but aren't master data (`DocumentCategory.cs`, `SafeZoneType.cs`, `FinanceCategory.cs`, `HealthRecordEventType.cs`; there is no `HealthRecordType`) | `FamilyFirst.Domain/Enums/` |
| B10 | No cache warmup at startup. `tblRegularExpression` (075) and `tblAPIMethod` (076) exist but nothing reads them. Founder rules 3 and 5 aren't implemented. | — |
| B11 | Data access is about 96% EF LINQ and 4% stored procedures (stored procedures only in ApiLog, ErrorCode, MasterDataResolver, Permission, StaticData) | Parked (19.8.7) |
| B12 | No Finance ingest endpoint (`POST /finance/transactions`), consent reminder worker or hard-purge worker | `FinanceController.cs`, `FinanceService.cs:407` (purge on revoke only) |
| B13 | No endpoints for AC-03 (Doc Category config), AC-05 (Safe Zone default radius), AC-07 (Report automation), Escalation config, AC-09 | `FamilyAdminController.cs`, `AdminController.cs` |
| B14 | No backend for SupportTickets or Content Manager | Mobile screens are mocked `[VERIFY in L1 spec]` |
| B15 | No test project (only 4 csproj) | — |

**Security and operations**

| # | Finding | Location | Risk |
|---|---|---|---|
| S1 | **Resolved in P2.** OTP challenges and rolling request quotas are persisted in `tblOtpRequest`. | `106_CreateOtpRequest.sql`, `SqlOtpStore.cs` | Verified restart survival, fourth-request rejection, and concurrent issuance. |
| S2 | **Resolved in P2.** OTP logs are Development-only for unconfigured MSG91. Phone numbers are masked and MSG91 HTTP client logging is disabled. | `OtpService.cs`, `DependencyInjection.cs` | Verified Production returns a database-driven provider-unavailable failure. |
| S3 | **Resolved in P2.** All 13 repositories have a family-scoped GUID lookup. Services with an authenticated family scope use it. | `GetByIdForFamilyAsync`, ProjectOverview §18.4 | Verified a Parent was denied access to all five tested resources in another family. |
| S4 | Level 2 child lookups use the parent id only (family-checked earlier): `FinanceRepository.cs:21, 110`; `SafetyRepository.cs:53, 185`; `VaultDocumentRepository.cs:132`; `MedicalRepository.cs:56, 94, 153, 191` | — | Low (defence in depth) |
| S5 | **Resolved in P2.** Configuration uses `Fcm:ProjectId` and `Fcm:ServiceAccountFilePath`. Startup warns when FCM configuration is incomplete. | `Program.cs`, `appsettings*.json` | Live push delivery still requires credentials. |
| S6 | **Resolved in P2.** Production rejects a missing, short, or placeholder JWT secret. Missing OTP settings warn at startup and requests fail closed outside Development. | `Program.cs`, `JwtOptions.cs`, `OtpService.cs` | Verified startup and request behavior using placeholder configuration. |

### A.4 Mobile findings (`Mobile/src`)

| # | Finding | Location |
|---|---|---|
| M1 | `tsc --noEmit` = 0 errors only because `@types/react` and `@types/react-dom` aren't installed and `strict` is off. A `--strict` run gives about **6,900 errors** (6,021 × TS7026 untyped JSX). This breaks the CLAUDE.md "TypeScript strict mode" rule. | `package.json`, `tsconfig.json` |
| M2 | **No role guards.** `ProtectedRoute` supports `allowedRoles` but no route passes it, so any logged-in user can open `/admin` | `src/core/router/AppRouter.tsx` |
| M3 | Public share routes are behind the login: `vault/share/:token`, `medical/emergency-card/:token` (breaks business rule 14: "never behind a login wall") | `AppRouter.tsx` |
| M4 | Broken navigation targets (fall through to `*` and redirect to `/`): see the table in P6 | multiple |
| M5 | `ProfileScreen.tsx` is imported but never routed | `features/profile/screens/ProfileScreen.tsx` |
| M6 | Emergency Card Share button never renders: compares `'Parent'`/`'FamilyAdmin'` but roles are `'PARENT'`/`'FAMILY_ADMIN'` | `features/medical/screens/EmergencyCardScreen.tsx:32`, `src/core/auth/UserRole.ts` |
| M7 | Repository methods without the `AppConfig.isDemo` check (demo mode calls the live API) | `AuthRepository.ts:60 loginWithPassword, :129 getMe`; `TaskCompletionRepository.ts:235`; `NotificationRepository.ts:311`; `ChildRepository.ts:190, 192`; `ReportsRepository.ts:211 getDigestArchive`; `TaskRepository.ts:317, 321` |
| M8 | `FCMService.ts:60` calls `axios.put('/api/users/${userId}/fcm-token')` directly, with no base URL or token; the route isn't in `MasterApiReference` | `src/core/notifications/FCMService.ts` |
| M9 | Token refresh has no queue (parallel 401s each refresh), no request timeout, no 403 handling | `src/core/network/apiClient.ts` |
| M10 | Hardcoded data in screens: `AnalyticsScreen.tsx:11-28`, `SupportTicketsScreen.tsx:26`, `ContentManagerScreen.tsx:33-38` (Save does nothing), `AdminDashboardScreen.tsx:32`, `EmergencyFolderScreen.tsx:21, 95` (demo cards in live mode), `:59` (`familyId ?? 'demo'`), `ScoresReportsScreen.tsx:69, 76-78` and `AttendanceSummaryScreen.tsx:30` (child id `'c1'`) | `src/features/**` |
| M11 | Hardcoded lookup lists in screens: AddMember `LINK_TYPES`; EditHealthProfile `BLOOD_GROUPS`/`ALLERGY_CATEGORIES`/`CHRONIC_CONDITIONS`; AddEditSafeZone `ZONE_TYPES`; LocationAlertHistory `FILTER_OPTIONS`; AddTask `pillarTags`; TaskTemplatePicker `categories`; CreateEvent visibility/reminder options; FamilyManagement statuses; NotificationCampaign/ModuleVisibility/NotificationRules roles; AttendanceMarking `FALLBACK_STATUSES`; PhoneLogin countries; EmergencyAccessRules/StorageConfig modes; `VaultRepository.ts:10 CATEGORY_LABELS` | `src/features/**` |
| M12 | Level 2 and admin screens hold API data in `useState` (breaks CLAUDE.md). Worst: EditHealthProfile (14), DocumentUpload (12), DocumentDetail (9), CreateSession (9), EmergencyCard (8), WeeklyDigest (8), MonthlyReport (7), LocationSettings (7), EmergencyAccessRules (7), FamilyManagement (7) | — |
| M13 | No offline support: no service worker, IndexedDB or Cache API. `CacheService.ts` is unused. The Emergency Folder offline banner is fake. | `features/vault/screens/EmergencyFolderScreen.tsx` |
| M14 | `public/manifest.json` lists `favicon.ico`, `logo192.png`, `logo512.png`, which don't exist. There's no `firebase-messaging-sw.js`. Firebase config is `PLACEHOLDER`. | `public/`, `FCMService.ts` |
| M15 | Missing `.catch` / error state: `EmergencyCardScreen.tsx:40-42`, `EmergencyFolderScreen.tsx:60` | — |
| M16 | Unused code: `DeepLinkHandler.ts`, `retryUtility.ts`, `CacheService.ts` | `src/core/**` |
| M17 | 13 explicit `any`: `CacheService.ts:40`, `FCMService.ts:67`, `AuthRepository.ts:30, 41`, `SecureStorageService.ts:14`, `AdminRepository.ts:157, 224`, `TaskChip.tsx:14`, `AttendanceRepository.ts:141`, `FeedbackRepository.ts:177, 315`, `FeedbackCard.tsx:56` | — |
| M18 | No tests and no ESLint. The `lint` script is just `tsc --noEmit`. 6 × `console.log` (FCMService ×5, NotificationPayloadHandler ×1). | `package.json` |
| M19 | `.env.local` has two `VITE_IS_DEMO` lines and an empty `VITE_API_BASE_URL` | `Mobile/.env.local` |
| M20 | §22.20 GetMasters: Level 1 is partial (screens partly wired; demo arrays still present); Level 2 not started | ProjectOverview §22.20 |

### A.5 Angular admin
There is **no Angular project** anywhere in the repo. All admin screens live inside the React PWA (`features/admin`, `features/family_admin`). Building an Angular portal is **not in this plan**. If it's still wanted, raise it as a separate T3 decision.

---

## PART B — PHASE PLAN

### B.0 Execution order

| Phase | Name | Layer | Priority | Depends on |
|---|---|---|---|---|
| P0 | Baseline & environment verification | Ops/DB | HIGH | — |
| P1 | Backend runtime 500 fixes | Backend | **CRITICAL** | P0 |
| P2 | Backend security hardening | Backend + SQL | HIGH | P1 |
| P3 | Level 2 enums + DB seeds | DB + Domain | HIGH | P0 |
| P4 | Level 2 services: cross-cutting integration | Backend | HIGH | P3 |
| P5 | Mobile type foundation (5A / 5B / 5C) | Mobile | HIGH | — |
| P6 | Mobile routing, role guards & network fixes | Mobile | HIGH | P5A |
| P7 | Level 1 GetMasters completion + admin screen wiring | Mobile | MEDIUM | P0, P6 |
| P8 | Level 2 Mobile: GetMasters + state cleanup | Mobile | MEDIUM | P3, P4, P6 |
| P9 | Emergency-only offline | Mobile | MEDIUM | P6, P8 |
| P10 | Deferred features (PDF export, AC configs, finance workers) | Full stack | MEDIUM | P4, P8 |
| P11 | Cache warmup + regex from DB | Backend | LOW | P4 |
| P12 | Integrations go-live (FCM, MSG91, S3) | Integrations | **BLOCKED (credentials)** | P2 |
| P13 | Quality gates, tests, visual check, doc sync | QA | MEDIUM | all |

**Next SQL script number: `123`.** P10A added script 117. P11 added scripts 118–119. R2 added scripts 120–121. R6 added script 122. Never reuse 087.

### B.0.1 Standing rules (included in every Codex prompt)
- Follow `CLAUDE.md` startup flow. Read `ModuleIndex.md`, then only the relevant `ProjectOverview.md` section.
- Any DB change: read `API/Docs/Flow/New SQL Format.txt` first. Use manual `.sql` scripts only, never EF migrations.
- Any API change: read `API/Docs/Flow/New API Format.txt` first. Use the `ApiResponse<T>` envelope.
- Only ADD to files from earlier phases. The exception is a **bug fix** this phase names explicitly.
- Never rename or remove a mobile route or repository method.
- Every query is scoped by FamilyId; `IsDeleted = 0` applies through the global filter.
- Each repository method starts with an `AppConfig.isDemo` check. Demo data must have the same shape as the live response.
- `dotnet build` must give 0 errors and 0 warnings; `tsc --noEmit` must give 0 errors.
- UI is not COMPLETE until it has been rendered and checked. Otherwise report it as **UNVERIFIED — needs visual check**.
- Update `ProjectOverview.md` per the CLAUDE.md update rules, and end with the Task Completion Block.

---

### P0 — Baseline & Environment Verification
**Goal:** Prove the current code builds and the database runs before anything changes. This closes the §22.25 runtime `[VERIFY]`.
**Depends on:** none · **Layer:** Ops/DB · **No code changes.**

**Tasks**
- [x] `dotnet build API/FamilyFirst.sln`: record the error and warning counts.
- [x] Create a fresh dev DB and run `API/Docs/Task/RunAllScripts.ps1` (scripts 001–104, 087 absent). Record any failure.
- [x] Run `EXEC dbo.uspGetMasterDataByCode @MasterDataCode='X'` for all 13 static codes (Role, Plan, Module, Permission, TaskType, TaskStatus, AttendanceStatus, RewardType, CoinTransactionType, FeedbackRating, CalendarEventType, NotificationType, OTPType). Each must return rows.
- [x] Start the API. Call `POST /api/GetMasters {"masterDataCode":"TaskType"}` in Postman and confirm it returns items.
- [x] `cd Mobile && npx tsc --noEmit`: record the result. Start with `VITE_IS_DEMO=true` and open 5 random screens.

**Done criteria:** a baseline report exists, and every failure is listed with a file and error message.

**Docs to update:** ProjectOverview §22.25. Mark the runtime check done, or list the failing codes.

**Codex Prompt**
```
Read CLAUDE.md, then API/Docs/Flow/ModuleIndex.md, then ProjectOverview.md §22.25 and §22.30 only.
TASK: Phase P0 — baseline verification. DO NOT change any source code.
1. Run `dotnet build API/FamilyFirst.sln` and report error/warning counts with the first 20 messages.
2. Run API/Docs/Task/RunAllScripts.ps1 against a fresh dev database (scripts 001–104; 087 intentionally absent). Report any script that fails, with the SQL error.
3. For each of these codes run EXEC dbo.uspGetMasterDataByCode @MasterDataCode='<code>' and report the row count:
   Role, Plan, Module, Permission, TaskType, TaskStatus, AttendanceStatus, RewardType, CoinTransactionType, FeedbackRating, CalendarEventType, NotificationType, OTPType.
4. Start the API and call POST /api/GetMasters {"masterDataCode":"TaskType"}; report the response.
5. In Mobile/ run `npx tsc --noEmit` and report the result.
6. Update ProjectOverview.md §22.25 with the verified runtime result (or the exact failures). End with the Task Completion Block.
```

---

### P1 — Backend Runtime 500 Fixes
**Goal:** Remove every confirmed runtime 500 error. These count as bug fixes, so editing earlier-phase files is allowed.
**Depends on:** P0 · **Layer:** Backend (Infrastructure, Domain, Application)

**Tasks**
- [x] Replace `.CreatedAt`→`.DateCreated`, `.UpdatedAt`→`.LastUpdated`, `.DeletedAt`→`.DateDeleted` **inside EF LINQ only** at:
  - `AdminRepository.cs:36, 48, 226`
  - `ChildProfileRepository.cs:32`
  - `CoinTransactionRepository.cs:23`
  - `FamilyAdminConfigRepository.cs:214-215, 267-268`
  - `FeedbackRepository.cs:55, 69-70`
  - `MedicalRepository.cs:172`
  - `NotificationRepository.cs:51, 66, 119`
  - `VaultDocumentRepository.cs:52, 55, 69`
- [x] Don't touch the safe lines listed in A.3.
- [x] `VaultExpiryReminderLog.cs:9`: add `builder.Ignore(e => e.DocumentId)` in its EF configuration. Alternatively make it get-only and confirm EF ignores it. Either way, check it against `044_CreateVaultExpiryReminderLogs.sql`.
- [x] `ReportService.ExportReportAsync` (`:791`): replace the `NotImplementedException` with a typed business failure, so the endpoint returns a clean `ApiResponse` error and not a 500. Use an existing `FamilyFirstErrorCode` (e.g. `Technical_Error`, or add `Feature_Not_Available` via a new script `105_SeedErrorCode_FeatureNotAvailable.sql`). The real export comes in P10.
- [x] Grep the whole Infrastructure layer again for `.CreatedAt|.UpdatedAt|.DeletedAt` inside `IQueryable` chains so nothing is missed.
- [x] **Added from the P0 run (2026-11-20), from the API log of observed worker failures:**
  - [x] `RunAllScripts.ps1`: the configured scripts path doesn't exist. Point it at `API/FamilyFirst.Infrastructure/Data/Scripts`. Run `sqlcmd` with `-I` (QUOTED_IDENTIFIER ON) and `-b` (stop on error). Without `-I`, 66 scripts failed with Msg 1934 and script 093 failed with Msg 208.
  - [x] `VaultExpiryWorker`: `Invalid column name 'MemberInternalId'`. EF created a shadow FK because the navigation has no explicit `HasForeignKey(e => e.FamilyMemberId)` + `HasPrincipalKey(p => p.InternalId)`.
  - [x] `ReminderDeliveryWorker`: `Invalid column name 'EventInternalId'`. Same shadow-FK cause on the EventReminder → CalendarEvent navigation.
  - [x] **Shadow-FK sweep:** list every shadow property in `DbContext.Model` (entity type → `GetProperties().Where(p => p.IsShadowProperty())`) and fix every FK one with explicit `HasForeignKey` / `HasPrincipalKey`. These two are probably not the only ones.
  - [x] `SafetyWorker`: `TimeOnly.FromDateTime` can't be translated by EF. Run the time-of-day comparison in memory after a family- and date-bounded query, or compare `DateTime` hour and minute parts.
  - [x] `NotificationDeliveryWorker`: `Notification.CreatedAt` is already covered above (`NotificationRepository.cs:119`).

**Done criteria**
- [x] `dotnet build` gives 0 errors and 0 warnings.
- [x] Authenticated HTTP checks return 200 for: `GET /api/users/{id}/notifications`, admin families list, feedback list, vault document list with date filter, medical lists, coin history.
- [x] The API ran for over two minutes with no worker exceptions in its log. All 9 workers (Reminder, BirthdayEvent, NotificationDelivery, MorningDigest, EveningDigest, WeeklyDigest, VaultExpiry, VaccinationReminder, Safety) also completed one cycle in a temporary harness using the production worker code and LocalDB.
- [x] The checked-in `RunAllScripts.ps1` passed on fresh databases: 103 scripts through 104 before script 105 was added, then 104 scripts through 105, with zero failures. This closes the P0 runner gate.
- [x] The shadow-FK sweep returns 0 unintended shadow FK properties.
- [x] `POST …/reports/export` returns a non-500 `ApiResponse` failure.

**Verification finding:** The medical profile list and a timeline backed by a valid profile returned HTTP 200. A timeline request for a member without a health profile hit a separate existing first-use error in `MedicalService.GetOrCreateProfileAsync` (zero-valued family and member FKs).

**Docs updated:** ProjectOverview §22.25 records closure of the runner gate, §23.2 lists the resolved alias references, §23.4 records the shadow FK fix, and §16.8 documents the export failure response.

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §23.2 and §23.3, and API/Docs/Flow/New API Format.txt.
TASK: Phase P1 — fix confirmed runtime 500s. These are bug fixes; editing existing files is permitted ONLY at the lines below.
1. EF LINQ uses ignored alias properties (BaseEntityConfiguration ignores CreatedAt/UpdatedAt/DeletedAt). Inside IQueryable expressions only, replace
   CreatedAt→DateCreated, UpdatedAt→LastUpdated, DeletedAt→DateDeleted at:
   API/FamilyFirst.Infrastructure/Data/Repositories/Implementations/
     AdminRepository.cs:36,48,226 | ChildProfileRepository.cs:32 | CoinTransactionRepository.cs:23 |
     FamilyAdminConfigRepository.cs:214-215,267-268 | FeedbackRepository.cs:55,69-70 | MedicalRepository.cs:172 |
     NotificationRepository.cs:51,66,119 | VaultDocumentRepository.cs:52,55,69
   Do NOT change in-memory uses (AdminRepository 78/110, EventReminderRepository 51/72/82, FamilyAdminConfigRepository 182,
   FinanceRepository 99/148/234, Morning/EveningDigestWorker 60). Then grep the Infrastructure project for any other alias use inside IQueryable and fix it.
2. VaultExpiryReminderLog.DocumentId (Domain entity, line 9) has a no-op setter and is not ignored in EF. Add builder.Ignore(e => e.DocumentId) in its configuration; verify against 044_CreateVaultExpiryReminderLogs.sql.
3. ReportService.ExportReportAsync (~line 791) throws NotImplementedException → returns 500. Replace with a ValidationException/business failure whose message comes from IErrorCodeService. If no suitable code exists, add script 105_SeedErrorCode_FeatureNotAvailable.sql (follow New SQL Format.txt) and the matching FamilyFirstErrorCode enum value.
4. dotnet build → 0 errors 0 warnings. List every changed line.
5. Update ProjectOverview.md §23.2 with the confirmed instances and §16.8 with the export placeholder behaviour. End with the Task Completion Block.
```

---

### P2 — Backend Security Hardening
**Goal:** Close S1, S2, S3, S5 and S6.
**Depends on:** P1 · **Layer:** Backend + SQL · **Tier:** T2. Security Architect review is required.

**Tasks**
- [x] **Medical first use:** resolve the authenticated family and target member before inserting a health profile. Reuse validated caller IDs at the other medical and vault zero-ID fallback sites.
- [x] **OTP store:** added `tblOtpRequest` in script 106, with digests, expiry, attempt count, consumption state, and audit columns. `IOtpStore` uses SQL. Validity is five minutes with the existing five-wrong-attempt limit.
- [x] **Rate limiter:** SQL atomically counts and reserves up to three requests per phone in the last hour. Middleware retains the existing HTTP 429 response.
- [x] **OTP log:** an unconfigured MSG91 provider can log a code only in Development, with the phone masked. Other environments return HTTP 503 with the database-driven code seeded by script 107.
- [x] **Family-scoped reads:** added `GetByIdForFamilyAsync(Guid id, long familyId, CancellationToken)` to all 13 repositories and switched family-scoped call sites. Existing repository methods remain available.
- [x] **Startup config check:** `ValidateOnStart` rejects missing, placeholder, or short JWT secrets outside Development. Startup warns about incomplete FCM settings and warns about incomplete OTP settings outside Development.
- [x] Added placeholder-only Development and Production configuration templates and corrected the FCM project and service-account keys.

**Done criteria:** `dotnet build` 0/0. OTP send and verify still work after an API restart. A 4th OTP request within an hour returns 429. A Parent from family A gets 404 or 403 on a family B task, reward or event GUID. Production startup with a placeholder JWT secret fails.

**Verification:** Build 0 errors and 0 warnings. Fresh LocalDB setup passed 106 scripts. OTP survived restart and rejected replay, expiration and attempts beyond the limit. The fourth request returned HTTP 429. Family B task, reward, event, feedback and child-profile requests all returned HTTP 403 under a family A Parent JWT. The medical first-use timeline returned HTTP 200 and created one correctly scoped profile. Production rejected a placeholder JWT secret. The API ran for 125 seconds with zero worker exceptions.

**Docs updated:** ProjectOverview §1.7, §2.2–2.5, §13.4, §18.4, and Drift Entry 009.

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §2 (Authentication) and §18.4, New SQL Format.txt and New API Format.txt.
TASK: Phase P2 — backend security hardening. Security-sensitive: keep all existing public contracts unchanged.
1. OTP persistence: OtpService (API/FamilyFirst.Infrastructure/Services/OtpService.cs:12) stores OTPs in a static ConcurrentDictionary.
   Create script 106_CreateOtpRequest.sql (New SQL Format: GUID Id + BIGINT PK, audit columns) storing phone, OTP hash (never plain), ExpiresAt, AttemptCount.
   Add IOtpStore (Application) + SqlOtpStore (Infrastructure); register in DependencyInjection.cs (append only). Preserve rules: OTP valid 5 min, max 3 requests/hr/phone.
2. Replace the in-memory OTP rate counter in API/FamilyFirst.API/Middleware/RateLimitingMiddleware.cs (~line 81) with a count from the same table (last 60 minutes). Keep the 429 ApiResponse with FamilyFirstErrorCode.OTP_Rate_Limit.
3. OtpService.cs:83 logs the OTP in plain text when the MSG91 key/template is a placeholder. Only allow that when IHostEnvironment.IsDevelopment(); otherwise return an ApiResponse failure via IErrorCodeService.
4. ADD (do not modify existing) GetByIdForFamilyAsync(Guid id, long familyId, CancellationToken) to: AttendanceRecord, AttendanceSession, CalendarEvent, ChildProfile, FamilyMember, Feedback, Notification, RewardRedemption, Reward, TaskCompletion, TaskItem, TeacherProfile, User repositories (+ interfaces). Use it in services wherever familyId is already resolved.
5. Add options validation with ValidateOnStart: outside Development, fail if Jwt:Secret starts with '<' or length < 32. Warn if Fcm:ProjectId / service account missing. Add appsettings.Development.json and appsettings.Production.json with placeholder values only; correct FCM keys to Fcm:ProjectId + Fcm:ServiceAccountFilePath (FcmPushNotificationService.cs:53,101 expects these).
6. dotnet build → 0/0. Update ProjectOverview §2.3, §2.4, §18.4 and mark Drift Entry 009 resolved. End with the Task Completion Block.
```

---

### P3 — Level 2 Enums & DB Seeds
**Goal:** Make Level 2 modules first-class in the permission and master-data system. This unblocks P4 and P8.
**Depends on:** P0 · **Layer:** DB + Domain · **Tier:** T3

**Status: COMPLETE (2026-11-21, done by Claude in parallel with Codex P2).** Scripts are numbered 108–115, not 107–114, because P2 had already used 106–107.

**Tasks**
- [x] `FamilyFirstModule`: add `DocumentVault=11, MedicalRecords=12, Safety=13, Finance=14, Reports=15`.
- [x] `MasterDataCodes`: add `DocumentCategory=20, HealthRecordType=21, SafeZoneType=22, FinanceCategory=23`. Names must match `tblMasterData` exactly.
- [x] Scripts (New SQL Format, idempotent):
  - `108_SeedModules_Level2.sql`: tblModule rows 11–15 (VAULT, MEDICAL, SAFETY, FINANCE, REPORTS)
  - `109_SeedModulePermissions_Level2.sql` (no Admin View on 11–14)
  - `110_SeedRolePermissions_Level2.sql`: derived from the §18.3 matrix. SuperAdmin has **zero** rows on 11–15. Three derived rows are marked `[VERIFY]` (Safety AR for FamilyAdmin and Parent; Finance Delete for FamilyAdmin).
  - `111_CreateLookupTables_Level2.sql`: tblDocumentCategory, tblHealthRecordType, tblSafeZoneType, tblFinanceCategory
  - `112_SeedLookupTables_Level2.sql`: each Code equals the C# value persisted today. DocumentCategory `SortOrder` equals the enum int.
  - `113_SeedMasterData_Level2.sql`: tblMasterData pointer rows 20–23
  - `114_AlterSP_GetMasterDataByCode_Level2.sql` (v1.3) and `115_AlterSP_GetMasterDataByCodeInternal_Level2.sql` (v1.2). The diff against 085/086 shows additions only.
- [x] `RunAllScripts.ps1` already finds new scripts automatically since P1, so it needed no edit.

**Done criteria (all verified 2026-11-21)**
- [x] `dotnet build` 0 errors, 0 warnings.
- [x] Fresh LocalDB, full runner: **114/114 passed** (001–115, with 087 absent).
- [x] A second run of 108–115 inserts 0 rows, so the scripts are idempotent.
- [x] `uspGetMasterDataByCode` returns DocumentCategory 8, HealthRecordType 6, SafeZoneType 7 and FinanceCategory 14 rows. Level 1 regression checks pass: TaskType 5, Role 6, OTPType 2, Module 15.
- [x] Lookup by a single GUID works. `uspGetMasterDataByCodeInternal` resolves a valid GUID to its MasterDataId, and returns empty for a random GUID or for a GUID sent under the wrong code.
- [x] `uspCheckRolePermission`: 18 of 18 role checks passed, including SuperAdmin denied on Vault, Medical, Safety and Finance, Teacher denied on all Level 2 modules, Elder read-only on Safety, and a Level 1 regression check.
- [ ] Live `POST /api/GetMasters {"masterDataCode":"DocumentCategory"}` was not called. The validator and service both accept any name in the enum (confirmed in source), so P8 will exercise it.

**Note for P4/P8:** Level 2 entities still store the code string (or the enum int for DocumentCategory), not a lookup FK. Resolve the UI's GUID through `IMasterDataResolver`, then store the row's Code. No entity schema change is needed. Details are in ProjectOverview §22.18.

**Docs to update:** §19.7 (script inventory), §22.17 and §22.18 (mark implemented), §18.3 (if the matrix was confirmed).

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §18.3, §19.7, §22.17, §22.18, and API/Docs/Flow/New SQL Format.txt (mandatory).
TASK: Phase P3 — Level 2 enums and seed scripts. Last existing script is 104 (+105/106 if P1/P2 added them); 087 must not be reused.
1. FamilyFirst.Domain/Enums/FamilyFirstModule.cs: add DocumentVault=11, MedicalRecords=12, Safety=13, Finance=14, Reports=15.
2. FamilyFirst.Domain/Enums/MasterDataCodes.cs: add DocumentCategory=20, HealthRecordType=21, SafeZoneType=22, FinanceCategory=23.
3. Create idempotent scripts in API/FamilyFirst.Infrastructure/Data/Scripts/ following New SQL Format.txt exactly:
   107_SeedModules_Level2.sql, 108_SeedModulePermissions_Level2.sql, 109_SeedRolePermissions_Level2.sql (derive from §18.3; SuperAdmin has NO Vault/Medical access; Elder read-only; mark any row you cannot confirm with a [VERIFY] SQL comment),
   110_CreateLookupTables_Level2.sql (tblDocumentCategory, tblHealthRecordType, tblSafeZoneType, tblFinanceCategory),
   111_SeedLookupTables_Level2.sql (values = existing enums DocumentCategory.cs, SafeZoneType.cs, FinanceCategory.cs, HealthRecordEventType.cs — same int values),
   112_SeedMasterData_Level2.sql (tblMasterData rows for codes 20–23),
   113_AlterSP_GetMasterDataByCode_Level2.sql and 114_AlterSP_GetMasterDataByCodeInternal_Level2.sql (add routing branches, keep existing branches byte-identical).
   Renumber sequentially if 105/106 are not yet used.
4. Append the new scripts to API/Docs/Task/RunAllScripts.ps1.
5. dotnet build → 0/0. Run scripts twice to prove idempotency. Update ProjectOverview §19.7, §22.17, §22.18. End with the Task Completion Block.
```

---

### P4 — Level 2 Services: Cross-cutting Integration
**Goal:** Bring Finance, Medical, Safety, Report and DocumentVault services up to the same 4-service standard as Level 1 (§22.1 TASK-CC-01..04).
**Depends on:** P3 · **Layer:** Backend · **Tier:** T2

**Tasks**
- [x] `DocumentVaultService`: inject `IPermissionService`, then call `CheckAsync(role, FamilyFirstModule.DocumentVault, CreateUpdate|Delete)` before upload, update, delete and share.
- [x] `MedicalService`, `SafetyService`, `FinanceService`: integrate all four services per §22.13–22.15.
- [x] `ReportService`: integrate CC-01 and CC-03 per §22.16.
- [x] Use `IErrorCodeService` for exception messages in the five Level 2 services, including the former bare forbidden exceptions.
- [x] Resolve the remaining Coin and FamilyAdmin exception messages through the error catalog.
- [x] Seed eight error messages in `116_SeedErrorCodes_Level2.sql` and add their enum values.
- [x] Enforce Finance consent before transaction reads and restrict authenticated Vault and Medical operations to their family role gates.

**Done criteria:** `dotnet build` 0/0. A grep for `throw new \w+Exception\("` in `Services/Implementations` returns 0. Postman: a Parent can create a vault document, an Elder gets 403 on create, a Medical profile read works, and the Finance list without consent returns the consent error.

**Verified 2026-11-21:** Build 0 errors and 0 warnings. Fresh LocalDB passed 115 scripts through 116. The hardcoded exception scan found zero matches. Authenticated HTTP checks covered the Vault write lifecycle, Elder and SuperAdmin denials, Medical first use, Finance reads before and after consent and revocation, Safety zone permissions, and the weekly report. Database audit entries were verified for all five modules.

**Docs updated:** ProjectOverview §22.12–22.16 records the implemented integrations.

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §22.0, §22.1, §22.12–§22.16, and API/Docs/Flow/New API Format.txt (mandatory).
TASK: Phase P4 — apply TASK-CC-01..04 to Level 2 services in API/FamilyFirst.Application/Services/Implementations/.
Reference pattern: copy exactly how TaskService.cs / RewardService.cs inject and use IApiLogService, IPermissionService, IErrorCodeService, IMasterDataResolver.
1. DocumentVaultService: inject IPermissionService; CheckAsync(role, FamilyFirstModule.DocumentVault, CreateUpdate/Delete) before every write/delete/share.
2. MedicalService (module MedicalRecords), SafetyService (Safety), FinanceService (Finance): inject all four; log every public method (fire-and-forget); permission check before every write/delete/approve; resolve UI GUIDs via IMasterDataResolver before writes.
3. ReportService: IApiLogService + IErrorCodeService only (read-only).
4. Replace every hardcoded exception string and every bare `new ForbiddenAccessException()` in those five services, plus CoinService.cs:214,298 and the one in FamilyAdminService.cs, with IErrorCodeService.GetMessageAsync(FamilyFirstErrorCode.X). If a needed code is missing, add script 116_SeedErrorCodes_Level2.sql (New SQL Format) + enum values.
5. Preserve business rules: Finance consent before SMS data; SuperAdmin cannot access Vault/Medical; Elder read-only. Do not change any controller route or DTO shape.
6. Verify: dotnet build 0/0; grep `throw new \w+Exception\("` in Services/Implementations returns nothing. Update ProjectOverview §22.12–§22.16. End with the Task Completion Block.
```

---

### P5 — Mobile Type Foundation
**Goal:** Make the TypeScript gate real (M1, M7, M17). Do this in 3 sub-phases so each Codex run stays manageable.
**Depends on:** none (can run alongside P1–P4) · **Layer:** Mobile

**Status: COMPLETE (2026-11-21, done by Claude in parallel with Codex). 5A, 5B and 5C were finished in one pass.**
Once `@types/react` was installed, the "~6,900 strict errors" from the audit shrank to **30 non-strict + 62 strict errors**. Most of the 6,900 were really just untyped JSX. A `tsconfig.strict.json` scoped to some folders turned out to be unworkable, because `core/router` imports every screen and pulls the whole app into the check. So strict mode went straight into `tsconfig.json`.

**P5A — core and shared**
- [x] Added `@types/react` 19.2.18 and `@types/react-dom` 19.2.7 (React 19.2.7). Added a `typecheck` script.
- [x] Fixed all 30 non-strict errors. Several were real bugs:
  - The EmergencyCard Share button never rendered (M6).
  - The admin hero badge used `variant="neutral"`, which doesn't exist, so it rendered with no style.
  - The Parent profile always showed "Phone number not available" because the auth mapper dropped `phoneNumber`.
  - The `FFButton` loading state could be overridden by a caller's `disabled` prop.
  - 18 section headers rendered an empty gold icon tile.
- [x] Added the `isDemo` branch to all 9 M7 methods. Lookup demo data now lives in `src/core/repositories/DemoMasterData.ts`, which mirrors seed 093.
- [x] Removed all explicit `any`. 0 remain.
- [x] `.env.local`: already had one `VITE_IS_DEMO` line, so nothing to change.

**P5B / P5C — features**
- [x] Fixed all 62 strict errors. 55 were Level 2 repositories doing `return response.data.data` with a `T | null` payload. The new rule:
  - single objects: `requireApiData`
  - lists: `?? []`
  - booleans: `?? false`
- [x] A damaged local `firebase` install (missing `messaging` type files) was reinstalled from the lockfile at the same version, 12.15.0.
- [x] `"strict": true` is now in `tsconfig.json`. No `tsconfig.strict.json` was needed.

**Verified:**
- `npm run typecheck` with strict on: 0 errors.
- `npm run build`: passes.
- 14 changed screens rendered in demo mode in headless Chrome at 390×844:
  - none blank, no page errors
  - the Share button is present
  - the only 404 is `favicon.ico`, which is M14 and scheduled for P9

**New findings from P5 (not fixed; outside P5 scope):**
- [ ] **F-P5-1 (P7):** Monthly Report headline renders white-on-white. `<FFCard className="bg-primary …">` loses to FFCard's default `bg-white`. Four feature files pass `bg-*` classes to FFCard. The fix is `variant="primary"`.
- [ ] **F-P5-2 (P7):** `AttendanceRepository` demo statuses (`Present`, `LeftEarly`) don't match the DB codes (`PRESENT`, `HALFDAY`). Demo and live have drifted apart.
- [ ] **F-P5-3 (P7) [VERIFY]:** `AttendanceRepository.createSession` posts `subject`/`sessionName` as-is. Check them against backend `CreateSessionRequest` (Drift Entry 028 says SubjectName/BatchName).
- [ ] **F-P5-4 (P9):** a single 1.58 MB JS bundle. Code-split before the P9 service worker precaches the app shell.
- [ ] **F-P5-5 (P13):** 13 files already had mixed CRLF/LF line endings before P5 (e.g. `apiClient.ts`, `CalendarRepository.ts`). Normalize them when ESLint and Prettier are added.

**Done criteria (each sub-phase):** `npx tsc --noEmit` = 0 with strict on for that scope. The app starts in demo mode and the touched screens render. No route or repository method is renamed.

**Docs to update:** §20.1 (strict and types note), §1.5.

**Codex Prompt (run 3 times: replace `<SCOPE>` with 5A, 5B or 5C)**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §20.1–§20.5.
TASK: Phase P5<SCOPE> — make TypeScript strict real in Mobile/. Never rename/remove a route or repository method; behaviour must not change.
Context: @types/react and @types/react-dom are NOT installed and tsconfig has no "strict", so tsc passes while ~6,900 strict errors exist.
P5A: 1) npm i -D @types/react@^19 @types/react-dom@^19; add "typecheck": "tsc --noEmit" script.
     2) Add the missing AppConfig.isDemo branch (same shape as live) to: AuthRepository.ts loginWithPassword(~60) & getMe(~129); TaskCompletionRepository.ts getTaskStatuses(~235); NotificationRepository.ts getNotificationTypes(~311); ChildRepository.ts getTaskTypes/getTaskStatuses(~190,192); ReportsRepository.ts getDigestArchive(~211); TaskRepository.ts getTaskTypes/getTaskStatuses(~317,321).
     3) Remove all explicit `any`: CacheService.ts:40, FCMService.ts:67, AuthRepository.ts:30,41, SecureStorageService.ts:14, AdminRepository.ts:157,224, TaskChip.tsx:14, AttendanceRepository.ts:141, FeedbackRepository.ts:177,315, FeedbackCard.tsx:56.
     4) Create tsconfig.strict.json (extends tsconfig.json, strict:true, include src/core/** and src/shared/**) and fix to 0 errors.
     5) Remove the duplicate VITE_IS_DEMO line in .env.local.
P5B: extend tsconfig.strict.json include to src/features/{auth,family,parent,child,elder,teacher,tasks,calendar,notifications,profile,admin,family_admin}/** and fix to 0.
P5C: include all of src/**, fix to 0, move "strict": true into tsconfig.json, delete tsconfig.strict.json.
Verify: npx tsc --noEmit = 0 for the scope; run `npm run dev` with VITE_IS_DEMO=true and open each touched screen — report any that you could not render as UNVERIFIED.
Update ProjectOverview §20.1. End with the Task Completion Block.
```

---

### P6 — Mobile Routing, Role Guards & Network Fixes
**Goal:** Close M2–M6, M8, M9, M15 and M16.
**Depends on:** P5A · **Layer:** Mobile · **Tier:** T2 (security)

**Broken link fixes (M4).** Fix the link, not the route. Routes are part of the immutable contract.

| File:line | Wrong target | Correct target | Browser check |
|---|---|---|---|
| `FamilyAdminPanelScreen.tsx:46` | `/family-admin/emergency` | `/family-admin/emergency-access` | ✓ |
| `FamilyAdminPanelScreen.tsx:64` | `/family-admin/safety-thresholds` | `/family-admin/alert-thresholds` | ✓ |
| `ParentHomeScreen.tsx:200` | `/parent/child/:id` | `/parent/children/:childId` | ✓ |
| `HealthHomeScreen.tsx:25, 46` | `/medical/add` | `/medical/new` opens `EditHealthProfileScreen` in create mode. | ✓ |
| `ParentProfileScreen.tsx:141`, `ParentSettingsScreen.tsx:129`, `ProfileScreen.tsx:79` | `/profile/subscription` | Hide the entry when `subscriptionEnabled=false` (subscriptions are parked) | ✓ |
| `NotificationPayloadHandler.ts:45` | `/parent/verification-queue` | `/parent/verification` | ✓ |
| `NotificationPayloadHandler.ts:48` | `/parent/reward-shop` | `/parent/rewards` | ✓ |
| `NotificationPayloadHandler.ts:54` | `/parent/calendar` | `/calendar` | ✓ |
| `NotificationPayloadHandler.ts:63` | `/parent/safety` | `/safety` | ✓ |

**Tasks**
- [x] Pass `allowedRoles` on every role route group in `AppRouter.tsx`, following the role data-scope table in CLAUDE.md:
  - `/admin/*` → SUPER_ADMIN
  - `/family-admin/*` and `/parent/admin` → FAMILY_ADMIN
  - `/teacher/*` → TEACHER
  - `/child/*` → CHILD
  - `/elder/*` → ELDER
  - `/parent/*` → PARENT, FAMILY_ADMIN
  - `vault` and `medical` → not SUPER_ADMIN
- [x] Move `vault/share/:token` and `medical/emergency-card/:token` **outside** `ProtectedRoute`, so they are public (rule 14).
- [x] Route `ProfileScreen` at `/profile`, or remove the unused import `[VERIFY]` (keep the screen file either way).
- [x] `EmergencyCardScreen.tsx:32`: compare using `UserRole` constants from `src/core/auth/UserRole.ts`. Add `.catch` and an error state with retry at `:40-42`; do the same in `EmergencyFolderScreen.tsx:60`.
- [x] `FCMService.ts:60`: call through `apiClient`, add `Users.FcmToken` to `MasterApiReference.ts`, and remove the 5 `console.log` calls. Also remove the one in `NotificationPayloadHandler.ts:30`.
- [x] `apiClient.ts`: add a single-flight refresh queue so parallel 401s wait for one refresh, add a request timeout (e.g. 20s), and handle 403 with an error toast, not a logout.
- [x] Wire `DeepLinkHandler.ts` and `retryUtility.ts` into the app, or delete them if unused. `CacheService.ts` is kept for P9.

**Done criteria:** `tsc` 0. In demo mode, a CHILD user going to `/admin` is redirected. A logged-out browser opens `/vault/share/demo-token` and sees the shared view. Each of the 9 fixed links reaches the right screen; tick each one off after it is checked visually.

**Verified 2026-11-21:** Strict `npx tsc --noEmit` passed. Chrome demo sessions exercised all six role groups, both signed-out token views, and the nine rows above.
A local HTTP mock exercised the empty-state health link and save, both emergency retry actions, concurrent and late 401s with one refresh, the 403 toast, refresh failure, and the authenticated FCM registration request.

**Docs updated:** §20.3 (route map and guards), §20.5 (API client), §20.7 (screen reference), and §21 (drift entry 078).

**Codex Prompt**
```
Read CLAUDE.md (Role Data Scope Rules, business rule 14), ModuleIndex.md, ProjectOverview.md §20.3, §20.5, §20.7.
TASK: Phase P6 — routing, role guards and network fixes in Mobile/src. Routes are an immutable contract: fix LINKS, do not rename routes. You may ADD routes.
1. src/core/router/AppRouter.tsx: ProtectedRoute supports allowedRoles but no route uses it. Apply: /admin/*→SUPER_ADMIN; /family-admin/* and /parent/admin→FAMILY_ADMIN; /teacher/*→TEACHER; /child/*→CHILD; /elder/*→ELDER; /parent/*→PARENT,FAMILY_ADMIN; vault & medical → every role except SUPER_ADMIN. Use constants from src/core/auth/UserRole.ts.
2. Move vault/share/:token and medical/emergency-card/:token OUTSIDE ProtectedRoute (public share links must never require login).
3. Fix links: FamilyAdminPanelScreen.tsx:46 → /family-admin/emergency-access; :64 → /family-admin/alert-thresholds; ParentHomeScreen.tsx:200 → /parent/children/:childId;
   NotificationPayloadHandler.ts:45 → /parent/verification, :48 → /parent/rewards, :54 → /calendar, :63 → /safety;
   hide /profile/subscription entries (ParentProfileScreen.tsx:141, ParentSettingsScreen.tsx:129, ProfileScreen.tsx:79) when AppConfig subscriptionEnabled is false;
   HealthHomeScreen.tsx:25,46 '/medical/add' → add route 'medical/new' (declared before 'medical/:memberId') rendering EditHealthProfileScreen in create mode — if that screen cannot support create mode, stop and report instead of guessing.
4. Route ProfileScreen at /profile.
5. EmergencyCardScreen.tsx:32 compares 'Parent'/'FamilyAdmin' but stored roles are 'PARENT'/'FAMILY_ADMIN' — use UserRole constants. Add .catch + FFErrorState with retry at EmergencyCardScreen.tsx:40-42 and EmergencyFolderScreen.tsx:60.
6. FCMService.ts:60 uses raw axios with a relative path — use apiClient + add the route to MasterApiReference.ts. Remove console.log in FCMService.ts and NotificationPayloadHandler.ts:30.
7. src/core/network/apiClient.ts: single-flight refresh queue for concurrent 401s; 20s timeout; 403 → error message, not logout.
8. tsc --noEmit = 0. Render-check every changed link/guard in demo mode; anything not rendered is UNVERIFIED. Update ProjectOverview §20.3, §20.7 and add a §21 drift entry. End with the Task Completion Block.
```

---

### P7 — Level 1 GetMasters Completion + Admin Screen Wiring
**Goal:** Finish §22.20 GMAS-01/02 for Level 1 and remove hardcoded screen data (M10 admin part, M11 Level 1 part, M20).
**Depends on:** P0 (GetMasters returns rows), P6 · **Layer:** Mobile

**Status (2026-11-21):** Codex left this partial; **R6 completed it in source.**
- **Screens:** error-with-retry loading added on the affected screens.
- **Admin Analytics:** fabricated charts removed.
- **Demo lookups:** unified in `DemoMasterData`.
- **CalendarEventType seed:** aligned with the enum (script 122).

`tsc` = 0. **UNVERIFIED — visual check (founder-owned testing).** The checkboxes below remain unticked until that check.

**Tasks** (one module at a time, in order: Attendance → Tasks → Task Completion → Feedback → Rewards → Calendar → Notifications → Family → Admin → Child → Elder → Reports)
- [ ] Wire each screen to its repository's `getMasters` method. Tasks: AddTaskScreen and RoutineBuilderScreen use `TaskType`/`TaskStatus`. Rewards: catalog screens use `RewardType`/`CoinTransactionType`.
- [ ] Apply the GMAS-02 rule per module, for **lookup data only** (no hardcoded arrays in live mode). Keep demo-mode inline mock data: CLAUDE.md requires demo mode to stay complete, so delete only the arrays that live mode uses.
- [ ] Screen lookup lists with no master code (`LINK_TYPES`, `pillarTags`, countries, visibility and reminder options, roles in admin screens) move into the feature repository as typed constants. Mark them `[VERIFY — candidate master data]`.
- [ ] `AttendanceMarkingScreen.tsx:85` `FALLBACK_STATUSES`: use only in demo mode.
- [ ] `AnalyticsScreen` and the `AdminDashboardScreen` activity log: wire to the existing `AdminController` analytics and dashboard endpoints through `AdminRepository`, with demo data in the repository.
- [ ] `SupportTicketsScreen` and `ContentManagerScreen`: move their mock data into `AdminRepository` (demo branch only). Live mode shows `FFEmptyState` "Coming soon" until P10 builds the backend. No dead Save buttons.

**Done criteria (per module):** `tsc` 0. Live dropdown shows DB values (check in the browser against the dev API). Demo mode still shows data. The module's §22.20 checklist box is ticked.

**Docs to update:** §22.20 status table + GMAS-02 checklist, §11.6.

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, ProjectOverview.md §22.19, §22.20, §22.28, §11.6.
TASK: Phase P7 — finish Level 1 GetMasters integration (Mobile/src), one module at a time in this order:
Attendance, Tasks, TaskCompletion, Feedback, Rewards, Calendar, Notifications, Family, Admin, Child, Elder, Reports.
For each module:
 a) Every dropdown/type/status picker loads from the feature repository's getMasters-backed method (shared util src/core/repositories/MasterDataRepository.ts). Tasks screens (AddTaskScreen, RoutineBuilderScreen) must consume TaskType/TaskStatus.
 b) Remove hardcoded lookup arrays used in LIVE mode. KEEP demo-mode inline mock data inside the repository's AppConfig.isDemo branch (demo must never be blank).
 c) Screen-level lists with no master code (AddMemberScreen LINK_TYPES, AddTaskScreen pillarTags, TaskTemplatePicker categories, CreateEventScreen visibility/reminder options, PhoneLoginScreen countries, admin role lists in NotificationCampaignScreen/ModuleVisibilityScreen/NotificationRulesScreen, FamilyManagementScreen statuses) → move into the feature repository as exported typed constants with a `// [VERIFY — candidate master data]` comment. Screens must not define data arrays.
 d) AttendanceMarkingScreen.tsx:85 FALLBACK_STATUSES → demo only.
Admin: AnalyticsScreen.tsx (kpis/trendData/featureUsage L11-28) and AdminDashboardScreen.tsx activityLogs (L32) → load via AdminRepository from the existing AdminController analytics/dashboard endpoints (check MasterApiReference.ts), demo data inside the repository.
SupportTicketsScreen (TICKETS L26) and ContentManagerScreen (L33-38): move mock data into AdminRepository demo branch; live mode shows FFEmptyState until a backend exists; remove no-op Save/Add handlers.
Verify after each module: tsc --noEmit = 0; demo mode renders; if a live API is available, dropdown shows DB values. Update ProjectOverview §22.20 table + GMAS-02 checklist per module. End with the Task Completion Block.
```

---

### P8 — Level 2 Mobile: GetMasters + State Cleanup
**Goal:** TASK-REACT-11..14. Also close the Level 2 parts of M10, M11 and M12.
**Depends on:** P3, P4, P6 · **Layer:** Mobile

**Status (2026-11-21):** Codex left this partial; **R6 completed it in source.**
- **State:** the listed screens moved to `useApiResource`.
- **Lookups:** no live use of `CATEGORY_LABELS`; finance categories come from GetMasters.
- **Emergency folder:** renders before file caching finishes.
- **Still open (LOW):** AddTask, SupportTickets and ContentManager still hold API data in `useState`.

**UNVERIFIED — visual check.**

**Tasks**
- [ ] Vault: `getMasters('DocumentCategory')`. Replace the exported `CATEGORY_LABELS` (`VaultRepository.ts:10`) used by VaultHomeScreen in live mode.
- [ ] Medical: `getMasters('HealthRecordType')`. Move `BLOOD_GROUPS`, `ALLERGY_CATEGORIES`, `CHRONIC_CONDITIONS` (`EditHealthProfileScreen.tsx:10-12`) into the repository as constants `[VERIFY]`.
- [ ] Safety: `getMasters('SafeZoneType')` replaces `ZONE_TYPES` (`AddEditSafeZoneScreen.tsx:10`). Move `FILTER_OPTIONS` into the repository.
- [ ] Finance: `getMasters('FinanceCategory')`.
- [ ] Reports:
  - Remove the hardcoded child ids: `ScoresReportsScreen.tsx:69, 76-78`, `AttendanceSummaryScreen.tsx:30`. Use the family's children from `GetMasters('ChildProfile')` or the family provider, with a child picker.
- [ ] `EmergencyFolderScreen.tsx`: show `DEMO_EMERGENCY_CARDS` only when `isDemo`. Remove `familyId ?? 'demo'` and show an error state if `familyId` is missing.
- [ ] Move API data from component `useState` into a feature Provider or `useReducer`, worst first: EditHealthProfile, DocumentUpload, DocumentDetail, EmergencyCard, WeeklyDigest, MonthlyReport, LocationSettings, then the family_admin and admin sub-screens (EmergencyAccessRules, FamilyManagement), and CreateSession. Form input state may stay in `useState`; only API response data moves.

**Done criteria:** `tsc` 0. Demo mode renders all 38 Level 2 screens with data. Live dropdowns show the seeded Level 2 lookups. A grep for `'c1'` in `src/features/reports` returns 0.

**Docs to update:** §22.20 Level 2 row, §12.6–§16.6.

**Codex Prompt**
```
Read CLAUDE.md (no useState for API data, no hardcoded data in screens, demo completeness), ModuleIndex.md, ProjectOverview.md §12.6, §13.6, §14.6, §15.6, §16.6, §22.20.
TASK: Phase P8 — Level 2 mobile integration (requires P3 seeds: DocumentCategory, HealthRecordType, SafeZoneType, FinanceCategory).
1. Add getMasters-backed lookup methods (with isDemo branch) to VaultRepository (DocumentCategory), MedicalRepository (HealthRecordType), SafetyRepository (SafeZoneType), FinanceRepository (FinanceCategory). Replace VaultRepository.ts:10 CATEGORY_LABELS usage in live mode; AddEditSafeZoneScreen.tsx:10 ZONE_TYPES → SafeZoneType.
2. EditHealthProfileScreen.tsx:10-12 BLOOD_GROUPS/ALLERGY_CATEGORIES/CHRONIC_CONDITIONS and LocationAlertHistoryScreen.tsx:21 FILTER_OPTIONS → typed constants in their repositories, marked // [VERIFY — candidate master data].
3. ScoresReportsScreen.tsx:69,76-78 and AttendanceSummaryScreen.tsx:30 hardcode child id 'c1' → load the family's children (GetMasters 'ChildProfile' via a repository method) and add a child selector.
4. EmergencyFolderScreen.tsx: DEMO_EMERGENCY_CARDS (L21, L95) only when AppConfig.isDemo; remove `familyId ?? 'demo'` (L59) and show FFErrorState when familyId is missing.
5. Move API response data out of component useState into a feature Provider or useReducer (form input state may stay): EditHealthProfileScreen, DocumentUploadScreen, DocumentDetailScreen, EmergencyCardScreen, WeeklyDigestScreen, MonthlyReportScreen, LocationSettingsScreen, EmergencyAccessRulesScreen, FamilyManagementScreen, CreateSessionScreen. Keep loading → success → error-with-retry states.
6. tsc --noEmit = 0; demo-render every touched screen; report un-rendered ones as UNVERIFIED. Update ProjectOverview §22.20 and the module §x.6 sections. End with the Task Completion Block.
```

---

### P9 — Emergency-only Offline
**Goal:** Meet business rules 13 and 14 (Emergency Folder and Emergency Card work offline) **without** a full offline app.
**Depends on:** P6 (public routes), P8 · **Layer:** Mobile · **Tier:** T2

**Tasks**
- [x] Add `vite-plugin-pwa` (pinned version) with the `injectManifest` or `generateSW` strategy. Precache the app shell plus **only** the routes and assets of `/vault/emergency` and the medical emergency-card screen and public share route.
- [x] Add an `EmergencyOfflineStore` in `src/core/cache/` that uses IndexedDB (via `idb`, pinned). It stores emergency folder document metadata and files (readable document URLs, 25 MiB total), plus the emergency card JSON per member. Refresh on every successful online load. Reuse or replace the unused `CacheService.ts`.
- [x] Repositories (`VaultRepository.getEmergencyDocuments`, `MedicalRepository.getEmergencyCard`): live mode tries the network first, saves to IndexedDB, and falls back to IndexedDB on a network error. The returned shape is unchanged.
- [x] Show the offline banner only when data really came from the cache, using `navigator.onLine` plus the fallback flag.
- [x] On logout, clear the emergency cache (PII). Document metadata, files, and card JSON are encrypted with a non-extractable browser-session AES-GCM key.
- [x] Add the missing manifest icons (`favicon.ico`, `logo192.png`, `logo512.png`) using FamilyFirst navy and gold.

**Done criteria:** Load the emergency folder online, switch DevTools to Offline, reload `/vault/emergency`, and documents still open. The same works for the emergency card. Other screens show a normal offline error (no offline support). Chrome DevTools reports no installability errors. Visually verified.

**Verified 2026-11-21:** Strict TypeScript and production build passed. Chrome DevTools verified the private folder and card reloaded offline, saved file bytes opened, a non-emergency route showed the offline error, and logout cleared IndexedDB and its key. Demo screens rendered from fixtures.
**Deployment [VERIFY]:** Browser checks used a local HTTP fixture. The production S3 object URL must permit GET by the browser and allow the app origin through CORS. The existing presigned upload URL grants PUT.

**Docs updated:** §12.8, §13.8, §20.4.

**Codex Prompt**
```
Read CLAUDE.md (business rules 13 and 14), ModuleIndex.md, ProjectOverview.md §12.8, §13.8, §20.4. Founder decision: offline ONLY for Emergency Folder + Emergency Card — do NOT make the whole app offline.
TASK: Phase P9 — emergency-only offline in Mobile/.
1. Add vite-plugin-pwa (exact pinned version) to vite.config.ts. Precache the app shell and only the chunks needed for /vault/emergency, the medical emergency-card screen and the public share routes. Use navigation fallback only for those routes.
2. Create src/core/cache/EmergencyOfflineStore.ts using IndexedDB (idb, pinned): store emergency document metadata + file blobs (cap 25 MB total, evict oldest) and emergency card JSON per member. Remove or repurpose the unused src/core/cache/CacheService.ts.
3. VaultRepository.getEmergencyDocuments and MedicalRepository.getEmergencyCard: live mode = network first → save to store → on network failure return cached data with a fromCache flag. Response shape unchanged. Demo mode unchanged.
4. EmergencyFolderScreen / EmergencyCardScreen: show the offline banner only when fromCache is true.
5. Clear the emergency store on logout (AuthContext logout path).
6. Add missing public/ icons referenced by manifest.json (favicon.ico, logo192.png, logo512.png) in FamilyFirst navy #1A2E4A / gold #C8922A.
7. tsc --noEmit = 0. Verify in the browser: load online → DevTools Offline → reload /vault/emergency and the emergency card → data shown. If you cannot run a browser, report UNVERIFIED. Update ProjectOverview §12.8, §13.8, §20.4. End with the Task Completion Block.
```

---

### P10 — Deferred Features
**Goal:** Build the deferred items listed in ProjectOverview §15.9, §16.8 and §17.9, plus the missing admin backend.
**Depends on:** P4, P8 · **Layer:** Full stack · **Tier:** T2/T3. **Run as 4 separate Codex runs: 10A–10D.**

**Status (2026-11-21):**
- **10A PDF export:** built, then hardened in R5 (canonical URL persisted, `Failed` rows, required dependencies).
- **10B / 10C / 10D:** **not built — deferred past P13** by founder decision on 2026-11-21. See ProjectOverview §16.8.

| Sub | Scope | Files |
|---|---|---|
| 10A | **PDF export:** add QuestPDF (pinned; check the Community license fits); `ReportExport` entity + configuration + DbSet mapped to `tblReportExport` (script 058); implement `ReportService.ExportReportAsync` (upload to S3, record a row, return a presigned URL); Mobile export button on MonthlyReport | ReportService.cs, new Entities/ReportExport.cs, ReportsRepository.ts |
| 10B | **Advanced admin configs:** AC-03 Document Category config (table + endpoint + screen); AC-05 Safe Zone default radius (extend safety-config); AC-07 Report automation config; Escalation config; AC-09 campaign targeting by module adoption (extend `NotificationCampaignRequest`). Check the exact shapes against `FamilyFirst_Level2_ProductDocument.docx` first: `[VERIFY]`. | FamilyAdminController, AdminController, family_admin screens |
| 10C | **Finance workers:** monthly consent reminder (reads `FinanceConsents.LastReminderSentAt`); 30-day grace hard-purge for opted-out rows. Both are BackgroundServices registered in DI (append only). | Infrastructure/Data/BackgroundServices |
| 10D | **Admin SupportTickets + Content Manager backend:** `[VERIFY]` that they're in the L1 spec (`FamilyFirst_L1_TechSpec.docx`) **before** building. If they aren't in the spec, hide those screens instead. | AdminController, AdminRepository.ts |

**Done criteria (each):** `dotnet build` 0/0, `tsc` 0, scripts run with no FK errors, Postman checks, screens render in demo and live.

**Codex Prompt (template; run once per sub-phase)**
```
Read CLAUDE.md, ModuleIndex.md, New API Format.txt, New SQL Format.txt, and ProjectOverview.md §15.9, §16.8, §17.9 (and §11 for 10D).
TASK: Phase P10<A|B|C|D> — <paste the Scope cell from the P10 table>.
Before coding: if any request/response shape, table column or business rule is not documented in ProjectOverview.md, read ONLY the relevant source doc
(Level 2 → API/Docs/Source/FamilyFirst_Level2_ProductDocument.docx; L1 admin → FamilyFirst_L1_TechSpec.docx), state the gap, and record the confirmed contract in ProjectOverview.md BEFORE implementing. If the spec does not define it, STOP and report — do not invent.
Rules: Clean Architecture (Domain→Application→Infrastructure→API); controllers use ApiResponse<T>; services apply IApiLogService/IPermissionService/IErrorCodeService/IMasterDataResolver; SQL scripts continue numbering from the last script; DI append-only; Mobile repository with isDemo branch + MasterApiReference entry.
Verify: dotnet build 0/0, tsc --noEmit 0, scripts run cleanly, Postman primary endpoint, screen rendered (else UNVERIFIED). Update the matching ProjectOverview sections to stable-contract level and remove the item from the Deferred table. End with the Task Completion Block.
```

---

### P11 — Cache Warmup + Regex from DB (Founder rules 3 and 5)
**Goal:** Load error codes, master data, role permissions and field regexes into `IMemoryCache` at startup, so the cross-cutting services stop hitting the DB on every call.
**Depends on:** P4 · **Layer:** Backend · **Priority:** LOW

**Tasks**
- [x] `CacheWarmupService : IHostedService` loads `tblErrorCode`, `tblRolePermission`, the static `tblMasterData` lookups and `tblRegularExpression` + `tblAPIMethod`. Cache keys follow `Flow_Change_DB_GapAnalysis.md` §7.3.
- [x] `ErrorCodeService` and `PermissionService` read from the cache first and fall back to the stored procedure. Add an invalidation method for admin edits.
- [x] Seed `tblRegularExpression` for the phone, PIN (4 digits), OTP (6 digits `[VERIFY]`) and name fields used by existing FluentValidation validators. Add `IRegexRuleProvider`. Validators use it, with the current hardcoded pattern as a fallback.

**Done criteria:** `dotnet build` 0/0. Startup log shows the cache counts. After warmup, an error-code lookup triggers no DB call (check with SQL Profiler or logging).

**Verified 2026-11-21:** Solution build completed with zero errors and warnings. Fresh LocalDB ran all 118 scripts. Startup reported 41 error codes, 119 role permissions, 10 API methods, 15 regex rules, and 17 static master lookups (103 rows). Cache hit and invalidation checks passed. Local HTTP checks returned 200 for authenticated GetMasters and 400 for invalid OTP and PIN inputs.

**Codex Prompt**
```
Read CLAUDE.md, ModuleIndex.md, New API Format.txt, New SQL Format.txt, API/Docs/Task/Flow_Change_Analysis.md Rules 3 and 5, Flow_Change_DB_GapAnalysis.md §4.9, §4.10, §7.3.
TASK: Phase P11 — startup cache + DB-driven regex.
1. Add CacheWarmupService (IHostedService, Infrastructure) loading tblErrorCode, tblRolePermission, static tblMasterData lookups, tblRegularExpression + tblAPIMethod into IMemoryCache using keys from Flow_Change_DB_GapAnalysis §7.3. Register in DependencyInjection.cs (append only).
2. ErrorCodeService and PermissionService: cache-first, SP fallback; add InvalidateAsync for admin edits.
3. Script <next>_SeedRegularExpressions.sql (New SQL Format) for the fields validated today by FluentValidation (phone, PIN 4-digit, OTP, names) — take the exact patterns from the existing validators. Add IRegexRuleProvider; validators read patterns from it with the existing pattern as fallback.
4. dotnet build 0/0; log cache counts at startup. Update ProjectOverview §1.3/§19 with the cache rule. End with the Task Completion Block.
```

---

### P12 — Integrations Go-Live — BLOCKED (needs credentials from founder)
**Blocked on:** a Firebase project with a service-account JSON and web config, an MSG91 auth key with an approved template ID, and an AWS S3 bucket in ap-south-1 with an IAM user.

**Preparation verified 2026-11-21.** The API supports external configuration and Development
user-secrets. Mobile reads Firebase web settings from `VITE_FIREBASE_*`, registers through the
existing authenticated token endpoint, and activates the push worker alongside the emergency
worker. Chrome verified the permission-denial UI with fixture settings. Local provider tests
verified the MSG91 rejection response and FCM authentication failure. Live credentials and
provider end-to-end results are pending.

**Tasks once unblocked**
- [ ] Backend: put the secrets in user-secrets or environment variables, **never** in `appsettings.json`.
- [ ] Mobile: real Firebase config through `VITE_FIREBASE_*` env vars in `FCMService.ts` (it's `PLACEHOLDER` today). Add `public/firebase-messaging-sw.js`.
- [ ] End-to-end checks:
  - OTP SMS arrives within 30s
  - A push arrives on attendance submit
  - Photo upload via a presigned URL succeeds
  - Push and SMS failures are retried or logged, never silently lost

**Codex Prompt**
```
Read CLAUDE.md (Integrations Engineer rules), ProjectOverview.md §1.7 and §10.
TASK: Phase P12 — integrations go-live. Credentials are provided via environment variables / user-secrets ONLY (never commit them).
1. Backend: bind MSG91 (Otp:ApiKey, Otp:TemplateId), FCM (Fcm:ProjectId, Fcm:ServiceAccountFilePath), S3 (bucket, region ap-south-1) from env/user-secrets; startup validation from P2 must pass.
2. Mobile: replace the PLACEHOLDER Firebase config in src/core/notifications/FCMService.ts with VITE_FIREBASE_* env vars; add public/firebase-messaging-sw.js; register the token via the apiClient route added in P6.
3. Confirm retries/logging on provider failure (no silent drops).
4. Report a manual E2E checklist: OTP SMS received, push received on attendance submit, S3 photo upload. Update ProjectOverview §1.7. End with the Task Completion Block.
```

---

### R1–R8 — Post-Codex Review Fix Phases (Claude, 2026-11-21)
A phase-wise code review of P1–P12 found defects that were then fixed before P13. Full records are in ProjectOverview
§21 (Drift Entries 079–083), §2.4, §6.4, §12.4, §13.4, §14.4, §16.4/§16.8, §20.3/§20.5, §22.20 and §23.5–23.6.

| Phase | Scope | Verification |
|---|---|---|
| R1 | Remaining runtime 500s: EF alias/computed properties, the LocationAlert.ZoneId column, worker Includes, SafetyWorker IST window | LocalDB + HTTP + worker run; SQL-backed schema-parity and repository read-sweep tests |
| R2 | Auth: PIN/password lockout (script 120), per-IP limiter, Child/Elder-only PIN, keyed OTP phone hash, dev JWT key, OTP purge (script 121) | LocalDB + HTTP |
| R3 | Level 2 exposure: vault share link, private-bucket presign, FileUrl prefix, Elder/Child medical shaping, location consent, atomic SOS, report access, DB View checks | LocalDB + HTTP |
| R4 | Mobile: real task photo upload, vault upload check, route guards, refresh-failure handling, familyId/childProfileId from JWT claims | Live-mode browser harness and a real device (partial; founder took over testing) |
| R5 | FCM credential cache and retry, HTTP timeouts, MSG91 header, report export hardening, cache warmup refresh, shared S3 client, vaccination reminder dedupe | build only |
| R6 | Finish P7/P8 screens, NotificationRules real contract, Analytics honesty, CalendarEventType seed (script 122) | `tsc` only |
| R7 | P10B/C/D deferred | n/a |
| R8 | Hygiene (dependencies, precache, logger, purge, FK mapping, headers) | build + `tsc` |

**Demo mode removed (founder decision, 2026-11-21).**
- **Mobile:** all `AppConfig.isDemo` branches, mock data, `DemoMasterData.ts`, the demo fixture and the
  `VITE_IS_DEMO` flag are gone. The app always uses the live API; master data comes from GetMasters (DB, unchanged).
  The three demo-only features (child PIN change, elder voice note, emergency contacts) are hidden and PENDING.
- **Standards:** CLAUDE.md and UIStandertds.md now carry "Live Data Only" rules.
- **Verified:** `tsc` = 0 and `npm run build` passes. Runtime testing is founder-owned. See ProjectOverview §20.4
  and Drift Entry 084.

**Testing rule (founder, 2026-11-21):** before any runtime, local or device testing, ask whether Claude or the founder
runs it. Everything since R4 is **UNVERIFIED — needs founder runtime/visual check**.

### P13 — Quality Gates, Tests, Visual Verification & Doc Sync
**Goal:** Add the gates that are missing (B15, M18, D2, D3) and move modules from "Unverified" to "Done".

**Status (2026-11-21). Testing is founder-owned; nothing below has been run by Claude.**
- **13A (test code written):** `FamilyFirst.Tests` gained:
  - `AuthSecurityTests` (PIN/password lockout, Child/Elder-only PIN, inactive users, PinHasher);
  - `S3ObjectUrlTests` (vault and task URL prefix rules, presigned reads);
  - from R1: `SqlEfContractTests` (schema parity, query translation, late-alert window) and
    `SqlRepositoryReadSweepTests`;
  - from R2: OTP purge in `SqlServerRulesTests`.

  The solution builds 0/0. SQL-backed tests need `FAMILYFIRST_TEST_DATABASE=FamilyFirstP13A_yyyyMMddHHmmss`
  (LocalDB built by `RunAllScripts.ps1`). **Run:** `dotnet test API/FamilyFirst.Tests`.
- **13B (tooling only):** Vitest 3.2.4, jsdom, React Testing Library and ESLint 9 (typescript-eslint, react-hooks;
  `no-explicit-any`, `no-console`) are installed with `vitest.config.ts`, `src/test/setup.ts` and
  `eslint.config.js`. Scripts: `npm test` (`vitest run --passWithNoTests`), `npm run lint` (`eslint src`),
  `npm run typecheck`. No app test files were written (founder decision). **Lint has never been run, so expect
  findings on first run.**
- **13C:** founder-owned screen walk (demo and live) and `AllUIScreens.md` statuses.
  `LIVE_API_INTEGRATION_CHECKLIST.md` is still stale.
**Depends on:** all phases · **Layer:** QA. **Run as 13A (backend tests), 13B (mobile tooling and tests), 13C (visual check and docs).**

**Tasks**
- [x] 13A: `API/FamilyFirst.Tests` (xUnit + FluentAssertions; EF InMemory or SQLite). Cover the business rules from CLAUDE.md:
  - OTP 5-minute expiry and 3 per hour
  - Attendance 1-hour edit window, plus a duplicate submit returning 409
  - Feedback 24-hour window
  - Coin earn/spend under concurrency (RowVersion)
  - Redemption idempotency, plus insufficient coins returning 422
  - Photo-required task completion
  - FamilyId isolation for every `GetByIdForFamilyAsync`
- [ ] 13B: Vitest + React Testing Library. Repository demo/live shape-parity tests: each demo mock must satisfy the live DTO type. Add ESLint (typescript-eslint, react-hooks) and a `lint` script that is real ESLint.
- [ ] 13C: Rewrite `Mobile/LIVE_API_INTEGRATION_CHECKLIST.md` from `MasterApiReference.ts` (real endpoints only). Walk all 85 screens in `AllUIScreens.md` in demo **and** live mode and mark each Verified or Issue. Update `AllUIScreens.md` statuses. Bring ProjectOverview §22.30 up to date.

**Done criteria:** `dotnet test` green, `npm test` green, `npm run lint` 0 errors, every screen in AllUIScreens.md has a verified status, and ProjectOverview has no stale BLOCKING or PENDING marks.

**Codex Prompt (template)**
```
Read CLAUDE.md (QA gate, §5a visual verification), ModuleIndex.md, ProjectOverview.md §22.30, API/Docs/Task/AllUIScreens.md.
TASK: Phase P13<A|B|C>.
13A: create API/FamilyFirst.Tests (xUnit, FluentAssertions), add to FamilyFirst.sln, and write tests for: OTP 5-min expiry + 3/hr limit; attendance 1-hour edit window + 409 on duplicate submit; feedback 24-hour window; coin spend concurrency (RowVersion); redemption idempotency + 422 insufficient coins; photo-required completion; FamilyId isolation of GetByIdForFamilyAsync. `dotnet test` must pass.
13B: add Vitest + React Testing Library + ESLint (typescript-eslint, eslint-plugin-react-hooks) to Mobile/; make `lint` run ESLint and `typecheck` run tsc; write demo/live shape-parity tests per repository (demo return value must satisfy the live DTO type). All green.
13C: rewrite Mobile/LIVE_API_INTEGRATION_CHECKLIST.md from src/core/api/MasterApiReference.ts (only real endpoints); walk all 85 screens in AllUIScreens.md in demo and live mode, record Verified/Issue per screen with notes; update ProjectOverview §22.30 to the true current state.
Never mark UI verified without rendering it. End with the Task Completion Block.
```

---

## PART C — PARKED (not in any phase)

| Item | Reason | Revisit when |
|---|---|---|
| Subscriptions / plan screens / plan-limit UX | Founder decision: keep hidden (`subscriptionEnabled=false`); founder notes say remove | Founder decides whether to monetise |
| Full move of data access to stored procedures (§19.8.7) | Large rewrite (~96% EF LINQ); New SQL Format says SPs only | After P13, as a separate initiative |
| FamilyLedger Android SDK, WhatsApp Business API, SMS parser + `POST /finance/transactions` ingest | External dependencies; separate companion app | SDK and WhatsApp approval available |
| Angular admin portal | Doesn't exist; all admin screens are in the React PWA | Founder makes a T3 decision |
| Full-app offline PWA | Founder decision: emergency-only | — |
| Script 087 `uspGetMasterDataSpNameByCode` | Not referenced by code | Only if the generic search needs it |

---

## PART D — PER-PHASE CHECKLIST (copy into each Codex run review)

```
[ ] Intent met exactly as the phase describes — nothing extra
[ ] No route / repository method / API route renamed or removed
[ ] Clean Architecture respected; ApiResponse<T> everywhere
[ ] FamilyId scope + IsDeleted honoured on every new query
[ ] New SQL follows New SQL Format.txt; numbering continues; idempotent
[ ] dotnet build 0 errors 0 warnings  |  tsc --noEmit 0 errors
[ ] Demo mode: touched screens render meaningful data (no blank, no endless spinner)
[ ] UI actually rendered — otherwise reported as UNVERIFIED
[ ] ProjectOverview.md updated per rules; Task Completion Block present
```

*End of audit — FamilyFirst_Audit_Codex_Plan.md — 2026-11-20*
