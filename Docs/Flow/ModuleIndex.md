# FamilyFirst — Module Index
Version: 2.0 | Maps to: ProjectOverview.md v2.0 | Last Updated: 2026-05-30

## Purpose
This file is a navigation map only. It maps keywords to sections in ProjectOverview.md.
It is not a logic source. Never use this file to make implementation decisions.

---

## Keyword → Section Map

| Keyword / Topic | ProjectOverview.md Section | Subsection |
|---|---|---|
| login, OTP, PIN, JWT, refresh token, auth | Section 2 | 2.2 Key APIs |
| OTP rate limit, token expiry | Section 2 | 2.4 Business Rules |
| auth flow, login flow, PIN flow | Section 2 | 2.5 Flow Summaries |
| family creation, family setup | Section 3 | 3.2 Key APIs |
| add member, invite member, join code | Section 3 | 3.2 Key APIs |
| family scope, row-level security | Section 18 | 18.4 Row-Level Security |
| dashboard, parent home, family summary, FamilyScore | Section 4 | 4.2 Key APIs |
| attendance, session, mark present, mark absent | Section 5 | 5.2 Key APIs |
| attendance edit window, correction, 1-hour rule | Section 5 | 5.4 Business Rules |
| attendance flow, submit session | Section 5 | 5.5 Flow Summaries |
| comment templates | Section 5 | 5.2 Key APIs |
| task, routine, daily tasks, task CRUD | Section 6 | 6.2 Key APIs |
| task photo, photo proof, photo required, IsPhotoRequired | Section 6 | 6.4 Business Rules |
| task completion, approval, verification queue | Section 6 | 6.5 Flow Summaries |
| feedback, teacher feedback, observation, weekly summary | Section 7 | 7.2 Key APIs |
| feedback edit window, 24-hour rule | Section 7 | 7.4 Business Rules |
| coins, rewards, coin earn, coin spend, streak | Section 8 | 8.2 Key APIs |
| coin transaction, RowVersion, optimistic concurrency | Section 8 | 8.4 Business Rules |
| reward redemption, redeem flow, duplicate redemption | Section 8 | 8.5 Flow Summaries |
| calendar, family event, event creation, recurrence | Section 9 | 9.2 Key APIs |
| reminder delivery, quiet hours bypass, birthday event | Section 9 | 9.4 Business Rules |
| notification, push, FCM, notification preferences | Section 10 | 10.2 Key APIs |
| quiet hours, weekly digest, morning digest, evening digest | Section 10 | 10.4 Business Rules |
| plan, subscription, plan limits, feature flags | Section 11 | 11.4 Business Rules |
| SuperAdmin panel, family block, analytics | Section 11 | 11.2 Key APIs |
| module visibility, FamilyAdmin panel, custom status | Section 11 | 11.2 Key APIs |
| document vault, documents, upload, expiry | Section 12 | 12.2 Key APIs |
| offline documents, emergency folder, vault offline | Section 12 | 12.8 Offline Behavior |
| medical records, health profile, vaccination, prescription | Section 13 | 13.2 Key APIs |
| emergency card, share link, no-login access | Section 13 | 13.8 Emergency Card Behavior |
| safety, location, safe zone, SOS, geofence | Section 14 | 14.2 Key APIs |
| finance, SMS ledger, transactions, budget | Section 15 | 15.2 Key APIs |
| finance privacy, consent, privacy tier, CFO | Section 15 | 15.8 Privacy Tier Rules |
| reports, weekly digest, monthly report, insights | Section 16 | 16.2 Key APIs |
| storage config, Google Drive, S3, hybrid routing | Section 17 | 17.2 Key APIs |
| roles, permissions, role matrix, authorization | Section 18 | 18.1–18.4 |
| SuperAdmin, FamilyAdmin, Parent, Child, Teacher, Elder | Section 18 | 18.1 Role Definitions |
| database, SQL, table, column, index, script | Section 19 | 19.1–19.6 |
| BaseEntity, audit columns, soft delete, GUID PK | Section 19 | 19.3–19.4 |
| React, Context API, React Router, Axios, Tailwind | Section 20 | 20.1–20.7 |
| demo mode, AppConfig.isDemo, mock data, repository pattern | Section 20 | 20.4 Demo vs Live Mode |
| design system, colors, typography, shared components | Section 20 | 20.6 Design System |
| drift, regression, broken flow, known issues | Section 21 | 21 Known Drift |
| pending tasks, implementation tasks, cross-cutting, GUID contract, GetDataBySearch, GetDataByCode | Section 22 | 22.0–22.21 |
| GetMasters, get master data, dropdown integration, master data API, remove hardcoded arrays | Section 22 | 22.20 |

