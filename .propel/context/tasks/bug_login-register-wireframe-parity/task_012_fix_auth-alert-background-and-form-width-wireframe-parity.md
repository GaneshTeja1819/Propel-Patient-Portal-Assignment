# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_10]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + SCR-001 login wireframe HTML [SOURCE:INPUT] Basis: user provided explicit parity deltas and attached wireframe reference.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UX parity defect across auth shell sizing, background, and error affordances
- **Affected Version**: Current React auth implementation after task_011_fix_auth-triangle-icon-and-shell-background-parity
- **Environment**: Windows, React 18 + Vite, Chromium-based browser and responsive viewport testing

### Steps to Reproduce
1. Open login and registration pages in the current frontend implementation.
2. Trigger field-level validation errors on both forms, and trigger login submit-error state.
3. Inspect auth page backgrounds and card widths at desktop and narrow breakpoints.
4. **Expected**: field errors display a white `!` inside a red triangle directly before each error message; login/register shells include `#f0f5ff`; login form max width is 420px; register form max width is 480px; login submit alert copy is exactly `⚠ Your email or password is incorrect. Please try again.` [SOURCE:INPUT] Basis: user explicitly specified all target values and wording.
5. **Actual**: current implementation uses tokenized surface background instead of `#f0f5ff`; width values are currently 420px-equivalent login and 480px-equivalent register but require strict parity confirmation against wireframe intent; login submit alert content is sourced from backend message and not guaranteed to match exact wireframe copy; triangle icon treatment may not fully match the wireframe semantics under all error contexts. [SOURCE:INPUT] Basis: user reported multiple remaining inconsistencies against wireframe HTML.

**Error Output**:
```text
No runtime exception. Defect is visual/content parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: `frontend/src/pages/LoginPage.module.css:7`
- **Component**: Login page shell
- **Function**: `.page` background treatment
- **Cause**: page shell currently uses `var(--color-surface-default)` and does not enforce wireframe-specific `#f0f5ff` background requirement. [SOURCE:INPUT] Basis: user explicitly requested `#f0f5ff` on login/register pages.

Additional root-cause evidence:
- **File**: `frontend/src/pages/RegistrationPage.module.css:7`
- **Component**: Registration page shell
- **Function**: `.page` background treatment
- **Cause**: registration shell mirrors login shell and also omits wireframe `#f0f5ff` background. [SOURCE:INFERRED] Basis: registration page currently uses matching background tokenized value.

- **File**: `frontend/src/components/auth/LoginForm.module.css:13`
- **Component**: Login generic form alert
- **Function**: `.formAlert` icon and message styling
- **Cause**: generic alert icon uses plain text icon (`!`) and alert message text is dynamic, so wireframe-specific iconography/copy (`⚠ Your email or password is incorrect. Please try again.`) is not guaranteed. [SOURCE:INPUT] Basis: user explicitly requested exact login error message and wireframe icon semantics.

- **File**: `frontend/src/components/auth/LoginForm.tsx:39`
- **Component**: Login submit error handling
- **Function**: `onSubmit` + `submitMessage` rendering
- **Cause**: submit alert message comes from service result and may differ from required fixed wireframe copy. [SOURCE:INFERRED] Basis: current flow renders `result.message` rather than constant copy.

- **File**: `frontend/src/components/auth/LoginForm.module.css:88`
- **Component**: Login field error icon
- **Function**: `fieldErrorIconTriangle`
- **Cause**: current triangle marker exists, but shape/contrast/positioning must be verified and normalized to exact wireframe expectation (white `!` centered in red triangle directly before text) across login and registration implementations. [SOURCE:INPUT] Basis: user explicitly reiterated this icon requirement.

Hypotheses evaluated before selecting fix strategy:
- H1: Only global alert text is wrong. Rejected [SOURCE:INPUT] Basis: user also required background, width, and triangle semantics updates.
- H2: Widths are already correct and need no plan entry. Rejected [SOURCE:INPUT] Basis: user explicitly requested exact 420px/480px compliance, requiring verification and lock-in.
- H3: Background should stay tokenized and not use explicit value. Rejected [SOURCE:INPUT] Basis: user explicitly specified `#f0f5ff` wireframe value.
- H4: Triangle class already guarantees parity everywhere. Rejected [SOURCE:INFERRED] Basis: field-level icon and alert-level icon are implemented differently and may diverge.

### Impact Assessment
- **Affected Features**: SCR-001 login error messaging and shell styling, SCR-002 registration shell styling and error affordance parity
- **User Impact**: users see inconsistent visual semantics and messaging compared to approved wireframe, reducing trust and acceptance-test pass rate.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; ensure generic auth failure wording remains non-enumerative.

## Fix Overview
Apply a focused parity pass that standardizes auth-shell background to `#f0f5ff`, enforces card max-width rules (login 420px, registration 480px), normalizes red-triangle white-exclamation error icon treatment before field messages, and hardens login submit-error alert text to exact wireframe copy.

