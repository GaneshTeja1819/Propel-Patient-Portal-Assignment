# Task - TASK_002

## Requirement Reference
- **User Story:** us_018
- **Story Location:** .propel/context/tasks/EP-004/us_018/us_018.md
- **Acceptance Criteria:**
  - AC-001: Gemini drives conversation using structured output / function calling; each question maps to intake field
  - AC-002: Patient response mapped to intake field with schema validation; unparseable → manual fallback for that field; conversation continues
- **Edge Cases:**
  - Gemini API rate limit mid-conversation → current field returns fallback manual prompt; `intakeMethod` flagged as "AI-Partial"
  - Gemini returns schema-invalid response after max retries → field marked manual; conversation continues
  - Patient returns to in-progress session → session state rehydrated from DB or Redis cache

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

---

## AI References [CONDITIONAL: AI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-001 |
| **AI Pattern** | Structured Output / Function Calling |
| **Prompt Template Path** | backend/src/UPACIP.Infrastructure/AI/IntakeQuestions.json |
| **Guardrails Config** | Max 2 Gemini retries per field; default to manual on failure |
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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — intake session API endpoints |
| AI | Google Gemini API (gemini-1.5-pro) | Google.Ai.Generativelanguage 1.x | TR-008, AIR-001 — structured output / function calling |
| Caching | Upstash Redis | Serverless | NFR-002 — intake session state cache (5-min TTL) |

---

## Task Overview
Implement the AI intake session API: `POST /api/v1/intake/start`, `POST /api/v1/intake/answer`, and `GET /api/v1/intake/session/{appointmentId}`. The `IntakeSessionService` manages conversation state in Redis (TTL = 5 min, refreshed on each answer). Each answer submission calls the Gemini API via `IGeminiService.CallStructuredOutputAsync`, passing the field schema and the patient's raw response. Gemini returns a structured JSON object matching the `IntakeFieldSchema`; the service validates it; on validation failure it retries once. After two failures the field is marked as `ManualRequired = true` and `intakeMethod` becomes `"AI-Partial"`. Responses are accumulated in the session state object and never persisted until the `POST /api/v1/intake/confirm` call (task_003).

## Dependent Tasks
- `task_002_ai-gemini-sdk.md` (US_008) — `IGeminiService` must be registered; Gemini SDK wired
- `task_001_backend-redis.md` (US_003) — Redis cache for session state must be available

## Impacted Components
- `backend/src/UPACIP.Application/Services/IntakeSessionService.cs` — new service managing session state + Gemini calls
- `backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs` — new Gemini adapter for structured intake calls
- `backend/src/UPACIP.Infrastructure/AI/IntakeQuestions.json` — intake question definitions (field key, prompt template, schema)
- `backend/src/UPACIP.API/Controllers/IntakeController.cs` — new controller

## Implementation Plan
1. Create `IntakeQuestions.json`: array of `{ fieldKey, promptTemplate, schema: { type, constraints } }` for each intake field (e.g., chiefComplaint, allergies, currentMedications, smokingStatus, etc.)
2. Create `IntakeSessionState` record: `{ appointmentId, currentFieldIndex, capturedFields: Dictionary<string, object?>, method: "AI" | "AI-Partial", manualRequiredFields: List<string> }`; serialised to Redis key `intake-session-{appointmentId}` with 5-min TTL
3. Create `GeminiIntakeAdapter.CallFieldAsync(fieldKey, patientResponse, fieldSchema)`: constructs Gemini function-calling request; calls `IGeminiService.CallStructuredOutputAsync`; parses response; validates against `fieldSchema`; on failure throws `IntakeSchemaValidationException`
4. Create `IntakeSessionService`:
   - `StartSessionAsync(appointmentId)` → initialise `IntakeSessionState` in Redis; return first question prompt
   - `SubmitAnswerAsync(appointmentId, fieldKey, rawAnswer)` → load state; call `GeminiIntakeAdapter`; on success set field; on `IntakeSchemaValidationException` retry once; on second failure set `manualRequiredFields.Add(fieldKey)`, `method = "AI-Partial"`; advance index; return next question or `{ phase: "summary" }`
   - `GetSessionAsync(appointmentId)` → load state from Redis; return current captured fields (session resume)
5. Create `IntakeController` with `[Authorize(Policy = "PatientPolicy")]`:
   - `POST /api/v1/intake/start` → call `StartSessionAsync`
   - `POST /api/v1/intake/answer` → call `SubmitAnswerAsync`
   - `GET /api/v1/intake/session/{appointmentId}` → call `GetSessionAsync`

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/AI/GeminiService.cs  (from US_008)
    UPACIP.Infrastructure/Caching/             (Redis from US_003)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/AI/IntakeQuestions.json | Intake field definitions with Gemini prompt templates |
| CREATE | backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs | Gemini function-calling adapter for structured intake |
| CREATE | backend/src/UPACIP.Application/Services/IntakeSessionService.cs | Session state management + Gemini orchestration |
| CREATE | backend/src/UPACIP.API/Controllers/IntakeController.cs | intake start/answer/session endpoints |

## External References
- [Gemini API — Function Calling](https://ai.google.dev/api/generate-content#v1beta.GenerationConfig)
- [Gemini structured output docs](https://ai.google.dev/gemini-api/docs/structured-output)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `POST /api/v1/intake/start` returns first question text
- [ ] Valid patient answer → Gemini maps to field; next question returned; session state updated in Redis
- [ ] Force Gemini schema validation failure twice → field marked `ManualRequired`; next question still returned; `method = "AI-Partial"` in session state
- [ ] `GET /api/v1/intake/session/{id}` returns current partial captured fields (session resume)

## Implementation Checklist
- [ ] Create `IntakeQuestions.json` with all intake fields and prompt templates (AC-001)
- [ ] Implement `GeminiIntakeAdapter` using function calling; validate response against field schema (AC-002)
- [ ] Retry once on validation failure; on second failure mark field as `ManualRequired`; set method to "AI-Partial" (AC-002, edge case)
- [ ] Implement `IntakeSessionService` with Redis session state (5-min TTL, refreshed on each answer) (AC-001, AC-002)
- [ ] `POST /api/v1/intake/answer` returns `{ nextQuestion }` or `{ phase: "summary" }` (AC-001)
- [ ] `GET /api/v1/intake/session/{appointmentId}` returns partial state for resume (edge case)
- [ ] All endpoints `[Authorize(Policy = "PatientPolicy")]` (OWASP A01)
