# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY_FOLLOWUP_2]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report plus wireframe HTML for SCR-001 and SCR-002 [SOURCE:INPUT] Basis: user explicitly requested another parity pass against the attached login wireframe and the paired registration wireframe.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UI parity defect across login and registration auth flows
- **Affected Version**: Current React auth implementation after task_003_fix_auth-page-wireframe-parity
- **Environment**: Windows, React + Vite frontend, browser auth pages

### Steps to Reproduce
1. Open the current React auth screens implemented by [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L1) and [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L1) in the workspace.
2. Compare them to [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) and [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html).
3. Inspect checkbox sizing, footer line wrapping, terms/hipaa text sizing, alternate-link border styling, field spacing, text sizing, and password validation/helper messaging.
4. **Expected**: The React pages match the wireframe-level structure, typography, copy, border treatment, and spacing, including the login footer on a single line followed by a separate password-reset line, the registration terms line at 12px with a separate HIPAA line, the bordered `Sign in instead` action, and the password helper/validation text shown in the wireframe source.
5. **Actual**: The current React implementation still diverges in a few areas, including checkbox size, footer copy line wrapping, registration helper/validation messaging, link border styling, and small spacing/text-size mismatches across both forms.

**Error Output**:
```text
No runtime exception. Defect is visual and copy-level parity drift between implemented React auth screens and the wireframe HTML.
```

