# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PAGE_PARITY]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + wireframe references [SOURCE:INPUT] Basis: user explicitly reported remaining missing page elements, spacing inconsistencies, missing user shortcut references, and layout mismatches versus the HTML wireframes.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect across login and registration shells
- **Affected Version**: Current workspace state after task_001_fix_login-register-wireframe-parity implementation
- **Environment**: Windows, React + Vite frontend, browser-based auth pages

### Steps to Reproduce
1. Open the current React auth screens implemented by `frontend/src/pages/LoginPage.tsx` and `frontend/src/pages/RegistrationPage.tsx` with their child form components. [SOURCE:INPUT] Basis: bug is reported against implemented React code.
2. Compare them to `.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html` and `.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html`. [SOURCE:INPUT] Basis: user supplied the login wireframe and referenced registration wireframe context.
3. Inspect page-shell content, footer/callout copy, required-field markers, error-state presentation, checkbox sizing, field spacing, and demo shortcut/user reference areas.
4. **Expected**: React pages match the approved wireframe shell and supporting content, including branding header/logo, footer/copy, required markers, error/success state presentation, spacing/alignment, and any explicitly requested shortcut references. [SOURCE:INFERRED] Basis: wireframes are the design baseline and user reported missing parity elements.
5. **Actual**: Current React pages still miss page-level shell elements and show visual inconsistencies, including missing logo/header brand block, missing mandatory asterisk markers, partial footer/copy mismatch, missing demo shortcut/user-reference section, left-alignment mismatch for alternate auth links, incomplete error-state parity, and spacing/alignment drift between fields. [SOURCE:INPUT] Basis: user enumerated the missing and inconsistent elements.

**Error Output**:
```text
No runtime exception. Defect is visual/UX parity mismatch between implemented React pages and the wireframe HTML.
```

### Root Cause Analysis
- **File**: `frontend/src/pages/LoginPage.tsx:14` / `frontend/src/pages/RegistrationPage.tsx:8`
- **Component**: Auth page shell composition
- **Function**: Page render structure
- **Cause**: The current implementation renders only title/subtitle plus form, and does not include the wireframe's brand header block, footer/footer-copy structure, or demo/auxiliary sections defined at the page level. [SOURCE:INFERRED] Basis: current page components contain no logo/brand markup, while SCR-001 and SCR-002 wireframes define `auth-logo` blocks at login lines 116-120 and registration lines 70-74.

Additional root-cause evidence:
- **File**: `frontend/src/components/auth/LoginForm.tsx:38` / `frontend/src/components/auth/RegistrationForm.tsx:111`
- **Component**: Auth form parity layer
- **Function**: Form render return block
- **Cause**: The previous parity fix focused on form controls and submit flow, but did not restore all wireframe-only or shell-level content such as required-field asterisks, footer copy variants, left-aligned alternate-link layout, demo shortcut section, or wireframe visual states. [SOURCE:INFERRED] Basis: login form has no footer wrapper, no error-alert shell, and no shortcut block; registration form lacks required marker glyphs, HIPAA sentence, password-strength meter, and exact alternate-link alignment.

Planning-gap evidence:
- **File**: `.propel/context/tasks/EP-001/us_010/task_001_frontend-login.md:49` / `.propel/context/tasks/EP-001/us_009/task_001_frontend-registration.md:53`
- **Component**: Original task scope
- **Function**: Task Overview / Implementation Plan
- **Cause**: Original login and registration implementation tasks only required accessible form behavior and a limited set of fields, not full page-shell parity with every wireframe element. That constrained the initial implementation and allowed page-level mismatches to remain untracked. [SOURCE:INFERRED] Basis: original tasks describe form capture/validation behavior but do not require logo, demo shortcuts, HIPAA note, required-marker visuals, or footer-shell parity.

Why this was not caught earlier:
- The prior bug-fix task closed based on control-level parity and automated component tests, but not a full manual or screenshot-based page-shell comparison. [SOURCE:INFERRED] Basis: task_001 design-compliance report marked visual diff and state capture as skipped.

