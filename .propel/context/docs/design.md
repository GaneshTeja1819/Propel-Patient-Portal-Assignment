---
title: "Architecture Design – Unified Patient Access & Clinical Intelligence Platform"
version: "1.0"
date: "2026-05-14"
source: "spec.md v1.0, BRD Consolidated v1.0"
status: "Draft"
---

# Architecture Design

## Project Overview

The Unified Patient Access & Clinical Intelligence Platform is a greenfield, web-based healthcare system serving three user roles — Patient, Staff, and Admin. It combines intelligent appointment scheduling (booking, preferred-slot swap, multi-channel reminders, calendar sync) with a Trust-First clinical intelligence engine (Gemini-powered PDF data extraction, 360° patient profile, ICD-10/CPT code suggestion with human verification). The platform is deployed on free/open-source infrastructure exclusively and must be 100% HIPAA-compliant throughout Phase 1.

---

## Architecture Goals

- **Architecture Goal 1**: Constraint-driven technology selection — every technology choice is traceable to an NFR, DR, or AIR; no speculative additions.
- **Architecture Goal 2**: HIPAA-first data design — PHI encryption, immutable audit log, and minimum-necessary access are non-negotiable and enforced at the architecture level.
- **Architecture Goal 3**: Trust-First AI — all AI outputs pass through a mandatory human verification gate before any clinical decision is persisted; model inputs, outputs, and metadata are logged per invocation.
- **Architecture Goal 4**: Free-tier sustainability — the architecture operates within free-tier limits on all Phase 1 platforms (InfinityFree, MonsterASP, Supabase, Upstash Redis, GitHub Actions).
- **Architecture Goal 5**: Background job resilience — all asynchronous operations (reminders, slot swap, extraction, notifications, PDF generation) are idempotent and retry-safe.
- **Architecture Goal 6**: Concurrency safety — slot reservation uses optimistic locking to prevent double-booking under concurrent load.

---

## Non-Functional Requirements

- NFR-001: [SOURCE:INPUT] System MUST maintain 99.9% uptime measured on a rolling 30-day window, across both the React frontend and .NET backend.
  Basis: BRD §7.2 — "99.9% uptime target".

- NFR-002: [SOURCE:INFERRED] System MUST serve API responses at P95 ≤ 500 ms under normal operating load; slot availability reads MUST complete in ≤ 100 ms via Redis cache.
  Basis: Implied by real-time slot display requirement (FR-008) and competitive healthcare scheduling UX expectations; no explicit latency target in BRD.

- NFR-003: [SOURCE:INPUT] System MUST be architected to support additional clinics, providers, and increased patient volume without requiring a major structural redesign.
  Basis: BRD §7.3 — "Scalability: The platform must support growth in increasing patient volume, additional clinics and providers, and future third-party integrations without requiring major architectural redesign."

- NFR-004: [SOURCE:INPUT] System MUST encrypt all PHI at rest using AES-256 (or equivalent) and all PHI in transit using TLS 1.2 or higher.
  Basis: BRD §7.1 — "Encrypted data transmission and storage"; §7.1 — "100% HIPAA-compliant data handling".

- NFR-005: [SOURCE:INPUT] System MUST enforce role-based access control (RBAC) restricting Patient, Staff, and Admin actors to their respective permitted operations on every API endpoint and UI route.
  Basis: BRD §7.1 — "Strict role-based access control (RBAC)"; FR-004.

- NFR-006: [SOURCE:INPUT] System MUST automatically expire any inactive authenticated session after exactly 15 minutes and require re-authentication.
  Basis: BRD §7.2 — "15-minute automatic session timeout"; FR-003.

- NFR-007: [SOURCE:INPUT] System MUST maintain an immutable, tamper-proof audit log that records every patient and staff action with actor identity, action type, target entity, and timestamp; no UPDATE or DELETE operations MUST be permitted on audit log records.
  Basis: BRD §7.1 — "Immutable audit logging for all patient and staff actions"; FR-041.

