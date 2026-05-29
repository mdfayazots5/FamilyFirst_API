You are the Project AI Engineer for FamilyFirst. Update the CLAUDE.md file in this project root with the following changes. Do not rewrite the entire file. Make only the targeted edits described below.

---

CHANGE 1 — REPLACE the entire "SESSION STARTUP — MANDATORY FLOW" section with this:

## SESSION STARTUP — MANDATORY FLOW

Every session begins with this exact sequence. No exceptions.

STEP 1 — Extract keywords from the user's request.
         Examples:
           "attendance API"       → keywords: attendance, API
           "child reward screen"  → keywords: rewards, Flutter
           "feedback flow"        → keywords: feedback, flow
           "task completion DB"   → keywords: tasks, database

STEP 2 — Identify the Level scope (Level 1 or Level 2).
         Default to Level 1 unless the request mentions:
         Document Vault, Medical Records, Safety/Location, Finance, Reports, or Level 2.

STEP 3 — Open ProjectOverview.md. Navigate to the relevant section and module only.
         Never read the full file. Never scan the repo.
         If ModuleIndex.md exists, use it to locate the correct section fast.

STEP 4 — Assess what is already documented for this module:
         - Is the information present?          → YES: use it. NO: go to Step 5.
         - Is the information current/valid?    → YES: use it. NO: go to Step 5.
         - Is the information complete enough?  → YES: proceed. NO: go to Step 5.

STEP 5 — Only if a genuine gap exists: read the minimum required source file.
         State before reading:
         "Gap confirmed in ProjectOverview.md for [module]. Reading: [filename]. Will update after."
         Read one file at a time. After each read, decide if ProjectOverview.md can now be updated.

STEP 6 — Execute the task using only confirmed, verified project data.

STEP 7 — After task completion, evaluate whether ProjectOverview.md needs updating.
         Apply the UPDATE DECISION RULES below before writing anything.

STEP 8 — Update ModuleIndex.md only if a new module, section, or API group was added.

**Confirm startup with:**
"FamilyFirst Mode Active. Level [1/2] identified. Module: [module name]. ProjectOverview section: [section]. Context: [sufficient / gap found — reading source]. Proceeding."

---

CHANGE 2 — REPLACE the entire "ULTRA FAST INDEX MAPPING — TOKEN CONTROL" section with this:

## PROJECT OVERVIEW — UPDATE DECISION RULES

ProjectOverview.md is the central project brain. It must stay clean, structured, and reliable.
Update it only when the information is genuinely reusable for future development.

**UPDATE — only when ALL of the following are true:**
- The information was verified against source files or live implementation
- The information is reusable across future tasks (not task-specific)
- The existing entry is missing, outdated, or incorrect
- The information belongs to a stable section (API contract, DB schema, flow, business rule, config)

**DO NOT UPDATE — when any of the following are true:**
- The task was completed using existing, already-correct documentation
- The information is temporary, task-specific, or one-off
- The change is a minor bug fix with no structural impact
- The existing entry already covers this accurately
- The information is a debug log, execution note, or test result

**Before writing to ProjectOverview.md, answer these three questions:**
  Q1: Is this information missing or wrong in the current ProjectOverview.md?
  Q2: Will a future developer need this to understand or build this module?
  Q3: Is this validated — not assumed or inferred?
  → All three YES → Update. Any NO → Do not update.

**Documentation structure inside ProjectOverview.md:**
- Organised section-wise (Authentication, Attendance, Tasks, Feedback, Rewards, Calendar, etc.)
- Under each section, organised module-wise
- Each module entry includes (only what is confirmed):
    · Key APIs — method, path, request DTO, response DTO
    · DB tables involved — table name, key columns used
    · Business rules — conditions, limits, validations
    · Flow summary — trigger → API → DB → response
    · Dependencies — other modules or services this module depends on
    · Configuration — env vars, feature flags, limits relevant to this module
    · Known drift / resolved issues — only if recurrence risk is high
- No task steps, debug output, execution logs, or temporary notes — ever
- No duplicate information across sections
- No speculative or unverified entries

**Token control:**
- Read 1 source file at a time
- After each read: update ProjectOverview.md before reading the next file
- If more than 3 source files are read in one task, state:
  "Read budget exceeded. Cause: [reason]. Updating ProjectOverview.md now before continuing."

---

CHANGE 3 — REPLACE the entire "STABILITY / REGRESSION PREVENTION" section with this:

## STABILITY / REGRESSION PREVENTION

If the same flow or issue recurs more than once, it is a documentation gap — not just a code problem.

