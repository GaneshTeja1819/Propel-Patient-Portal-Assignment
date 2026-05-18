# Bug Fix Task - [BUG_LOGIN_REGISTER_WIREFRAME_PARITY]

## Bug Report Reference
- Bug ID: bug_login-register-wireframe-parity
- Source: User report + wireframe references [SOURCE:INPUT] Basis: user explicitly reported missing fields/placeholders/labels in login and register pages compared to wireframe HTML.

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Major UX parity defect across auth entry flows
- **Affected Version**: Current workspace state (frontend React implementation)
- **Environment**: Windows, React + Vite frontend, browser-based login/register screens

### Steps to Reproduce
1. Open login and registration wireframes: `.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html` and `.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html` [SOURCE:INPUT] Basis: user-provided wireframe reference.
2. Open React implementation files: `frontend/src/components/auth/LoginForm.tsx` and `frontend/src/components/auth/RegistrationForm.tsx`.
3. Compare field inventory, placeholders, labels, helper text, and secondary actions.
4. **Expected**: React screens preserve required UI content parity with approved wireframes (required fields, placeholders, labels, helper/legal text, and linked secondary actions) [SOURCE:INFERRED] Basis: wireframe is accepted visual/UX baseline for SCR-001 and SCR-002.
5. **Actual**: Multiple wireframe elements are absent in React forms, resulting in incomplete auth UI implementation and lower usability/accessibility context.

**Error Output**:
```text
No runtime exception. Defect is functional/UX parity gap between reference wireframes and implemented React forms.
```

### Root Cause Analysis
- **File**: `frontend/src/components/auth/LoginForm.tsx:60` / `frontend/src/components/auth/LoginForm.tsx:101`
- **Component**: Login form UI composition
- **Function**: LoginForm render return block
- **Cause**: Initial implementation focused on minimum validation and submit behavior, but omitted non-core wireframe UI elements (placeholder copy, remember-me control, show/hide password, register CTA, forgot-password link). [SOURCE:INFERRED] Basis: file includes only two inputs + submit button, while wireframe defines additional controls at `.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html:143`, `:159`, `:172`, `:184`, `:201`.

Additional root-cause evidence:
- **File**: `frontend/src/components/auth/RegistrationForm.tsx:112` / `frontend/src/components/auth/RegistrationForm.tsx:220`
- **Component**: Registration form field model
- **Function**: RegistrationForm render and form value interface
- **Cause**: Data model and form schema were scoped to task acceptance criteria for basic account creation only, leaving out wireframe parity fields and content (DOB, phone, placeholders, terms/legal copy, password visibility toggles, sign-in alternate CTA, success banner/redirect cue). [SOURCE:INFERRED] Basis: interface lacks `dateOfBirth` and `phone`; no terms text or CTA in rendered JSX, while wireframe explicitly defines these at `.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html:111`, `:118`, `:150`, `:154`.

Hypotheses evaluated before confirming fix strategy:
- H1: API contract intentionally excludes DOB/phone and parity mismatch is expected. Rejected [SOURCE:INFERRED] Basis: user bug report requests UI parity restoration and wireframe defines these as visible form elements.
- H2: Missing items are implemented at page level (not form level). Rejected [SOURCE:INFERRED] Basis: `frontend/src/pages/LoginPage.tsx:25` and `frontend/src/pages/RegistrationPage.tsx:17` only mount form components without missing controls.
- H3: Missing content is hidden in CSS. Rejected [SOURCE:INFERRED] Basis: corresponding JSX nodes do not exist in form components.
- H4: Feature regression from prior commits. Inconclusive [SOURCE:INFERRED] Basis: no meaningful fix-history output available for target files in this session.

### Impact Assessment
- **Affected Features**: Patient login UX (SCR-001), patient registration UX (SCR-002), authentication entry conversion path
- **User Impact**: Users lose expected affordances and context (example input hints, remember-me preference, password visibility, legal notice, alternate navigation), increasing friction and possible form abandonment.
- **Data Integrity Risk**: No
- **Security Implications**: Moderate UX-security impact; missing password visibility control and context hints can increase entry errors. No direct auth bypass/injection risk identified.

## Fix Overview
Implement wireframe-to-React parity restoration for login and registration forms by adding missing UI elements and controlled fields while preserving existing validation and API integration behavior. [SOURCE:INFERRED] Basis: defect scope is presentation and UX structure mismatch, not backend contract failure.

