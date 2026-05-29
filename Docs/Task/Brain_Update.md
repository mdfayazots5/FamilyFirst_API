You are the Project AI Engineer for FamilyFirst.

Your task is to rephase and restructure the existing ProjectOverview.md and ModuleIndex.md
files located at:

  C:\Live\FamilyFirst\API\Docs\Flow\ProjectOverview.md
  C:\Live\FamilyFirst\API\Docs\Flow\ModuleIndex.md

Do not create new files. Read the existing files first, then restructure them in place
following all rules defined in CLAUDE.md.

---

STEP 1 — READ EXISTING FILES FIRST

Read both files completely before making any changes:
  - C:\Live\FamilyFirst\API\Docs\Flow\ProjectOverview.md
  - C:\Live\FamilyFirst\API\Docs\Flow\ModuleIndex.md

Identify:
  - What sections already exist
  - What is well-documented vs incomplete
  - What is outdated, vague, duplicated, or task-specific
  - What is missing that should be there based on CLAUDE.md structure

Do not modify anything yet. Build a clear picture first.

---

STEP 2 — READ SUPPORTING SPEC FILES (only if gaps exist after Step 1)

If the existing ProjectOverview.md is missing confirmed information for any module,
read the relevant spec file to fill the gap. Read one file at a time.

Available spec files (in order of priority):
  C:\Live\FamilyFirst\API\Docs\Rules\FamilyFirst_L1_TechSpec.docx
  C:\Live\FamilyFirst\API\Docs\Rules\FamilyFirst_Level1_ProductDocument.docx
  C:\Live\FamilyFirst\API\Docs\Rules\FamilyFirst_L1_Codex_DevPlan.docx
  C:\Live\FamilyFirst\API\Docs\Rules\FamilyFirst_Flutter_AI_Studio_DevPlan.docx
  C:\Live\FamilyFirst\API\Docs\Rules\FamilyFirst_Level2_ProductDocument.docx

Before reading any spec file, state:
"Gap confirmed in ProjectOverview.md for [module/section]. Reading: [filename]. Will update after."

After reading each file, extract only what is confirmed and reusable, then update
ProjectOverview.md before reading the next file.

---

STEP 3 — REPHASE ProjectOverview.md

Rewrite the file in place using exactly this section structure.
Preserve all existing valid content — restructure it, do not delete it.
Remove anything that is task-specific, temporary, duplicated, or unverified.
Add [VERIFY] marker to any field that could not be confirmed from available sources.

TARGET STRUCTURE:

