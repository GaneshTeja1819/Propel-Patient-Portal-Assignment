# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_4]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report plus wireframe HTML for SCR-001 and SCR-002 [SOURCE:INPUT] Basis: user explicitly reported remaining layering, link-behavior, button-box, and required-marker parity gaps.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect across login and registration auth flows
- **Affected Version**: Current React auth implementation after task_005_fix_auth-wireframe-structure-and-control-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages

### Steps to Reproduce
1. Open current login and registration pages rendered by [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L1) and [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L1).
2. Compare against [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) and [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html).
3. Inspect for double-layer wrappers causing text wraps, secondary-action button container sizing/placement, password hint separator line, default underline behavior, center alignment for `Create an account`, and red required indicators.
4. **Expected**: Layout follows wireframe shell depth with no unnecessary layered wrappers; `Create an account` and `Sign in instead` are rendered in well-sized ghost boxes; separator line above password hint is visible; footer links are not underlined by default and only underline on hover; `Create an account` text is center-aligned; all required inputs consistently show red required markers.
5. **Actual**: Current implementation still shows reported layout layering and control-treatment inconsistencies compared with wireframe behavior.

**Error Output**:
```text
No runtime exception. Defect is visual/layout parity mismatch versus wireframe HTML.
```

### Root Cause Analysis
- **File**: [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L132), [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L158), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L348)
- **Component**: Auth form composition and footer/link structure
- **Function**: Footer content flow, link placement, and wrapper hierarchy
- **Cause**: Auth sections still rely on wrapper combinations that can produce line wrapping and spacing behavior drifting from wireframe shell flow. [SOURCE:INFERRED] Basis: user reports double-layer structures and wrap inconsistencies.

Additional root-cause evidence:
- **File**: [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L296), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L120)
- **Component**: Registration password helper visual block
- **Function**: Separator line above helper text
- **Cause**: Password helper section parity is sensitive to placement/order and can regress if separator track and hint spacing diverge from wireframe structure. [SOURCE:INPUT] Basis: user explicitly called out missing separator line above helper text.

- **File**: [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L163), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L187), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L248)
- **Component**: Secondary actions and footer links
- **Function**: Ghost-button sizing/placement, text alignment, and underline behavior
- **Cause**: Secondary controls and links require strict wireframe-style sizing and hover-only underline behavior; current styling may still be inconsistent under layout constraints. [SOURCE:INPUT] Basis: user explicitly requested well-sized containers, centered `Create an account`, and hover-only underline behavior.

- **File**: [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L60), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L141), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L62)
- **Component**: Required-field marker system
- **Function**: Red asterisk visibility and consistency
- **Cause**: Required marker display relies on both JSX presence and CSS style consistency; any mismatch yields missing/inconsistent red indicators. [SOURCE:INPUT] Basis: user explicitly reported missing mandatory-field alert indicators.

Planning-gap evidence:
- Prior parity tasks fixed several visual deltas, but repeated follow-up reports indicate container hierarchy and fine-grained style interactions still require a dedicated stabilization pass.

Hypotheses evaluated before confirming fix strategy:
- H1: Remaining issue is only the password separator line. Rejected as incomplete [SOURCE:INFERRED] Basis: user reported multiple issues including layering, links, alignment, and required markers.
- H2: Remaining issue is only login page. Rejected [SOURCE:INFERRED] Basis: report includes both login and register pages.
- H3: Underline behavior should remain globally consistent with default anchor styling. Rejected for this scope [SOURCE:INPUT] Basis: user explicitly requested hover-only underline for these footer links.
- H4: Required markers are present so no action needed. Rejected as unresolved [SOURCE:INPUT] Basis: user still reports missing/inconsistent red indicators, so parity validation must explicitly include marker consistency checks.

### Impact Assessment
- **Affected Features**: SCR-001 login shell/footer, SCR-002 registration helper and secondary actions
- **User Impact**: Users still observe wireframe drift in layout depth, control sizing, text flow, and required-field signaling, reducing design fidelity and trust.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; impact is UX/parity quality.

