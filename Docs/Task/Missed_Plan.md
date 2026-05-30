Read CLAUDE.md first. Then read:
  C:\Live\FamilyFirst\API\Docs\Flow\ProjectOverview.md
  C:\Live\FamilyFirst\API\Docs\Flow\ModuleIndex.md

Do not make any changes yet. Do not write any code yet.

---

IMPORTANT — STACK CLARIFICATION

Before scanning, note the actual implemented stack from ModuleIndex.md:
  Backend  : .NET 8 (C#) · Clean Architecture · ASP.NET Core Web API
  Mobile   : React / TypeScript (NOT Flutter — ignore any Flutter references)
  Database : SQL Server 2022 · Manual .sql scripts

Scan these folders:
  C:\Live\FamilyFirst\API\          ← Backend (.NET 8)
  C:\Live\FamilyFirst\Mobile\       ← React / TypeScript app
---

STEP 1 — SCAN THE CODEBASE

For every module documented in ProjectOverview.md, check what actually exists.

BACKEND — check for each documented module:
  - Controller exists?             → C:\Live\FamilyFirst\API\Controllers\v1\
  - Service / Use Case exists?     → C:\Live\FamilyFirst\API\Application\
  - Repository exists?             → C:\Live\FamilyFirst\API\Infrastructure\Repositories\
  - SQL scripts exist?             → C:\Live\FamilyFirst\API\Infrastructure\Scripts\
  - Request + Response DTOs exist? → C:\Live\FamilyFirst\API\Application\DTOs\
  - FluentValidation validator?    → C:\Live\FamilyFirst\API\Application\Validators\
  - DI registered?                 → C:\Live\FamilyFirst\API\Infrastructure\DependencyInjection.cs

REACT MOBILE — check for each documented module:
  - Feature folder exists?         → C:\Live\FamilyFirst\Mobile\src\features\
  - Screen / Page component?       → src/features/[module]/pages/ or /screens/
  - API service / hook exists?     → src/features/[module]/services/ or /hooks/
  - State management exists?       → src/features/[module]/store/ or context/
  - Route registered?              → src/App.tsx or src/routes/
  - Mock data exists?              → src/mocks/ or AppConfig.isDemo

DATABASE — check for each documented table:
  - SQL script exists in Scripts folder?
  - Script number matches expected sequence (001–040)?

---

STEP 2 — GAP REPORT

Show findings in this exact format:

═══════════════════════════════════════════════════════
FAMILYFIRST — IMPLEMENTATION GAP REPORT
Generated: [today's date]
Based on: ProjectOverview.md v2.0
═══════════════════════════════════════════════════════

---

### SECTION 2 — Authentication & Session
Overall Status: ✅ COMPLETE / ⚠️ PARTIAL / ❌ NOT STARTED

| # | Layer | What is Missing | Expected Location | Priority |
|---|---|---|---|---|
| 1 | Backend | [specific file or endpoint missing] | [exact path] | HIGH |
| 2 | React | [specific component or hook missing] | [exact path] | HIGH |
| 3 | DB | [specific SQL script missing] | [exact path] | HIGH |

---

### SECTION 3 — Family & User Management
[same format]

---

[repeat for every section in ProjectOverview.md — Sections 2 through 17]

---

### SUMMARY TABLE

| # | Module | Backend | React | Database | Overall |
|---|---|---|---|---|---|---|
| 1 | Authentication | ✅ | ✅ | ✅ | ✅ Complete |
| 2 | Family & User Mgmt | ⚠️ | ❌ | ✅ | ⚠️ Partial |
| 3 | Family Dashboard | ❌ | ❌ | ❌ | ✅ Not Started |

Legend: ✅ Complete · ⚠️ Partial (exists but incomplete) · ❌ Not Started

---

STEP 3 — DEVELOPMENT PLAN

After the gap report, produce a phased development plan.
Order phases by dependency — what must exist before what.
One phase per module. Backend + React + Angular together per module.

═══════════════════════════════════════════════════════
FAMILYFIRST — DEVELOPMENT PLAN
Based on: Gap Report above · Standards: CLAUDE.md + ProjectOverview.md
═══════════════════════════════════════════════════════

---

### PHASE [N] — [Module Name]
Status Before: ⚠️ PARTIAL / ❌ NOT STARTED
Priority: HIGH / MEDIUM / LOW
Depends on: [list phases that must be complete first]
Total missing files: [number]

#### Backend Tasks
- [ ] [NNN_CreateTableName.sql]
      → Creates [TableName] table with columns: [list key columns]
- [ ] [EntityName.cs] — Domain/Entities/
      → Entity class inheriting BaseEntity
- [ ] [IEntityRepository.cs] — Application/Common/Interfaces/
      → Interface with methods: [list method signatures]
- [ ] [EntityRepository.cs] — Infrastructure/Repositories/
      → Implementation. Key queries: [describe]
- [ ] [ActionNameCommand.cs / Query.cs] — Application/[Module]/
      → Use case: [what it does]
- [ ] [ActionNameHandler.cs] — Application/[Module]/
      → Handler: [logic summary]
- [ ] [RequestDto.cs + ResponseDto.cs] — Application/[Module]/DTOs/
      → Request fields: [list]. Response fields: [list]
- [ ] [RequestValidator.cs] — Application/[Module]/Validators/
      → Validates: [list rules]
- [ ] [ModuleController.cs] — API/Controllers/v1/
      → Endpoints: [list METHOD /path]
- [ ] DependencyInjection.cs — append registration only

#### React Tasks
- [ ] [ModulePage.tsx] — src/features/[module]/pages/
      → Shows: [what the user sees]
- [ ] [useModuleHook.ts] — src/features/[module]/hooks/
      → Manages: [state / API calls]
- [ ] [moduleService.ts] — src/features/[module]/services/
      → API calls: [list endpoints called]
- [ ] [moduleTypes.ts] — src/features/[module]/types/
      → Types: [list DTOs as TypeScript interfaces]
- [ ] [mockModuleData.ts] — src/mocks/
      → Demo data for: [list screens]
- [ ] Route registration in src/routes/ or App.tsx
      → Path: [route path] · Role guard: [which roles]

#### Done Criteria
- [ ] dotnet build → 0 errors, 0 warnings
- [ ] All new SQL scripts run without FK violations
- [ ] Primary endpoints tested via Postman (list which)
- [ ] React screen renders in demo mode — no blank screens
- [ ] React screen renders with live API — correct data shown
- [ ] flutter analyze equivalent: no TypeScript errors (tsc --noEmit)
- [ ] Role guard tested — correct roles can access, others cannot

---

[repeat PHASE block for every module with missing items]

---

### EXECUTION ORDER SUMMARY

| Phase | Module | Priority | Depends On | Est. Files |
|---|---|---|---|---|
| 1 | [module] | HIGH | None | [n] |
| 2 | [module] | HIGH | Phase 1 | [n] |
| 3 | [module] | HIGH | Phase 1, 2 | [n] |

---

RULES — APPLY THROUGHOUT:

1. Only report something as missing after confirming the file does not exist.
   If a folder does not exist → entire module is NOT STARTED.
   If files exist but are empty or stub-only → PARTIAL.

2. Every planned file must reference its exact expected path.

3. Every backend task must follow Clean Architecture:
   Domain → Application → Infrastructure → API
   Never skip a layer.

4. Every React task must follow the pattern in ProjectOverview.md Section 20:
   Feature folder → pages / hooks / services / types / mocks

5. SQL scripts must follow naming: NNN_ActionTableName.sql
   Numbers must continue from the last existing script number.

6. Do not write any code in this step.
   Show the gap report and development plan only.
   Wait for my confirmation before starting any development.

---

After showing the full report and plan, end with:

"Gap analysis complete. [X] modules fully implemented. [Y] modules partial.
[Z] modules not started. Ready to begin Phase 1 — [module name] on your confirmation."