Hypotheses evaluated before confirming fix strategy:
- H1: Remaining gaps are hidden in CSS and only require styling tweaks. Rejected [SOURCE:INFERRED] Basis: several missing items are absent from JSX entirely, including logo block and demo shortcut section.
- H2: The missing elements were intentionally excluded as non-production/demo content and therefore are not bugs. Partially rejected [SOURCE:INFERRED] Basis: some shortcut content is labeled wireframe-only, but the user explicitly requested those references; the bug task must capture that product decision instead of silently excluding them.
- H3: Current parity is sufficient because original tasks did not require full wireframe fidelity. Rejected [SOURCE:INFERRED] Basis: user has now raised explicit parity defects beyond the original task scope.
- H4: The issue is limited to registration page only. Rejected [SOURCE:INFERRED] Basis: user listed missing elements affecting both login and registration.

### Impact Assessment
- **Affected Features**: SCR-001 login screen, SCR-002 registration screen, first-run user onboarding, visual trust/branding cues
- **User Impact**: Missing branding, missing required markers, inconsistent spacing, missing error-state framing, and absent supporting content reduce trust and make the auth experience feel incomplete or inconsistent with approved design.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; moderate trust/usability impact because error and brand presentation are part of safe auth UX expectations.

## Fix Overview
Implement a second-pass auth-page parity fix that restores page-shell and supporting wireframe content for login and registration, normalizes layout/alignment, and explicitly decides how the wireframe-only demo shortcut/user-reference section should appear in the React app. [SOURCE:INFERRED] Basis: remaining defects are now concentrated in page-level structure and visual fidelity rather than API/form logic.

## Fix Dependencies
- Existing login/registration submit behavior must remain unchanged. [SOURCE:INPUT] Basis: current auth flow has already been implemented and validated.
- Design-token usage must remain the single source of truth for all new styling. [SOURCE:INPUT] Basis: project tasks and existing code require token-based styling.
- A product/engineering decision is needed for wireframe shortcut references (`Patient (Sarah J.)`, `Staff (Alex T.)`, `Admin (Jennifer P.)`): implement visibly, gate to non-production/demo mode, or document exclusion. [SOURCE:INPUT] Basis: user explicitly requested the missing references, while wireframe labels them as demo navigation.

