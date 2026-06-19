# FamilyFirst UI Standards

This file is the working UI rulebook for all FamilyFirst frontend work. It is derived from the confirmed mobile architecture and design-system contract in `API/Docs/Flow/ProjectOverview.md`, especially Section 20.6 and the platform rules in `AGENTS.md`.

## 1. Source Of Truth

- `API/Docs/Flow/ProjectOverview.md` is the canonical design-system and flow contract.
- `API/Docs/Flow/UIStandertds.md` is the fast operational checklist for UI work.
- If there is any conflict, follow `ProjectOverview.md` and update this file to match after verification.

## 2. Core Stack Rules

- Mobile app stack is React 19 + TypeScript + Vite.
- Styling is Tailwind CSS 4 with theme tokens defined in `Mobile/src/index.css`.
- Routing uses React Router DOM.
- API access uses Axios through `src/core/network/apiClient.ts`.
- API data must not be held in local screen `useState` when it represents server state. Use feature providers / context and repository methods.

## 3. Design Tokens

### Colors

| Token | Hex | Usage |
|---|---|---|
| Primary | `#1A2E4A` | Main actions, headers, emphasis |
| Accent | `#C8922A` | Highlights, premium CTAs, active accents |
| Success | `#2D6A4F` | Success states, confirmations |
| Alert | `#C1121F` | Errors, destructive actions |
| Background | `#F8F4EE` | Base page background |

### Typography

| Use | Font |
|---|---|
| Headings | Poppins Bold |
| Body | Nunito Regular |
| Numbers / metrics | Space Grotesk Medium |

### Shape And Elevation

- Default card radius: `16px`
- Small radius: `8px`
- Large radius: `24px`
- Default card shadow: soft premium shadow only
- Standard card treatment should reuse `FFCard` or the `.ff-card` class

## 4. Interaction Rules

- Every interactive target must be at least `48x48px`
- Every async action must present all 3 states:
  loading
  success
  error with retry
- Do not ship blank states without a meaningful message or CTA
- Do not leave users on infinite spinners
- Role-gated actions must check the current authenticated role before rendering action buttons

## 5. Demo Mode Rules

- Demo mode is controlled by `AppConfig.isDemo`
- Every repository method must branch inline at the top of the method for demo/live behavior
- Screen components must not hardcode demo data
- Demo responses must match live response shape
- Demo mode must never render an empty first-load screen unless the real product intentionally does so and that behavior is documented

## 6. Component Rules

- Shared reusable components live under `Mobile/src/shared/components/`
- Shared component names must use the `FF` prefix
- Do not place business logic inside shared components
- Prefer existing shared components before creating new UI primitives:
  `FFButton`
  `FFCard`
  `FFAvatar`
  `FFBadge`
  `FFEmptyState`
  `FFErrorState`
  `FFShimmer`
  `AppNavShell`

## 7. Screen Construction Rules

- Feature files must stay inside `src/features/{feature}/`
- Use the existing structure:
  `screens/`
  `widgets/`
  `repositories/`
  `providers/`
- New screens must follow the existing FamilyFirst visual language; do not introduce a separate design style per feature
- Use loading, empty, success, and error states explicitly rather than collapsing them into a single branch
- Use robust sizing for overlays, drawers, dialogs, and modals; avoid fragile layouts that can collapse to zero width/height

## 8. API And State Rules

- Mobile API contracts are immutable once published; do not rename or remove existing mobile routes/methods
- If the app and backend drift, fix the backend or add a thin compatibility wrapper
- Keep HTTP concerns inside repositories or core networking layers, not screen components
- Use typed DTOs; avoid `any` in production code

## 9. Verification Rules

- `tsc --noEmit` passing is required but not sufficient for UI work
- Any visual or interactive UI change must also be render-verified before being called complete
- If render verification is not possible in the current environment, report it as:
  `UNVERIFIED - needs visual check`

## 10. Delivery Checklist

Use this checklist before closing UI work:

- Colors and typography follow FamilyFirst tokens
- Touch targets are at least `48x48px`
- Async states cover loading, success, and error with retry
- Demo mode shows meaningful data and matches live shape
- No API data is hardcoded in screen components
- Shared components are reused where appropriate
- TypeScript passes with `tsc --noEmit`
- UI was actually rendered and checked, or explicitly marked unverified