# FamilyFirst — Project Overview
Version: 2.0 | Status: Active | Last Updated: [today's date]

---

## 1. Project Architecture

### 1.1 Platform Stack
### 1.2 Solution Structure (folder tree — key paths only)
### 1.3 API Conventions (base URL, response envelope, pagination, status codes)
### 1.4 Database Standards (naming, data types, audit columns, script rules)
### 1.5 Flutter App Architecture (state, navigation, demo mode, folder structure)
### 1.6 Angular Admin Architecture (structure, lazy loading, guards)
### 1.7 External Services & Configuration (JWT, OTP, FCM, S3, connection strings)

---

## 2. Authentication & Session

### 2.1 Module Purpose
### 2.2 Key APIs
  Each API entry must include:
  - Method + Path
  - Request DTO (field, type, required/optional, constraints)
  - Response DTO (fields returned on success)
  - Auth required: YES / NO
  - Rate limit (if any)
  - Business rules
  - Error cases (condition → status code)

### 2.3 DB Tables
  Each table entry must include:
  - Table name
  - Key columns used by this module
  - Indexes relevant to this module

### 2.4 Business Rules
  Each rule must be specific — field name, limit, condition, error code.
  No vague rules like "validates input" or "checks permission".

### 2.5 Flow Summaries
  Format: Trigger → API call → Validation → DB operation → Response → Side effects
  One flow per named user action.

### 2.6 Flutter Integration
  - Screen(s) that use this module
  - MockDataService method names for demo mode
  - Known integration notes

### 2.7 Dependencies
  Other modules or external services this module depends on.

---

## 3. Family & User Management
  [same subsection structure as Section 2]

---

## 4. Family Dashboard
  [same subsection structure as Section 2]

---

## 5. Attendance System
  [same subsection structure as Section 2]

---

## 6. Task & Routine System
  [same subsection structure as Section 2]

---

## 7. Teacher Feedback
  [same subsection structure as Section 2]

---

## 8. Rewards & Coins
  [same subsection structure as Section 2]

---

## 9. Family Calendar
  [same subsection structure as Section 2]

---

## 10. Notification Engine
  [same subsection structure as Section 2]

---

## 11. Admin Configuration
  [same subsection structure as Section 2]

---

## 12. Level 2 — Document Vault
  [same subsection structure as Section 2]
  Additional subsection:
  ### 12.8 Offline Behavior
    - What is cached locally
    - What must work without internet
    - Cache invalidation rules

---

## 13. Level 2 — Medical & Health Records
  [same subsection structure as Section 2]
  Additional subsection:
  ### 13.8 Emergency Card Behavior
    - Offline access rules
    - Share link behavior
    - PIN vs no-login access

---

## 14. Level 2 — Safety, Location & Emergency
  [same subsection structure as Section 2]

---

## 15. Level 2 — Family Finance & SMS Ledger
  [same subsection structure as Section 2]
  Additional subsection:
  ### 15.8 Privacy Tier Rules
    - Tier definitions
    - What each tier exposes
    - Consent flow requirements

---

## 16. Level 2 — Reports & Insights
  [same subsection structure as Section 2]

---

## 17. Level 2 — Advanced Admin Configuration
  [same subsection structure as Section 2]

---

## 18. Role & Permission Reference

### 18.1 Role Definitions (Int value, who, daily time, emotional goal)
### 18.2 Role-wise Data Scope Rules
### 18.3 API Endpoint Authorization Matrix
### 18.4 Row-Level Security Rules

---

## 19. Database Standards & Shared Patterns

### 19.1 Naming Conventions (tables, columns, indexes, scripts)
### 19.2 Mandatory Audit Columns
### 19.3 Soft Delete Pattern
### 19.4 BaseEntity Definition
### 19.5 Common Query Patterns (pagination, soft-delete filter, family scope filter)
### 19.6 Transaction Patterns (coin operations, redemption idempotency)

---

## 20. Flutter App Architecture

### 20.1 Project Structure
### 20.2 State Management (Riverpod — AuthNotifier, feature StateNotifiers)
### 20.3 Navigation (GoRouter — route map, guards, role redirects)
### 20.4 Demo vs Live Mode (AppConfig.isDemo, MockDataService, Repository pattern)
### 20.5 API Client (Dio setup, interceptors, token refresh)
### 20.6 Design System (colors, typography, spacing, shared widgets)
### 20.7 Screen-to-API Master Reference

---

## 21. Known Drift & Resolved Issues

Format for each entry:
  - Module: [module name]
  - Drift Type: [request / SQL / DB / stale docs / demo-live / other]
  - What drifted: [specific description]
  - How resolved: [specific fix applied]
  - Recurrence risk: HIGH / LOW
  - Date resolved: [date]

Only document issues with HIGH recurrence risk or that caused repeated failures.
Do not document one-off bugs or minor fixes.

---

DOCUMENTATION QUALITY RULES — APPLY TO EVERY SECTION:

1. Every API entry must have a complete request DTO and response DTO.
   If a field cannot be confirmed → mark [VERIFY].

2. Every business rule must be specific.
   BAD:  "Validates the OTP"
   GOOD: "OTP valid for 5 minutes. Max 3 requests per phone per hour.
          Expired OTP returns 400 with code: OTP_EXPIRED."

3. Every flow summary must follow the format:
   Trigger → API call → Validation → DB operation → Response → Side effects
   BAD:  "User logs in with OTP"
   GOOD: "User submits phone number →
          POST /api/v1/auth/send-otp →
          Validates E.164 format, checks rate limit (3/hr/phone) →
          Inserts OTP record in OtpTokens table (expires GETUTCDATE()+5min) →
          Returns 200 { OtpToken } →
          Side effect: MSG91 SMS dispatched"

4. Every DB table entry must name the table and the key columns used by that module.
   Do not just say "uses Users table" — list which columns are read/written.

5. No vague dependency statements.
   BAD:  "Depends on auth module"
   GOOD: "Requires valid JWT. Claims used: UserId, FamilyId, Role.
          FamilyMember record must exist with IsDeleted = 0."

6. If a subsection has no confirmed data → write:
   "[VERIFY] — No confirmed data available. Read [filename] to populate."
   Do not leave any subsection blank.

---

STEP 4 — REPHASE ModuleIndex.md

Rewrite ModuleIndex.md as a clean navigation map for ProjectOverview.md.

TARGET STRUCTURE:

# FamilyFirst — Module Index
Version: 2.0 | Maps to: ProjectOverview.md v2.0 | Last Updated: [today's date]

## Purpose
This file is a navigation map only. It maps keywords to sections in ProjectOverview.md.
It is not a logic source. Never use this file to make implementation decisions.

---

## Keyword → Section Map

| Keyword / Topic | ProjectOverview.md Section | Subsection |
|---|---|---|
| login, OTP, PIN, JWT, refresh token, auth | Section 2 | 2.2 Key APIs |
| OTP rate limit, token expiry | Section 2 | 2.4 Business Rules |
| auth flow, login flow | Section 2 | 2.5 Flow Summaries |
| family creation, family setup | Section 3 | 3.2 Key APIs |
| add member, invite member, roles | Section 3 | 3.2 Key APIs |
| family scope, row-level security | Section 18 | 18.4 Row-Level Security |
| dashboard, parent home, family summary | Section 4 | 4.2 Key APIs |
| attendance, session, mark present | Section 5 | 5.2 Key APIs |
| attendance edit window, correction | Section 5 | 5.4 Business Rules |
| attendance flow | Section 5 | 5.5 Flow Summaries |
| task, routine, daily tasks | Section 6 | 6.2 Key APIs |
| task photo, photo proof, completion | Section 6 | 6.4 Business Rules |
| task flow, completion flow | Section 6 | 6.5 Flow Summaries |
| feedback, teacher feedback, observation | Section 7 | 7.2 Key APIs |
| feedback edit window, delete window | Section 7 | 7.4 Business Rules |
| coins, rewards, coin earn, coin spend | Section 8 | 8.2 Key APIs |
| coin transaction, coin safety | Section 8 | 8.4 Business Rules |
| reward redemption, redeem flow | Section 8 | 8.5 Flow Summaries |
| calendar, family event, event creation | Section 9 | 9.2 Key APIs |
| notification, push, FCM, quiet hours | Section 10 | 10.2 Key APIs |
| plan, subscription, plan limits | Section 11 | 11.4 Business Rules |
| admin config, templates, flags | Section 11 | 11.2 Key APIs |
| document vault, documents, upload | Section 12 | 12.2 Key APIs |
| offline documents, emergency folder | Section 12 | 12.8 Offline Behavior |
| medical records, health profile | Section 13 | 13.2 Key APIs |
| emergency card, share link | Section 13 | 13.8 Emergency Card Behavior |
| safety, location, safe zone, SOS | Section 14 | 14.2 Key APIs |
| finance, SMS ledger, transactions | Section 15 | 15.2 Key APIs |
| finance privacy, consent, privacy tier | Section 15 | 15.8 Privacy Tier Rules |
| reports, weekly digest, insights | Section 16 | 16.2 Key APIs |
| storage config, Google Drive, S3 | Section 17 | 17.2 Key APIs |
| roles, permissions, role matrix | Section 18 | 18.1–18.4 |
| SuperAdmin, FamilyAdmin, Parent, Child, Teacher, Elder | Section 18 | 18.1 Role Definitions |
| database, SQL, table, column, index | Section 19 | 19.1–19.6 |
| BaseEntity, audit columns, soft delete | Section 19 | 19.3–19.4 |
| Flutter, Riverpod, GoRouter, Dio | Section 20 | 20.1–20.7 |
| demo mode, MockDataService, AppConfig | Section 20 | 20.4 Demo vs Live Mode |
| design system, colors, typography | Section 20 | 20.6 Design System |
| drift, regression, broken flow | Section 21 | 21 Known Drift |

---

## Module → Controller Map

| Module | Backend Controller | Flutter Feature Folder | Build Phase |
|---|---|---|---|
| Authentication | AuthController | lib/features/auth/ | Backend Ph02 · Flutter Ph02 |
| Family & User Management | FamiliesController, UsersController | lib/features/family/ | Backend Ph03–05 · Flutter Ph03–04 |
| Family Dashboard | FamiliesController | lib/features/dashboard/ | Backend Ph05 · Flutter Ph05 |
| Attendance | AttendanceController | lib/features/attendance/ | Backend Ph06–07 · Flutter Ph06–07 |
| Tasks & Routines | TasksController | lib/features/tasks/ | Backend Ph08–09 · Flutter Ph08–09 |
| Teacher Feedback | FeedbackController | lib/features/feedback/ | Backend Ph11–12 · Flutter Ph11–12 |
| Rewards & Coins | RewardsController | lib/features/rewards/ | Backend Ph10,13–14 · Flutter Ph10,13 |
| Family Calendar | CalendarController | lib/features/calendar/ | Backend Ph15 · Flutter Ph14–15 |
| Notifications | NotificationsController | lib/features/notifications/ | Backend Ph16–17 · Flutter Ph17 |
| Admin Configuration | AdminController | lib/features/admin/ | Backend Ph19–20 · Flutter Ph19–20 |
| Document Vault (L2) | DocumentVaultController | lib/features/vault/ | L2 Priority 1 |
| Medical Records (L2) | MedicalController | lib/features/medical/ | L2 Priority 2 |
| Safety & Location (L2) | SafetyController | lib/features/safety/ | L2 Priority 3 |
| Reports & Insights (L2) | ReportsController | lib/features/reports/ | L2 Priority 4 |
| Family Finance (L2) | FinanceController | lib/features/finance/ | L2 Priority 5 |
| Advanced Admin (L2) | AdminController (extended) | lib/features/admin/ | L2 — alongside modules |

---

## Section → File Reference Map

| ProjectOverview Section | Primary Source File |
|---|---|
| Section 1 — Architecture | FamilyFirst_L1_TechSpec.docx |
| Sections 2–11 — Level 1 Modules | FamilyFirst_L1_TechSpec.docx + FamilyFirst_L1_Codex_DevPlan.docx |
| Sections 2–11 — Flutter details | FamilyFirst_Flutter_AI_Studio_DevPlan.docx |
| Sections 12–17 — Level 2 Modules | FamilyFirst_Level2_ProductDocument.docx |
| Section 18 — Roles & Permissions | FamilyFirst_Level1_ProductDocument.docx + FamilyFirst_L1_TechSpec.docx |
| Section 19 — DB Standards | FamilyFirst_L1_TechSpec.docx |
| Section 20 — Flutter Architecture | FamilyFirst_Flutter_AI_Studio_DevPlan.docx |
| Section 21 — Known Drift | Populated during development — not from spec files |

---

EXECUTION RULES:

1. Complete Step 1 (read existing files) before making any edits.
2. Preserve all existing valid content from the current files — restructure, do not delete.
3. Every section must have content or a [VERIFY] marker — no blank sections.
4. Apply documentation quality rules to every module section without exception.
5. After completing both files, output the Task Completion Block from CLAUDE.md.
6. Do not modify any file outside of:
   C:\Live\FamilyFirst\API\Docs\Flow\ProjectOverview.md
   C:\Live\FamilyFirst\API\Docs\Flow\ModuleIndex.md