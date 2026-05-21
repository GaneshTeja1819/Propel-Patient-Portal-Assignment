# Task - TASK_001

## Requirement Reference
- **User Story:** us_018
- **Story Location:** .propel/context/tasks/EP-004/us_018/us_018.md
- **Acceptance Criteria:**
  - AC-001: Gemini prompts with first structured intake question in conversational language on session initialise
  - AC-003: Summary of all captured fields displayed for review + edit before confirmation; no persistence until confirmed
  - AC-005: Progress bar / step counter visible during conversation (e.g. "Question 3 of 8") with completion percentage
- **Edge Cases:**
  - Patient leaves mid-conversation and returns → partially captured fields re-hydrated from session state; conversation resumes from last unanswered question
  - Gemini rate limit → field presented as manual input fallback; mode shown as "AI-Partial"

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | figma_spec.md §4 — SCR-007 AI Intake |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-007-ai-intake.html |
| **Screen Spec** | SCR-007 |
| **UXR Requirements** | UXR-502 |
| **Design Tokens** | `--color-primary`, `--spacing-*`, `--radius-*` from variables.css; progress bar fill uses `--color-primary`; PHI fields use `color-surface-phi` background token; AI content uses `color-ai-accent` pill badge per designsystem.md §SCR-007 |

---

## AI References [CONDITIONAL: AI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-001 |
| **AI Pattern** | Structured Output / Function Calling |
| **Prompt Template Path** | .propel/context/prompts/intake-questions.json (to be created in task_002) |
| **Guardrails Config** | N/A — guardrails enforced server-side in task_002 |
| **Model Provider** | Google Gemini (gemini-1.5-pro) |

---

## Mobile References [CONDITIONAL: Mobile Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

---

## Applicable Technology Stack

| Layer | Technology | Version | Justification |
|-------|------------|---------|---------------|
| Frontend | React (SPA) | 18.x | TR-001 — AI intake conversation UI on SCR-007 |
| Library | TypeScript | 5.5.x | NFR-012 — type-safe component props; enforces IntakeSessionState shape at build time |

---

## Task Overview
Build the AI intake conversation UI on SCR-007. The component `AIIntakeChat` renders a message-thread-style conversation: each question is a "bot message" bubble; the patient types a response in a fixed input row. A `IntakeProgressBar` component renders the current question index and total count as a fraction and a CSS progress fill. The summary screen renders a review card for each captured field with inline edit inputs. A "Switch to manual form" link is present throughout the AI flow. Session state (partial answers) is persisted in `sessionStorage` so a browser refresh restores progress.

## Dependent Tasks
- `task_001_frontend-scaffold.md` (US_001) — design tokens must exist
- `task_001_frontend-login.md` (US_010) — `AuthContext` must exist; `appointmentId` available in context

## Impacted Components
- `frontend/src/pages/IntakePage.tsx` — new SCR-007 / SCR-008 shared route
- `frontend/src/components/intake/AIIntakeChat.tsx` — new conversation component
- `frontend/src/components/intake/IntakeProgressBar.tsx` — new progress bar
- `frontend/src/components/intake/IntakeSummaryReview.tsx` — new summary + edit component
- `frontend/src/hooks/useAIIntake.ts` — new hook driving Gemini API calls via backend

## Implementation Plan
1. Create `IntakePage.tsx` at `/intake/:appointmentId`; route-level state: `mode: 'ai' | 'manual'`; persists mode switch to `sessionStorage`
2. Create `AIIntakeChat.tsx`:
   - Renders message list (`AIMessage`, `PatientMessage` styled bubbles)
   - Input row: controlled `<textarea>` with "Send" button; Enter-to-submit (Shift+Enter for newline)
   - On mount: call `useAIIntake.startSession(appointmentId)` → receives first question
   - On submit: call `useAIIntake.submitAnswer(fieldKey, answer)` → receives next question or `{ phase: 'summary' }`
   - On rate-limit / fallback: renders `<ManualFieldFallback fieldKey={key} />` inline instead of chat input
   - Partial state persisted in `sessionStorage['intake-{appointmentId}']` on each answer
3. Create `IntakeProgressBar.tsx`: accepts `{ current, total }`; displays "Question {current} of {total}"; fills CSS progress bar `width: (current/total * 100)%` using `--color-primary`; `role="progressbar"`, `aria-valuenow`, `aria-valuemax`
4. Create `IntakeSummaryReview.tsx`: renders a table/card grid of `{ fieldLabel, capturedValue }` pairs; each row has an "Edit" button that renders an inline input; "Confirm" CTA calls `useAIIntake.confirmIntake()` which triggers the backend persistence (task_003)
5. "Switch to manual form" link: sets `mode = 'manual'`; passes current `capturedFields` to `ManualIntakeForm` (US_019 task_001); no data loss

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx  (from US_010)
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/IntakePage.tsx | Shared intake page with mode state (ai/manual) |
| CREATE | frontend/src/components/intake/AIIntakeChat.tsx | Conversation UI with fallback fields |
| CREATE | frontend/src/components/intake/IntakeProgressBar.tsx | Accessible progress bar |
| CREATE | frontend/src/components/intake/IntakeSummaryReview.tsx | Captured field summary + inline edit |
| CREATE | frontend/src/hooks/useAIIntake.ts | Backend-driven Gemini session hook |
| MODIFY | frontend/src/App.tsx | Add /intake/:appointmentId route |

## External References
- [wireframe-SCR-007-ai-intake.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-007-ai-intake.html)
- [WCAG 2.2 SC 4.1.3 Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
- [ARIA progressbar pattern](https://www.w3.org/WAI/ARIA/apg/patterns/meter/)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] AI intake session opens; first question renders as bot bubble; progress bar shows "Question 1 of N"
- [ ] After each answer submission, progress bar advances; question number increments
- [ ] Simulate mid-session browser refresh → session restored from `sessionStorage`; progress resumes
- [ ] Reach summary step → all captured fields displayed; edit a field inline; confirm CTA visible

## Implementation Checklist
- [x] Build `AIIntakeChat` with message-thread UI; bot and patient message bubbles (AC-001)
- [x] Build `IntakeProgressBar` with `role="progressbar"`, aria attributes; advances per answer (AC-005)
- [x] Persist partial answers in `sessionStorage` keyed by appointmentId (edge case — resume)
- [x] Render `ManualFieldFallback` inline on rate-limit / schema-invalid response (edge case)
- [x] Build `IntakeSummaryReview` with inline field editing; no persistence until "Confirm" clicked (AC-003)
- [x] Annotate PHI fields with `🔒` lock icon and `color-surface-phi` background token; AI content with `color-ai-accent` badge (UXR-402)
- [x] "Switch to manual form" link passes current captured fields to manual mode; no data loss (US_019 AC-004 enabler)
- [x] Add `/intake/:appointmentId` route to `App.tsx` (AC-001)
