# FamilyFirst — UI Design System
**Version 4.0 — Premium Mobile-First Design System**
**Applies to: ALL 85 screens (Level 1 + Level 2) — No screen is exempt**
**Codex must read this entire file before touching any UI file. No exceptions.**

---

## 0. Design Philosophy

FamilyFirst is a **premium family management platform**. Every screen must feel:

- **Warm** — Families, not enterprises. Homes, not command centers.
- **Trustworthy** — Clean, structured, and calm. No visual noise.
- **Premium** — Elevated typography, soft shadows, purposeful spacing. Feels like it costs ₹999/month.
- **Role-aware** — Each role has a distinct emotional tone (see §4). A child's screen feels different from a teacher's.
- **Efficient** — Users are busy. Every screen must surface the most important action in under 2 seconds.

### What This App Must NOT Feel Like

| ❌ Prohibited Aesthetic | Why It's Wrong |
|---|---|
| Military / tactical / surveillance software | "TACTICAL STATION", "GLOBAL INTEL", "GATE >", "TELEMETRY", "DUMP_LOG", "BOOTING_COMMAND_STATION" destroy trust with families |
| Raw developer tool | Terminal log panels, system status monitors, "STATION_ONLINE", "CORE_OS_v3.4.0" are for engineers, not parents |
| Enterprise SaaS | Cold grids, monospaced fonts, pulse arrays, "UPTIME_PULSE_ARRAY" on a parent dashboard are alienating |
| Cheap consumer app | Gradients on gradients, neon colors, emoji overload on non-child screens |
| Chinese/foreign-language UI | Greeting text like `晨 MORNING` is completely wrong for this Indian family platform |
| Surveillance aesthetic | `Fingerprint`, `Radar`, `Network`, `Terminal`, `Cpu`, `Workflow`, `Layers`, `Command` icons have no place in a family app |

### What This App MUST Feel Like

- A trusted family companion — warm cream base, navy anchors, gold rewards
- Premium iOS/Android native apps (Apple Health, Google Photos, Notion, Headspace)
- Role-aware: Parent gets calm; Child gets joy; Elder gets warmth; Teacher gets speed; SuperAdmin gets clarity

---

## 1. Design Tokens — Single Source of Truth

### 1.1 Color Palette

| Token Name | Hex | CSS Variable | Tailwind Class | Usage |
|---|---|---|---|---|
| `primary` | `#1A2E4A` | `--color-primary` | `text-primary` / `bg-primary` | Headers, key text, nav bar, primary buttons |
| `accent` | `#C8922A` | `--color-accent` | `text-accent` / `bg-accent` | Gold highlights, coins, rewards, premium badges, active nav |
| `success` | `#2D6A4F` | `--color-success` | `text-success` / `bg-success` | Confirmations, attendance present, task complete, streaks |
| `alert` | `#C1121F` | `--color-alert` | `text-alert` / `bg-alert` | Errors, destructive actions, SOS, urgent alerts |
| `background` | `#F8F4EE` | `--color-bg-cream` | `bg-bg-cream` | All page backgrounds — warm cream, never pure white |
| `surface` | `#FFFFFF` | — | `bg-white` | Card backgrounds, modals, drawers |
| `surface-warm` | `#FDF9F4` | — | `bg-[#FDF9F4]` | Elevated card variation, inner sections |
| `border` | `#E8E2D9` | — | `border-black/5` | Card borders, dividers — warm, never cold gray |
| `text-primary` | `#1A2E4A` | — | `text-primary` | Primary text — headings, key values |
| `text-secondary` | `#5A6A7A` | — | `text-gray-500` | Supporting text, labels, timestamps |
| `text-muted` | `#9BA8B5` | — | `text-gray-400` | Placeholder text, empty states, hint text |
| `text-on-dark` | `#FFFFFF` | — | `text-white` | Text on dark/primary backgrounds |
| `coin-gold` | `#F0A500` | — | `text-amber-500` | Coin values, reward icons — brighter gold than accent |
| `streak-fire` | `#E8521A` | — | `text-orange-600` | Streak counts, fire icons |

**Role gradient pairs (header backgrounds only):**

| Role | Gradient | Badge color |
|---|---|---|
| Parent | `from-[#1A2E4A] to-[#2A4A6A]` | `#2D6A4F` (green) |
| Child | `from-[#1A2E4A] to-[#2A3F6A]` | `#C8922A` (gold) |
| Elder | `from-[#2A3A4A] to-[#3A4A5A]` | `#7B9E87` (soft green) |
| Teacher | `from-[#1A2E4A] to-[#243755]` | `#4A7FA5` (steel blue) |
| SuperAdmin | `from-[#0F1E30] to-[#1A2E4A]` | `#C8922A` (gold) |
| FamilyAdmin | `from-[#1A2E4A] to-[#243755]` | `#C8922A` (gold) |

### 1.2 Typography

**Three fonts. Each has one job. No mixing or overriding.**

| Use Case | Font | `font-` class | Weight | Style |
|---|---|---|---|---|
| Page hero title | Poppins | `font-display` | `font-bold` (700) | Never italic |
| Section heading | Poppins | `font-display` | `font-semibold` (600) | Never italic |
| Card title | Poppins | `font-display` | `font-semibold` (600) | Never italic |
| Body / paragraph | Nunito | `font-body` | `font-normal` (400) | Never italic |
| Supporting label | Nunito | `font-body` | `font-semibold` (600) | Never italic |
| Numbers / coins / metrics | Space Grotesk | `font-numbers` | `font-medium` (500) | Never italic |
| Badge / pill text | Nunito | `font-body` | `font-bold` (700) | UPPERCASE, max 2 words |
| Button text | Nunito | `font-body` | `font-bold` (700) | Title case |

**Font weight cap — ENFORCED:**
- Maximum weight is `font-bold` (700). `font-black` (900) is **PROHIBITED everywhere** — it produces the military/harsh look.
- `font-extrabold` (800) is prohibited on any text visible to end users.

**Italic — ENFORCED:**
- `italic` is **PROHIBITED on all UI text** except for genuine quoted speech or publication titles.
- No italic headings. No italic badges. No italic button labels. No italic section titles.

**ALL CAPS rules:**
- Badges (`FFBadge`): UPPERCASE is allowed. Max 2 words. Max 12 characters.
- Section headers (`FFSectionHeader`): UPPERCASE is allowed. Max 3 words.
- Everything else: Title Case or sentence case only. **No ALL CAPS headings, labels, or descriptions.**

**Letter-spacing cap:**
- `tracking-wider` (`0.05em`) is the maximum for body/label text.
- `tracking-widest` (`0.1em`) is allowed ONLY on badge pill text.
- `tracking-[0.2em]` and wider are **PROHIBITED everywhere** — they produce the military stencil look.

**Type Scale — Mobile First:**

| Context | Mobile | sm: | lg: |
|---|---|---|---|
| Hero title | `text-xl` (20px) | `sm:text-2xl` | `lg:text-3xl` |
| Section heading | `text-sm` (14px) | `sm:text-base` | `lg:text-lg` |
| Card title | `text-sm` (14px) | `sm:text-base` | `lg:text-lg` |
| Card metric / number | `text-2xl` (24px) | `sm:text-3xl` | `lg:text-4xl` |
| Body text | `text-sm` (14px) | same | `lg:text-base` |
| Supporting label | `text-xs` (12px) | same | `lg:text-sm` |
| Badge / pill | `text-[10px]` | same | same |
| Button label | `text-sm` (14px) | same | `lg:text-base` |

