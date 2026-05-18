# Task - TASK_002

## Requirement Reference
- **User Story:** us_008
- **Story Location:** .propel/context/tasks/EP-DATA/us_008/us_008.md
- **Acceptance Criteria:**
  - AC-003: Gemini SDK configured with `gemini-1.5-pro` and structured output (JSON schema); test invocation deserialises to target C# type without regex parsing
  - AC-004: Every Gemini API call (success or failure) writes an `AI_INVOCATION` audit log entry with modelVersion, promptHash, inputTokenCount, outputTokenCount, responseLatencyMs, httpStatusCode, and timestamp
- **Edge Cases:**
  - Gemini API key not configured → `InvalidOperationException` at startup; application exits non-zero; no request reaches Gemini

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
| **AIR Requirements** | AIR-006 |
| **AI Pattern** | Tool Calling / Structured Output |
| **Prompt Template Path** | N/A (foundation task — prompt templates added per feature) |
| **Guardrails Config** | N/A (schema validation enforced at SDK level per AIR-002) |
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
| AI/ML | Gemini API (Google.Ai.Generativelanguage) | gemini-1.5-pro; 1.x | AIR-001–AIR-003, TR-006 — BRD §5; structured output mode; free-tier AI engine |
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — Infrastructure layer hosts Gemini SDK client |
| Database | PostgreSQL via Supabase | 15 | DR-003, AIR-006 — `AI_INVOCATION` entries written to `audit.audit_log` |

---

## Task Overview
Configure the Google Gemini SDK (`Google.Ai.Generativelanguage 1.x`) in the Infrastructure layer targeting `gemini-1.5-pro` with structured output (function calling / JSON schema mode). Implement `IGeminiClient` with a decorator pattern that captures invocation metadata (model version, prompt SHA-256 hash, token counts, latency, HTTP status) and writes an `AI_INVOCATION` audit log entry via `IAuditLogService` after every call — success or failure. The Gemini API key is loaded exclusively from an environment variable; missing key causes a startup failure.

## Dependent Tasks
- `task_001_backend-phi-encryption.md` (US_006) — `IAuditLogService` must exist for AI invocation logging
- `task_002_database-audit-schema.md` (US_006) — `audit.audit_log` table must exist for `AI_INVOCATION` entries

## Impacted Components
- `backend/src/UPACIP.Application/Interfaces/IGeminiClient.cs` — new Gemini client interface
- `backend/src/UPACIP.Infrastructure/AI/GeminiClient.cs` — new SDK wrapper with structured output
- `backend/src/UPACIP.Infrastructure/AI/GeminiInvocationLogger.cs` — new logging decorator
- `backend/src/UPACIP.API/Program.cs` — register Gemini client in DI; validate API key at startup

## Implementation Plan
1. Add `Google.Ai.Generativelanguage` 1.x NuGet to Infrastructure project
2. Define `IGeminiClient` in Application layer: `InvokeStructuredAsync<TResponse>(string prompt, object jsonSchema, CancellationToken ct): Task<TResponse>` where TResponse is a C# type matching the JSON schema
3. Implement `GeminiClient` in Infrastructure: initialise `GenerativeModel` with model name `gemini-1.5-pro` and `GenerationConfig` specifying `response_mime_type = "application/json"` and the provided JSON schema for structured output mode; validate API key from `Environment.GetEnvironmentVariable("GEMINI_API_KEY")` — throw `InvalidOperationException` at construction if absent
4. Implement `GeminiInvocationLogger` as a decorator around `IGeminiClient`: before invocation, compute `promptHash = SHA256(prompt)`; start a `Stopwatch`; after invocation (in a try/finally), record `responseLatencyMs`, `inputTokenCount`, `outputTokenCount`, and `httpStatusCode`; call `IAuditLogService.LogAsync` with `actionType = "AI_INVOCATION"` and all metadata fields serialised into `metadata`
5. Register `GeminiClient` wrapped by `GeminiInvocationLogger` as the `IGeminiClient` singleton in `Program.cs` DI; validate key presence at startup — exit non-zero if missing
6. Write a smoke test in `UPACIP.Tests` that invokes `IGeminiClient` with a minimal schema and confirms the response deserialises to the target type without an exception (integration test — requires a valid API key in CI secrets)

## Current Project State
```
backend/
  src/
    UPACIP.Application/Interfaces/  (IAuditLogService registered in US_006)
    UPACIP.Infrastructure/Audit/AuditLogService.cs  (from US_006)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj | Add Google.Ai.Generativelanguage 1.x NuGet |
| CREATE | backend/src/UPACIP.Application/Interfaces/IGeminiClient.cs | Generic structured-output invocation interface |
| CREATE | backend/src/UPACIP.Infrastructure/AI/GeminiClient.cs | Gemini SDK wrapper; structured output; key from env var |
| CREATE | backend/src/UPACIP.Infrastructure/AI/GeminiInvocationLogger.cs | Decorator capturing promptHash, tokens, latency, httpStatus → AI_INVOCATION audit entry |
| MODIFY | backend/src/UPACIP.API/Program.cs | Register IGeminiClient (GeminiClient + GeminiInvocationLogger); startup key validation |
| CREATE | backend/tests/UPACIP.Tests/SmokeTests/GeminiClientSmokeTest.cs | Integration smoke test: invoke with minimal schema; confirm deserialisation |

## External References
- [Google AI .NET SDK (Generativelanguage)](https://www.nuget.org/packages/Google.Ai.Generativelanguage)
- [Gemini Structured Output (Function Calling)](https://ai.google.dev/gemini-api/docs/function-calling)
- [Gemini gemini-1.5-pro Model Card](https://ai.google.dev/gemini-api/docs/models/gemini)
- [SHA-256 in .NET (System.Security.Cryptography.SHA256)](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Smoke test: `IGeminiClient.InvokeStructuredAsync` with a `{ "type": "object", "properties": { "answer": { "type": "string" } } }` schema deserialises to `AnonymousResponse { string Answer }` without regex parsing
- [ ] After invocation, `audit.audit_log` contains one new row with `action_type = 'AI_INVOCATION'` and all required metadata fields populated
- [ ] API startup with `GEMINI_API_KEY` absent → `InvalidOperationException` logged; process exits non-zero
- [ ] `promptHash` in the audit entry matches `SHA256(prompt)` computed independently

## Implementation Checklist
- [x] Add Google.Ai.Generativelanguage 1.x to Infrastructure; configure `GenerativeModel` with `gemini-1.5-pro` and structured output JSON schema mode (AC-003)
- [x] Validate `GEMINI_API_KEY` env var at client construction; throw `InvalidOperationException` and exit non-zero if absent (AC-003 edge case)
- [x] Implement `GeminiInvocationLogger` decorator: compute `promptHash` (SHA-256), capture latency, token counts, HTTP status (AC-004)
- [x] Write `AI_INVOCATION` audit entry via `IAuditLogService` in `finally` block — runs on both success and failure (AC-004)
- [x] Register `IGeminiClient` (decorated with logger) as singleton in `Program.cs` DI (AC-003, AC-004)
- [x] Smoke test confirms structured response deserialises to target C# type without regex parsing (AC-003)