### Root Cause Analysis
- **File**: [LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx#L159), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L4), [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L232)
- **Component**: Login form footer composition and CSS
- **Function**: Footer copy rendering and link alignment
- **Cause**: The login footer was previously split into multiple blocks and styled with larger body text, which does not match the wireframe's single-line account prompt followed by a separate forgot-password line at 12px. [SOURCE:INFERRED] Basis: SCR-001 shows the account prompt on one line at [wireframe-SCR-001-login.html#L200](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html#L200) and the footer text uses the smaller wireframe typography.

Additional root-cause evidence:
- **File**: [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L40), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L324), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L408), [RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx#L414)
- **Component**: Registration form validation/content rendering
- **Function**: Password validation messaging, terms note, HIPAA note, and alternate-link treatment
- **Cause**: The registration form currently renders only a generic password error and omits the wireframe's password helper/validation text, while the terms and HIPAA lines need to be restored to the smaller wireframe text size and the `Sign in instead` action needs border styling parity. [SOURCE:INFERRED] Basis: SCR-002 includes a password hint at [wireframe-SCR-002-registration.html#L131](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html#L131), a bordered ghost link at [wireframe-SCR-002-registration.html#L154](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html#L154), and smaller body/caption sizing throughout the form.

- **File**: [LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css#L4), [RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css#L1), [LoginPage.module.css](frontend/src/pages/LoginPage.module.css#L1), [RegistrationPage.module.css](frontend/src/pages/RegistrationPage.module.css#L1)
- **Component**: Auth layout CSS
- **Function**: Checkbox sizing, spacing, font sizing, and border styling
- **Cause**: The shared auth CSS still needs fine-grained alignment to the Figma/wireframe sizing rules, especially checkbox dimensions, 12px footer/terms text, and consistent field spacing across both screens. [SOURCE:INFERRED] Basis: the user explicitly called out text sizing and spacing drift, and the wireframe defines the target values directly.

Planning-gap evidence:
- The prior parity pass intentionally simplified some password messaging and footer layout, but this follow-up report reintroduces the original wireframe copy requirements and tighter typography rules.

Hypotheses evaluated before confirming fix strategy:
- H1: The issue is only checkbox sizing. Rejected as incomplete [SOURCE:INFERRED] Basis: the bug report also calls out footer copy wrapping, helper text, and border styling.
- H2: The issue is only registration styling. Rejected [SOURCE:INFERRED] Basis: the login footer line wrapping and 12px sizing are also mismatched.
- H3: The missing password helper/validation text is intentional because the earlier parity pass removed clutter. Rejected for this bug scope [SOURCE:INPUT] Basis: the user explicitly requested the helper text to be restored.
- H4: The border styling on `Sign in instead` is a cosmetic preference, not a bug. Rejected [SOURCE:INPUT] Basis: the user explicitly identified it as missing wireframe parity.

### Impact Assessment
- **Affected Features**: SCR-001 login, SCR-002 registration, first-run onboarding, auth trust/sign-in clarity
- **User Impact**: Users see smaller but noticeable wireframe drift in text size, spacing, helper messaging, and border styling, making the auth experience feel inconsistent with the approved design.
- **Data Integrity Risk**: No
- **Security Implications**: Low direct security risk; the impact is primarily visual fidelity and trust in the auth flow.

## Fix Overview
Restore the missing password helper/validation copy, re-align the login footer into the same-line plus next-line structure, size the footer and terms text to the wireframe's 12px spec, restore the bordered ghost styling on the registration back-link, and tighten the checkbox and field spacing across both auth forms.

## Fix Dependencies
- Existing login and registration submit/login flow must remain unchanged.
- Design-token usage must remain the source of truth for any spacing, sizing, and border adjustments.
- The wireframe HTML remains the canonical source for copy wrapping, helper text, and font sizing.

## Impacted Components
### Frontend (React + TypeScript)
- [frontend/src/components/auth/LoginForm.tsx](frontend/src/components/auth/LoginForm.tsx) (UPDATE)
- [frontend/src/components/auth/LoginForm.module.css](frontend/src/components/auth/LoginForm.module.css) (UPDATE)
- [frontend/src/components/auth/LoginForm.test.tsx](frontend/src/components/auth/LoginForm.test.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.tsx](frontend/src/components/auth/RegistrationForm.tsx) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.module.css](frontend/src/components/auth/RegistrationForm.module.css) (UPDATE)
- [frontend/src/components/auth/RegistrationForm.test.tsx](frontend/src/components/auth/RegistrationForm.test.tsx) (UPDATE)
- [frontend/src/pages/LoginPage.module.css](frontend/src/pages/LoginPage.module.css) (UPDATE if text sizing or shell spacing must be normalized at page scope)
- [frontend/src/pages/RegistrationPage.module.css](frontend/src/pages/RegistrationPage.module.css) (UPDATE if shell spacing or text sizing must be normalized at page scope)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Restore the single-line account prompt followed by the next-line forgot-password link while preserving the existing login flow. [SOURCE:INPUT] Basis: the user explicitly specified the footer line behavior. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Set footer, prompt, and link typography to 12px, tighten spacing, and keep the login footer aligned to the wireframe. [SOURCE:INPUT] Basis: the user explicitly specified 12px text sizes for both footer lines. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Restore the password helper/validation text required by the wireframe while preserving the existing password validation behavior. [SOURCE:INPUT] Basis: the user explicitly requested the missing helper text. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Restore bordered ghost styling for `Sign in instead`, reduce checkbox size, and align form spacing/text sizing to the wireframe. [SOURCE:INPUT] Basis: the user explicitly reported the missing border styling and checkbox size drift. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add assertions for helper text presence, bordered alternate-link styling, and the 12px terms/HIPAA split. [SOURCE:INFERRED] Basis: prevent regressions in the restored wireframe copy and layout. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add assertions for the single-line footer prompt, the separate forgot-password line, and the smaller footer typography behavior. [SOURCE:INFERRED] Basis: protect the login footer parity scope. |
| MODIFY | frontend/src/pages/LoginPage.module.css | Normalize page-level type scale and spacing if the footer/text sizing must be corrected outside the form. [SOURCE:INFERRED] Basis: shell-level typography may need to follow the wireframe more closely. |
| MODIFY | frontend/src/pages/RegistrationPage.module.css | Normalize page-level type scale and spacing if the registration footer/help text must be corrected outside the form. [SOURCE:INFERRED] Basis: shell-level typography may need to follow the wireframe more closely. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Reintroduce the registration password helper/validation copy in the form body so the user sees the exact guidance the wireframe shows.
- Rebuild the login footer copy structure to keep the account prompt on one line and the forgot-password action on the next.
- Reduce the footer and terms text sizes to the 12px spec and keep the HIPAA sentence on its own line.
- Restore the bordered ghost treatment for the registration back-link and reduce checkbox dimensions to the spec.
- Add regression tests for the restored helper text and the line-wrapped footer behavior.
- Validate at desktop and 375px widths after the refactor.

## Regression Prevention Strategy
- [x] Unit test that registration helper/validation text is rendered when required
- [x] Unit test that the registration back-link keeps bordered ghost styling
- [x] Unit test that login footer copy stays on the same line for the account prompt and the next line for forgot-password
- [x] Edge case test that checkbox and footer typography stay within the 12px wireframe spec

## Rollback Procedure
1. Revert the auth-page parity commit(s) if restoring the helper text or footer wrapping introduces a regression.
2. Confirm the auth screens return to the previous green build and test state.
3. No data recovery is needed because the changes are presentation-only.

## External References
- [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html) [SOURCE:INPUT] Basis: login wireframe source of truth.
- [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html) [SOURCE:INPUT] Basis: registration wireframe source of truth.

## Build Commands
- `cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx`
- `cd frontend && npm run build`

## Implementation Validation Strategy
- [x] Registration helper/validation text matches the wireframe copy
- [x] Registration back-link shows bordered ghost styling
- [x] Login footer prompt stays on one line and forgot-password moves to the next line
- [x] Footer and terms typography match the 12px wireframe spec
- [x] Checkbox size and form spacing match the wireframe at desktop and 375px

## Implementation Checklist
- [x] Restore the registration password helper/validation text
- [x] Restore bordered `Sign in instead` button styling
- [x] Rebuild the login footer copy into the requested line structure
- [x] Adjust checkbox size and footer/terms text sizing to the wireframe spec
- [x] Add regression tests for the restored helper and footer layout
- [ ] Manually verify desktop and 375px parity against SCR-001 and SCR-002