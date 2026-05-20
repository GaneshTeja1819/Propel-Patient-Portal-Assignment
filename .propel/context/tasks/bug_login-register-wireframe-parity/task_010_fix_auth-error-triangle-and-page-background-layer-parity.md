# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_8]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + SCR-001 wireframe HTML reference [SOURCE:INPUT] Basis: user explicitly requested red triangle-styled error icon and removal of page background layer causing dual-layer wrap drift.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major visual parity defect in auth shell and error affordance
- **Affected Version**: Current React auth implementation after task_009_fix_auth-label-indicator-removal-and-password-eye-toggle-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages (desktop/mobile)

### Steps to Reproduce
1. Open login and registration pages in current implementation.
2. Trigger field-level validation errors and inspect the icon shown before error text.
3. Inspect auth page shell background treatment at desktop and narrow widths.
4. **Expected**: error icon appears as wireframe-style red triangle with exclamation before error text; main auth page has no extra background layer causing dual-layer effect and text wrapping drift.
5. **Actual**: error icon is currently plain exclamation text without triangle treatment; auth page shell still applies gradient background layer, causing dual-layer visual drift and wrap pressure.

**Error Output**:
```text
No runtime exception. Defect is visual/layout parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: frontend/src/components/auth/LoginForm.module.css
- **Component**: Login field error presentation
- **Function**: `fieldErrorIcon` visual treatment
- **Cause**: error icon styling currently sets text weight/spacing only and does not render a triangle badge treatment matching wireframe expectations. [SOURCE:INPUT] Basis: user explicitly requested red triangle with exclamation.

Additional root-cause evidence:
- **File**: frontend/src/components/auth/RegistrationForm.module.css
- **Component**: Registration field error presentation
- **Function**: `fieldErrorIcon` visual treatment
- **Cause**: same icon class pattern exists in registration and likewise lacks triangle geometry/shape styling. [SOURCE:INFERRED] Basis: shared icon-first pattern was introduced without triangle shape semantics.

- **File**: frontend/src/pages/LoginPage.module.css
- **Component**: Login page shell
- **Function**: `.page` background rendering
- **Cause**: `.page` still applies multi-stop linear gradient background, producing layered shell effect against card/background expectations. [SOURCE:INPUT] Basis: user explicitly reported main page background layer causing dual-layer effect.

- **File**: frontend/src/pages/RegistrationPage.module.css
- **Component**: Registration page shell
- **Function**: `.page` background rendering
- **Cause**: registration shell also uses gradient background layer, reproducing the same dual-layer visual and wrap drift under constrained viewports. [SOURCE:INFERRED] Basis: registration page uses analogous page background contract.

Hypotheses evaluated before selecting fix strategy:
- H1: Only error icon placement is wrong. Rejected [SOURCE:INPUT] Basis: user also explicitly requested background layer removal.
- H2: Only login page background needs change. Rejected [SOURCE:INFERRED] Basis: both auth pages share shell pattern and user referenced main page layer behavior generally.
- H3: Triangle icon can be deferred because exclamation already exists. Rejected [SOURCE:INPUT] Basis: user explicitly requested triangle-styled icon as defined in wireframe.
- H4: Wrapping issue is solely typography. Rejected [SOURCE:INPUT] Basis: report identifies page background layering as contributing factor.

### Impact Assessment
- **Affected Features**: SCR-001 login page shell and field error row style; SCR-002 registration page shell and field error row style
- **User Impact**: Users see non-wireframe error affordance and layered backgrounds that break intended layout clarity.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security impact; issue is UX parity and readability.

## Fix Overview
Apply a parity pass that removes gradient background layers from auth page shells and updates field-error icon styling to display a red triangle-with-exclamation treatment before error messages in both login and registration forms.

## Fix Dependencies
- Existing auth validation behavior must remain unchanged.
- Icon-before-text error structure introduced in task_008/task_009 must remain intact.
- Tokenized styling remains mandatory.

## Impacted Components
### Frontend (React + TypeScript)
- frontend/src/pages/LoginPage.module.css (UPDATE)
- frontend/src/pages/RegistrationPage.module.css (UPDATE)
- frontend/src/components/auth/LoginForm.module.css (UPDATE)
- frontend/src/components/auth/RegistrationForm.module.css (UPDATE)
- frontend/src/components/auth/LoginForm.test.tsx (UPDATE)
- frontend/src/components/auth/RegistrationForm.test.tsx (UPDATE)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/LoginPage.module.css | Remove page-level gradient background layer causing dual-layer shell effect and potential wrap drift. [SOURCE:INPUT] Basis: user explicitly requested no main page background layer. |
| MODIFY | frontend/src/pages/RegistrationPage.module.css | Remove analogous page-level gradient background layer for registration shell parity. [SOURCE:INFERRED] Basis: registration shares same layered shell pattern. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Style `fieldErrorIcon` as triangle-styled red alert marker with exclamation while preserving icon-before-text order. [SOURCE:INPUT] Basis: user explicitly requested red triangle icon before messages. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Apply same triangle-styled `fieldErrorIcon` treatment for registration field errors. [SOURCE:INFERRED] Basis: parity consistency across login/register error rows. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add/adjust assertions for triangle-styled error icon class hooks before message text. [SOURCE:INFERRED] Basis: regression prevention for icon treatment parity. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add/adjust assertions for triangle-styled error icon class hooks before message text. [SOURCE:INFERRED] Basis: regression prevention for registration icon treatment parity. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Remove gradient background from login and registration `.page` shells to eliminate dual-layer effect.
- Keep card and spacing layout intact while removing only the extra background layer.
- Update `fieldErrorIcon` styles to render a compact red triangle marker with visible exclamation.
- Keep existing icon-first DOM ordering for all field errors.
- Extend auth tests to verify triangle-style marker hooks and ordering remain correct.
- Run focused auth tests and production build.

## Regression Prevention Strategy
- [x] Unit test that login field errors retain icon-before-text structure with triangle marker class
- [x] Unit test that registration field errors retain icon-before-text structure with triangle marker class
- [x] Visual-regression guard assertion that label indicators remain absent while error markers remain present
- [x] Integration test ensuring auth validation behavior remains unchanged after style updates

## Rollback Procedure
1. Revert task_010 commits affecting page shell backgrounds and error icon styles if parity regressions appear.
2. Re-run focused auth tests and build to confirm baseline restoration.
3. No data recovery required because changes are presentational.

## External References
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html [SOURCE:INPUT] Basis: error icon treatment and no dual-layer shell baseline.
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html [SOURCE:INPUT] Basis: registration shell and error row parity baseline.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] Login/register page shells no longer use layered gradient background
- [x] Field error icon appears as triangle-styled red marker before error text
- [x] Existing auth tests pass with updated icon-style assertions
- [x] Production build succeeds

## Implementation Checklist
- [x] Remove page-level gradient background layers from login/register shells
- [x] Preserve auth card layout and spacing while flattening shell background
- [x] Update field error icon styling to triangle-styled red marker in login and registration forms
- [x] Keep icon-before-text structure for all field errors
- [x] Update auth tests for triangle-icon style hooks and ordering
- [x] Validate with focused auth tests and frontend build
- [ ] Manually verify desktop and 375px visual parity against SCR-001 and SCR-002