- NFR-008: [SOURCE:INFERRED] All background jobs (reminder dispatch, slot swap, AI extraction, PDF generation, calendar sync) MUST be idempotent and implement retry logic with exponential back-off up to 3 attempts before entering a failed state.
  Basis: Derived from multiple async FRs (FR-009, FR-011–FR-013, FR-018–FR-019, FR-033, FR-037–FR-038, FR-025) and Hangfire selection in BRD §5.

- NFR-009: [SOURCE:INFERRED] System MUST prevent concurrent double-booking of the same appointment slot using optimistic concurrency control; a slot reservation MUST be atomic and conflict-free.
  Basis: Derived from FR-005, FR-008, FR-011 — multiple users can attempt to book the same slot simultaneously.

- NFR-010: [SOURCE:INPUT] System MUST use exclusively free and open-source platforms, libraries, and tools throughout Phase 1; no paid cloud services (e.g., AWS, Azure) are permitted.
  Basis: BRD §5 — "Free and open-source-friendly platforms only"; §7.4.

- NFR-011: [SOURCE:INPUT] System MUST comply with HIPAA for all data handling, transmission, and storage, including Business Associate Agreements (BAAs) with applicable service providers.
  Basis: BRD §7.1 — "100% HIPAA-compliant data handling, transmission, and storage".

- NFR-012: [SOURCE:INFERRED] System MUST apply Clean Architecture layering (Domain → Application → Infrastructure → API) to ensure maintainability, testability, and dependency isolation.
  Basis: Implied by multi-layer complexity (AI, background jobs, external integrations, RBAC, HIPAA) and the team's .NET stack choice; no explicit mention in BRD.

---

## Data Requirements

- DR-001: [SOURCE:INPUT] System MUST encrypt all PHI fields at the application layer before persisting them to the database; encryption keys MUST be stored in GitHub Secrets / environment variables, never in source code.
  Basis: BRD §7.1 — "Encrypted data transmission and storage"; NFR-004; NFR-011.

- DR-002: [SOURCE:INFERRED] System MUST enforce referential integrity (foreign key constraints) across all relational entities to prevent orphaned records and data inconsistency.
  Basis: HIPAA minimum-necessary standard; clinical data correctness requirements derived from FR-033–FR-036.

- DR-003: [SOURCE:INPUT] System MUST implement the audit log as an append-only PostgreSQL table in a dedicated schema with no UPDATE or DELETE privileges granted to the application database role.
  Basis: BRD §7.1 — "Immutable audit logging"; NFR-007; FR-041.

- DR-004: [SOURCE:INPUT] System MUST provision the pgvector extension on Supabase to store and query embedding vectors for AI-related features.
  Basis: BRD §5 — "PostgreSQL (via Supabase). Includes pgvector for AI/embeddings."

- DR-005: [SOURCE:INFERRED] Clinical PDF documents MUST be stored in encrypted object/blob storage; only the storage path (not file content) MUST be persisted in the relational database.
  Basis: HIPAA minimum-necessary; Supabase Storage is available within the free tier and provides server-side encryption.

- DR-006: [SOURCE:INFERRED] PHI MUST be retained for no longer than the HIPAA minimum-necessary period (6 years from creation or last effective date); the system MUST support patient-requested data deletion with audit trail of the deletion event.
  Basis: HIPAA data retention standards (45 CFR §164.530); NFR-011.

- DR-007: [SOURCE:INFERRED] System MUST rely on Supabase managed daily backups with point-in-time recovery (PITR) capability to meet a Recovery Point Objective (RPO) ≤ 24 hours and Recovery Time Objective (RTO) ≤ 4 hours.
  Basis: NFR-001 (99.9% uptime); HIPAA contingency plan requirements (45 CFR §164.308(a)(7)).

### Domain Entities

- **User**: Represents all authenticated actors. Attributes: `id` (UUID), `email`, `passwordHash`, `role` (Patient/Staff/Admin), `status` (Active/Inactive), `createdAt`, `lastLoginAt`, `failedLoginCount`. Relationships: one-to-many Appointments (as Patient), one-to-many Appointments (as Staff creator).

- **Appointment**: A scheduled visit. Attributes: `id`, `patientId` (FK → User), `slotId` (FK → AppointmentSlot), `status` (Booked/Cancelled/Rescheduled/Arrived), `noShowRiskScore`, `createdByStaffId` (nullable FK → User), `insuranceValidationResult`, `createdAt`, `rowVersion` (optimistic lock). Relationships: one-to-one IntakeRecord, one-to-many Notifications, one-to-one CalendarSync.

