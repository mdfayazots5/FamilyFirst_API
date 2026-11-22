# FamilyFirst — End-to-End Test Guide (Laptop)

**Version:** 2026-11-21 · **Build under test:** post-review fixes R1–R8, demo mode removed (app always uses the live API)
**How to use:** work top to bottom. For each check, do the **Steps** and compare with **Expected**. Mark **Result**:
✅ Pass · ❌ Fail · ⛔ Blocked. For every ❌, add a row to the **Defect Log** at the end (screenshot + what you saw).

---

## 1. Setup

| Item | Value |
|---|---|
| App (open this in Chrome) | **http://localhost:3000** |
| API | http://localhost:60817 (runs in its own console window: keep it open) |
| Database | `FamilyFirstDB` on `(localdb)\MSSQLLocalDB` (scripts up to 122 applied) |
| Phone-sized view | In Chrome press **F12**, then **Ctrl+Shift+M**, choose a phone such as **390 × 844** |

**Test accounts:** all in *Sharma Test Family*, all with password **`123456`**. Country is India (+91).

| Role | Phone | Name |
|---|---|---|
| SuperAdmin | 9000000001 | Super Admin |
| FamilyAdmin | 9000000002 | Rahul Sharma |
| Parent | 9000000003 | Priya Sharma |
| Child | 9000000004 | Arjun Sharma |
| Teacher | 9000000005 | Anjali Verma |
| Elder | 9000000006 | Dadi Ji |

**Good to know**
- **Sign out between roles:** use the **Logout** button (top right).
- **Lockout:** 5 wrong passwords lock that account for **15 minutes** (this is expected). To unlock early, wait or
  ask a developer.
- **Family join code:** `TEST01`.

### 1.1 One-time setup: give the Teacher a child (required for the Teacher tests)
The seed data has no teacher-to-child assignment, and there is no screen for it yet. Run this once in **PowerShell**
while the API is running:

```powershell
$api = "http://localhost:60817/api"
$login = Invoke-RestMethod -Method Post -Uri "$api/auth/login" -ContentType "application/json" `
  -Body '{"phoneNumber":"+919000000002","countryCode":"+91","password":"123456"}'
$h = @{ Authorization = "Bearer $($login.data.accessToken)" }
Invoke-RestMethod -Method Post -Headers $h `
  -Uri "$api/families/0afdd862-cd70-48fc-92a2-c821c69551ce/teachers/62d5cafc-d779-4881-944e-e1131f8f4cb9/assign/2df8eae8-4bcc-4b3e-aeb2-20068de4b424"
```
**Expected:** the response shows `succeeded : True` and `Teacher assigned.`

---

## 2. Known limitations (do not log these as defects)

| # | Area | Status |
|---|---|---|
| L1 | **Photo uploads** (child task photo, vault document upload) and **PDF report export** | ⛔ Need real AWS S3 credentials. Without them these actions show an error. Test with tasks that **do not require a photo**. |
| L2 | **Push notifications** (FCM) and **real OTP SMS** (MSG91) | ⛔ Need provider credentials. In-app notification lists still work. |
| L3 | **Child "Change PIN"** in Child Settings | Hidden on purpose (it never saved). PIN sign-in can't be tested until a PIN can be set, so Child and Elder use **phone + password**. |
| L4 | **Elder voice note** on "Send appreciation" | Hidden on purpose. Text notes work. |
| L5 | **Emergency Folder "Family Emergency Info" cards** | Hidden on purpose (they were demo-only). |
| L6 | **Admin Support Tickets / Content Manager** | No backend yet: the screens show an empty state. |
| L7 | **Admin Analytics** | Shows all-time totals only (no charts by date yet). |
| L8 | **Adults turning on location sharing** | No adult opt-in screen yet. Only children's sharing can be switched on. |
| L9 | **OTP sign-in** | The sign-in screen has no "sign in with OTP" option; the OTP screen exists but nothing opens it. The OTP API itself works. |

---

## 3. Every screen (check while you test everything below)