---

## Module → Controller Map

| Module | Backend Controller | React Feature Folder | Build Phase |
|---|---|---|---|
| Authentication | `AuthController` | `src/features/auth/` | Backend Ph02 · React Ph02 |
| Family & User Management | `FamiliesController`, `UsersController` | `src/features/family/` | Backend Ph03–05 · React Ph03–04 |
| Family Dashboard | `FamiliesController` | `src/features/parent/` | Backend Ph05 · React Ph05 |
| Attendance | `AttendanceController` | `src/features/teacher/` | Backend Ph06–07 · React Ph06–07 |
| Tasks & Routines | `TasksController` | `src/features/tasks/` | Backend Ph08–09 · React Ph08–09 |
| Teacher Feedback | `FeedbackController` | `src/features/teacher/` | Backend Ph11–12 · React Ph11–12 |
| Rewards & Coins | `RewardsController` | `src/features/child/`, `src/features/parent/` | Backend Ph10, 13–14 · React Ph10, 13 |
| Family Calendar | `CalendarController` | `src/features/calendar/` | Backend Ph15–16 · React Ph14–15 |
| Notification Preferences | `NotificationsController` | `src/features/notifications/` | Backend Ph16–17 · React Ph17 |
| Admin Configuration | `AdminController`, `FamilyAdminController` | `src/features/admin/`, `src/features/family_admin/` | Backend Ph19–20 · React Ph19–20 |
| Document Vault (L2) | `DocumentVaultController` | `src/features/vault/` | L2 Priority 1 |
| Medical Records (L2) | `MedicalController` | `src/features/medical/` | L2 Priority 2 |
| Safety & Location (L2) | `SafetyController` | `src/features/safety/` | L2 Priority 3 |
| Reports & Insights (L2) | `ReportsController` | `src/features/reports/` | L2 Priority 4 |
| Family Finance (L2) | `FinanceController` | `src/features/finance/` | L2 Priority 5 |
| Advanced Admin (L2) | `AdminController` + `FamilyAdminController` (extended) | `src/features/admin/` | L2 — alongside each module |

---

## Section → File Reference Map

| ProjectOverview.md Section | Primary Source File |
|---|---|
| Section 1 — Architecture | `FamilyFirst_L1_TechSpec.docx` |
| Sections 2–11 — Level 1 Modules | `FamilyFirst_L1_TechSpec.docx` + `FamilyFirst_L1_Codex_DevPlan.docx` |
| Sections 2–11 — React/TypeScript details | `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` (original spec reference — actual implementation is React/TypeScript in `Mobile/`) |
| Sections 12–17 — Level 2 Modules | `FamilyFirst_Level2_ProductDocument.docx` |
| Sections 12–17 — Finance detail | `FamilyLedger_India_Design_Document.docx` |
| Section 18 — Roles & Permissions | `FamilyFirst_Level1_ProductDocument.docx` + `FamilyFirst_L1_TechSpec.docx` |
| Section 19 — DB Standards | `FamilyFirst_L1_TechSpec.docx` |
| Section 20 — React/TypeScript App Architecture | `FamilyFirst_Flutter_AI_Studio_DevPlan.docx` (original spec reference — actual implementation is React/TypeScript) |
| Section 21 — Known Drift | Populated during development — not from spec files |