- **AppointmentSlot**: A bookable time slot. Attributes: `id`, `startDateTime`, `endDateTime`, `isBooked`, `rowVersion` (optimistic lock). Relationships: one-to-many WaitlistEntry.

- **WaitlistEntry**: A patient's preferred slot registration. Attributes: `id`, `patientId`, `appointmentId`, `preferredSlotId`, `registeredAt`. Relationships: FK to Appointment and AppointmentSlot.

- **IntakeRecord**: Patient's pre-appointment intake. Attributes: `id`, `appointmentId`, `method` (AI/Manual), `fields` (encrypted JSON), `completedAt`. Relationships: FK to Appointment.

- **ClinicalDocument**: An uploaded PDF document. Attributes: `id`, `patientId`, `documentType` (Historical/PostVisit), `storagePath` (encrypted), `uploadedAt`, `extractionStatus` (Pending/Processing/Completed/Failed), `mimeType`. Relationships: one-to-many ExtractedClinicalData.

- **ExtractedClinicalData**: Structured data extracted from a ClinicalDocument. Attributes: `id`, `documentId`, `patientId`, `vitals` (encrypted JSON), `medications` (encrypted JSON), `diagnoses` (encrypted JSON), `rawGeminiResponse` (encrypted), `modelVersion`, `promptHash`, `extractedAt`. Relationships: FK to ClinicalDocument.

- **PatientProfile360**: Aggregated, de-duplicated patient view. Attributes: `id`, `patientId`, `unifiedVitals` (encrypted JSON), `unifiedMedications` (encrypted JSON), `unifiedHistory` (encrypted JSON), `conflictCount`, `lastUpdatedAt`. Relationships: one-to-one with User (Patient).

- **DataConflict**: A detected conflict between extracted data points. Attributes: `id`, `patientId`, `conflictType`, `conflictedFields` (JSON — includes values and source document IDs), `severity`, `status` (Open/Resolved/ReviewedUnresolved), `resolvedByStaffId` (nullable), `resolvedAt` (nullable). Relationships: FK to User (Patient), FK to User (Staff resolver).

- **MedicalCodeSuggestion**: An AI-generated ICD-10 or CPT code suggestion. Attributes: `id`, `encounterId` (FK → Appointment), `codeType` (ICD10/CPT), `suggestedCode`, `confidenceScore`, `status` (Pending/Accepted/Modified/Rejected), `finalCode` (nullable), `verifiedByStaffId` (nullable), `verifiedAt` (nullable), `modelVersion`, `promptHash`. Relationships: FK to Appointment.

- **AuditLog**: Immutable event record. Attributes: `id`, `actorId`, `actorRole`, `actionType`, `targetEntity`, `targetId`, `timestamp`, `metadata` (encrypted JSON). No FK enforcement (actor may be deleted); stored in dedicated `audit` schema.

- **Notification**: An outbound notification event. Attributes: `id`, `recipientId`, `channel` (Email/SMS), `notificationType`, `payload` (encrypted JSON), `status` (Queued/Sent/Failed), `sentAt`, `retryCount`, `lastAttemptAt`.

- **InsuranceRecord**: Predefined dummy insurance dataset. Attributes: `id`, `providerName`, `insuranceIdPattern` (regex). Read-only reference data.

- **CalendarSync**: Calendar integration state per patient. Attributes: `id`, `patientId`, `provider` (Google/Outlook), `accessToken` (encrypted), `refreshToken` (encrypted), `tokenExpiresAt`, `lastSyncAt`, `syncStatus`, `errorMessage`.

---

## AI Consideration

**Status:** Applicable

The upstream spec (`spec.md`) contains `[AI-CANDIDATE]` tags on FR-015, FR-032, FR-033, FR-034, FR-036 and `[HYBRID]` tags on FR-035, FR-037, FR-038. $AI_SIGNAL = true. AI Requirements section below is active.

---

## AI Requirements