## Fix Dependencies
- Existing auth validation behavior and route flow must remain unchanged.
- Existing accessibility semantics (`role=alert`, `aria-live`, labels, and keyboard support) must be preserved.
- Token governance exception for explicit wireframe color must be documented/contained to page shell styles only. [SOURCE:INFERRED] Basis: user-requested explicit hex requirement.

## Impacted Components
### Frontend (React + TypeScript)
- frontend/src/pages/LoginPage.module.css (UPDATE)
- frontend/src/pages/RegistrationPage.module.css (UPDATE)
- frontend/src/components/auth/LoginForm.module.css (UPDATE)
- frontend/src/components/auth/RegistrationForm.module.css (UPDATE)
- frontend/src/components/auth/LoginForm.tsx (UPDATE)
- frontend/src/components/auth/LoginForm.test.tsx (UPDATE)
- frontend/src/components/auth/RegistrationForm.test.tsx (UPDATE)
- frontend/src/pages/LoginPage.test.tsx (UPDATE)
- frontend/src/pages/RegistrationPage.test.tsx (UPDATE)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/LoginPage.module.css | Set login shell `.page` background to `#f0f5ff` and preserve login card `max-width: 420px`. [SOURCE:INPUT] Basis: user explicitly requested background value and 420px login width. |
| MODIFY | frontend/src/pages/RegistrationPage.module.css | Set registration shell `.page` background to `#f0f5ff` and enforce registration card `max-width: 480px`. [SOURCE:INPUT] Basis: user explicitly requested background value and 480px registration width. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Ensure field-error icon renders as white `!` centered inside red triangle and positioned immediately before error text. [SOURCE:INPUT] Basis: user explicitly requested triangle icon behavior. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Mirror the same red-triangle white-`!` field-error icon behavior for registration errors. [SOURCE:INFERRED] Basis: parity consistency across auth forms. |
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Replace dynamic login submit-alert copy with exact wireframe text: `⚠ Your email or password is incorrect. Please try again.` while preserving generic auth security posture. [SOURCE:INPUT] Basis: user explicitly provided required message text. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add/update assertions for exact login submit-alert copy and triangle icon ordering/style hooks. [SOURCE:INFERRED] Basis: regression prevention for requested parity behavior. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add/update assertions for triangle icon ordering/style hooks in registration field errors. [SOURCE:INFERRED] Basis: regression prevention for shared error affordance. |
| MODIFY | frontend/src/pages/LoginPage.test.tsx | Add/update shell background and card width parity assertions for login page contract. [SOURCE:INFERRED] Basis: prevent future width/background drift. |
| MODIFY | frontend/src/pages/RegistrationPage.test.tsx | Add/update shell background and card width parity assertions for registration page contract. [SOURCE:INFERRED] Basis: prevent future width/background drift. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Inspect auth shell CSS and set explicit `#f0f5ff` background for login/register `.page` containers.
- Verify and lock card max-width contracts to 420px for login and 480px for registration.
- Normalize `fieldErrorIconTriangle` styling in both auth form stylesheets to render white `!` in red triangle directly before error message nodes.
- Update login submit-error rendering to always show exact wireframe message copy on failed sign-in attempts.
- Keep existing validation rules, aria semantics, and button interactions unchanged.
- Update focused auth/page tests to cover icon semantics, width/background constraints, and exact login alert text.
- Run focused auth tests and frontend build.

## Regression Prevention Strategy
- [x] Unit test that login field errors render white `!` triangle icon immediately before message text
- [x] Unit test that registration field errors render white `!` triangle icon immediately before message text
- [x] Unit test that login submit alert shows exact message `⚠ Your email or password is incorrect. Please try again.`
- [x] UI contract test that login card max width is 420px and registration card max width is 480px
- [x] UI contract test that login/register shells use `#f0f5ff` background

## Rollback Procedure
1. Revert the task_012 styling/content commits if parity regressions or accessibility regressions are detected.
2. Re-run focused auth tests and `npm run build` to verify baseline restoration.
3. No data recovery required because this fix is UI/content only.

## External References
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html [SOURCE:INPUT] Basis: login shell background, card width baseline, and exact login error alert copy.
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html [SOURCE:INPUT] Basis: registration shell and field error parity expectations.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] Login and registration pages both render with `#f0f5ff` shell background
- [x] Login card max width is 420px and registration card max width is 480px
- [x] Field-level errors show white `!` inside red triangle before error text in both forms
- [x] Login submit error displays exact wireframe message copy
- [x] Focused auth tests and production build pass

## Implementation Checklist
- [x] Update login shell background to `#f0f5ff`
- [x] Update registration shell background to `#f0f5ff`
- [x] Verify lock-in of login card max-width: 420px
- [x] Verify lock-in of registration card max-width: 480px
- [x] Normalize triangle icon treatment in login field-error rows
- [x] Normalize triangle icon treatment in registration field-error rows
- [x] Set exact login submit-alert message to wireframe copy
- [x] Update auth and page regression tests for all new parity contracts
- [x] Validate via focused tests and frontend build
- [ ] Manually verify desktop and 375px parity versus SCR-001/SCR-002
