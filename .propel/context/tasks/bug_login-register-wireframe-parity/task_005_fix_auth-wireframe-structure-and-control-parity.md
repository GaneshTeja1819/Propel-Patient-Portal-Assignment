# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_3]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report plus wireframe HTML for SCR-001 and SCR-002 [SOURCE:INPUT] Basis: user explicitly reported remaining structure and control-level parity issues after task_004.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect across login and registration auth flows
- **Affected Version**: Current React auth implementation after task_004_fix_auth-page-wireframe-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages

### Steps to Reproduce
1. Open current login and registration pages rendered from [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L1) and [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L1).
2. Compare against [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) and [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html).
3. Inspect footer/link layering, button box sizing, underline behavior, and the missing line above the password helper hint.
4. **Expected**: The React auth pages match wireframe layout structure and control rendering, including: clean single-layer copy flow, correctly sized ghost buttons, no underline under auth-footer links, and a visible line (password-strength bar) above `Minimum 8 characters, at least one uppercase letter and one number.`
5. **Actual**: Current React UI still shows structural/styling drift, including unnecessary layered text flow, control sizing mismatch, missing password-strength line above helper hint, and underlined footer links that do not match wireframe style.

**Error Output**:
```text
No runtime exception. Defect is visual and structural parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L231), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L242), [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L158)
- **Component**: Login footer content flow and link presentation
- **Function**: Footer composition and link styling
- **Cause**: Footer content is still constrained by extra layout layering (`authFooter` flex-column treatment and inherited global anchor styling), which can push text/link flow away from wireframe parity and keeps underlines visible where wireframe links are not underlined. [SOURCE:INFERRED] Basis: global anchor rule in [global.css](frontend/src/styles/global.css#L102) enforces underline by default; wireframe anchor baseline is non-underlined.

Additional root-cause evidence:
- **File**: [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L298), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L302)
- **Component**: Registration password guidance block
- **Function**: Password hint and visual guidance rendering
- **Cause**: The helper text is present, but the visual line above it (wireframe password-strength track) is not rendered, causing a direct parity miss. [SOURCE:INPUT] Basis: user explicitly reported missing line above helper text; wireframe shows `pw-strength` block at [wireframe-SCR-002-registration.html#L130](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html#L130).

- **File**: [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L180), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L163)
- **Component**: Secondary action buttons (`Create an account`, `Sign in instead`)
- **Function**: Box sizing and ghost-button treatment
- **Cause**: The current sizing/typography and box treatment differ from wireframe button scale and rhythm, producing undersized or inconsistent button appearance. [SOURCE:INPUT] Basis: user explicitly asked both controls to be in well-sized boxes per wireframe HTML.

Planning-gap evidence:
- Previous parity passes fixed copy and some sizing but did not fully normalize structural layering and control treatment to wireframe class-level behavior.

Hypotheses evaluated before confirming fix strategy:
- H1: Remaining issue is only link underline. Rejected as incomplete [SOURCE:INFERRED] Basis: missing password-strength line and control box sizing are separate defects.
- H2: Remaining issue is only registration form. Rejected [SOURCE:INFERRED] Basis: login footer structure and link underline are still cited.
- H3: Missing password-strength line is optional because helper text exists. Rejected [SOURCE:INPUT] Basis: user explicitly requested the missing line above helper text.
- H4: Global anchor underline can remain for accessibility. Rejected for this scope [SOURCE:INFERRED] Basis: wireframe and reported visual baseline require non-underlined auth footer links; hover/contrast can still preserve affordance.

### Impact Assessment
- **Affected Features**: SCR-001 login shell/footer, SCR-002 registration helper and secondary-action controls
- **User Impact**: Users see layout and control styling that still feels inconsistent with approved wireframe, reducing visual trust and design fidelity.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; impact is UX/parity quality.

## Fix Overview
Remove unnecessary layered footer/control structure, align button sizing to wireframe ghost-button proportions, restore the password-strength line above the helper text, and suppress unwanted auth-footer underlines while preserving interactive affordance.

## Fix Dependencies
- Existing login/register auth behaviors must remain unchanged.
- Token-based styling must remain the source of truth.
- Wireframe HTML is authoritative for structure, line placement, and control dimensions.

## Impacted Components
### Frontend (React + TypeScript)
- [frontend/src/components/auth/LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx) (UPDATE)
- [frontend/src/components/auth/LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css) (UPDATE)
- [frontend/src/components/auth/LoginForm.test.tsx](frontend/src/components/auth/LoginForm.test.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.test.tsx](frontend/src/components/auth/RegistrationForm.test.tsx) (UPDATE)
- [frontend/src/styles/global.css](frontend/src/styles/global.css) (UPDATE only if scoped override is required for auth-link underline control)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Simplify footer/content layering to match wireframe flow and avoid unintended line breaks. [SOURCE:INPUT] Basis: user explicitly reported unnecessary layered structure and text flow issues. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Normalize footer link presentation, remove unintended underlines in auth footer scope, and match button box sizing to wireframe. [SOURCE:INPUT] Basis: user requested no underline and well-sized button box. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Restore a visible line element above password helper text (wireframe-equivalent strength track) without changing auth logic. [SOURCE:INPUT] Basis: user explicitly requested the missing line above helper text. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Adjust `Sign in instead` box sizing and ghost style to match wireframe control proportions. [SOURCE:INPUT] Basis: user explicitly asked for wireframe-sized control box. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add assertions for strength-line presence and helper block structure. [SOURCE:INFERRED] Basis: prevent regression of restored visual element. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions for non-underlined auth footer links and expected footer text flow. [SOURCE:INFERRED] Basis: protect structure/styling parity in login footer. |
| MODIFY | frontend/src/styles/global.css | Scope/override anchor underline behavior if component-level override is insufficient. [SOURCE:INFERRED] Basis: global anchor underline currently conflicts with wireframe-style auth links. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Align login/footer DOM and CSS to wireframe flow to remove unnecessary layered behavior that forces unintended wraps.
- Restore the password-strength track line above the registration helper hint.
- Tune ghost-button sizing (`Create an account`, `Sign in instead`) to wireframe dimensions and typography.
- Remove default underline for auth-footer links in scoped auth styles while keeping hover affordance.
- Add targeted regression tests for structure and visual-element presence.
- Validate at desktop and 375px viewports.

## Regression Prevention Strategy
- [x] Unit test that login footer links are rendered in expected flow and without default underline in auth scope
- [x] Unit test that registration password-strength line renders above helper text
- [x] Unit test that `Create an account` and `Sign in instead` keep wireframe-style ghost-box sizing
- [x] Edge case test for 375px layout to ensure no unintended wrapping from layering

## Rollback Procedure
1. Revert the parity commit(s) if layout/control changes regress auth interaction.
2. Validate rollback by confirming focused auth tests and build return to prior green baseline.
3. No data recovery is needed because changes are presentational.

## External References
- [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) [SOURCE:INPUT] Basis: login structure and footer style baseline.
- [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html) [SOURCE:INPUT] Basis: password helper block and ghost-button style baseline.

## Build Commands
- `cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx`
- `cd frontend && npm run build`

## Implementation Validation Strategy
- [x] Login footer no longer shows unintended layering/wrapping against wireframe baseline
- [x] Auth footer links are not underlined by default in auth scope
- [x] Password-strength line is visible above registration helper text
- [x] `Create an account` and `Sign in instead` controls match wireframe-sized box treatment
- [x] Desktop and 375px parity checks pass for login and registration

## Implementation Checklist
- [x] Remove unnecessary layering in login/register footer/control structure
- [x] Restore password-strength line above registration helper text
- [x] Normalize ghost-button box sizing for `Create an account` and `Sign in instead`
- [x] Remove default underline for auth-footer links in scoped styles
- [x] Add regression tests for flow, line presence, and control sizing
- [ ] Manually verify desktop and 375px parity against SCR-001 and SCR-002