- AIR-001: [SOURCE:INPUT] System MUST use Gemini to drive the AI conversational intake pathway, extracting structured intake fields from patient natural-language responses with schema validation before persistence.
  Basis: FR-015 — "[AI-CANDIDATE] System MUST provide an AI conversational intake pathway"; BRD §4.1.

- AIR-002: [SOURCE:INPUT] System MUST invoke Gemini to extract structured clinical data (vitals, medications, diagnoses) from ingested PDF text, and MUST validate the extracted schema before storing results.
  Basis: FR-032, FR-033 — "[AI-CANDIDATE] System MUST extract patient vitals, medical history, and medication data"; BRD §3.

- AIR-003: [SOURCE:INPUT] System MUST invoke Gemini to generate ranked ICD-10 and CPT code candidates with confidence scores for each patient encounter.
  Basis: FR-037, FR-038 — "[HYBRID] System MUST generate ICD-10/CPT code suggestions"; BRD §4.5.

- AIR-004: [SOURCE:INFERRED] System MUST implement a semantic de-duplication and conflict detection pipeline that compares extracted clinical records across documents and flags contradictory data points (e.g., conflicting medication entries) with severity classification.
  Basis: FR-034, FR-035 — "[AI-CANDIDATE] de-duplicate; [HYBRID] detect and highlight critical data conflicts"; BRD §4.4.

- AIR-005: [SOURCE:INPUT] System MUST enforce a mandatory human verification gate before any AI-generated clinical output (intake data, extracted records, code suggestions) is persisted as a confirmed clinical decision; no AI output is committed to patient records without explicit Staff or Patient approval.
  Basis: BRD §4.5 — "human verification support, ensuring clinical accuracy and audit traceability"; FR-039.

- AIR-006: [SOURCE:INFERRED] System MUST log the following metadata for every Gemini API invocation: model version, prompt hash (SHA-256), input token count, output token count, response latency (ms), HTTP status code, and timestamp. Logs MUST be stored in the AuditLog entity under action type `AI_INVOCATION`.
  Basis: HIPAA audit requirements; BRD §8.3 — ">98% AI-Human Agreement Rate" (requires traceability for measurement); NFR-007.

- AIR-007: [SOURCE:INFERRED] System MUST degrade gracefully to manual workflows when the Gemini API is unavailable or returns an error; Staff MUST be notified of AI service degradation; no appointment or clinical workflow MUST be blocked by AI unavailability.
  Basis: NFR-001 (99.9% uptime); NFR-008 (retry safety); BRD §3 — "manual fallback" implied by dual AI/manual intake pathway.

### AI Architecture Pattern

**Selected Pattern:** Tool Calling / Structured Output (Gemini function calling with JSON schema enforcement)

**Rationale:** The three AI use cases (conversational intake AIR-001, clinical extraction AIR-002, medical coding AIR-003) all require structured, schema-validated JSON outputs mapped to domain entities — not free-form generation. Gemini's function calling / structured output mode enforces response schemas at the model level, reducing hallucination risk and eliminating fragile regex parsing. This directly supports AIR-005 (human verification) and AIR-006 (observability) by providing deterministic, auditable model invocations. RAG is deferred to Phase 2 (when pgvector embeddings mature); fine-tuning is out of scope for Phase 1.

---

## Architecture and Design Decisions

- **Clean Architecture (Layered)**: The .NET backend is structured as Domain → Application → Infrastructure → API layers. Domain contains entities and interfaces; Application contains use cases and orchestration; Infrastructure contains database adapters, Gemini SDK integration, Hangfire job implementations, external API clients; API contains controllers and middleware. This ensures the domain model is never polluted by framework dependencies and facilitates unit testing without external service stubs.

- **Optimistic Concurrency on Slot Reservation**: The `AppointmentSlot` entity carries a `rowVersion` column. Booking and swap operations issue a conditional UPDATE that checks the current `rowVersion`; a conflict (HTTP 409) triggers a client-side retry with a refreshed slot view. This prevents double-booking without requiring pessimistic table locks, which would degrade under concurrent bookings.

