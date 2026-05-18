# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_5]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + wireframe references for SCR-001 and SCR-002 [SOURCE:INPUT] Basis: user explicitly reported remaining login/register mismatches and provided SCR-001 wireframe HTML.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect in authentication entry flows
- **Affected Version**: Current React auth implementation after task_006_fix_auth-layout-layering-and-link-behavior-parity
- **Environment**: Windows, React + Vite frontend, desktop + mobile viewport rendering

### Steps to Reproduce
1. Open login and register screens implemented by frontend auth components.
2. Compare rendered output with SCR-001 and SCR-002 wireframe HTML layouts.
3. Inspect structure depth, secondary-action container size, and required-input indicators.
4. **Expected**: login/register forms use wireframe-aligned structure without unnecessary layered wrappers; secondary actions (`Create an account`, `Sign in instead`) use containers matching primary button sizing and centered text; all mandatory fields display a red triangle/exclamation required indicator per requested wireframe parity behavior.
5. **Actual**: users still report wrapping/alignment drift from layered structures, undersized secondary-action button containers, and missing required-field alert indicators.

**Error Output**:
```text
No runtime exception. Defect is visual/layout parity mismatch versus wireframe and requested required-indicator behavior.
```

### Root Cause Analysis
- **File**: frontend/src/components/auth/LoginForm.tsx
- **Component**: Login form structure and required marker rendering
- **Function**: Footer/action layout and field-label marker composition
- **Cause**: Structure and marker composition still depend on prior parity assumptions (asterisk markers + existing wrappers) that do not satisfy the newly requested required-indicator treatment. [SOURCE:INPUT] Basis: user explicitly requested red triangle exclamation next to mandatory inputs.

Additional root-cause evidence:
- **File**: frontend/src/components/auth/RegistrationForm.tsx
- **Component**: Registration required-marker and action control composition
- **Function**: Required-field signaling and secondary action placement
- **Cause**: Registration currently uses red asterisk-only markers and existing ghost-link treatment, while the report requires an alert-triangle required indicator style and container parity with primary action dimensions. [SOURCE:INPUT] Basis: user explicitly requested required-indicator change and secondary-container sizing correction.

- **File**: frontend/src/components/auth/LoginForm.module.css
- **Component**: Login action and field-marker styling
- **Function**: Secondary action sizing and text alignment
- **Cause**: CSS size/alignment tokens may still diverge from exact primary-control height/box behavior under current layout constraints. [SOURCE:INFERRED] Basis: repeated user reports of button box size mismatch after prior fixes.

- **File**: frontend/src/components/auth/RegistrationForm.module.css
- **Component**: Registration action and required-marker styling
- **Function**: Secondary action dimensions and required marker visuals
- **Cause**: Existing required marker class supports asterisk-only visual semantics; no dedicated visual contract for triangle/exclamation indicator currently enforced. [SOURCE:INPUT] Basis: user explicitly requested red triangle exclamation indicator.

Hypotheses evaluated before selecting fix direction:
- H1: Remaining issue is only wrapper layering. Rejected as incomplete [SOURCE:INFERRED] Basis: report also calls out button container size and required indicators.
- H2: Remaining issue is only secondary button text alignment. Rejected [SOURCE:INFERRED] Basis: request includes both sizing parity and new required indicator semantics.
- H3: Existing required asterisk markers already satisfy requirement. Rejected [SOURCE:INPUT] Basis: user explicitly requested red triangle exclamation icon.
- H4: Changes are login-only. Rejected [SOURCE:INPUT] Basis: request explicitly includes both login and register pages.

### Impact Assessment
- **Affected Features**: SCR-001 login parity, SCR-002 registration parity, mandatory-field affordance consistency
- **User Impact**: Authentication screens appear inconsistent with wireframe intent; required-field discoverability may be reduced for some users.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security impact; primary impact is UX fidelity and form affordance clarity.

## Fix Overview
Apply a focused parity pass that removes any remaining layout layering causing wrap drift, normalizes secondary action containers to primary-button dimensions with centered labels, and introduces a consistent red triangle/exclamation required indicator for mandatory fields on both login and registration forms.

## Fix Dependencies
- Existing login/register submission behavior must remain unchanged.
- Token-based styling and accessibility semantics remain mandatory.
- Wireframe HTML remains baseline for layout sizing and control structure.

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
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Replace mandatory-field marker rendering with required-indicator element capable of red triangle/exclamation semantics while preserving label accessibility. [SOURCE:INPUT] Basis: user explicitly requested red triangle exclamation indicator. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Ensure `Create an account` container matches primary button dimensions and keeps centered text; add style for red required indicator icon. [SOURCE:INPUT] Basis: user requested button box parity and centered text. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Apply same required-indicator rendering pattern to all mandatory registration fields. [SOURCE:INPUT] Basis: user requested mandatory indicator consistency across login/register. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Normalize `Sign in instead` container dimensions relative to primary button; add shared red required-indicator styling contract. [SOURCE:INPUT] Basis: user reported undersized secondary button box and missing required alert markers. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions for required-indicator presence/semantics and secondary-action sizing class hooks. [SOURCE:INFERRED] Basis: regression protection for newly requested marker behavior and box parity. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add assertions for required-indicator consistency on all mandatory fields and secondary-action container parity hooks. [SOURCE:INFERRED] Basis: prevent recurrence of registration parity drift. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Audit login/register component tree for leftover nested wrappers that can force text wrap drift.
- Normalize secondary-action controls to the same container sizing contract as primary auth buttons.
- Add a dedicated required-indicator element and style (red triangle/exclamation treatment) for all required labels.
- Preserve existing validation messages and accessibility attributes while adjusting indicator visuals.
- Extend component tests to assert required indicator rendering and secondary action class-level parity.
- Run focused auth tests and production build.

## Regression Prevention Strategy
- [x] Unit test that required indicator renders on all mandatory login fields
- [x] Unit test that required indicator renders on all mandatory registration fields
- [x] Unit test that secondary actions keep centered text and primary-equivalent container sizing hooks
- [x] Integration test that login/register interactions still submit and validate as before

## Rollback Procedure
1. Revert the task_007 parity commit set if required-indicator or container-size changes regress auth behavior.
2. Re-run focused auth tests and build to confirm baseline restoration.
3. No data recovery required because modifications are presentational.

## External References
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html [SOURCE:INPUT] Basis: login layout/button structure baseline.
- .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html [SOURCE:INPUT] Basis: registration layout/button structure baseline.

## Build Commands
- cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx
- cd frontend && npm run build

## Implementation Validation Strategy
- [x] Login/register text flow remains aligned with wireframe layout intent (no unnecessary wrapper-induced wraps)
- [x] `Create an account` and `Sign in instead` containers match primary button dimensions and centered labels
- [x] Required fields on both forms render red triangle/exclamation indicator treatment consistently
- [x] Existing auth tests pass with new assertions
- [x] Production build succeeds

## Implementation Checklist
- [x] Remove/simplify remaining layered structures causing wrap/alignment drift
- [x] Normalize secondary-action button container dimensions to primary button contract
- [x] Keep secondary-action text centered (`Create an account`, `Sign in instead`)
- [x] Add red triangle/exclamation required indicator to all mandatory fields in login/register
- [x] Update auth tests to cover required-indicator and button-container parity
- [x] Validate with focused auth tests and frontend build
- [ ] Manually verify desktop and 375px visual parity against SCR-001 and SCR-002