## Impacted Components
### Frontend (React + TypeScript)
- `frontend/src/pages/LoginPage.tsx` (UPDATE)
- `frontend/src/pages/LoginPage.module.css` (UPDATE)
- `frontend/src/pages/LoginPage.test.tsx` (UPDATE)
- `frontend/src/pages/RegistrationPage.tsx` (UPDATE)
- `frontend/src/pages/RegistrationPage.module.css` (UPDATE)
- `frontend/src/components/auth/LoginForm.tsx` (UPDATE)
- `frontend/src/components/auth/LoginForm.module.css` (UPDATE)
- `frontend/src/components/auth/LoginForm.test.tsx` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.tsx` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.module.css` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.test.tsx` (UPDATE)
- Optional: shared auth brand/demo section component if duplication reduction is warranted

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/LoginPage.tsx | Add wireframe brand/logo block, footer copy structure, and page-level sections that belong outside the login form. [SOURCE:INPUT] Basis: logo/footer shell is missing from current login page. |
| MODIFY | frontend/src/pages/LoginPage.module.css | Implement layout, spacing, footer alignment, and left-aligned alternate-link styling to match SCR-001 more closely. [SOURCE:INFERRED] Basis: current shell spacing/alignment does not match wireframe. |
| MODIFY | frontend/src/pages/RegistrationPage.tsx | Add brand/logo block and any page-level structure required for SCR-002 parity. [SOURCE:INPUT] Basis: registration wireframe contains page-level brand shell absent in current page. |
| MODIFY | frontend/src/pages/RegistrationPage.module.css | Normalize shell spacing/alignment for brand block, card spacing, and footer/link positioning. [SOURCE:INFERRED] Basis: remaining inconsistencies are partly page-shell spacing issues. |
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Add missing error-state shell treatment, footer/copy variants, left-aligned “Sign in instead”/account-copy parity, and demo shortcut section or documented decision path for the user references. [SOURCE:INPUT] Basis: user explicitly listed these remaining missing login items. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Correct checkbox sizing, field spacing, divider spacing, footer alignment, and shortcut/demo section styling. [SOURCE:INPUT] Basis: user explicitly flagged checkbox sizing and spacing/alignment issues. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Add required-field asterisk markers, HIPAA sentence, stronger wireframe success/error state parity, password-strength indicator if required, and exact alternate-link presentation. [SOURCE:INPUT] Basis: user explicitly listed required markers, HIPAA copy, and visual inconsistencies. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Adjust field spacing/alignment and supporting content styling to match wireframe structure. [SOURCE:INFERRED] Basis: current form spacing is broader/uniform rather than wireframe-specific. |
| MODIFY | frontend/src/pages/LoginPage.test.tsx | Add assertions for page-shell brand/header/footer content. [SOURCE:INFERRED] Basis: prevent page-shell parity regressions. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions for required missing login-specific items, including any approved shortcut references. [SOURCE:INFERRED] Basis: protect the new parity scope. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add assertions for required markers, HIPAA copy, and refined layout-supporting content. [SOURCE:INFERRED] Basis: protect remaining registration parity fixes. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Compare page-shell ownership in SCR-001 and SCR-002 and move shared brand/footer sections to page components when they do not semantically belong inside the form.
- Add a reusable auth-brand block to eliminate duplication if both pages share the same logo/title-subtext header structure.
- Restore required-field marker visuals and supporting helper/copy content where the wireframes explicitly show them.
- Normalize layout spacing and alignment for checkbox rows, field groups, divider spacing, footer copy, and alternate auth links.
- Implement or explicitly gate the demo shortcut/user-reference section based on the requested bug scope; if gated, use a clear dev-only/non-production condition and test it accordingly.
- Add tests for newly restored shell content and remaining parity items.
- Validate manually at desktop and 375px because the remaining gap is primarily visual/layout fidelity.

## Regression Prevention Strategy
- [x] Unit test for login page shell content (logo, footer copy, alternate-link placement, approved shortcut references)
- [x] Unit test for registration shell content (required markers, HIPAA sentence, link placement, brand block)
- [x] Edge case test for generic login error state rendering without field-level disclosure

## Rollback Procedure
1. Revert the page-shell parity commit(s) if the added structural content introduces layout regressions or conflicts with auth flow behavior.
2. Validate rollback by confirming the existing login/register build and tests return to the prior green baseline.

## External References
- `.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html` [SOURCE:INPUT] Basis: primary login shell parity baseline.
- `.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html` [SOURCE:INPUT] Basis: primary registration shell parity baseline.
- `.propel/context/tasks/EP-001/us_010/task_001_frontend-login.md` [SOURCE:INPUT] Basis: original login task scope for planning-gap analysis.
- `.propel/context/tasks/EP-001/us_009/task_001_frontend-registration.md` [SOURCE:INPUT] Basis: original registration task scope for planning-gap analysis.

## Build Commands
- `cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx`
- `cd frontend && npm run build`

## Implementation Validation Strategy
- [x] All listed missing shell/content items are either implemented or explicitly documented as intentionally dev-only/excluded
- [x] Login and registration pages visually include brand/logo block and corrected footer/link structure
- [ ] Checkbox size, field spacing, and alignment match wireframe expectations at desktop and 375px
- [x] Generic login error state is presented with wireframe-level alert treatment and no field-level credential disclosure

## Implementation Checklist
- [x] Restore login page shell parity (logo/brand block, footer text, alternate-link structure)
- [x] Restore registration page shell parity (logo/brand block, required markers, HIPAA copy, link alignment)
- [x] Fix spacing/alignment drift and checkbox sizing in auth CSS
- [x] Add or explicitly resolve wireframe shortcut/user-reference section (`Patient`, `Staff`, `Admin`) for login page
- [x] Add targeted regression tests for remaining parity scope
- [ ] Manually verify desktop and 375px parity against SCR-001 and SCR-002