## Fix Overview
Perform a stabilization pass focused on layout-layer simplification, strict ghost-button container sizing, hover-only underline behavior, separator-line placement above registration hint text, centered `Create an account` text, and consistent red required markers across all mandatory fields.

## Fix Dependencies
- Existing login/register auth behavior must remain unchanged.
- Token-based styling remains mandatory.
- Wireframe HTML remains the canonical structure and style baseline.

## Impacted Components
### Frontend (React + TypeScript)
- [frontend/src/components/auth/LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx) (UPDATE)
- [frontend/src/components/auth/LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css) (UPDATE)
- [frontend/src/components/auth/LoginForm.test.tsx](frontend/src/components/auth/LoginForm.test.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.test.tsx](frontend/src/components/auth/RegistrationForm.test.tsx) (UPDATE)
- [frontend/src/styles/global.css](frontend/src/styles/global.css) (UPDATE only if scoped component styles cannot enforce hover-only underline behavior)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Remove unnecessary wrapper layering and ensure footer/action flow follows wireframe structure without forced wraps. [SOURCE:INPUT] Basis: user explicitly reported double-layer structure and wrap inconsistency. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Enforce centered `Create an account` button text, proper ghost-box sizing, and hover-only underline behavior for footer links. [SOURCE:INPUT] Basis: user explicitly requested alignment, sizing, and underline behavior. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Ensure separator line placement above password hint remains present and structurally aligned with wireframe helper block. [SOURCE:INPUT] Basis: user explicitly reported missing separator line. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Normalize `Sign in instead` container sizing/placement and required-marker red indicator consistency. [SOURCE:INPUT] Basis: user explicitly requested well-sized controls and consistent mandatory indicators. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add/assert coverage for footer flow, centered create-account action, and hover-only underline class behavior. [SOURCE:INFERRED] Basis: prevent regression of reported login parity issues. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add/assert coverage for separator-line presence above helper text and red required-marker consistency. [SOURCE:INFERRED] Basis: prevent regression of reported registration parity issues. |
| MODIFY | frontend/src/styles/global.css | Add scoped exception if auth footer links still inherit default underline behavior. [SOURCE:INFERRED] Basis: fallback control if component-level CSS cannot fully enforce hover-only behavior. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Audit login/register component wrappers and remove or simplify extra layers that cause unexpected wrapping.
- Confirm and stabilize ghost-button dimensions/placement for both `Create an account` and `Sign in instead`.
- Keep separator line immediately above registration password hint and align spacing to wireframe.
- Enforce default no-underline with hover-only underline for auth footer links in scoped styles.
- Ensure all required fields include and visibly style red markers consistently.
- Add targeted regression tests and run focused auth tests plus build.

## Regression Prevention Strategy
- [x] Unit test that login footer content and action links render without extra wrapping layers
- [x] Unit test that `Create an account` and `Sign in instead` remain center-aligned in wireframe-sized ghost boxes
- [x] Unit test that registration separator line remains above helper hint
- [x] Unit test that required fields consistently render red markers across login and registration

## Rollback Procedure
1. Revert the parity commit(s) if layout/control changes regress auth interaction.
2. Validate rollback by confirming focused auth tests and build return to prior green baseline.
3. No data recovery needed because changes are presentational.

## External References
- [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) [SOURCE:INPUT] Basis: login layout and footer baseline.
- [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html) [SOURCE:INPUT] Basis: registration helper and secondary-action baseline.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] No extra layering-driven wraps in login/register footer/action sections
- [x] `Create an account` and `Sign in instead` match wireframe container size and center alignment
- [x] Separator line remains visible above registration password hint
- [x] Footer links show underline only on hover
- [x] Required fields consistently show red indicators in both forms

## Implementation Checklist
- [x] Remove unnecessary layout layers from login/register footer/action sections
- [x] Normalize ghost-button container sizing and centered text alignment
- [x] Ensure separator line appears above registration password hint
- [x] Enforce hover-only underline behavior for auth footer links
- [x] Restore/verify consistent red required markers on mandatory fields
- [ ] Manually verify desktop and 375px parity against SCR-001 and SCR-002
