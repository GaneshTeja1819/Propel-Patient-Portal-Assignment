# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report plus wireframe HTML for SCR-001 and SCR-002 [SOURCE:INPUT] Basis: user explicitly requested parity fixes against the attached wireframe and called out the remaining mismatches.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect across login and registration auth flows
- **Affected Version**: Current React auth implementation in the workspace
- **Environment**: Windows, React + Vite frontend, browser auth pages

### Steps to Reproduce
1. Open the current React auth screens implemented by [LoginPage.tsx](frontend/src/pages/LoginPage.tsx#L23) and [RegistrationPage.tsx](frontend/src/pages/RegistrationPage.tsx#L23). [SOURCE:INPUT] Basis: bug is reported against the implemented React pages.
2. Compare them to [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) and [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html). [SOURCE:INPUT] Basis: user attached the login wireframe and the registration wireframe is the paired design source.
3. Inspect page-shell branding, footer copy, checkbox sizing, spacing between fields, alternate-link alignment, password helper text, and error-state presentation.
4. **Expected**: The React pages match the wireframe-level structure, spacing, typography, and copy, including the separated footer messages, centered registration back-link, and the simplified password messaging shown in the HTML wireframes. [SOURCE:INFERRED] Basis: wireframes define the visual baseline and the user explicitly called out these parity items.
5. **Actual**: The current React implementation still diverges in a few areas, especially registration password helper content, link alignment, field spacing, and wireframe-style error presentation. [SOURCE:INPUT] Basis: the user explicitly listed the remaining mismatches.

**Error Output**:
```text
No runtime exception. Defect is visual and behavioral parity drift between implemented React auth screens and the wireframe HTML.
```

### Root Cause Analysis
- **File**: [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L324), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L342), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L408), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L414)
- **Component**: Registration form content and alternate-link rendering
- **Function**: Password helper block, password-rule list, terms note, and sign-in link
- **Cause**: The registration form still renders a password helper line, an inline password-rule list, and a left-aligned alternate-link presentation, while the wireframe expects a simpler message flow and centered back-link. [SOURCE:INFERRED] Basis: the wireframe includes a single hint line at [wireframe-SCR-002-registration.html#L131](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html#L131) and the centered `Sign in instead` action at [wireframe-SCR-002-registration.html#L154](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html#L154), but the React form still layers extra rule text and different alignment logic.

Additional root-cause evidence:
- **File**: [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L56), [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L143), [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L159)
- **Component**: Login form alert/footer composition
- **Function**: Error-alert shell, demo shortcuts, and footer copy
- **Cause**: The login form has the right content blocks, but their spacing and copy flow still need to be normalized against the wireframe's line breaks and visual rhythm. [SOURCE:INFERRED] Basis: wireframe login footer line [wireframe-SCR-001-login.html#L200](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html#L200) is a dedicated footer line, while the React footer and supporting text remain coupled through shared form layout rules.

- **File**: [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L4), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L232), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L176), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L202)
- **Component**: Auth layout CSS
- **Function**: Form spacing, checkbox sizing, footer alignment, and alternate-link alignment
- **Cause**: The shared auth CSS is still driving small layout drift between the React screens and the HTML wireframes, particularly in field spacing, checkbox footprint, and link alignment. [SOURCE:INFERRED] Basis: the user explicitly called out spacing/alignment issues, and the current CSS still owns those presentation decisions directly.

Planning-gap evidence:
- The prior implementation focused on getting the auth flows working and only partially closed the wireframe parity gap. That left text-size fidelity, helper-text suppression, and alignment details for a follow-up pass. [SOURCE:INFERRED] Basis: the user request now narrows in on visual parity rather than auth behavior.

Hypotheses evaluated before confirming fix strategy:
- H1: The remaining issue is only CSS spacing. Rejected as insufficient [SOURCE:INFERRED] Basis: the registration password helper content is still rendered in JSX, so CSS alone cannot fully fix the mismatch.
- H2: The issue is only missing error-state styling. Rejected as incomplete [SOURCE:INFERRED] Basis: the wireframe mismatch also includes footer copy breaks, alternate-link alignment, and helper-text behavior.
- H3: The helper text and password rule list should remain for accessibility. Rejected for this bug scope [SOURCE:INPUT] Basis: the user explicitly said there is no need to display those helper texts in the implemented UI.
- H4: The login and registration pages can share one CSS tweak and be done. Rejected [SOURCE:INFERRED] Basis: the two screens have different wireframe-specific alignment rules and different content requirements.

### Impact Assessment
- **Affected Features**: SCR-001 login, SCR-002 registration, first-time onboarding, auth trust/sign-in clarity
- **User Impact**: Users see a UI that looks less like the approved wireframe, with cluttered helper text, different line breaks, and alignment drift that undermines design consistency.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; the impact is primarily usability and trust in the auth flow.

## Fix Overview
Bring the auth screens back into wireframe parity by removing the unnecessary password helper/rule text from registration, centering the registration back-link, splitting copy into separate lines where the wireframe does so, and tightening spacing and typography to match the HTML references. [SOURCE:INFERRED] Basis: the bug is now a fidelity problem rather than a functional auth defect.

## Fix Dependencies
- Existing login and registration submit/login flow must remain unchanged. [SOURCE:INPUT] Basis: the user asked for UI parity, not new auth behavior.
- Design-token-based styling remains the source of truth for any spacing and text-size adjustments. [SOURCE:INPUT] Basis: the frontend already uses tokenized CSS and the project standards require it.
- The wireframe HTML is the canonical source for text wrapping, alternate-link placement, helper-message visibility, and error-state framing. [SOURCE:INPUT] Basis: the user attached the login wireframe and referenced it as the comparison target.