| ID | Check | Expected | Result |
|---|---|---|---|
| G1 | Any screen while it loads | A loading shimmer, then data **or** a clear "nothing here yet" message. **Never** a blank page or a spinner that never stops. | |
| G2 | Stop the API for a moment, then press **Retry** on a screen | You see an error message with **Retry**. After you restart the API, Retry loads the data. | |
| G3 | Phone width (390 px) | Nothing is cut off on the right and there's no sideways scrolling. Buttons are easy to tap. | |
| G4 | Browser **Back** button | Goes to the previous screen and never to a blank page. | |

---

## 4. Sign-in & security

| ID | Steps | Expected | Result |
|---|---|---|---|
| A1 | Sign in as **Parent** (9000000003 / 123456) | Opens the Parent home. | |
| A2 | Sign out, then sign in as each other role | SuperAdmin → Admin dashboard; FamilyAdmin → Family Admin panel; Child → "My Day"; Teacher → Teacher home; Elder → Elder home. | |
| A3 | Sign in with a wrong password | "Sign-in failed" message. | |
| A4 | Use a **different** account, enter a wrong password **6 times** | Attempts 1–5 say the credentials are invalid. Attempt 6 says **too many failed attempts, try again in 15 minutes**. A correct password is also refused during the lockout. | |
| A5 | OTP sign-in | ⛔ Not reachable from the UI: the sign-in screen offers password and PIN only (see L9). Skip. | |
| A6 | OTP 3-per-hour limit | ⛔ Same as A5. Skip. | |
| A7 | Sign in, wait, then use the app normally | You stay signed in. Tokens refresh silently. | |
| A8 | Signed in as **Child**, type `http://localhost:3000/finance` in the address bar | You're sent back to the Child home (no access). | |
| A9 | Signed in as **Teacher**, open `/safety/map` | Sent back to the Teacher home. | |
| A10 | Signed in as **SuperAdmin**, open `/calendar` | Sent back to the Admin dashboard. | |
| A11 | Signed out, open `/family-setup` | Sent to the sign-in screen. | |

---

## 5. Parent & family

| ID | Steps | Expected | Result |
|---|---|---|---|
| P1 | Parent: home screen | Shows the family overview and Arjun as a child. | |
| P2 | Parent: **Family** tab (members) | Lists all six family members with roles. | |
| P3 | Parent: Join code screen | Shows the join code (`TEST01`). | |
| P4 | Parent: tap Arjun to open the child detail | Child detail loads (scores, recent activity, or an empty state). | |
| P5 | Parent: Settings and Profile | Both open. The phone number shows correctly. | |

---

## 6. Tasks & routines (use tasks that do **not** require a photo; see L1)

| ID | Steps | Expected | Result |
|---|---|---|---|
| T1 | Parent: Arjun → Routine → **Add task**. Name "Read 20 minutes", time block Morning, 10 coins, photo **off**, every day, save | Task saved and shown in the routine. Task type and status dropdowns are filled from the server. | |
| T2 | Sign in as **Child**: My Day | "Read 20 minutes" appears for today. | |
| T3 | Child: open the task → **Submit task** | Sent for review, and you return to My Day. The task shows as submitted. | |
| T4 | Sign in as **Parent**: Verification queue | Arjun's submission appears. **Approve** it. | |
| T5 | Child: Rewards / coins screen | The coin balance went up by the task's coins. | |
| T6 | Parent: add a task **with photo required**; Child tries to submit it without a photo | Submission is refused with a photo-required message. *(Uploading a real photo is blocked by L1.)* | |

---

## 7. Rewards

| ID | Steps | Expected | Result |
|---|---|---|---|
| R1 | Child: Rewards → pick a reward costing **less** than the balance → redeem | Redemption created (pending). | |
| R2 | Child: redeem a reward costing **more** than the balance | Refused with an insufficient-coins message. | |
| R3 | Child: redeem the **same** reward twice quickly | Only one redemption is created. | |
| R4 | Parent: Rewards screen | The pending redemption appears. Approving it updates the status. | |

---

## 8. Attendance (Teacher) (do setup 1.1 first)