**When fixing a repeated issue:**
1. Identify which ProjectOverview.md field was missing or wrong
2. Classify the drift type:
   - Request / response drift — Flutter sent wrong shape to API
   - UI / API mismatch — wrong endpoint called from Flutter screen
   - SQL drift — script changed, ProjectOverview.md not updated
   - DB contract drift — column or type changed without propagating
   - Missing fallback — not documented, not implemented
   - Stale docs — fix made in code, ProjectOverview.md not updated
   - Demo / live mismatch — MockDataService returns different shape than live API
3. Verify the fix against the actual implementation
4. Update the relevant module section in ProjectOverview.md to stable contract level
5. Add a "Known Drift" note only if the risk of recurrence is high

**A flow is at stable contract level when:**
Its ProjectOverview.md entry is complete enough that a future developer
can understand, debug, and extend it without reading any source file.

**A flow is NOT stable if:**
- Any field is blank without a [VERIFY] marker
- The request/response shape is missing or approximate
- The DB tables or key columns are not listed
- Business rules are vague ("validates input") instead of specific ("OTP valid 5 min, max 3/hr/phone")

---

CHANGE 4 — REPLACE the entire "TASK COMPLETION BLOCK — MANDATORY" section with this:

## TASK COMPLETION BLOCK — MANDATORY

End every task with this exact block. No exceptions.

TASK STATUS               : COMPLETE / INCOMPLETE
LEVEL SCOPE               : Level 1 / Level 2
MODULE IDENTIFIED         : [module name]

PROJECTOVERVIEW UPDATED   : YES / NO
  → If YES  : SECTION UPDATED: [section name · module name]
              UPDATE TYPE: NEW ENTRY / CORRECTED EXISTING / EXTENDED EXISTING
              CONTRACT LEVEL: STABLE / PARTIAL / STUB
  → If NO   : REASON: [choose one]
              - Existing documentation was sufficient and accurate
              - Task was a bug fix with no structural change
              - Information is task-specific and not reusable
              - Information not yet verified — marked [VERIFY]

MODULEINDEX UPDATED       : YES / NO
  → If YES  : WHAT CHANGED: [new section / new module / new API group]
  → If NO   : REASON: No structural change to module or section map

SOURCE FILES READ         : [list filenames or NONE]
  → Per file: REASON READ | GAP FILLED | PROJECTOVERVIEW UPDATED AFTER READ: YES / NO

DRIFT DETECTED            : YES / NO
  → If YES  : TYPE: [request / SQL / DB / stale docs / demo-live / other]
              RESOLVED: YES / NO
              RECURRENCE RISK: HIGH / LOW
              DOCUMENTED IN PROJECTOVERVIEW: YES / NO

FLOW STABLE               : YES / NO
  If NO — what is missing:
  [ ] Request / response shape
  [ ] DB tables and key columns
  [ ] Business rules (specific, not vague)
  [ ] Failure cases and status codes
  [ ] Flutter screen mapping
  [ ] Demo mode behavior

PENDING ITEMS             : [list or NONE]

---

CHANGE 5 — ADD this new section after "STRICT PROHIBITIONS", before "TASK COMPLETION BLOCK":

## MODULEINDEX — UPDATE RULES

ModuleIndex.md is the navigation map for ProjectOverview.md — not a logic source.

**Update ModuleIndex.md ONLY when:**
- A new module is introduced
- A new section is added to ProjectOverview.md
- A new API group or controller is created
- An existing section is renamed or restructured

**Do NOT update for:**
- Bug fixes
- UI or style corrections
- Refactoring with no structural change
- Adding a field to an existing documented module
- Any task-specific or temporary change

ModuleIndex.md maps: keyword → section → module → ProjectOverview.md location
It must always reflect the current structure of ProjectOverview.md — nothing more.

---

CHANGE 6 — ADD the following entry to the "STRICT PROHIBITIONS" table:

| Update ProjectOverview.md for every task by default | Only update when information is verified, missing, or incorrect — and reusable |
| Document task steps, debug logs, or execution notes in ProjectOverview.md | ProjectOverview.md contains only validated, reusable architectural knowledge |
| Write vague business rules ("validates input", "checks permission") | Every rule must be specific: field name, limit, condition, error code |
| Leave a section blank or with placeholder text | Use [VERIFY] marker and log it in PENDING ITEMS |

---

After making all six changes:
- Do not change any other section
- Do not reformat or rewrite sections not listed above
- Increment the version in the file header from v1.0 to v2.0
- Save the file