## Impacted Components
### Frontend (React + TypeScript)
- [frontend/src/components/auth/RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.test.tsx](frontend/src/components/auth/RegistrationForm.test.tsx) (UPDATE)
- [frontend/src/components/auth/LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx) (UPDATE)
- [frontend/src/components/auth/LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css) (UPDATE)
- [frontend/src/components/auth/LoginForm.test.tsx](frontend/src/components/auth/LoginForm.test.tsx) (UPDATE)
- [frontend/src/pages/LoginPage.tsx](frontend/src/pages/LoginPage.tsx) (UPDATE if error-shell or footer spacing moves to page scope)
- [frontend/src/pages/LoginPage.module.css](frontend/src/pages/LoginPage.module.css) (UPDATE if page-shell spacing/text-size tuning is required)
- [frontend/src/pages/RegistrationPage.tsx](frontend/src/pages/RegistrationPage.tsx) (UPDATE if page-level copy split is needed)
- [frontend/src/pages/RegistrationPage.module.css](frontend/src/pages/RegistrationPage.module.css) (UPDATE if page-level alignment is needed)
- [frontend/src/pages/LoginPage.test.tsx](frontend/src/pages/LoginPage.test.tsx) (UPDATE)
- [frontend/src/pages/RegistrationPage.test.tsx](frontend/src/pages/RegistrationPage.test.tsx) (UPDATE)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Remove or explicitly suppress the password helper/rule text that the wireframe does not require, and ensure the registration messaging matches the HTML source. [SOURCE:INPUT] Basis: the user explicitly said those helper texts should not be displayed. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Center-align the `Sign in instead` link, normalize field spacing, and tighten supporting text sizes so the layout matches the wireframe. [SOURCE:INPUT] Basis: the user explicitly requested centered alternate-link alignment and spacing corrections. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Assert the password helper text is absent, the sign-in link is centered via the expected class/structure, and required fields still render with the right labels. [SOURCE:INFERRED] Basis: protect the revised registration parity scope. |
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Normalize footer copy line breaks and wireframe-style error presentation, while preserving approved login flow behavior. [SOURCE:INPUT] Basis: the user called out separate-line copy and missing error-state handling. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Correct checkbox sizing, field spacing, divider spacing, footer alignment, and text-size fidelity against SCR-001. [SOURCE:INPUT] Basis: the user explicitly flagged checkbox sizing and spacing/alignment issues. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions for the error-state shell, footer line breaks, and login-page copy that must remain visible after the refactor. [SOURCE:INFERRED] Basis: prevent regressions in the remaining login parity area. |
| MODIFY | frontend/src/pages/LoginPage.tsx | If required by ownership boundaries, move any remaining shell copy or error state out of the form and into the page wrapper to better match SCR-001. [SOURCE:INFERRED] Basis: page-shell layout should follow the wireframe's structure. |
| MODIFY | frontend/src/pages/RegistrationPage.tsx | If required by ownership boundaries, move any remaining shell copy or centered alternate-link structure into the page wrapper to better match SCR-002. [SOURCE:INFERRED] Basis: page-shell layout should follow the wireframe's structure. |
| MODIFY | frontend/src/pages/LoginPage.test.tsx | Add coverage for shell-level copy separation and error-state visibility if those concerns live at the page layer. [SOURCE:INFERRED] Basis: protect the shell refactor if page ownership changes. |
| MODIFY | frontend/src/pages/RegistrationPage.test.tsx | Add coverage for the registration subtitle and shell-level alignment if those concerns live at the page layer. [SOURCE:INFERRED] Basis: protect the shell refactor if page ownership changes. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Compare the wireframe HTML against the current JSX and move or remove any UI blocks that add noise the design does not call for, especially the password helper/rule list.
- Keep the approved auth flow behavior, but make the visual output match the wireframe's line breaks, link alignment, and spacing rhythm.
- Normalize layout spacing and typography with the shared token system so text sizes are consistent across both auth pages.
- Add or update tests to assert that the removed helper text stays removed and that the centered/line-separated copy remains intentional.
- Validate the login and registration pages at desktop and 375px widths after the refactor.

## Regression Prevention Strategy
- [x] Unit test that registration does not render the unnecessary password helper/rule text
- [x] Unit test that the registration back-link remains center-aligned and visible
- [x] Unit test that login footer copy remains split into the expected separate lines
- [x] Edge case test that login error-state presentation still appears without exposing field-level credential detail

## Rollback Procedure
1. Revert the auth-page parity commit(s) if the revised copy/spacing causes a regression in login or registration behavior.
2. Confirm the auth screens return to the last known good build and test state.
3. No data recovery is needed because the changes are presentation-only.

## External References
- [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) [SOURCE:INPUT] Basis: login wireframe source of truth.
- [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html) [SOURCE:INPUT] Basis: registration wireframe source of truth.

## Build Commands
- `cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx src/pages/LoginPage.test.tsx src/pages/RegistrationPage.test.tsx`
- `cd frontend && npm run build`

## Implementation Validation Strategy
- [x] Registration no longer shows the helper/rule text that the wireframe omits
- [x] Registration alternate link is center-aligned
- [x] Login footer copy appears on separate lines as requested
- [x] Login and registration spacing matches the wireframe at desktop and 375px
- [x] Login error state is still visible and does not leak field-level credential detail

## Implementation Checklist
- [x] Remove or suppress the registration password helper/rule text
- [x] Center-align the registration `Sign in instead` link
- [x] Split the requested login/footer copy into separate lines
- [x] Tighten field spacing, checkbox sizing, and text-size fidelity across both auth pages
- [x] Add regression tests for the new parity scope
- [ ] Manually verify desktop and 375px parity against SCR-001 and SCR-002
