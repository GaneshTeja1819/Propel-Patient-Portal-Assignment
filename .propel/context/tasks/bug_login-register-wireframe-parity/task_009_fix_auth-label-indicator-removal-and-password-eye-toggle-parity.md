# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_7]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + SCR-001 wireframe HTML reference [SOURCE:INPUT] Basis: user explicitly reported structure drift, label indicator mismatch, and password toggle icon mismatch.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect in auth forms
- **Affected Version**: Current React auth implementation after task_008_fix_auth-structure-and-error-icon-order-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages (desktop/mobile)

### Steps to Reproduce
1. Open login and registration pages in the current frontend implementation.
2. Compare password fields, label decorations, and error rows with SCR-001/SCR-002 wireframe HTML.
3. Trigger field validation errors and inspect required labels and password toggle controls.
4. **Expected**: structure matches wireframe without unnecessary dual layers causing wraps; error rows show red exclamation icon before text; field labels do not show the red exclamation indicator; password toggles use eye icon control instead of `Show/Hide` text.
5. **Actual**: label-level required indicators are still rendered; password toggles still display text (`Show` / `Hide`); parity drift remains against wireframe controls and visual structure.

**Error Output**:
```text
No runtime exception. Defect is visual/layout parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: frontend/src/components/auth/LoginForm.tsx
- **Component**: Login field labels and password toggle
- **Function**: Required label marker and password visibility control content
- **Cause**: label markup still includes a `requiredIndicator` node and toggle button text is rendered as `Show/Hide`, diverging from icon-only wireframe behavior. [SOURCE:INPUT] Basis: user explicitly requested removing label icon and using eye icon toggles.

Additional root-cause evidence:
- **File**: frontend/src/components/auth/RegistrationForm.tsx
- **Component**: Registration field labels and password toggles
- **Function**: Required label marker and password visibility controls for password/confirm fields
- **Cause**: same required-indicator and text toggle pattern is duplicated in registration, producing consistent mismatch on both forms. [SOURCE:INFERRED] Basis: login and registration share equivalent marker/toggle implementation pattern.

- **File**: frontend/src/components/auth/LoginForm.module.css
- **Component**: Login required indicator and toggle styling
- **Function**: Label indicator and toggle button presentation
- **Cause**: dedicated styles for `requiredIndicator` and text-based toggle sizing still assume textual controls rather than icon presentation. [SOURCE:INFERRED] Basis: CSS currently supports existing text toggle and label marker pattern.

- **File**: frontend/src/components/auth/RegistrationForm.module.css
- **Component**: Registration required indicator and toggle styling
- **Function**: Label indicator and password toggle presentation
- **Cause**: same style contract reinforces non-wireframe toggle mode and label icon visibility. [SOURCE:INFERRED] Basis: parallel CSS rules and component usage across forms.

Hypotheses evaluated before selecting fix strategy:
- H1: Only error row icon ordering is broken. Rejected [SOURCE:INPUT] Basis: user additionally requested label icon removal and eye toggle replacement.
- H2: Password toggle text is acceptable if aria-label is correct. Rejected [SOURCE:INPUT] Basis: user explicitly requires eye icon to match wireframe.
- H3: Label indicator should remain because fields are required. Rejected [SOURCE:INPUT] Basis: user explicitly requested removal of icon from labels.
- H4: Registration-only fix is sufficient. Rejected [SOURCE:INPUT] Basis: report includes login and register pages.

### Impact Assessment
- **Affected Features**: SCR-001 login field labels/toggles, SCR-002 registration field labels/toggles, auth layout fidelity
- **User Impact**: users see non-wireframe controls and label markers, reducing design consistency and visual trust.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security impact; issue is presentation and UX parity.

## Fix Overview
Perform a targeted parity pass to remove label-level warning indicators, keep error-row icon-before-text behavior, replace password `Show/Hide` text with eye icon toggles in all auth password fields, and ensure structure remains flattened to avoid wrap drift against wireframe.

## Fix Dependencies
- Existing auth validation and accessibility behavior must remain unchanged.
- Tokenized styling remains mandatory.
- Wireframe HTML remains source baseline for control treatment and structure.

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
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Remove label-level required indicator node and change password toggle content to eye icon while preserving accessible aria labels. [SOURCE:INPUT] Basis: user requested label icon removal and eye icon toggles. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Remove/adjust label indicator styles and update toggle styles for icon-based rendering parity. [SOURCE:INFERRED] Basis: existing CSS supports text-based toggle and label indicator. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Remove required indicators from labels and replace password/confirm toggle text with eye icons. [SOURCE:INPUT] Basis: user requested this behavior on login and register pages. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Update icon-toggle styling and remove obsolete label indicator visual contract while retaining field-error icon row behavior. [SOURCE:INFERRED] Basis: CSS must align with component markup changes. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Update assertions to reflect no label indicator and icon-only password toggle rendering with proper aria labels. [SOURCE:INFERRED] Basis: regression prevention for new control treatment. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add/update coverage for icon-only toggles and absence of label indicator across required fields. [SOURCE:INFERRED] Basis: prevent recurrence of parity drift in registration flow. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Remove required indicator nodes from login/register label markup.
- Keep field-level error row icon-before-text structure introduced in task_008.
- Replace toggle button text with eye icon content for password fields in login and registration.
- Ensure aria labels and pressed state remain accessible and unchanged semantically.
- Adjust CSS for icon-based toggles and remove unused label-indicator styles.
- Extend tests for icon toggles and label-indicator absence.
- Run focused auth tests and production build.

## Regression Prevention Strategy
- [x] Unit test that login required labels no longer render warning indicators
- [x] Unit test that registration required labels no longer render warning indicators
- [x] Unit test that all password toggles render eye icons with correct aria labels/pressed state
- [x] Integration test ensuring auth validation and error rows still function as expected

## Rollback Procedure
1. Revert task_009 commits affecting auth labels and toggle controls if regressions appear.
2. Re-run focused auth tests and build to confirm baseline restoration.
3. No data recovery required because changes are presentational.

## External References
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html [SOURCE:INPUT] Basis: login error row/icon and password toggle baseline.
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html [SOURCE:INPUT] Basis: registration toggle behavior and field composition baseline.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] Label-level warning indicators are removed from login/register required fields
- [x] Field-level error rows still render exclamation icon before message text
- [x] Password visibility controls use eye icon toggles instead of text
- [x] Existing auth tests pass with updated assertions
- [x] Production build succeeds

## Implementation Checklist
- [x] Remove required-indicator nodes from login/register labels
- [x] Keep icon-before-text error row structure intact for field errors
- [x] Replace `Show/Hide` text with eye icon toggles in all auth password fields
- [x] Update auth CSS to support icon-based toggle controls and remove obsolete label indicator styles
- [x] Update tests for label indicator removal and eye-toggle behavior
- [x] Validate with focused auth tests and frontend build
- [ ] Manually verify desktop and 375px visual parity against SCR-001 and SCR-002