| ID | Steps | Expected | Result |
|---|---|---|---|
| AT1 | Teacher: **Create session** (any subject) | Session created. Arjun is in the list. | |
| AT2 | Mark Arjun Present / Absent / Late (statuses come from the server), then **Submit** | Submitted successfully. | |
| AT3 | Try to submit the same session again | Refused: already submitted. | |
| AT4 | Within 1 hour, edit Arjun's attendance | Allowed. *(After 1 hour edits are refused; optional check.)* | |
| AT5 | Parent: open Arjun's detail or reports | The attendance record is visible. | |

---

## 9. Teacher feedback

| ID | Steps | Expected | Result |
|---|---|---|---|
| F1 | Teacher: **New feedback** for Arjun, with a rating from the server list, then send | Sent. It appears in Teacher → feedback history. | |
| F2 | Parent: Feedback inbox → open it | Feedback details are shown. You can acknowledge or respond. | |
| F3 | Teacher: edit or delete own feedback within 24 h | Allowed. | |
| F4 | Teacher: attendance comment templates (inside attendance marking) | Templates load. A family template can be saved and edited. | |

---

## 10. Calendar & notifications

| ID | Steps | Expected | Result |
|---|---|---|---|
| C1 | Parent: Calendar → **Create event** (type from the list, e.g. "Birthday", with a reminder) | Event saved and shown on the calendar. | |
| C2 | Open the event → edit → save; then delete | Changes are saved and the delete works. | |
| C3 | Elder: Calendar / home timeline | The event is visible. The type filter chips filter the list correctly. | |
| N1 | Bell icon → Notifications | The list opens. Tapping an item opens the right screen (never a blank page). "Mark all read" works. | |
| N2 | Notification preferences | Open, change and save. | |

---

## 11. Family Admin (sign in as FamilyAdmin, 9000000002)

| ID | Steps | Expected | Result |
|---|---|---|---|
| FA1 | Admin panel | Opens. Every tile opens its screen (none goes to a blank page). | |
| FA2 | Module visibility: toggle a module, save | Saved. | |
| FA3 | **Notification rules**: switch a rule off, Save, reload the page | The switch stays off after reload. | |
| FA4 | Storage settings, Alert thresholds, Emergency access, Finance privacy: change one value each, save, reload | Each value persists after reload. Errors show a message, not a silent failure. | |
| FA5 | FamilyAdmin opens a child's **weekly report** and **attendance summary** (Reports) | Both load (FamilyAdmin is allowed). | |

---

## 12. Document Vault

| ID | Steps | Expected | Result |
|---|---|---|---|
| V1 | Parent: Vault home | Opens. Categories come from the server. | |
| V2 | Upload a document | ⛔ Blocked by L1 unless AWS is configured. If configured: the upload succeeds and the document shows its category name. | |
| V3 | Expiry dashboard | Opens (list or empty state). | |
| V4 | Emergency folder | Opens. Shows priority documents (or an empty state). There are no fake "emergency info" cards. | |
| V5 | Elder or Child opens `/vault` | Not allowed (sent home or shown an error). | |
| V6 | *(If documents exist)* Create a **share link**, open it in a private/incognito window | The shared document is visible without signing in. Only that document's link is listed, with no other links or old versions. | |

---

## 13. Medical

| ID | Steps | Expected | Result |
|---|---|---|---|
| M1 | Parent: Health → open Arjun → **Edit**: set blood group, an allergy and a doctor, then save | Saved and shown on the profile. | |
| M2 | Vaccination tracker: add a vaccination, mark it as given | Saved. The status changes to Given. | |
| M3 | Sign in as **Child** → own health profile | Shows blood group, allergies, medicines and vaccinations only. **Doctor and emergency contact are hidden.** | |
| M4 | Sign in as **Elder** → Arjun's health profile | **Summary only**: name and vaccination status. Blood group, allergies, doctor and medicines are hidden. | |
| M5 | Elder tries to open Arjun's health **timeline** or the profile list | Not allowed. | |
| M6 | Parent: Emergency card → share | A share link is created. Opening it signed out shows the card. | |
| M7 | Open a member's profile that has never been filled in | Shows an empty profile, not an error. | |

