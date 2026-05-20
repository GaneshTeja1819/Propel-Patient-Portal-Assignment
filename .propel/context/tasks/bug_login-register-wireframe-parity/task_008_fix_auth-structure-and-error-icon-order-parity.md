# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_6]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + SCR-001 wireframe HTML reference [SOURCE:INPUT] Basis: user explicitly reported dual-layer wrapping drift and required error-icon order mismatch.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect in login/registration forms
- **Affected Version**: Current React auth implementation after task_007_fix_auth-required-indicator-and-button-container-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages (desktop/mobile)

### Steps to Reproduce
1. Open current login and registration pages in the frontend app.
2. Compare with wireframe structure and error behavior in SCR-001/SCR-002 HTML.
3. Trigger validation errors on required fields.
4. **Expected**: form structure follows wireframe depth without unnecessary dual layers that force text wrapping; error affordance places the exclamation icon before each error message text.
5. **Actual**: auth pages still show layout wrapping inconsistencies from structure layering, and field-level errors do not consistently render the icon before text as in the wireframe pattern.

**Error Output**:
```text
No runtime exception. Defect is visual/layout parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: frontend/src/components/auth/LoginForm.tsx
- **Component**: Login form field/error composition
- **Function**: Field error rendering for email/password
- **Cause**: field error text currently renders as plain text inside error containers, without an explicit icon node preceding the text, so ordering parity with wireframe is not guaranteed. [SOURCE:INPUT] Basis: user explicitly requested exclamation icon before error messages.

Additional root-cause evidence:
- **File**: frontend/src/components/auth/RegistrationForm.tsx
- **Component**: Registration form field/error composition
- **Function**: Error rendering across required fields
- **Cause**: same error-text pattern is reused without explicit icon-first structure, causing wireframe divergence for field-level error lines. [SOURCE:INFERRED] Basis: registration uses the same field error rendering style as login.

- **File**: frontend/src/components/auth/LoginForm.module.css
- **Component**: Login field layout styles
- **Function**: Field grouping and text wrapping behavior
- **Cause**: current grouping still allows layered wrapper interactions that can induce unintended line wraps under constrained widths. [SOURCE:INPUT] Basis: user explicitly reported dual-layer structures causing multi-line wraps.

- **File**: frontend/src/components/auth/RegistrationForm.module.css
- **Component**: Registration field layout styles
- **Function**: Field grouping and layout depth behavior
- **Cause**: analogous wrapper and spacing composition can drift from strict wireframe structure depth, especially on narrow viewports. [SOURCE:INFERRED] Basis: user reported the issue on both login and registration.

Hypotheses evaluated before selecting fix strategy:
- H1: Only login is affected. Rejected [SOURCE:INPUT] Basis: report explicitly includes login and register pages.
- H2: Icon order issue is already solved by form alert icon. Rejected [SOURCE:INFERRED] Basis: report references error message lines, not only top-level alert banner.
- H3: Wrapping issue is purely typography-related. Rejected [SOURCE:INPUT] Basis: report identifies unnecessary dual-layer structures as cause.
- H4: CSS pseudo-element is sufficient for icon order parity. Rejected as unstable [SOURCE:INFERRED] Basis: explicit DOM icon before text is more testable and deterministic for order assertions.

### Impact Assessment
- **Affected Features**: SCR-001 login error states, SCR-002 registration error states, auth form layout fidelity
- **User Impact**: users see wrapped/misaligned text and inconsistent error affordances compared to wireframe intent.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security impact; issue is UX parity and clarity.

## Fix Overview
Stabilize auth layout structure to remove unnecessary dual-layer wrapping effects and update field-level error rendering to explicit icon-first composition so the exclamation icon appears before each error message text in both login and registration forms.

## Fix Dependencies
- Existing login/register validation behavior must remain unchanged.
- Token-based styling and accessibility semantics remain mandatory.
- Wireframe HTML remains source baseline for layout depth and error affordance order.

## Impacted Components
### Frontend (React + TypeScript)
- frontend/src/components/auth/LoginForm.tsx (UPDATE)
- frontend/src/components/auth/LoginForm.module.css (UPDATE)
- frontend/src/components/auth/LoginForm.test.tsx (UPDATE)
- frontend/src/components/auth/RegistrationForm.tsx (UPDATE)
- frontend/src/components/auth/RegistrationForm.module.css (UPDATE)
- frontend/src/components/auth/RegistrationForm.test.tsx (UPDATE)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Refactor field-level error output to explicit icon-before-text structure and simplify wrappers that contribute to wrap drift. [SOURCE:INPUT] Basis: user requested icon order and strict wireframe-aligned structure. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Align login field/error layout to wireframe depth and style icon-before-text error rows. [SOURCE:INPUT] Basis: user reported dual-layer wraps and icon order mismatch. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Apply the same explicit icon-before-text error rendering and structure simplification for registration required fields. [SOURCE:INPUT] Basis: user explicitly included register page mismatches. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Align registration field/error layout depth to wireframe and style icon-first error rows. [SOURCE:INFERRED] Basis: parity consistency with login and shared mismatch pattern. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions that field-level error icon appears before error text and no structure regressions are introduced. [SOURCE:INFERRED] Basis: regression prevention for icon-order issue. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add assertions for registration field-level icon-before-text error structure and wrap-sensitive layout hooks. [SOURCE:INFERRED] Basis: prevent recurrence in registration flow. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Inspect login/register field composition and remove unnecessary nested wrappers that affect line wrapping.
- Introduce explicit error-row markup: icon element first, message text second, preserving ARIA behavior.
- Update auth CSS so error rows match wireframe structure and maintain predictable wrapping.
- Keep top-level alert behavior intact while standardizing field-level error visual order.
- Extend tests to verify icon-before-text ordering for invalid field states.
- Run focused auth tests and production build.

## Regression Prevention Strategy
- [x] Unit test that login field-level errors render icon before message text
- [x] Unit test that registration field-level errors render icon before message text
- [x] Unit test covering wrap-sensitive auth field container structure hooks
- [x] Integration test ensuring existing login/register validation behavior remains unchanged

## Rollback Procedure
1. Revert task_008 auth form commits if icon-order/layout changes regress behavior.
2. Re-run focused auth tests and build to confirm baseline restoration.
3. No data recovery required because changes are presentational.

## External References
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html [SOURCE:INPUT] Basis: login error icon-first pattern and layout depth baseline.
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html [SOURCE:INPUT] Basis: registration layout and error treatment parity baseline.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] Login/register structure no longer exhibits dual-layer wrap drift versus wireframe
- [x] Field-level error rows render exclamation icon before message text
- [x] Existing auth tests pass with new icon-order assertions
- [x] Production build succeeds

## Implementation Checklist
- [x] Remove/simplify dual-layer form structures causing wrapping drift
- [x] Implement icon-before-text field error row structure in login form
- [x] Implement icon-before-text field error row structure in registration form
- [x] Update auth CSS for icon-first error row styling and structure parity
- [x] Add/adjust auth tests for icon order and structure regression coverage
- [x] Validate with focused auth tests and frontend build
- [ ] Manually verify desktop and 375px visual parity against SCR-001 and SCR-002