**PROHIBITED typography values:**
- `text-4xl` or larger as a mobile base — must use responsive prefix (`sm:`, `lg:`)
- `text-5xl`, `text-6xl`, `text-7xl`, `text-8xl` — never used in any FamilyFirst screen
- `font-black`, `font-extrabold`
- `italic` on any label, heading, badge, or button
- `tracking-[0.2em]` or wider
- `whitespace-nowrap` on text reaching the screen edge
- ALL CAPS on headings longer than 3 words

### 1.3 Spacing Scale

| Context | Mobile | sm: | lg: |
|---|---|---|---|
| Page horizontal padding | `px-4` | `sm:px-6` | `lg:px-8` |
| Page vertical padding | `py-5` | `sm:py-6` | `lg:py-8` |
| Card internal padding | `p-4` | `sm:p-5` | `lg:p-6` |
| Between sections | `space-y-6` | `sm:space-y-8` | `lg:space-y-10` |
| Card gap in grid | `gap-3` | `sm:gap-4` | `lg:gap-6` |
| Header height | `h-16` (64px) | `sm:h-16` | `lg:h-16` |
| Bottom nav height | `h-16` (64px) | same | same |
| Bottom padding (clears nav) | `pb-24` | same | same |

**PROHIBITED spacing values:**
- `p-8`, `p-10`, `p-12`, `p-14` as mobile base — use `p-4 sm:p-5 lg:p-6`
- `space-y-16`, `space-y-24`, `space-y-32` as mobile base
- `gap-8`, `gap-10` as mobile base
- `pb-48`, `pb-36` — bottom padding never exceeds `pb-24`
- `p-14`, `lg:p-14`, `lg:py-14` — never use on mobile, extreme on desktop

### 1.4 Border Radius Scale

| Context | Class | px equivalent |
|---|---|---|
| Page card | `rounded-ff` | 16px (`--radius-ff`) |
| Inner card / sub-card | `rounded-ff-sm` | 8px (`--radius-ff-sm`) |
| Large card / hero | `rounded-ff-lg` | 24px (`--radius-ff-lg`) |
| Button (primary) | `rounded-ff-sm` or `rounded-xl` | 8–12px |
| Badge / pill | `rounded-full` | 50% |
| Input field | `rounded-xl` | 12px |
| Bottom sheet / drawer | `rounded-t-3xl` | 24px top only |
| Icon container | `rounded-ff-sm` or `rounded-xl` | 8–12px |

**PROHIBITED radius values:**
- `rounded-[28px]`, `rounded-[32px]`, `rounded-[40px]`, `rounded-[48px]`, `rounded-[56px]`, `rounded-[64px]` — arbitrarily large radii produce the "soft toy" look
- `rounded-none` on cards or buttons — always use at least `rounded-xl`
- `rounded-3xl` as mobile default (only acceptable on drawers/sheets)

### 1.5 Shadow System

These values MUST be set in `index.css` under `@theme`. The existing cold gray shadows must be replaced.

```css
/* In index.css @theme block: */
--shadow-card: 0 2px 12px rgba(26, 46, 74, 0.08), 0 1px 4px rgba(26, 46, 74, 0.04);
--shadow-elevated: 0 8px 32px rgba(26, 46, 74, 0.12), 0 2px 8px rgba(26, 46, 74, 0.06);
--shadow-header: 0 1px 8px rgba(26, 46, 74, 0.10);
--shadow-pressed: 0 1px 4px rgba(26, 46, 74, 0.10);

/* Remove or replace: */
/* --shadow-premium: 0 4px 12px rgba(0, 0, 0, 0.05);  ← cold gray, REMOVE */
/* --shadow-premium-hover: 0 8px 20px rgba(0, 0, 0, 0.08);  ← cold gray, REMOVE */
/* --shadow-premium-lg: 0 12px 24px rgba(0, 0, 0, 0.1);  ← cold gray, REMOVE */
```

Usage:
- `shadow-card` — all standard FFCards
- `shadow-elevated` — modals, drawers, action sheets
- `shadow-header` — AppNavShell top header
- `shadow-pressed` — active/pressed card state
- **PROHIBITED:** `shadow-md`, `shadow-lg`, `shadow-xl`, `shadow-2xl` Tailwind defaults — they are cold and gray
- **PROHIBITED:** `shadow-primary/30`, `shadow-2xl shadow-primary/20` — these are the military dramatic glow look

---

## 2. Component Library — FF Shared Components

All shared components live in `Mobile/src/shared/components/`. All names use `FF` prefix.

### 2.1 FFCard

The foundational card. Every card in the app uses this or its variants.

```tsx
// FFCard.tsx — required implementation
interface FFCardProps {
  children: React.ReactNode;
  variant?: 'default' | 'warm' | 'primary' | 'accent';
  className?: string;
  onClick?: () => void;
  hoverable?: boolean;
}

// Variant base classes:
// default: "bg-white border border-black/5 shadow-card"
// warm:    "bg-[#FDF9F4] border border-black/5 shadow-card"
// primary: "bg-gradient-to-br from-[#1A2E4A] to-[#2A4A6A] border-none shadow-elevated"
// accent:  "bg-white border border-accent/20 shadow-card"

// All variants share: rounded-ff p-4 transition-all duration-150
// hoverable=true adds: cursor-pointer active:scale-[0.99] active:shadow-pressed
```

**NEVER add `shadow-2xl`, `shadow-xl`, `shadow-primary/30` to FFCard — always use `shadow-card`.**

### 2.2 FFButton

```
Primary:   bg-primary text-white rounded-xl h-12 px-5 font-body font-bold text-sm
Secondary: border border-primary text-primary bg-transparent (same shape)
Accent:    bg-accent text-white (same shape — child CTAs, rewards)
Destructive: bg-alert text-white (same shape — delete, SOS)
Ghost:     bg-transparent text-primary no border (inline links only)

All buttons: min h-12 (48px). Never smaller.
Active state: opacity-80 scale-[0.97] — no dramatic shadow glow.
PROHIBITED on buttons: italic, font-black, tracking-[0.2em]+, uppercase on Primary/Secondary
```

### 2.3 FFAvatar

```
Shape: rounded-full
Sizes: w-10 h-10 (small), w-12 h-12 (medium), w-16 h-16 (large)
Fallback: Initials on bg-primary background, text-white, font-display font-bold
Border: 2px solid border-black/10 on white backgrounds; 2px solid white/20 on dark backgrounds
```

### 2.4 FFBadge

```
Shape: rounded-full px-2.5 py-0.5
Text: text-[10px] font-body font-bold uppercase tracking-wider (max 2 words)
Variants:
  success: bg-success/10 text-success border border-success/20
  alert:   bg-alert/10 text-alert border border-alert/20
  accent:  bg-accent/10 text-accent border border-accent/20
  primary: bg-primary/10 text-primary border border-primary/20
  neutral: bg-black/5 text-gray-500
```

**NEVER use raw ALL CAPS text longer than 2 words on a badge. Use short warm labels: "Active", "Online", "7 days", "Due Today".**
**PROHIBITED badge labels: "HOT_STREAK", "STATION_ONLINE", "CORE_OS_v3.4.0", compound_word labels**

### 2.5 FFShimmer

```
Used for: every async load state — NEVER show blank white space during loading
Shape: match the shape of the content it replaces — rounded-ff for cards, rounded-full for avatars
Base: bg-black/5 with shimmer animation (gradient sweep left-to-right)
Duration: 1.5s infinite
```

### 2.6 FFEmptyState