- **Append-Only Audit Schema**: A dedicated PostgreSQL schema (`audit`) hosts the `AuditLog` table. The application database role is granted INSERT only on this schema; no UPDATE or DELETE privileges exist. A separate read-only role is granted SELECT for audit review queries. This enforces immutability at the database permission level rather than relying solely on application logic.

- **PHI Encryption at Application Layer**: PHI fields (vitals, medications, diagnoses, intake fields, tokens) are encrypted using AES-256-GCM before being written to PostgreSQL. This ensures PHI is protected even if database-level access controls are bypassed (e.g., a Supabase dashboard leak). Encryption keys are stored in GitHub Secrets and injected at runtime via environment variables — never committed to source control.

- **Gemini Structured Output Mode**: All Gemini invocations use function calling / structured output with explicit JSON schemas aligned to the domain entity definitions. This eliminates the need for a separate extraction parsing layer and produces schema-validated responses directly consumable by the Application layer. Prompt templates are versioned and their SHA-256 hashes logged per AIR-006.

- **Hangfire with PostgreSQL Persistence**: Background jobs (reminders, slot swap, AI extraction, PDF, calendar sync) are managed by Hangfire using the existing PostgreSQL instance (Supabase) as the job store. This avoids adding a separate message broker for Phase 1, stays within the free-tier constraint, and provides job retry, dashboarding, and failure tracking out of the box.

- **JWT in HttpOnly Secure Cookies**: Access tokens are issued as short-lived JWTs (15-minute expiry aligned with NFR-006) stored in HttpOnly, Secure, SameSite=Strict cookies. This eliminates XSS-based token theft vectors. Refresh tokens are stored server-side in Redis with a sliding expiry; the client never receives the refresh token directly.

- **Calendar OAuth Token Storage**: Google and Outlook OAuth access and refresh tokens are encrypted at the application layer (AES-256-GCM) before storage in the `CalendarSync` entity. Token refresh flows are handled transparently by the Infrastructure layer; expired token errors trigger re-authorisation prompts rather than silent failures.

- **No Mobile App (Phase 1)**: The platform is web-only in Phase 1. The React SPA is responsive for mobile browser use. Native mobile development is explicitly out of scope.

---

## Technology Stack

| Layer | Technology | Version | Justification |
|-------|------------|---------|---------------|
| Frontend | React (SPA) | 18.x | NFR-010 (free/open-source); TR-001; BRD §5 — "UI (Frontend): React". |
| Mobile | N/A | — | BRD §6 Out-of-Scope — no mobile app in Phase 1. |
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | NFR-010, NFR-012; TR-002; BRD §5 — "API (Backend): .NET". |
| Database | PostgreSQL via Supabase (+ pgvector) | PostgreSQL 15; pgvector 0.7+ | DR-001–DR-007; TR-003; BRD §5 — "PostgreSQL (via Supabase). Includes pgvector." |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008; TR-004; BRD §5 — "Backend Webjobs: Hangfire". |
| Caching | Upstash Redis | Serverless | NFR-002, NFR-009; TR-005; BRD §5 — "Caching: Upstash Redis". |
| AI/ML | Gemini API (Google AI .NET SDK); pgvector (embedding store) | gemini-1.5-pro; Google.Ai.Generativelanguage 1.x | AIR-001–AIR-003; TR-006; BRD §5 — "AI Engine: Gemini". |
| Testing | xUnit (.NET), React Testing Library, Playwright | Latest stable | NFR-012; TR-002 (unit/integration); NFR-003 (E2E coverage). |
| Infrastructure | InfinityFree (React hosting), MonsterASP (IIS/.NET hosting) | — | NFR-010; TR-018; BRD §5. |
| Security | ASP.NET Core Data Protection (AES-256-GCM), JWT Bearer, HTTPS | Built-in .NET 8 | NFR-004, NFR-005, NFR-006; TR-007, TR-008. |
| Deployment | GitHub Actions (CI/CD); IIS on MonsterASP | — | NFR-010, NFR-012; TR-012; BRD §5 — "CI/CD: GitHub Actions". |
| Monitoring | GitHub Actions logs; Hangfire dashboard; Supabase metrics | — | NFR-001; NFR-008; free-tier constraint NFR-010. |
| Documentation | OpenAPI / Swagger (Swashbuckle) | 6.x | NFR-012; auto-generated from controllers. |
| Secrets | GitHub Secrets (runtime injection via Actions env vars) | — | NFR-004, NFR-011; BRD §5 — "Secrets/Key Vault: GitHub Secrets". |
| PDF Generation | QuestPDF | 2024.x | NFR-010; TR-011; open-source MIT-licensed .NET PDF library. |
| PDF Parsing | PdfPig | 0.1.x | NFR-010; TR-017; open-source .NET PDF text extractor. |
| Calendar Integration | Google Calendar API v3; Microsoft Graph API v1.0 | — | TR-009; FR-020, FR-021. |
| Notifications | SMTP (free tier, e.g., Brevo/Mailgun free); free SMS gateway | — | NFR-008; TR-010; FR-018. |