---

## 14. Safety & location

| ID | Steps | Expected | Result |
|---|---|---|---|
| S1 | Parent: Safety → Safe zones → **Add zone** (type from the list, radius, late-alert time), save | Zone saved and listed. Edit and delete work. | |
| S2 | FamilyAdmin: Safety → **Location settings** → turn on sharing for **Arjun**, save | Saved. *(Turning it on for an adult is refused: see L8.)* | |
| S3 | Parent: Family map | Only members who are sharing appear. | |
| S4 | FamilyAdmin: turn Arjun's sharing **off**, then check the map again | Arjun no longer appears. | |
| S5 | Child: `/safety/emergency` → **hold the SOS button 2 seconds** | "Your parents have been notified" (the sent count may be 0 without push, see L2). If sending fails, a clear error with "hold again to retry" appears. | |
| S6 | Parent: Safety → Alerts | The SOS alert appears. **Mark as resolved** works. | |

---

## 15. Finance

| ID | Steps | Expected | Result |
|---|---|---|---|
| FN1 | Parent or FamilyAdmin: Finance dashboard | Loads, or shows a clear error/permission message, never a never-ending loading shimmer. *(Only the family's configured "CFO" member can see finance data.)* | |
| FN2 | Finance settings: invite a member to consent | Invite sent (status changes) or a clear error. | |
| FN3 | Budget manager: set a budget amount | Saved. Category names come from the server (e.g. "Groceries & Kirana"). | |

---

## 16. Reports

| ID | Steps | Expected | Result |
|---|---|---|---|
| RP1 | Parent: Reports home → pick Arjun | The child picker lists the family's real children. | |
| RP2 | Weekly digest, Monthly report, Attendance summary, Archive | Each opens with data or an empty state. | |
| RP3 | Elder opens `/reports` | Not allowed (Reports are for Parent and FamilyAdmin). | |
| RP4 | Export PDF | ⛔ Blocked by L1 unless AWS and the QuestPDF licence are configured. | |

---

## 17. Super Admin (sign in as 9000000001)

| ID | Steps | Expected | Result |
|---|---|---|---|
| SA1 | Admin dashboard | KPIs and activity load. | |
| SA2 | Families: search and filter | The list loads. Search narrows it. | |
| SA3 | Analytics | Real totals (users, children, teachers, tasks) and the feature-usage bars. No fake charts. | |
| SA4 | Task templates, Reward catalog, Campaigns, App config | Each opens and loads. | |
| SA5 | SuperAdmin opens `/vault` or `/medical` | Not allowed (SuperAdmin has no access to family vault or medical data). | |
| SA6 | **Task Templates** → **New** → enter a task name and category, choose its schedule, then click **Save template**. Refresh and search by the new name. | The page confirms the save and the template still appears after refresh. For a quick catalog check, search for **Read 20 minutes** or **Math homework** in the local starter data. | |

---

## 18. Emergency offline (optional, needs a production build)

| ID | Steps | Expected | Result |
|---|---|---|---|
| O1 | Developer runs `npm run build` and `npm run preview`. Open the emergency folder once while online. | The folder loads. | |
| O2 | DevTools → Network → **Offline**, then reload `/vault/emergency` | The folder still shows with an "offline copy" banner. | |
| O3 | Offline, open any other screen | A normal offline error. Only the emergency screens work offline. | |
| O4 | Sign out, then go offline again | The emergency copy is gone (cleared at sign-out). | |

---

## 19. Defect Log

| # | Test ID | Role | Screen / URL | What you did | What happened (vs expected) | Screenshot | Severity (High/Med/Low) |
|---|---|---|---|---|---|---|---|
| 1 | | | | | | | |
| 2 | | | | | | | |
| 3 | | | | | | | |

**Severity guide:** **High**: wrong data, security/role leak, crash, or you cannot continue. **Medium**: feature
doesn't work but there's a workaround. **Low**: text, layout or cosmetic issues.