```
Icon: 48x48 relevant Lucide icon, text-gray-400
Heading: font-display font-semibold text-primary text-base (Title Case, not ALL CAPS)
Body: font-body text-gray-500 text-sm text-center max-w-[240px] mx-auto
CTA: FFButton primary, centered (only when the user can take action)
Never leave a screen empty without this component.
Demo mode NEVER shows FFEmptyState on first load.
```

### 2.7 FFErrorState

```
Icon: alert-circle Lucide icon, text-alert
Heading: "Something went wrong" (friendly English — never technical)
Body: Short explanation in plain language + "Try again" FFButton secondary
Never show raw error codes, stack traces, or technical messages to users.
```

### 2.8 AppNavShell

The shell layout with top header, bottom nav (mobile), and sidebar (desktop).

**Top header spec:**
```
Height: h-16 (64px)
Background: bg-white border-b border-black/5
Left: App logo icon (32x32, bg-primary rounded-xl) + "FamilyFirst" text (font-display font-bold text-xl text-primary)
      + "PREMIUM CARE" subline (text-[9px] font-body font-bold tracking-wider text-accent uppercase)
Right: Bell (with unread badge) + Avatar/User icon + Logout button
Shadow: shadow-header
```

**Bottom navigation (mobile) — MANDATORY RULES:**
```
Height: h-16 (64px)
Background: bg-primary
Position: fixed bottom-0 left-0 right-0 (NOT floating with bottom-4 margin)
Border top: border-t border-white/10

Each nav item MUST have BOTH:
  - Icon: Lucide icon, size 22, centered
  - Label: text-[10px] font-body font-semibold mt-0.5 (below icon)

Active: icon + label text-accent; 3px rounded-full dot above icon bg-accent
Inactive: icon + label text-white/50
PROHIBITED: icon-only nav (no label). Labels are MANDATORY on all mobile nav items.
PROHIBITED: floating nav with bottom margin (use bottom-0 flush to edge)
```

**Sidebar (desktop only):**
```
Width: w-64
Background: bg-white border-r border-black/5
Active item: bg-primary text-white rounded-ff-sm
Inactive item: text-gray-400 hover:bg-gray-50 hover:text-primary
```

### 2.9 FFPageHeader ← REQUIRED SHARED COMPONENT (create if not present)

Every screen uses this. Never build a custom header in a screen file.

```tsx
// Mobile/src/shared/components/FFPageHeader.tsx
interface FFPageHeaderProps {
  title: string;                    // Max 24 chars. Title Case. No ALL CAPS.
  subtitle?: string;                // Optional supporting line below title
  showBack?: boolean;               // Shows chevron-left; onClick calls navigate(-1)
  rightAction?: React.ReactNode;    // Max 2 icon buttons (44x44 touch target)
  variant?: 'home' | 'detail';     // home = logo+name; detail = back+title
}

// Base styles:
// bg-primary, h-16, px-4, flex items-center justify-between
// shadow-header, sticky top-0 z-40

// Home variant left slot:
//   Logo icon (24x24 text-accent) + "FamilyFirst" (font-display font-bold text-base text-white)
//   + role subtitle (text-accent text-[10px] font-body font-semibold uppercase tracking-wider)

// Detail variant left slot:
//   ChevronLeft (24x24, text-white, 44x44 touch target button)
//   + title (font-display font-semibold text-base text-white truncate)
```

### 2.10 FFSectionHeader ← REQUIRED SHARED COMPONENT (create if not present)

Used to title every section within a screen.

```tsx
// Mobile/src/shared/components/FFSectionHeader.tsx
interface FFSectionHeaderProps {
  icon: React.ReactNode;       // Lucide icon — rendered at 18x18
  title: string;               // Max 3 words. No compound_word labels.
  rightAction?: React.ReactNode; // "See All" ghost button or count badge
}

// Layout: flex items-center gap-2
// Icon container: w-8 h-8 rounded-ff-sm bg-accent/10 flex items-center justify-center text-accent
// Title: font-display font-semibold text-xs text-primary uppercase tracking-wider
//   → Title MUST be max 3 words. Plain English. Warm language.
// Right action: text-accent text-xs font-body font-semibold (ghost, no border)
```

---

## 3. Screen Layout System

### 3.1 Base Page Structure (ALL screens)

```tsx
// Every screen uses this exact wrapper structure:
<div className="min-h-screen bg-bg-cream">
  {/* No custom header — always use AppNavShell (which provides the top header) */}
  {/* OR for screens without AppNavShell (detail screens): */}
  <FFPageHeader title="Screen Title" showBack />

  <main className="px-4 py-5 space-y-6 pb-24">
    {/* pb-24 ensures content clears the 64px bottom nav */}
    {children}
  </main>
  {/* AppNavShell provides bottom nav — no separate nav component needed */}
</div>
```

### 3.2 Dashboard / Home Screen Pattern

Used by: ParentHomeScreen, ChildHomeScreen, ElderHomeScreen, TeacherHomeScreen, AdminDashboardScreen

```
1. Welcome Header Card (full-width, bg-gradient-to-br from-[#1A2E4A] to-[...], rounded-ff-lg p-5)
   - Greeting: "Good morning, [FirstName]" — font-display font-bold text-xl text-white
     → ENGLISH ONLY. No Chinese characters. No "STATION" suffix. No military compound words.
   - Role subtitle + today's date — font-body text-white/70 text-xs
   - Optional: one-line role-specific quick stat in text-white/60

2. Stat Cards Row (grid-cols-2 gap-3)
   - Each: FFCard (default variant) with icon + metric + label + optional FFBadge trend
   - Metric: font-numbers font-medium text-2xl text-primary
   - Icon container: w-10 h-10 rounded-ff-sm bg-primary/10 text-primary
   - Label: font-body text-xs text-gray-400 uppercase tracking-wider

3. Section(s): FFSectionHeader + content cards
   - Each section has exactly one FFSectionHeader
   - Section order and count per role in §4

4. Loading: FFShimmer (shape-matched skeletons) — NOT custom animate-pulse divs
5. Empty: FFEmptyState — NOT blank screen
6. Error: FFErrorState — NOT raw error text
```

### 3.3 Detail Screen Pattern

```
1. FFPageHeader with showBack=true
2. Hero Card (FFCard variant="primary" or default — full-width, shows main entity)
3. Attribute rows (label + value pairs inside FFCard)
4. Action button(s) at bottom:
   - Primary: FFButton primary, full width
   - Secondary: FFButton secondary below
```

### 3.4 List Screen Pattern

```
1. FFPageHeader with showBack=true (+ optional right Add button)
2. Search bar (h-12 rounded-xl bg-white border border-black/5 font-body text-sm)
3. Filter chips row (horizontal scroll, rounded-full chips, active: bg-primary text-white)
4. List of FFCard items (icon/avatar + title + subtitle + right chevron or FFBadge)
5. FFEmptyState when list is empty
6. FFShimmer (3–5 skeleton rows) during load
7. "Load more" FFButton ghost at bottom
```

### 3.5 Form Screen Pattern

```
1. FFPageHeader showBack=true + "Save" ghost right action (text-accent font-body font-bold)
2. Form sections as FFCards (group related fields)
3. Input fields: h-12 rounded-xl border border-black/5 bg-white font-body text-sm text-primary
   - Label: above input, font-body font-semibold text-xs text-gray-500 uppercase tracking-wider
   - Error: border-alert + text-alert text-xs below
4. Submit: FFButton primary, full-width, fixed bottom above safe area
5. Validation errors inline — never alert() / confirm()
```

### 3.6 Loading Screen Pattern

When a full screen is loading (e.g., initial app load, auth check):