### AI Component Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Model Provider | Gemini API (gemini-1.5-pro) via Google AI .NET SDK | LLM inference for conversational intake, clinical extraction, code suggestion |
| Vector Store | Supabase PostgreSQL + pgvector extension | Embedding storage and similarity search for future RAG expansion (Phase 2) |
| AI Gateway | ASP.NET Core middleware (custom) + Hangfire | Request queuing, retry, and invocation logging for all Gemini calls |
| Guardrails | JSON Schema validation (System.Text.Json) + Gemini structured output mode | Schema-enforce AI responses; reject malformed outputs before persistence |

### Alternative Technology Options

- **Node.js / Express instead of .NET**: Considered but rejected — BRD §5 explicitly specifies .NET as the backend; MonsterASP is optimised for IIS/.NET hosting. A Node.js backend would require a different hosting platform, violating NFR-010.
- **MySQL / MariaDB instead of PostgreSQL**: Rejected — pgvector (required by DR-004) is a PostgreSQL-only extension; Supabase (BRD §5) is PostgreSQL-based.
- **Celery / RabbitMQ instead of Hangfire**: Rejected — requires a separate message broker service; Hangfire uses the existing PostgreSQL instance (free tier), aligning with NFR-010.
- **OpenAI GPT-4 instead of Gemini**: Rejected — BRD §5 explicitly specifies Gemini; OpenAI has no permanently free tier.
- **Bull / BullMQ (Redis-based job queue) instead of Hangfire**: Rejected — .NET ecosystem; Hangfire is idiomatic, has built-in PostgreSQL persistence, and avoids an additional Redis job store.
- **AWS Cognito / Auth0 instead of JWT + ASP.NET Core**: Rejected — paid tiers required for production volume; NFR-010 mandates free/open-source only.

### Technology Decision

| Metric (from NFR/DR) | PostgreSQL + Supabase | MySQL + PlanetScale |
|----------------------|----------------------|----------------------|
| pgvector support (DR-004) | Native extension | Not available |
| Free-tier managed service (NFR-010) | ✓ Supabase free tier | PlanetScale free tier discontinued 2024 |
| PITR backup (DR-007) | ✓ Supabase PITR | Limited on free tier |
| Row-level security for HIPAA (NFR-011) | ✓ PostgreSQL RLS | Available but less mature |
| .NET ORM compatibility (TR-002) | ✓ Npgsql / EF Core | ✓ Pomelo EF Core |
| **Winner** | **PostgreSQL + Supabase** | — |

| Metric (from AIR) | Gemini API | OpenAI GPT-4 |
|-------------------|-----------|--------------|
| Structured output / function calling (AIR-001–003) | ✓ Gemini function calling | ✓ OpenAI function calling |
| Free API tier (NFR-010) | ✓ Gemini free tier available | ✗ No permanently free production tier |
| .NET SDK availability (TR-006) | ✓ Google AI .NET SDK | ✓ Azure OpenAI SDK |
| Context window for PDF extraction (AIR-002) | ✓ 1M token context (gemini-1.5-pro) | 128K tokens (GPT-4o) |
| **Winner** | **Gemini API** | — |

| Metric (from AIR) | pgvector (Supabase) | Pinecone |
|-------------------|--------------------|---------|
| Free-tier availability (NFR-010) | ✓ Included in Supabase free tier | Limited free tier |
| Requires separate service (NFR-010) | ✗ Co-located with existing DB | ✓ Separate managed service |
| Phase 1 feature requirement | Provisioned for Phase 2 RAG | Not needed Phase 1 |
| **Winner** | **pgvector** | — |