## Fix Dependencies
- React Hook Form schema update for new optional/required visual fields [SOURCE:INFERRED] Basis: registration model currently lacks wireframe parity fields.
- Existing auth hooks (`useLogin`, `useRegistration`) must remain contract-compatible [SOURCE:INPUT] Basis: current hooks are already integrated and used across pages.
- Frontend tests must be expanded to cover parity-critical controls [SOURCE:INFERRED] Basis: current tests assert only minimal field/submit scenarios.

## Impacted Components
### Frontend (React + TypeScript)
- `frontend/src/components/auth/LoginForm.tsx` (UPDATE)
- `frontend/src/components/auth/LoginForm.module.css` (UPDATE)
- `frontend/src/components/auth/LoginForm.test.tsx` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.tsx` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.module.css` (UPDATE)
- `frontend/src/components/auth/RegistrationForm.test.tsx` (UPDATE)
- `frontend/src/pages/LoginPage.tsx` (POTENTIAL UPDATE: if cross-linking/help content placement remains page-level)
- `frontend/src/pages/RegistrationPage.tsx` (POTENTIAL UPDATE: if success redirect message handoff is page-level)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/auth/LoginForm.tsx | Add missing placeholders, remember-me checkbox, password show/hide toggle, create-account CTA link, and forgot-password link with accessible labels [SOURCE:INPUT] Basis: wireframe parity requirement. |
| MODIFY | frontend/src/components/auth/LoginForm.module.css | Add styles for checkbox row, password wrapper/toggle, divider, secondary CTA button, and footer links consistent with token system [SOURCE:INFERRED] Basis: new JSX requires corresponding styles. |
| MODIFY | frontend/src/components/auth/LoginForm.test.tsx | Add regression tests for remember-me rendering, placeholder visibility, password toggle behavior, and secondary links [SOURCE:INFERRED] Basis: prevent parity regressions. |
| MODIFY | frontend/src/components/auth/RegistrationForm.tsx | Add missing fields (`dateOfBirth`, `phone`), wireframe placeholders, password visibility toggles, legal text, alternate sign-in CTA, and success status/redirect cue handling [SOURCE:INPUT] Basis: SCR-002 wireframe includes these controls/content. |
| MODIFY | frontend/src/components/auth/RegistrationForm.module.css | Add styles for new fields, helper text, password wrapper/toggles, terms note, divider, and secondary button/link [SOURCE:INFERRED] Basis: layout parity and responsive consistency. |
| MODIFY | frontend/src/components/auth/RegistrationForm.test.tsx | Add regression tests for added field labels/placeholders, legal text, and sign-in alternate CTA presence [SOURCE:INFERRED] Basis: enforce wireframe parity via tests. |

> Only list concrete, verifiable file operations. No speculative directory trees.

## Implementation Plan
- Update login form JSX structure to include all wireframe-defined controls without breaking existing submit flow.
- Introduce local UI state for password visibility toggle(s) and preserve current accessibility semantics (`aria-required`, `aria-invalid`, `aria-describedby`).
- Extend registration form value model with added fields and validation rules aligned to wireframe-required indicators.
- Keep phone optional and DOB required if wireframe-required marker is present; if API does not consume these fields yet, confine them to UI state until backend contract is updated.
- Add legal/secondary action content and routing links for both auth pages.
- Expand component tests to validate new fields, placeholders, toggles, and links.
- Verify desktop/mobile rendering parity against SCR-001 and SCR-002 references.

## Regression Prevention Strategy
- [x] Unit test for login parity controls (placeholders, remember-me, password toggle, secondary links)
- [x] Unit test for registration parity controls (DOB, phone, placeholders, legal text, sign-in alternative)
- [x] Edge case test for password toggle + validation error coexistence in both forms

## Rollback Procedure
1. Revert the auth form parity commit(s) if UX regressions or auth flow breakage appears after deployment.
2. Validate rollback by confirming login/register submit paths and existing auth tests return to prior green baseline.

## External References
- `.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html` [SOURCE:INPUT] Basis: primary login UX baseline.
- `.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html` [SOURCE:INPUT] Basis: primary registration UX baseline.

## Build Commands
- `cd frontend && npm run test -- src/components/auth/LoginForm.test.tsx src/components/auth/RegistrationForm.test.tsx`
- `cd frontend && npm run dev`

## Implementation Validation Strategy
- [x] Bug no longer reproducible (all missing fields/placeholders/labels/secondary actions now present per wireframes)
- [x] All existing tests pass
- [x] New regression tests pass

## Implementation Checklist
- [x] Add login wireframe parity controls and content in React component
- [x] Add registration wireframe parity controls/content and field model updates
- [x] Add/adjust CSS for new controls in both auth modules using existing design tokens
- [x] Add targeted regression tests covering new parity requirements
- [ ] Manually verify 375px and desktop parity for SCR-001/SCR-002