```tsx
// Premium loading — clean, warm, no military text
<div className="min-h-screen bg-bg-cream flex flex-col items-center justify-center gap-6">
  <div className="w-16 h-16 bg-primary rounded-ff flex items-center justify-center">
    <Home className="w-8 h-8 text-accent" />
  </div>
  <div className="text-center space-y-2">
    <p className="font-display font-semibold text-lg text-primary">FamilyFirst</p>
    <p className="font-body text-sm text-gray-400">Loading your family...</p>
  </div>
  {/* Simple 3-dot shimmer — no bounce animation */}
  <div className="flex gap-1.5">
    <div className="w-1.5 h-1.5 rounded-full bg-primary/30 animate-pulse [animation-delay:0ms]" />
    <div className="w-1.5 h-1.5 rounded-full bg-primary/30 animate-pulse [animation-delay:150ms]" />
    <div className="w-1.5 h-1.5 rounded-full bg-primary/30 animate-pulse [animation-delay:300ms]" />
  </div>
</div>

// PROHIBITED loading text: "BOOTING_COMMAND_STATION...", "LOADING TACTICAL SYSTEMS...",
//   "INITIALIZING...", "CONNECTING TO STATION...", "BOOT SEQUENCE", any ALL_CAPS_WITH_UNDERSCORES
```

### 3.7 Settings Screen Pattern

```
1. FFPageHeader showBack=true
2. Profile summary FFCard (avatar + name + role FFBadge)
3. Settings groups inside FFCard:
   - Section label above card: font-body font-semibold text-xs text-gray-500 uppercase tracking-wider
   - Each row: icon (text-accent, 20x20) + label (font-body text-sm text-primary) + right slot (toggle/chevron/value)
   - Divider: border-t border-black/5 between rows
4. Danger zone: separate FFCard at bottom with alert-colored destructive actions
```

---

## 4. Role-Based Screen Tone & Language

### 4.1 SuperAdmin — "Control Center"

**Tone:** Confident, clear, professional. Platform operator. Not army general.

**Language replacement table (apply to every single label in admin screens):**

| ❌ PROHIBITED (current) | ✅ REQUIRED (correct) |
|---|---|
| TACTICAL STATION | Control Center |
| GLOBAL INTEL / GLOBAL_INTEL | Analytics |
| CORE_INFRASTRUCTURE_NODES | Platform |
| INFRASTRUCTURE | Platform |
| GATE > | Manage → |
| TELEMETRY / REAL_TIME_SIGNAL_TELEMETRY | Activity Log |
| DUMP_LOG | Export Log |
| STATION ONLINE / STATION_ONLINE | All Systems Active |
| BOOTING_COMMAND_STATION | Loading... |
| V4.4.2 • ONLINE | Version 4.4.2 |
| [INFO] [WARN] [ROOT] [AUTH] [SYS] (terminal badges) | Colored dot + plain text |
| FISCAL_LOGIC | Plans |
| GLOBAL_DIRECTIVES | Templates |
| SIGNAL_CAMPAIGNS | Broadcasts |
| REWARD_LOGISTICS | Rewards |
| SYSTEM_VARIABLES | Configuration |
| FAM_1427, PID_9982 (system IDs in UI) | Family names or masked IDs |
| STATION_ENGINE // v4.4.2 | (remove this footer entirely) |
| UPTIME_PULSE_ARRAY | (remove — not needed on any screen) |

**Dashboard structure (Admin):**
```
Hero welcome card: "Control Center" + "Platform overview" + version FFBadge neutral
Stat cards (2-col grid): Families · Active · New Today · Uptime
  — NO revenue or subscription metrics (subscription feature permanently disabled)
Section "Platform": 2-col grid module cards (Families, Templates, Rewards, Broadcasts, Configuration)
  — NO Plans/Subscription module card (subscription permanently removed)
Section "Activity Log": Clean FFCard list (see §9) — 5 items + Load More
Analytics: accessible via bottom nav Tab 3 (BarChart2 icon), NOT a header button
```

**Subscription — permanently removed (confirmed 2026-06-19):**
- `AppConfig.features.subscriptionEnabled` is and stays `false`
- All subscription/revenue/plan UI is removed from every screen
- `monthlyRevenue`, `churnRate`, `revenueTrend` fields removed from dashboard stats
- Plans module card removed from admin platform grid
- No conditional renders like `AppConfig.features.subscriptionEnabled && (...)` on any screen

**Module card description text (admin platform cards):**
```
Families → "Manage all registered families"
Plans → "Subscription tiers and limits"
Templates → "Task and routine templates"
Rewards → "Global reward catalog"
Broadcasts → "Push notification campaigns"
Configuration → "Platform settings and flags"
```

### 4.2 FamilyAdmin — "Family Dashboard"

**Tone:** CEO of the home. Empowered, organized, in control.

```
Header: "Family Dashboard" + family name subtitle
Stat cards: Members · Children · Pending Tasks · Events
Sections: "My Family" · "Modules" · "Settings"
```

### 4.3 Parent — "Home"

**Tone:** Calm, quick, at-a-glance. Never overwhelming.

**PROHIBITED parent language:**
- "[Name] STATION" as screen title → use "Good morning, [Name]"
- "HOT_STREAK" → use "7-day streak" or FFBadge with "On Fire"
- "OPERATIONAL_STREAK_STABLE" → remove entirely
- "CORE_OS_v3.4.0" → remove entirely
- "UPTIME_PULSE_ARRAY" (animated bars) → remove entirely

**Greeting function — required:**
```typescript
const getGreeting = () => {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
};
// Result: "Good morning, Priya" — English, warm, simple
// PROHIBITED: '晨 MORNING', '午 AFTERNOON', '晩 EVENING' (Chinese characters)
```

**Dashboard structure:**
```
Header card: "Good [time], [FirstName]" + today's date + quick summary line
Stat cards: Tasks Due · Pending Approvals · Unread Feedback
Child cards: one FFCard per child (avatar + name + today tasks done/total + streak)
Section "Today's Tasks": task card list
Section "Recent Feedback": feedback card list
Section "This Week": calendar mini-preview
```

### 4.4 Child — "My World"

**Tone:** Motivating, playful, celebratory. Age 5–17.

**Note:** Child role is the ONLY role where emoji is allowed (sparingly, max 1 per greeting).

```
Header card: "Hi, [Name]! 👋" + streak badge + coin balance visible
Hero: Coin balance (FFCard variant="primary", font-numbers font-bold text-3xl text-white)
Sections: "Today's Tasks" · "My Rewards" · "My Scores" · "Family"
```

**Key child UX rules:**
- Coin balance always visible (hero card, not buried)
- Streak: flame icon (🔥 emoji OR Flame Lucide icon) + count in text-orange-600
- Task completion: brief scale animation + coin earn float-up (see §10.4)

### 4.5 Elder — "Family"

**Tone:** Warm, inclusive, large and clear. No complexity.

```
Header: "Family" + time-of-day greeting in warm phrase ("Namaste, [Name]")
Large cards: one per grandchild — large avatar + name + one-line update
Sections: "Family Updates" · "Calendar" · "Send Appreciation"
```

**Elder-specific rules:**
- Body text at `text-base` (16px), not `text-sm` — one step larger
- Max 3 bottom nav items
- "Send Appreciation" is the primary CTA — always prominent

### 4.6 Teacher — "My Sessions"

**Tone:** Efficient, professional, fast. 60 seconds per session.

```
Header: "My Sessions" + today's date
Quick action: FFButton primary "Mark Attendance" — full-width, at top
Active session card (if any): FFCard accent variant, prominent
Sections: "Today" · "Recent Feedback" · "History"
```