---

## Technical Requirements

- TR-001: [SOURCE:INPUT] System MUST implement the frontend as a React 18 SPA deployed to InfinityFree, consuming the .NET backend via HTTPS REST API calls.
  Basis: NFR-010, NFR-001; BRD §5.

- TR-002: [SOURCE:INPUT] System MUST implement the backend as an ASP.NET Core 8 Web API deployed to MonsterASP via IIS, following Clean Architecture layering (Domain / Application / Infrastructure / API).
  Basis: NFR-010, NFR-012; BRD §5; Architecture Decision — Clean Architecture.

- TR-003: [SOURCE:INPUT] System MUST use PostgreSQL (Supabase) as the primary relational data store with the pgvector extension provisioned for embedding storage.
  Basis: DR-001–DR-007; BRD §5.

- TR-004: [SOURCE:INPUT] System MUST use Hangfire (PostgreSQL storage backend) to manage all background jobs (reminders, slot swap, AI extraction, PDF generation, calendar sync, notifications) with idempotency keys and max-3-retry exponential back-off.
  Basis: NFR-008; BRD §5.

- TR-005: [SOURCE:INPUT] System MUST use Upstash Redis to cache appointment slot availability data and session invalidation state, with a cache TTL ≤ 5 seconds for slot data to ensure near-real-time accuracy.
  Basis: NFR-002, NFR-009; BRD §5.

- TR-006: [SOURCE:INPUT] System MUST invoke the Gemini API using the Google AI .NET SDK with structured output (function calling) mode and JSON schema validation for all AI feature invocations (intake, extraction, coding).
  Basis: AIR-001–AIR-003; AIR-005; AIR-006; BRD §5.

- TR-007: [SOURCE:INFERRED] System MUST issue JWT access tokens (15-minute expiry) stored in HttpOnly, Secure, SameSite=Strict cookies; refresh tokens MUST be stored server-side in Redis and never transmitted to the client.
  Basis: NFR-004, NFR-006; OWASP ASVS session management requirements; Architecture Decision — JWT in HttpOnly Cookies.

- TR-008: [SOURCE:INPUT] System MUST implement RBAC using ASP.NET Core policy-based authorization with three policies — `PatientPolicy`, `StaffPolicy`, `AdminPolicy` — enforced at controller action level.
  Basis: NFR-005; FR-004, FR-028; BRD §7.1.

- TR-009: [SOURCE:INPUT] System MUST integrate with Google Calendar API v3 and Microsoft Graph API v1.0 via OAuth 2.0 authorization code flow for calendar event creation, update, and deletion.
  Basis: FR-020, FR-021; BRD §6.

- TR-010: [SOURCE:INFERRED] System MUST send Email notifications via SMTP (Brevo or equivalent free-tier provider) and SMS notifications via a free-tier SMS gateway; all notification dispatch MUST be handled asynchronously via Hangfire.
  Basis: NFR-008; FR-018, FR-019; BRD §6 — "Automated multi-channel reminders (SMS/Email)".

- TR-011: [SOURCE:INFERRED] System MUST generate PDF appointment confirmations using QuestPDF (MIT-licensed open-source .NET library) invoked as a Hangfire background job.
  Basis: NFR-010; FR-009, FR-025.

- TR-012: [SOURCE:INPUT] System MUST use GitHub Actions for all CI/CD pipelines; all secrets (database connection strings, Gemini API key, SMTP credentials, OAuth client secrets) MUST be stored in GitHub Secrets and injected at runtime via environment variables.
  Basis: NFR-010, NFR-012; NFR-004 (secrets management); BRD §5.

- TR-013: [SOURCE:INFERRED] System MUST use PostgreSQL row-version (`xmin` or explicit `rowVersion` column) for optimistic concurrency on `AppointmentSlot` updates; a concurrent modification MUST return HTTP 409 Conflict to the client.
  Basis: NFR-009; FR-005, FR-011; Architecture Decision — Optimistic Concurrency.