---

## 5. Navigation System

### 5.1 Bottom Navigation — Per Role

| Role | Tab 1 | Tab 2 | Tab 3 | Tab 4 |
|---|---|---|---|---|
| SuperAdmin | Dashboard | Families | Analytics | Config |
| FamilyAdmin | Dashboard | Family | Modules | Settings |
| Parent | Home | Family | Calendar | Settings |
| Child | My Day | Rewards | Scores | Family |
| Elder | Family | Calendar | Settings | — |
| Teacher | Sessions | Feedback | History | Settings |

**Every tab MUST have both icon AND label. No exceptions.**

**Nav item specs:**
```
Container: min-w-[64px] flex flex-col items-center justify-center py-2 gap-0.5
Icon: Lucide icon size=22
Label: font-body font-semibold text-[10px]
Active: text-accent (icon + label)
Inactive: text-white/50 (icon + label)
Active indicator: w-6 h-0.5 rounded-full bg-accent above icon
```

### 5.2 Page Navigation

- Back: always `ChevronLeft` Lucide icon in FFPageHeader, 44x44 touch target
- Never `window.history.back()` — always `navigate(-1)` or explicit `navigate('/path')`
- Deep screens (3+ levels): show parent name as subtitle in FFPageHeader

---

## 6. Card Patterns

### 6.1 Stat / KPI Card (2-col grid)

```tsx
<FFCard className="p-4">
  <div className="flex items-start justify-between mb-3">
    <div className="w-10 h-10 rounded-ff-sm bg-primary/10 flex items-center justify-center">
      <UsersIcon className="w-5 h-5 text-primary" />
    </div>
    <FFBadge variant="success">+5%</FFBadge>
  </div>
  <p className="font-numbers font-medium text-2xl text-primary">42</p>
  <p className="font-body text-xs text-gray-400 mt-0.5 uppercase tracking-wider">Families</p>
</FFCard>
```

### 6.2 Module / Navigation Card (2-col grid)

```tsx
<FFCard
  className="p-4 cursor-pointer"
  hoverable
  onClick={() => navigate(path)}
>
  <div className="w-10 h-10 rounded-ff-sm bg-accent/10 flex items-center justify-center mb-3">
    <UsersIcon className="w-5 h-5 text-accent" />
  </div>
  <p className="font-display font-semibold text-sm text-primary">{label}</p>
  <div className="flex items-center gap-1 mt-1">
    <p className="font-body text-xs text-gray-400">{description}</p>
    <ChevronRight className="w-3 h-3 text-gray-300" />
  </div>
</FFCard>

// PROHIBITED: "GATE >" · "Fiscal Logic" · "Global Directives" · "Signal Campaigns"
// REQUIRED: warm descriptions ("Manage all families", "Subscription tiers")
```

### 6.3 Person / Family Member Card

```tsx
<FFCard className="p-4 flex items-center gap-3" hoverable onClick={...}>
  <FFAvatar size="medium" name={member.name} imageUrl={member.photo} />
  <div className="flex-1 min-w-0">
    <p className="font-display font-semibold text-sm text-primary truncate">{member.name}</p>
    <p className="font-body text-xs text-gray-400">{member.role} · Age {member.age}</p>
  </div>
  <FFBadge variant="success">Active</FFBadge>
</FFCard>
```

### 6.4 Task Card

```tsx
<FFCard className="p-4">
  <div className="flex items-start gap-3">
    <div className={`w-10 h-10 rounded-ff-sm flex-shrink-0 flex items-center justify-center
      ${task.isComplete ? 'bg-success/10' : 'bg-accent/10'}`}>
      <CheckCircle2 className={`w-5 h-5 ${task.isComplete ? 'text-success' : 'text-accent'}`} />
    </div>
    <div className="flex-1 min-w-0">
      <p className="font-display font-semibold text-sm text-primary truncate">{task.title}</p>
      <p className="font-body text-xs text-gray-400 mt-0.5">{task.dueLabel}</p>
    </div>
    <div className="flex items-center gap-1 flex-shrink-0">
      <span className="font-numbers font-medium text-sm text-accent">{task.coins}</span>
      <span className="text-xs text-amber-500">⭐</span>
    </div>
  </div>
</FFCard>
```

### 6.5 Coin / Reward Hero Card (Child only)

```tsx
<FFCard variant="primary" className="p-5">
  <div className="flex items-center justify-between">
    <div>
      <p className="font-body text-white/70 text-xs uppercase tracking-wider">My Coins</p>
      <div className="flex items-center gap-2 mt-1">
        <Star className="w-6 h-6 text-amber-400" />
        <p className="font-numbers font-bold text-3xl text-white">{coinBalance}</p>
      </div>
    </div>
    <div className="text-right">
      <p className="font-body text-white/70 text-xs">Streak</p>
      <p className="font-numbers font-bold text-xl text-orange-400">{streak} days 🔥</p>
    </div>
  </div>
</FFCard>
```

### 6.6 Welcome / Hero Header Card (all roles)

```tsx
<div className="rounded-ff-lg bg-gradient-to-br from-[#1A2E4A] to-[#243755] p-5">
  <div className="flex items-center justify-between">
    <div className="flex-1 min-w-0">
      <p className="font-body text-white/70 text-xs">{getGreeting()}</p>
      <p className="font-display font-bold text-xl text-white truncate">{firstName}</p>
    </div>
    <FFAvatar size="medium" name={user.name} imageUrl={user.photo} />
  </div>
  <p className="font-body text-white/60 text-xs mt-3">{quickSummary}</p>
</div>
```

---

## 7. State Management Patterns

### 7.1 Loading State

```tsx
{isLoading && (
  <div className="space-y-3">
    <FFShimmer className="h-28 rounded-ff-lg w-full" />  {/* hero card */}
    <div className="grid grid-cols-2 gap-3">
      <FFShimmer className="h-20 rounded-ff" />
      <FFShimmer className="h-20 rounded-ff" />
    </div>
    <FFShimmer className="h-16 rounded-ff" />
    <FFShimmer className="h-16 rounded-ff" />
    <FFShimmer className="h-16 rounded-ff" />
  </div>
)}
// PROHIBITED: inline animate-pulse divs with rounded-[48px] or rounded-[56px]
// PROHIBITED: custom loading skeleton that doesn't match content shape
```

### 7.2 Empty State

```tsx
{!isLoading && items.length === 0 && (
  <FFEmptyState
    icon={<CheckCircle2 className="w-12 h-12 text-gray-300" />}
    title="No Tasks Yet"
    message="Tasks assigned to you will appear here."
    action={canCreate ? <FFButton>Add First Task</FFButton> : undefined}
  />
)}
// Demo mode: NEVER show FFEmptyState. Always return 3–5 mock items.
```

### 7.3 Error State

```tsx
{error && (
  <FFErrorState
    message="Couldn't load right now."
    onRetry={() => fetchData()}
  />
)}
```

### 7.4 Success Feedback

- Form save: toast notification — slides from top, bg-success text-white, rounded-ff, 3 seconds
- Task complete: card scale-up animation + coin float (see §10.4)
- NEVER: `window.alert()`, `window.confirm()`, browser dialogs

---

## 8. Form Standards

### 8.1 Input Field

```tsx
<div className="space-y-1.5">
  <label className="font-body font-semibold text-xs text-gray-500 uppercase tracking-wider">
    {label}
  </label>
  <input
    className="w-full h-12 px-4 bg-white border border-black/5 rounded-xl
               font-body text-sm text-primary
               focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/20
               placeholder:text-gray-300"
  />
  {error && <p className="font-body text-xs text-alert">{error}</p>}
</div>
```

### 8.2 Toggle / Switch

```
Custom toggle: w-12 h-6, thumb w-5 h-5
Active: bg-success; Inactive: bg-black/10
Always paired with a label: label (left) + toggle (right)
```

### 8.3 Photo Upload

```tsx
<div className="w-full h-40 rounded-ff-lg border-2 border-dashed border-black/10
                bg-[#FDF9F4] flex flex-col items-center justify-center gap-2 cursor-pointer">
  <Upload className="w-8 h-8 text-gray-300" />
  <p className="font-body text-sm text-gray-400">Tap to upload photo</p>
  <p className="font-body text-xs text-gray-300">JPG, PNG up to 10MB</p>
</div>
```

---

## 9. Activity Log Pattern (SuperAdmin Only)

The terminal-style dark log panel is **permanently removed**. Replace with:

```tsx
<FFCard className="p-4">
  <FFSectionHeader icon={<Activity />} title="Activity Log" rightAction={
    <button className="font-body text-xs text-accent font-semibold">Export</button>
  } />
  <div className="space-y-3 mt-4">
    {logs.map(log => (
      <div key={log.id} className="flex items-start gap-3 py-2 border-b border-black/5 last:border-0">
        <div className={`w-2 h-2 rounded-full mt-1.5 flex-shrink-0 ${dotColor(log.type)}`} />
        <div className="flex-1 min-w-0">
          <p className="font-body text-sm text-primary truncate">{log.message}</p>
          <p className="font-body text-xs text-gray-400">{log.timestamp}</p>
        </div>
        <FFBadge variant={log.variant}>{log.type}</FFBadge>
      </div>
    ))}
  </div>
  <FFButton variant="ghost" className="w-full mt-3">Load More</FFButton>
</FFCard>

// Log dot colors:
// INFO → bg-primary/60 · WARN → bg-accent · AUTH → bg-success · ERROR → bg-alert · SYS → bg-black/20
// Log badge labels: "Info" · "Warning" · "Auth" · "Error" · "System" (Title Case, not ALL CAPS)
```

**PROHIBITED: Any dark/black terminal panel in any FamilyFirst screen.**

---

## 10. Animation & Motion

### 10.1 Page Transitions

```css
/* All screens: fade-up entrance only */
@keyframes fadeUp {
  from { opacity: 0; transform: translateY(8px); }
  to   { opacity: 1; transform: translateY(0); }
}
.page-enter { animation: fadeUp 200ms ease-out; }
```

### 10.2 Card Interactions

```css
/* All hoverable/tappable cards */
active:scale-[0.98] transition-transform duration-150
/* hover (desktop): -translate-y-0.5 scale-[1.01] */
/* PROHIBITED: whileHover={{ y: -4, scale: 1.01 }} with duration 0.3+ (too dramatic) */
```

### 10.3 Button Press

```css
active:scale-[0.97] active:opacity-80
transition: transform 100ms ease, opacity 100ms ease;
```

### 10.4 Coin Earn Animation (Child screens only)

```
1. Task card: scale-[1.02] over 150ms then returns
2. Coin icon: translate-y from 0 to -20px, opacity 1→0, over 500ms
3. Coin balance: count increments over 600ms (no raw state mutation — use CSS counter animation)
```

### 10.5 Prohibited Animations

- `Math.random()` inside animation `transition.duration` — produces jittery "system pulse" look
- Animated bar charts on home/dashboard screens (the "uptime pulse array" pattern)
- `rotate-360` on loading — use FFShimmer
- Bounce animations on nav items
- Full-screen transitions longer than 300ms
- `whileTap` / `whileHover` with scale > 1.02 on cards
- `shadow-primary/30` dramatic glow on hover

---

## 11. Mobile Overflow Prevention

### 11.1 Text Overflow Rules

- Card titles: always `truncate` (single line) OR `line-clamp-2` (two lines max)
- Section headings: `truncate` if combined with `tracking-wider`
- No `whitespace-nowrap` unless inside an `overflow-hidden truncate` container
- Button labels: max 20 characters on mobile
- `min-w-0` on every `flex-1` text container

### 11.2 Grid Overflow Rules

- `grid-cols-2` items must have `min-w-0` on the text container
- Icons: always `flex-shrink-0`
- Text beside icons: always `flex-1 min-w-0`

### 11.3 Screen Width Check Protocol

Before signing off any screen, verify at 375px:
1. Header row fits without wrapping or overflow
2. All grid cards within bounds (no overflow-x)
3. All button labels fully visible
4. No horizontal scrollbar on main content
5. Bottom nav labels visible (not clipped)
6. No text overflowing card edges

---

## 12. Demo Mode Rules

- `AppConfig.isDemo = true` → every repository method returns mock data inline
- Mock data MUST match the live `ApiResponse<T>` shape — no field name differences
- Mock data MUST be meaningful (real Indian names, plausible numbers, valid dates)
- Every screen MUST show at least 3 meaningful items in demo mode
- PROHIBITED: `FFEmptyState` on any screen's first demo load
- REQUIRED demo data minimums:
  - At least 3 items in every list
  - Plausible coin balance (e.g., 240, not 0)
  - Plausible streak count (e.g., 5, not 0)
  - At least 1 unread notification
  - At least 1 pending task
  - Real-looking Indian family names (Priya, Aarav, Kavitha, Rajan — not "User 1")

---

## 13. Accessibility

- All interactive elements: minimum 48x48px touch target (use `.touch-target` CSS class)
- All images: meaningful `alt` attribute
- Color is NEVER the only differentiator — always pair with icon or text label
- Error states: always icon + text, not just color change
- Loading containers: `aria-busy="true"`
- Form labels: always visually present — no `sr-only` labels in production
- Bottom nav items: `aria-label` on each NavLink button

---

## 14. Shared Component Reuse Checklist

Before creating any new component, verify this list first:

| Need | Use |
|---|---|
| Card wrapper | `FFCard` (with appropriate variant) |
| Any button | `FFButton` (with appropriate variant) |
| User photo | `FFAvatar` |
| Status / count chip | `FFBadge` |
| No data / empty | `FFEmptyState` |
| Network error | `FFErrorState` |
| Loading skeleton | `FFShimmer` |
| Page shell + nav | `AppNavShell` |
| Section title | `FFSectionHeader` |
| Top header | `FFPageHeader` |

**NEVER build a custom header, nav bar, section title, or button from scratch in a screen file.**

---

## 15. Prohibited Patterns Registry

This section lists patterns found in the codebase that are permanently banned. Codex must search for and replace all instances.

### 15.1 Prohibited Text / Labels

```
PROHIBITED strings (exact match — replace with correct equivalent):
"TACTICAL STATION"     → "Control Center"
"GLOBAL INTEL"         → "Analytics"
"GLOBAL_INTEL"         → "Analytics"
"INFRASTRUCTURE"       → "Platform"
"GATE >"               → "Manage →"
"TELEMETRY"            → "Activity Log"
"DUMP_LOG"             → "Export Log"
"STATION ONLINE"       → "All Systems Active"
"STATION_ONLINE"       → "All Systems Active"
"BOOTING_COMMAND"      → "Loading..."
"CORE_OS_v"            → (remove)
"UPTIME_PULSE"         → (remove)
"OPERATIONAL_STREAK"   → (remove)
"HOT_STREAK"           → "On Fire" (or just show streak count)
"FISCAL_LOGIC"         → "Plans"
"GLOBAL_DIRECTIVES"    → "Templates"
"SIGNAL_CAMPAIGNS"     → "Broadcasts"
"REWARD_LOGISTICS"     → "Rewards"
"SYSTEM_VARIABLES"     → "Configuration"
"STATION_ENGINE"       → (remove)
"晨 MORNING"           → "Good morning"
"午 AFTERNOON"         → "Good afternoon"
"晩 EVENING"           → "Good evening"
"[Name] STATION"       → "Good [time], [Name]"
```