- TR-014: [SOURCE:INFERRED] System MUST create a dedicated `audit` PostgreSQL schema and grant the application database role INSERT-only access on the `AuditLog` table; no UPDATE or DELETE privileges MUST be assigned.
  Basis: NFR-007; DR-003; FR-041; Architecture Decision — Append-Only Audit Schema.

- TR-015: [SOURCE:INFERRED] System MUST apply AES-256-GCM encryption (via ASP.NET Core Data Protection or equivalent) to all PHI fields before persistence; encryption keys MUST be stored in GitHub Secrets and MUST NOT appear in source code, configuration files, or logs.
  Basis: NFR-004; DR-001; NFR-011; HIPAA §164.312(a)(2)(iv).

- TR-016: [SOURCE:INFERRED] System MUST version all REST API endpoints under the path prefix `/api/v1/`; breaking changes MUST increment the version prefix to `/api/v2/` rather than modifying v1 endpoints.
  Basis: NFR-003 (scalability for future integrations); Architecture Decision — RESTful API Design.

- TR-017: [SOURCE:INFERRED] System MUST extract text content from uploaded PDFs using PdfPig (open-source .NET library) before passing the text to the Gemini API for clinical data extraction; direct binary PDF upload to Gemini MUST be avoided.
  Basis: AIR-002; NFR-010; avoids Gemini's multimodal PDF quota limits.

- TR-018: [SOURCE:INPUT] System MUST be deployable to MonsterASP via IIS without containerisation; Docker is permitted as an optional local development environment but MUST NOT be required for production deployment.
  Basis: NFR-010; BRD §7.4 — "Native deployment capabilities (Windows Services / IIS)".

---

## Technical Constraints & Assumptions

- **Phase 1 is free-tier only**: All infrastructure components (InfinityFree, MonsterASP, Supabase free tier, Upstash Redis free tier, GitHub Actions free minutes) MUST remain within free-tier limits. Architectural decisions that require paid tiers are deferred to Phase 2.
- **MonsterASP is a Windows/.NET IIS host**: The backend MUST compile and run on the Windows IIS execution environment provided by MonsterASP; Linux-specific APIs or Docker-dependent deployments are not supported in Phase 1.
- **Supabase free tier storage limits apply**: PDF document storage uses Supabase Storage (1 GB free tier). Phase 1 assumes a limited initial patient volume; storage overflow handling is deferred to Phase 2.
- **Gemini API rate limits**: The Gemini API free tier has per-minute and per-day token limits. Hangfire job scheduling MUST respect rate limits via request throttling; AI features degrade gracefully (AIR-007) when limits are hit.
- **No EHR integration**: Direct bi-directional EHR integration is explicitly out of scope for Phase 1 (BRD §6 Out-of-Scope).
- **Insurance pre-check uses dummy data**: The InsuranceRecord table is seeded with predefined test data; no live insurer API is called in Phase 1.
- **Single clinic deployment assumed for Phase 1**: Multi-tenancy (multiple clinics) is architecturally planned (tenant ID placeholder on entities) but not operationally activated.
- **Calendar sync assumes OAuth consent is granted**: The calendar sync feature degrades gracefully if the patient has not granted OAuth consent; no fallback calendar format (e.g., iCal export) is required in Phase 1.
- **SMS provider TBD**: A free-tier SMS gateway (e.g., Vonage/Nexmo sandbox, Twilio trial) MUST be selected during implementation; the architecture is provider-agnostic via an `ISmsSender` interface.
- **HIPAA BAA availability on free tiers**: Supabase and Upstash MUST be verified for BAA availability before go-live; if BAAs are unavailable on free tiers, PHI MUST NOT be stored in those services unencrypted (application-layer encryption per TR-015 mitigates this for Supabase; Redis MUST store only non-PHI session references).
- **pgvector used for Phase 2 RAG**: The pgvector extension is provisioned in Phase 1 but actively used only for embedding storage scaffolding; production RAG queries are deferred to Phase 2.
- **React SPA CORS policy**: The .NET API MUST configure CORS to allow requests from the InfinityFree origin only; wildcard origins are prohibited (OWASP A05 Security Misconfiguration).