### 15.2 Prohibited Icon Usage

These Lucide React icons are **banned from all FamilyFirst screens**:

```
BANNED in ALL screens:
Terminal      — surveillance/developer aesthetic
Fingerprint   — surveillance/biometric, inappropriate for family app
Radar         — military
Command       — developer tool
Network       — infrastructure/IT
Cpu           — hardware/IT
Workflow      — enterprise software
Layers        — technical/developer
Activity      — allowed ONLY in Admin Activity Log section (§9); banned elsewhere
Zap           — allowed ONLY in system status; banned as decorative on dashboards
ShieldCheck   — allowed ONLY for auth/security-specific screens; banned as decorative logo
```

**Recommended alternatives:**

| Instead of | Use |
|---|---|
| Terminal | Activity, List, ClipboardList |
| Fingerprint | Lock, Key, ShieldCheck (security context only) |
| Radar | TrendingUp, BarChart2, LineChart |
| Command | Settings, Sliders |
| Network | Users, GitBranch |
| Cpu | Server (admin only), Zap (admin only) |
| Workflow | GitMerge, ArrowRight |
| Layers | LayoutDashboard |

### 15.3 Prohibited CSS Patterns

```
BANNED class combinations:
font-black                         → max font-bold (700)
italic (on headings/labels)        → remove italic entirely
tracking-[0.2em] or wider         → max tracking-widest (0.1em) on badges only
rounded-[28px]+                    → use rounded-ff, rounded-ff-sm, rounded-ff-lg
p-12 (as mobile base)             → use p-4 sm:p-5 lg:p-6
p-8 lg:p-14 (extreme desktop)    → use p-4 sm:p-6 lg:p-8
space-y-24 (as mobile base)       → use space-y-6 sm:space-y-8 lg:space-y-10
pb-48                              → use pb-24
gap-10 (as mobile base)           → use gap-3 sm:gap-4 lg:gap-6
min-h-[320px] on stat cards       → let content define height
shadow-2xl shadow-primary/30      → use shadow-card (warm navy shadow)
shadow-2xl shadow-black/[0.01]    → use shadow-card
bg-[#FDFCFB]                      → use bg-bg-cream (token, not raw hex)
```

### 15.4 Prohibited Component Patterns

```
BANNED structural patterns:
- Custom <header> built inside a screen file (use FFPageHeader)
- Custom section title <div> built inline (use FFSectionHeader)
- <nav> built inside a screen (AppNavShell provides this)
- Dark/black terminal panel with font-mono text
- Animated bar chart (pulse array) to show "uptime" on user screens
- Large (>100px) watermark icon as decorative background element
- Math.random() in Framer Motion transition.duration
- animate-pulse on an entire section wrapper
- rounded-[40px]+ arbitrary radius on standard cards
```

---

## 16. Icon Usage Guide Per Role

### Approved icon sets per role context:

**Parent screens:** Home, Users, Calendar, Bell, CheckCircle2, Clock, Star, FileText, Settings, ChevronRight, Plus, Trash2, Edit, Eye, AlertCircle, RefreshCw, Filter, Search, Download, Upload

**Child screens:** Home, Coins, Trophy, Star, CheckCircle2, Flame (streak), Target, Zap (energy/coins earned), Gift, Smile, BookOpen, Music, Palette, ChevronRight

**Elder screens:** Home, Calendar, Heart, Users, Bell, Send, Clock, Settings, ChevronRight

**Teacher screens:** Home, Star, Calendar, CheckCircle2, List, Edit, Clock, Users, ChevronRight, Plus, AlertCircle, BookOpen, Award

**SuperAdmin screens:** LayoutDashboard, Users, CreditCard, ShoppingBag, Bell, Settings, TrendingUp, BarChart2, FileText, ChevronRight, Plus, Edit, Trash2, Activity (log only), Server (status only), Download

**FamilyAdmin screens:** Shield, Users, Eye, Bell, Settings, Lock, ToggleLeft, ChevronRight

---

## 17. Required index.css State

The `Mobile/src/index.css` `@theme` block must contain exactly these values. Update if different.

```css
@theme {
  /* Fonts */
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
  --font-display: "Poppins", sans-serif;
  --font-body: "Nunito", sans-serif;
  --font-numbers: "Space Grotesk", sans-serif;

  /* Brand colors */
  --color-primary: #1A2E4A;
  --color-accent: #C8922A;
  --color-success: #2D6A4F;
  --color-alert: #C1121F;
  --color-bg-cream: #F8F4EE;

  /* Border radius tokens */
  --radius-ff: 16px;      /* rounded-ff — all standard cards */
  --radius-ff-sm: 8px;    /* rounded-ff-sm — inner cards, buttons, icon containers */
  --radius-ff-lg: 24px;   /* rounded-ff-lg — hero cards, bottom sheets */

  /* Warm navy-tinted shadows (replaces cold gray shadows) */
  --shadow-card:     0 2px 12px rgba(26, 46, 74, 0.08), 0 1px 4px rgba(26, 46, 74, 0.04);
  --shadow-elevated: 0 8px 32px rgba(26, 46, 74, 0.12), 0 2px 8px rgba(26, 46, 74, 0.06);
  --shadow-header:   0 1px 8px rgba(26, 46, 74, 0.10);
  --shadow-pressed:  0 1px 4px rgba(26, 46, 74, 0.10);
}

@layer base {
  body { @apply bg-bg-cream text-primary font-body antialiased; }

  /* h1-h4: Poppins Bold, tight tracking — NEVER italic, NEVER font-black */
  h1 { @apply text-xl font-display font-bold tracking-tight sm:text-2xl; }
  h2 { @apply text-lg font-display font-bold tracking-tight sm:text-xl; }
  h3 { @apply text-base font-display font-semibold tracking-tight; }
  h4 { @apply text-sm font-display font-semibold tracking-tight; }

  .font-numbers { font-family: var(--font-numbers); }
  .touch-target { @apply min-h-[48px] min-w-[48px] flex items-center justify-center; }
}

@layer components {
  /* Standard card — warm navy shadow, not cold gray */
  .ff-card {
    @apply bg-white rounded-ff shadow-card border border-black/5 p-4 transition-all duration-150;
  }
  .ff-card-hover {
    @apply hover:shadow-elevated hover:-translate-y-0.5 cursor-pointer;
  }
  /* Active/pressed state */
  .ff-card-tap {
    @apply active:scale-[0.98] active:shadow-pressed;
  }

  @keyframes fadeUp {
    from { opacity: 0; transform: translateY(8px); }
    to   { opacity: 1; transform: translateY(0); }
  }
  .page-enter { animation: fadeUp 200ms ease-out; }

  @keyframes shimmer {
    from { background-position: -200% 0; }
    to   { background-position: 200% 0; }
  }
  .shimmer {
    background: linear-gradient(90deg, #f0ebe4 25%, #e8e2d9 50%, #f0ebe4 75%);
    background-size: 200% 100%;
    animation: shimmer 1.5s infinite;
  }

  @keyframes shake {
    0%, 100% { transform: translateX(0); }
    25% { transform: translateX(-4px); }
    75% { transform: translateX(4px); }
  }
  .animate-shake { animation: shake 0.2s ease-in-out 2; }

  .sr-only {
    @apply absolute w-px h-px p-0 -m-px overflow-hidden whitespace-nowrap border-0;
    clip: rect(0, 0, 0, 0);
  }
}
```

---

## 18. Codex Execution Rules (Per Module)

For **every module** Codex touches, in this exact order:

1. **Read UIStandertds.md fully.** No exceptions.
2. Search the target screen file(s) for every string in §15.1 (Prohibited Text). Replace all found.
3. Search for every prohibited icon in §15.2. Replace with approved alternatives.
4. Search for every prohibited CSS pattern in §15.3. Replace with correct values.
5. Apply the correct role tone from §4 for this module's role.
6. Ensure the screen uses `FFPageHeader` (not a custom header).
7. Ensure every section uses `FFSectionHeader` (not a custom div).
8. Ensure all cards use `FFCard` with `shadow-card` (no custom shadow overrides).
9. Ensure all buttons use `FFButton` and are min `h-12` (48px).
10. Ensure bottom nav (in AppNavShell) has labels on all items for this role.
11. Ensure all async sections have 3 states: FFShimmer → content/FFEmptyState → FFErrorState.
12. Ensure demo mode returns 3+ meaningful items in every list.
13. Ensure `index.css` shadows are updated to warm navy values (§17).
14. Run `tsc --noEmit` — 0 TypeScript errors required.
15. Render-verify at 375px — no overflow, no blank states, no horizontal scroll.
16. Report `UNVERIFIED — needs visual check` if render cannot be verified. Never report `COMPLETE` on unverified UI.

---

## 19. Delivery Checklist (Required Before Any Screen Is Called DONE)

```
[ ] Design tokens used — no hardcoded hex colors in components
[ ] Role tone correct — language matches §4 for this role
[ ] All prohibited text replaced (§15.1) — zero military/tactical/compound labels
[ ] All prohibited icons replaced (§15.2)
[ ] All prohibited CSS patterns replaced (§15.3)
[ ] index.css shadow tokens updated to warm navy values (§17)
[ ] FFPageHeader used — no custom header div
[ ] FFSectionHeader used — no custom section title div
[ ] Mobile-first spacing — all base values are mobile size per §1.3
[ ] No prohibited spacing/radius/type values (§1.2, §1.3, §1.4)
[ ] Bottom nav has labels on ALL tabs (AppNavShell — no icon-only nav)
[ ] Bottom nav positioned bottom-0 (not floating with bottom-4 gap)
[ ] All cards use FFCard with shadow-card (no shadow-2xl, no shadow-primary/30)
[ ] All buttons use FFButton, min h-12 (48px)
[ ] All text truncated where overflow risk exists
[ ] All font weights max font-bold (700) — no font-black, no font-extrabold
[ ] No italic text anywhere in the screen
[ ] No ALL CAPS headings longer than 3 words
[ ] 3 async states present: FFShimmer / content or FFEmptyState / FFErrorState+retry
[ ] Demo mode shows 3+ meaningful items — no blank lists
[ ] Demo data matches live ApiResponse<T> shape exactly
[ ] tsc --noEmit passes — 0 errors
[ ] Rendered at 375px — no horizontal overflow, no blank screens, no bottom nav without labels
[ ] Visual UI: actually RENDERED and verified — if not renderable, marked UNVERIFIED, not COMPLETE
```

---

## 20. Issue Log (Ongoing — Append Only, Never Delete)

| Date | Screen | Issue Found | Fix Required |
|---|---|---|---|
| 2026-06-19 | AdminDashboardScreen | Military language throughout: "TACTICAL STATION", "GLOBAL_INTEL", "GATE >", "TELEMETRY", "DUMP_LOG", "BOOTING_COMMAND_STATION", "STATION_ONLINE", "STATION_ENGINE", "FISCAL_LOGIC" | Apply §15.1 replacement table. Replace terminal panel with §9 activity log. |
| 2026-06-19 | AdminDashboardScreen | `rounded-[40px]`, `rounded-[56px]`, `p-12`, `space-y-20`, `text-5xl italic font-black` on KPI values | Apply §1.3 spacing, §1.4 radius, §1.2 type scale corrections |
| 2026-06-19 | ParentHomeScreen | Chinese characters in greeting: `'晨 MORNING'`, `'午 AFTERNOON'`, `'晩 EVENING'` | Replace with English greeting function per §4.3 |
| 2026-06-19 | ParentHomeScreen | "[Name] STATION" as screen title, "CORE_OS_v3.4.0" badge, "UPTIME_PULSE_ARRAY" animated bars | Remove all per §15.1. Replace with welcome card per §6.6 |
| 2026-06-19 | ParentHomeScreen | `rounded-[56px]` on cards, `p-12`, `space-y-24`, `pb-48`, `text-8xl font-black italic` on streak | Apply §1.3 spacing, §1.4 radius, §1.2 type corrections |
| 2026-06-19 | ParentHomeScreen | Fingerprint, Cpu, Layers, Radar, Network, Command, Zap as decorative icons | Replace per §15.2 icon ban list |
| 2026-06-19 | ChildHomeScreen | Same prohibited icon imports: Fingerprint, Cpu, Layers | Replace per §15.2 |
| 2026-06-19 | AppNavShell (mobile bottom nav) | Icon-only nav — no labels on mobile bottom nav items | Add labels per §2.8. Bottom nav items require both icon AND label. |
| 2026-06-19 | AppNavShell (mobile bottom nav) | `fixed bottom-4 left-4 right-4` floating position with margin | Change to `fixed bottom-0 left-0 right-0` flush per §2.8 |
| 2026-06-19 | FFCard component | Uses cold gray `shadow-premium` (rgba 0,0,0,0.05) — mismatches UIStandertds warm navy shadow | Update `ff-card` CSS class and `index.css` shadow tokens per §17 |
| 2026-06-19 | index.css | Shadow tokens `--shadow-premium*` use cold gray `rgba(0,0,0,...)` | Replace with warm navy `rgba(26,46,74,...)` per §1.5 and §17 |
| 2026-06-19 | All screens | FFPageHeader and FFSectionHeader missing from `shared/components/` | Create both components per §2.9 and §2.10 specs |
| 2026-06-19 | AdminDashboardScreen | Complete rewrite: military aesthetic → premium Control Center | New screen: hero card, 4 KPI cards, Platform module grid, Activity Log per §9 |
| 2026-06-19 | AdminDashboardScreen | Subscription/revenue KPI removed (subscriptionEnabled=false permanently) | KPIs now: Families · Active · New Today · Uptime. No revenue card ever. |
| 2026-06-19 | AppNavShell (SuperAdmin bottom nav) | SuperAdmin had 3 tabs; Analytics was a header button ("INTEL") | Analytics moved to 4th bottom nav tab (BarChart2 icon). Header button removed. |
| 2026-06-19 | AppNavShell (all roles mobile) | Bottom nav floating with `bottom-4 left-4 right-4` gap | Fixed to `bottom-0 left-0 right-0` flush. Labels added. Active indicator bar above icon. |
| 2026-06-19 | AuthRepository.ts | Blank screen: `Cannot read properties of undefined (reading 'SUPER_ADMIN')` | Circular import fixed — `UserRole` moved to `src/core/auth/UserRole.ts` |
| 2026-06-19 | NotificationRepository | `GET /notifications` returned 500 Technical_Error | EF LINQ `.CreatedAt` fixed to `.DateCreated` |

---

*FamilyFirst UI Design System v4.0 — This file governs all 85 screens. Issue Log appended after every fix. No rule reduced without explicit product decision.*
