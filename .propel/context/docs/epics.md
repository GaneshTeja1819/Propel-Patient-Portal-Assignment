---
title: "Epic Decomposition – Unified Patient Access & Clinical Intelligence Platform"
version: "1.0"
date: "2026-05-14"
source: "BRD Consolidated v1.0, spec.md v1.0, design.md v1.0, figma_spec.md v1.0"
status: "Draft"
---

# Epic Decomposition — Unified Patient Access & Clinical Intelligence Platform

## Project Type Signal

**Project Type: GREEN-FIELD** — No existing codebase. No codeanalysis.md available (expected; artifact skipped). Two mandatory foundational epics created: `EP-TECH` (first, no dependencies) and `EP-DATA` (second, depends EP-TECH only). All feature epics depend exclusively on EP-TECH and EP-DATA with no feature-to-feature dependencies.

**Data Layer Signal: DETECTED** — PostgreSQL/pgvector domain entities identified in design.md (15+ entities). DR-001–DR-007 present. EP-DATA epic is required.

**AI Signal: ACTIVE** — `[AI-CANDIDATE]` and `[HYBRID]` tags found on FR-015, FR-032–FR-038. AIR-001–AIR-007 present. AI-specific requirements are mapped to their owning feature epic.

---

## Reference Context Status

| Reference | Path | Status |
|-----------|------|--------|
| BRD Input | `.vscode/BRD Consolidated.md` | Loaded ✓ |
| spec.md | `.propel/context/docs/spec.md` | Loaded ✓ |
| design.md | `.propel/context/docs/design.md` | Loaded ✓ |
| figma_spec.md | `.propel/context/docs/figma_spec.md` | Loaded ✓ |
| codeanalysis.md | `.propel/context/docs/codeanalysis.md` | Not found — GREEN-FIELD; skipped |

---

## Epic Summary Table

> **Traceability Note**: 136 requirement IDs mapped across 12 epics (41 FRs + 25 UCs + 12 NFRs + 18 TRs + 7 DRs + 7 AIRs + 26 UXRs). Every ID appears in exactly one epic. EP-TECH and EP-DATA exceed the ~12-item soft limit; both are retained as single cohesive epics due to interdependent infrastructure concerns documented in each epic description.

| Epic ID | Epic Title | Mapped Requirement IDs |
|---------|------------|------------------------|
| EP-TECH | Platform Foundation, DevOps & Design System | TR-001, TR-002, TR-004, TR-005, TR-012, TR-016, TR-018, NFR-001, NFR-002, NFR-003, NFR-010, NFR-012, UXR-201, UXR-202, UXR-301, UXR-401 |
| EP-DATA | Data Layer, Security & HIPAA Compliance | TR-003, TR-006, TR-007, TR-008, TR-013, TR-014, TR-015, TR-017, DR-001, DR-002, DR-003, DR-004, DR-005, DR-006, DR-007, NFR-004, NFR-005, NFR-006, NFR-007, NFR-008, NFR-009, NFR-011, FR-040, FR-041 |
| EP-001 | Patient Authentication & Access Control | FR-001, FR-002, FR-003, FR-004, UC-001, UC-002, UC-003, UXR-203, UXR-204, UXR-205, UXR-503 |
| EP-002 | Appointment Booking, Cancellation & Rescheduling | FR-005, FR-006, FR-007, FR-008, FR-009, FR-014, FR-029, UC-004, UC-005, UC-006, UC-018, UC-025, TR-011, UXR-101, UXR-102, UXR-302, UXR-501, UXR-504, UXR-601, UXR-602 |
| EP-003 | Preferred Slot Swap & Automated Waitlist | FR-010, FR-011, FR-012, FR-013, FR-019, UC-007, UC-008 |
| EP-004 | Patient Intake — AI Conversational & Manual | FR-015, FR-016, FR-017, UC-009, UC-010, UC-011, AIR-001, UXR-103, UXR-502 |
| EP-005 | Notifications, Reminders & Calendar Sync | FR-018, FR-020, FR-021, UC-012, UC-013, TR-009, TR-010, UXR-604 |
| EP-006 | Staff Walk-in Booking, Queue & Arrival Management | FR-022, FR-023, FR-024, FR-025, FR-026, UC-014, UC-015, UC-016, UXR-104 |
| EP-007 | Admin User & Role Management | FR-027, FR-028, UC-017, UXR-105 |
| EP-008 | Clinical Document Upload & AI Data Extraction | FR-030, FR-031, FR-032, FR-033, UC-019, UC-021, AIR-002, AIR-006, AIR-007, UXR-603 |
| EP-009 | 360° Patient Profile, De-duplication & Conflict Resolution | FR-034, FR-035, FR-036, UC-020, UC-022, AIR-004, UXR-106, UXR-402, UXR-403, UXR-404 |
| EP-010 | Medical Code Suggestion & Human Verification | FR-037, FR-038, FR-039, UC-023, UC-024, AIR-003, AIR-005, UXR-107 |

---

## Epic Descriptions

---

### EP-TECH: Platform Foundation, DevOps & Design System

**Business Value**: Establishes the entire project scaffold — hosting, CI/CD, API versioning, background job infrastructure, caching, and the system-wide design token and accessibility baseline. No feature work can proceed until EP-TECH is complete.

**Description**: Creates the full project skeleton for both the React 18 SPA (InfinityFree) and the .NET 8 Clean Architecture Web API (MonsterASP/IIS). Configures GitHub Actions pipelines for build, test, and deploy. Sets up Hangfire and Upstash Redis as infrastructure services (no job logic yet). Establishes REST API versioning under `/api/v1/`. Publishes the CSS design token system (24 semantic colour tokens, system-ui typography, spacing, radius, elevation). Enforces WCAG 2.2 AA accessibility baseline and responsive breakpoints (375 / 768 / 1280 px) as project-wide standards. All free/open-source; no paid services used.

**UI Impact**: No

**Screen References**: N/A

**Key Deliverables**:
- React 18 SPA project scaffolded, linted, tested, and deployed to InfinityFree
- .NET 8 solution with Domain / Application / Infrastructure / API layer separation deployed to MonsterASP/IIS
- GitHub Actions CI/CD pipelines for frontend and backend (build → test → deploy)
- Hangfire service registered with PostgreSQL job store (no jobs implemented yet)
- Upstash Redis client configured for slot caching and session invalidation
- REST API route prefix `/api/v1/` enforced; versioning strategy documented
- CSS design token system published (tokens/variables.css or equivalent)
- WCAG 2.2 AA contrast, keyboard navigation, and responsive breakpoint standards documented and enforced in the component baseline
- Free-tier resource constraints documented and enforced in CI gate

**Dependent EPICs**:
- None

---

### EP-DATA: Data Layer, Security & HIPAA Compliance

**Business Value**: Creates the secure, HIPAA-compliant data foundation required before any patient or clinical data can be stored. Implements all PHI encryption, immutable audit trail, RBAC policies, session management, and database schema migrations. Without EP-DATA no user-facing feature can safely persist data.

**Description**: Provisions the PostgreSQL database (Supabase) with the complete domain entity schema (User, Appointment, AppointmentSlot, WaitlistEntry, IntakeRecord, ClinicalDocument, ExtractedClinicalData, PatientProfile360, DataConflict, MedicalCodeSuggestion, AuditLog, Notification, InsuranceRecord, CalendarSync). Enables the pgvector extension for future RAG use. Creates the dedicated `audit` schema with INSERT-only application privileges. Integrates AES-256-GCM PHI encryption via ASP.NET Core Data Protection. Configures JWT access tokens (15-minute expiry) in HttpOnly/Secure/SameSite=Strict cookies with server-side Redis refresh token storage. Implements three ASP.NET Core RBAC policies (PatientPolicy, StaffPolicy, AdminPolicy). Adds optimistic concurrency (rowVersion) to AppointmentSlot. Integrates PdfPig for PDF text extraction (consumed by EP-008). Configures the Gemini API SDK with structured output mode and per-invocation metadata logging. Implements data retention and deletion scaffolding for HIPAA compliance. Configures Supabase PITR backup. Maps FR-040 and FR-041 (PHI encryption and immutable audit log) as the primary security/compliance functional requirements owned by this epic.

> **Note**: This epic maps 24 requirement IDs, exceeding the ~12 soft limit. All items are HIPAA-compliance infrastructure with hard interdependencies (e.g., audit schema requires database schema; encryption utilities require key configuration; RBAC requires JWT). Splitting would create artificial decomposed dependencies with no independent deployable value. Retained as one cohesive epic.

**UI Impact**: No

**Screen References**: N/A

**Key Deliverables**:
- All 15+ domain entity EF Core migrations applied and seeded (including InsuranceRecord dummy data)
- pgvector extension provisioned in Supabase
- Dedicated `audit` PostgreSQL schema; INSERT-only application role applied
- AES-256-GCM encryption utilities applied to all PHI fields (vitals, medications, diagnoses, tokens, storage paths)
- JWT + HttpOnly cookie session configuration with 15-minute expiry
- Server-side Redis refresh token store with sliding expiry
- ASP.NET Core RBAC policies: PatientPolicy, StaffPolicy, AdminPolicy
- Optimistic concurrency rowVersion on AppointmentSlot
- PdfPig library integrated into Infrastructure layer
- Gemini API SDK configured: structured output mode, JSON schema enforcement, SHA-256 prompt hashing, invocation log handler
- HIPAA data retention and patient-deletion endpoints scaffolded
- Supabase PITR backup confirmed and documented
- GitHub Secrets injection verified for all keys and credentials

**Dependent EPICs**:
- EP-TECH - Foundational - Requires project scaffold, database service, and Hangfire/Redis infrastructure before schema and security layer can be implemented

---

### EP-001: Patient Authentication & Access Control

**Business Value**: Enables all three user roles (Patient, Staff, Admin) to securely register, authenticate, and access the platform. Foundational gate for all feature epics — no user flow can begin without authentication.

**Description**: Implements patient self-registration (email + password with complexity rules, email-uniqueness check, role assignment). Implements login with credential verification, JWT issuance, role-based dashboard redirect, and login-failure lockout. Implements 15-minute session inactivity timeout with server-side invalidation and a 2-minute countdown modal (UXR-503). Applies form accessibility standards: htmlFor/id label association (UXR-203), aria-describedby on inline validation errors (UXR-204), and aria-live session timeout announcement (UXR-205). All endpoints enforce RBAC policies from EP-DATA.

**UI Impact**: Yes

**Screen References**: SCR-001 (Registration), SCR-002 (Login)

**Key Deliverables**:
- Patient self-registration page with email uniqueness guard and audit log entry on success
- Login page with secure credential check, JWT issuance, and role-specific redirect
- 15-minute inactivity timeout with server-side token invalidation and re-authentication redirect
- 2-minute countdown modal (aria-live="assertive") allowing session extension
- Generic error messages on credential failure (no field-level disclosure, no account existence enumeration)
- Account lockout after configurable failed-attempt threshold with email notification
- All form fields: visible labels, htmlFor/id, aria-describedby on validation errors

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA scaffold and .NET API project structure
- EP-DATA - Foundational - Requires User entity, JWT configuration, RBAC policies, and AES-256-GCM utilities

---

### EP-002: Appointment Booking, Cancellation & Rescheduling

**Business Value**: Delivers the primary patient-facing scheduling workflow — the highest-value capability in the platform. Directly targets the 15% no-show reduction (FR-014 no-show risk score) and reduces manual booking friction. Drives PDF confirmation delivery and insurance pre-check as part of the booking flow.

**Description**: Implements real-time slot availability display backed by Upstash Redis (≤5s TTL; UXR-102). Implements book, cancel, and reschedule appointment flows with atomic slot operations and optimistic concurrency conflict handling (UXR-601). Calculates rule-based no-show risk score at booking time (FR-014). Performs insurance soft-validation against dummy InsuranceRecord data with inline non-blocking result display (UXR-602). Triggers QuestPDF appointment confirmation generation as a Hangfire job and dispatches it via Email (UC-025). Implements responsive slot grid reflow (UXR-302: 4/2/1 columns at 1280/768/375px). Provides 200ms optimistic slot selection UI feedback (UXR-501) and PDF progress indicator (UXR-504). Enforces ≤3-interaction booking depth from dashboard (UXR-101).

> **Note**: This epic maps 20 requirement IDs, exceeding the ~12 soft limit. The booking domain is a tightly-coupled end-to-end flow: slot selection → insurance check → risk score → booking creation → PDF confirmation → email delivery. Splitting into Part I / Part II would force a decomposed dependency and delay PDF confirmation independently. Retained as one cohesive epic.

**UI Impact**: Yes

**Screen References**: SCR-003 (Appointment List / Dashboard), SCR-004 (Slot Selection & Booking)

**Key Deliverables**:
- Real-time slot grid with Redis-backed availability (≤5s TTL; no full page reload)
- Book appointment flow: slot selection → insurance check → risk score → confirmation
- Cancel appointment flow: confirmation prompt → slot release → waitlist trigger
- Reschedule appointment flow: atomic old-slot release + new-slot reservation
- No-show risk score calculation and storage at booking time
- Insurance soft-validation: dummy record lookup, inline Validated / Not Recognised badge
- QuestPDF confirmation PDF generation (Hangfire job) + email delivery
- Responsive slot grid: 4-column (1280px) / 2-column (768px) / 1-column (375px)
- Optimistic slot selection: "Selected" state within 200ms; 409 conflict error recovery with slot grid refresh
- All booking/cancel/reschedule actions written to immutable audit log

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA, .NET API, Hangfire service, and Redis client
- EP-DATA - Foundational - Requires Appointment, AppointmentSlot, InsuranceRecord entities, optimistic concurrency, and PDF generation library (QuestPDF via TR-011)

---

### EP-003: Preferred Slot Swap & Automated Waitlist

**Business Value**: Differentiating feature that automatically recovers no-show slots for waitlisted patients, further reducing the no-show rate and increasing slot utilisation without staff intervention.

**Description**: Extends the booking flow to allow a patient to simultaneously register a preferred (currently unavailable) slot. Creates a WaitlistEntry record linked to the patient, their booked appointment, and the preferred slot. Implements a Hangfire background job that detects slot releases (from cancellations/reschedules), evaluates all WaitlistEntry records for that slot ordered by registration timestamp, atomically moves the first eligible patient to the preferred slot, releases the original slot, removes the waitlist entry, and dispatches an Email confirmation notification (FR-019). Handles atomic swap failures with one retry, then logs and skips. All swap events written to audit log.

**UI Impact**: Yes

**Screen References**: SCR-004 (Preferred slot selection section within booking flow)

**Key Deliverables**:
- Preferred slot registration UI within booking flow (unavailable slots show "Register Preferred" option)
- WaitlistEntry creation and confirmation acknowledgement
- Hangfire slot-swap job: FIFO evaluation, atomic swap, original slot release, waitlist entry removal
- Email notification dispatched to patient upon successful swap (swap confirmation with new date/time)
- One-retry logic on atomic swap failure; failure logged; next eligible waitlist entry evaluated
- Swap events written to immutable audit log

**Dependent EPICs**:
- EP-TECH - Foundational - Requires Hangfire service and .NET API infrastructure
- EP-DATA - Foundational - Requires WaitlistEntry, AppointmentSlot (optimistic concurrency), Appointment entities, and Notification entity

---

### EP-004: Patient Intake — AI Conversational & Manual

**Business Value**: Reduces manual clinical preparation from 20+ minutes to a 2-minute verification action by capturing structured intake data before the appointment via AI conversation or traditional form. Directly supports the clinical intelligence pipeline downstream.

**Description**: Implements the dual-pathway patient intake system. AI pathway: Gemini-powered conversational form (structured output / function calling mode, AIR-001) that prompts patients with structured intake questions in natural language, parses responses to intake fields, presents a summary for patient review, and persists the IntakeRecord on confirmation. Manual pathway: traditional form presenting identical structured fields. Switch pathway: patient can switch between AI and manual at any point; all previously captured fields are preserved and pre-populated in the target form (FR-017; UXR-103). AI pathway renders a step progress indicator with question count and completion percentage (UXR-502).

**UI Impact**: Yes

**Screen References**: SCR-007 (AI Conversational Intake), SCR-008 (Manual Intake Form)

**Key Deliverables**:
- Gemini AI intake conversation flow (structured output, JSON schema for all intake fields)
- Manual intake form with identical fields to AI pathway
- Bidirectional switch preserving all captured fields without data loss
- Patient summary review screen before IntakeRecord persistence
- AI intake step progress indicator (question N of M, completion percentage)
- IntakeRecord persisted with intake method flag (AI / Manual) and audit log entry
- Graceful fallback: unparseable AI response rendered as manual input prompt

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA and Gemini SDK infrastructure
- EP-DATA - Foundational - Requires IntakeRecord entity, PHI encryption for intake fields (encrypted JSON), and Gemini API SDK configured with structured output mode

---

### EP-005: Notifications, Reminders & Calendar Synchronization

**Business Value**: Reduces no-show rates through automated multi-channel reminders and increases patient satisfaction through Google/Outlook calendar integration, requiring no manual staff action.

**Description**: Implements Hangfire-scheduled appointment reminder dispatch via Email (SMTP, Brevo/equivalent free tier) and SMS (free-tier gateway) at configurable pre-appointment intervals (FR-018). Implements Google Calendar API v3 and Microsoft Graph API v1.0 integrations via OAuth 2.0 authorization code flow: creates, updates, and deletes calendar events on booking, reschedule, and cancellation (FR-020, FR-021). All notification dispatch is asynchronous via Hangfire with max-3-retry exponential back-off. Calendar sync failures surface as non-blocking dismissible amber Toast notifications (UXR-604). Notification delivery status and sync status written to Notification and CalendarSync entities respectively.

**UI Impact**: Yes

**Screen References**: SCR-004 (calendar sync OAuth prompt during booking), SCR-003 (notification preferences)

**Key Deliverables**:
- Hangfire reminder jobs: configurable Email + SMS dispatch at pre-appointment intervals
- Email delivery via SMTP (Brevo or equivalent); SMS via free-tier gateway
- Google Calendar API v3 integration: OAuth 2.0 consent flow, event create/update/delete
- Microsoft Graph API v1.0 (Outlook) integration: OAuth 2.0 consent flow, event create/update/delete
- OAuth access/refresh tokens encrypted (AES-256-GCM) in CalendarSync entity
- Calendar sync failure: non-blocking amber Toast (UXR-604); appointment confirmation unaffected
- Retry logic: max 3 attempts with exponential back-off on all notification and sync operations
- Delivery status and sync status logged to Notification and CalendarSync entities

**Dependent EPICs**:
- EP-TECH - Foundational - Requires Hangfire, React SPA infrastructure
- EP-DATA - Foundational - Requires Notification entity, CalendarSync entity, encrypted token storage, and TR-009/TR-010 SDK configurations

---

### EP-006: Staff Walk-in Booking, Queue & Arrival Management

**Business Value**: Enables front-desk staff to handle same-day walk-in patients and manage the daily queue without requiring patient portal access, directly supporting the 20-minute → 2-minute clinical preparation target.

**Description**: Implements staff-only walk-in booking: staff searches for existing patient accounts or enters new patient details; selects an available slot; creates the appointment attributed to the Staff actor; optionally creates a new patient account and links it to the walk-in record (FR-023). Implements the same-day queue dashboard (single-page, all today's appointments in chronological order with status badges; UXR-104): staff can reorder entries and remove patients with reason capture. Implements "Mark Arrived" action that updates appointment status to Arrived and records a Staff-attributed audit log entry (FR-025). Blocks all patient self-check-in attempts at the API layer (FR-026: access-denied on any patient-initiated arrival action).

**UI Impact**: Yes

**Screen References**: SCR-011 (Same-Day Queue Dashboard), SCR-012 (Walk-in Booking)

**Key Deliverables**:
- Walk-in booking screen: patient search, new patient entry, slot selection, appointment creation
- Optional patient account creation and linking at walk-in time (FR-023)
- All walk-in appointments attributed to Staff actor in audit log
- Same-day queue dashboard: chronological list, status badges, no-scroll on 1280px (UXR-104)
- Queue reorder with slot-constraint warning and override
- Queue removal with mandatory reason capture, logged to audit
- "Mark Arrived" action with Staff-attributed timestamp in audit log
- Patient self-check-in endpoint blocked (HTTP 403 for any patient-role arrival request)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA and .NET API project structure
- EP-DATA - Foundational - Requires Appointment, AppointmentSlot, User entities and RBAC policies (StaffPolicy)

---

### EP-007: Admin User & Role Management

**Business Value**: Provides the operational control plane for the platform — Admins can manage all user accounts and roles, enabling onboarding, offboarding, and access correction without developer intervention.

**Description**: Implements the Admin user management dashboard with user search, account create, profile update, account deactivation, and role change (Patient / Staff / Admin). Role changes and deactivations invalidate any active sessions for the affected user. Blocks self-deactivation and Admin-to-lower-role changes require explicit confirmation. All actions attributed to the Admin actor in the immutable audit log. Cohesive ≤2-screen-transition workflow (UXR-105).

**UI Impact**: Yes

**Screen References**: SCR-015 (Admin User Management)

**Key Deliverables**:
- User search by name / email with paginated results
- Create user account (assign role, set initial password)
- Update profile fields (name, contact details)
- Deactivate account with active session invalidation (blocked for self-deactivation)
- Role assignment / change with confirmation prompt for downgrade; active session invalidated
- ≤2 screen transitions for all admin actions (UXR-105)
- All actions written to immutable audit log with Admin actor and timestamp

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA and .NET API
- EP-DATA - Foundational - Requires User entity, session invalidation via Redis, and RBAC AdminPolicy

---

### EP-008: Clinical Document Upload & AI Data Extraction

**Business Value**: Automates the extraction of structured clinical data from uploaded PDFs, eliminating manual data entry and feeding the 360° patient profile. Enables the target ≥98% AI-human agreement rate metric.

**Description**: Implements patient PDF upload (historical clinical documents and post-visit clinical notes) with file type and size validation, encrypted storage in Supabase Storage (DR-005). Uses PdfPig to extract text from the uploaded PDF (TR-017). Queues a Hangfire extraction job that invokes Gemini API with the extracted text and a structured prompt (AIR-002) to produce schema-validated JSON (vitals, medications, diagnoses). Stores results in ExtractedClinicalData with encrypted PHI fields. Triggers the de-duplication and conflict detection pipeline (feeds EP-009). Logs every Gemini invocation (model version, prompt SHA-256, token counts, latency, HTTP status; AIR-006) as an AuditLog entry. Implements graceful degradation: Gemini failure → 2 retries → failure status flagged for Staff review with retry CTA (AIR-007; UXR-603).

**UI Impact**: Yes

**Screen References**: SCR-010 (Document Upload)

**Key Deliverables**:
- PDF upload UI with file type and size validation, document type selection (historical / post-visit)
- Encrypted document storage in Supabase Storage; storage path (not content) in relational DB (DR-005)
- PdfPig text extraction pipeline (Infrastructure layer)
- Hangfire extraction job: Gemini structured output invocation, schema validation, ExtractedClinicalData persistence
- AI invocation log entry per Gemini call (model version, prompt hash, token counts, latency; AIR-006)
- Graceful degradation: max 2 retries on Gemini failure; failure status set; Staff retry CTA rendered (UXR-603)
- Document upload and extraction events written to immutable audit log

**Dependent EPICs**:
- EP-TECH - Foundational - Requires Hangfire service and React SPA infrastructure
- EP-DATA - Foundational - Requires ClinicalDocument, ExtractedClinicalData entities, PHI encryption utilities, PdfPig integration, Gemini SDK configuration, and Supabase Storage setup

---

### EP-009: 360° Patient Profile, De-duplication & Conflict Resolution

**Business Value**: Delivers the unified, clinician-readable patient view that eliminates fragmented clinical records. Surfaces data conflicts explicitly rather than silently overwriting, enabling the "Trust-First AI" principle and supporting clinical decision-making accuracy.

**Description**: Implements the de-duplication pipeline that aggregates extracted records from all ClinicalDocuments for a patient, removes duplicate entries, and detects critical data conflicts (conflicting medications, diagnoses) with severity classification — Critical (danger-red) and High (warning-amber) (FR-035, AIR-004). Builds and persists the PatientProfile360 unified view. Renders the 360° profile with collapsible sections for vitals, medications, history, and conflicts (UXR-106). Staff conflict review: Staff can select the authoritative value or mark as "Reviewed — unresolved"; resolution recorded in audit log (UC-022). Applies visual standards: PHI fields annotated with lock icon and surface-phi token (UXR-402), AI-generated data badged with ai-accent border and "AI" badge vs "Verified" badge for human-approved data (UXR-403), conflict cards styled with color-danger / color-warning taxonomy (UXR-404). Patient can view their own profile in read-only mode (UC-020).

**UI Impact**: Yes

**Screen References**: SCR-009 (Patient Profile — patient view), SCR-013 (Clinical Profile — staff view with conflict resolution)

**Key Deliverables**:
- De-duplication pipeline: cross-document aggregation, duplicate removal, conflict detection with severity (AIR-004)
- PatientProfile360 entity built and persisted after each extraction job (EP-008 → EP-009 pipeline)
- DataConflict records created with severity, conflicting values, and source document references
- 360° profile UI: collapsible vitals / medications / history / conflicts sections; conflict section above fold when count > 0 (UXR-106)
- Staff conflict review: select authoritative value or mark "Reviewed — unresolved"; all decisions in audit log
- PHI field visual annotation: lock icon, surface-phi background token (UXR-402)
- AI vs human-verified data provenance badges: ai-accent border + "AI" badge / success border + "Verified" badge (UXR-403)
- Conflict severity colour taxonomy: Critical = color-danger, High = color-warning; source document name + date visible (UXR-404)
- Patient read-only profile view (no editing of extracted data by patient)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA and .NET API infrastructure
- EP-DATA - Foundational - Requires ExtractedClinicalData, PatientProfile360, DataConflict entities and PHI encryption utilities

---

### EP-010: Medical Code Suggestion & Human Verification

**Business Value**: Automates ICD-10 and CPT code suggestion using Gemini, reducing manual coding effort and targeting ≥98% AI-human agreement rate. The mandatory human verification gate (AIR-005) ensures no AI code is committed without Staff approval, maintaining clinical accuracy and full audit traceability.

**Description**: Implements Gemini-powered ICD-10 and CPT code suggestion for each patient encounter (UC-023). Gemini is invoked with the extracted clinical data and coding context; returns ranked code candidates with confidence scores and supporting evidence. Stores suggestions in MedicalCodeSuggestion. Presents suggestions to Staff for verification (UC-024): each row shows code, description, confidence percentage, evidence snippet, and inline Accept / Modify / Reject controls (UXR-107). Modified codes are validated against the ICD-10 / CPT codeset before acceptance. Rejected-all encounters are flagged for manual coding. Writes an immutable audit entry for every individual AI suggestion and every Staff Accept / Modify / Reject decision, capturing actor, AI suggestion, final code, and timestamp (FR-039). Mandatory human gate (AIR-005): no AI suggestion is written to the encounter record until a Staff decision is recorded. Gemini invocation failure triggers one retry; Staff is notified to code manually on persistent failure. Audit telemetry enables ≥98% AI-human agreement rate measurement.

**UI Impact**: Yes

**Screen References**: SCR-014 (Code Verification)

**Key Deliverables**:
- Gemini code suggestion invocation (structured output: ICD-10 + CPT candidates with confidence scores)
- MedicalCodeSuggestion records persisted per encounter with model version and prompt hash
- Staff code verification UI: Accept / Modify / Reject inline per suggestion with confidence score and evidence snippet (UXR-107)
- Modified code validation against ICD-10 / CPT codeset; invalid codes rejected with feedback
- Rejected-all encounter flagged for manual coding; audit entry records mass rejection
- Immutable audit entry per suggestion and per Staff decision (actor, AI code, final code, timestamp; FR-039)
- Mandatory human gate: no code persisted to encounter without explicit Staff decision (AIR-005)
- Gemini failure: one retry; persistent failure surfaces Staff notification to code manually
- Audit telemetry structure enables ≥98% AI-human agreement rate measurement (BRD §8.3)

**Dependent EPICs**:
- EP-TECH - Foundational - Requires React SPA, Hangfire service, and Gemini SDK infrastructure
- EP-DATA - Foundational - Requires MedicalCodeSuggestion, Appointment (encounter FK) entities, Gemini SDK configuration, and immutable AuditLog schema

---

## Requirement Traceability Coverage

### Functional Requirements (41 total)

| Range | Epic |
|-------|------|
| FR-001–FR-004 | EP-001 |
| FR-005–FR-009, FR-014, FR-029 | EP-002 |
| FR-010–FR-013, FR-019 | EP-003 |
| FR-015–FR-017 | EP-004 |
| FR-018, FR-020–FR-021 | EP-005 |
| FR-022–FR-026 | EP-006 |
| FR-027–FR-028 | EP-007 |
| FR-030–FR-033 | EP-008 |
| FR-034–FR-036 | EP-009 |
| FR-037–FR-039 | EP-010 |
| FR-040–FR-041 | EP-DATA |

### Non-Functional Requirements (12 total)

| ID | Epic |
|----|------|
| NFR-001, NFR-002, NFR-003, NFR-010, NFR-012 | EP-TECH |
| NFR-004, NFR-005, NFR-006, NFR-007, NFR-008, NFR-009, NFR-011 | EP-DATA |

### Technical Requirements (18 total)

| IDs | Epic |
|-----|------|
| TR-001, TR-002, TR-004, TR-005, TR-012, TR-016, TR-018 | EP-TECH |
| TR-003, TR-006, TR-007, TR-008, TR-013, TR-014, TR-015, TR-017 | EP-DATA |
| TR-009, TR-010 | EP-005 |
| TR-011 | EP-002 |

### Data Requirements (7 total)

| IDs | Epic |
|-----|------|
| DR-001–DR-007 | EP-DATA |

### AI Requirements (7 total)

| ID | Epic |
|----|------|
| AIR-001 | EP-004 |
| AIR-002, AIR-006, AIR-007 | EP-008 |
| AIR-003, AIR-005 | EP-010 |
| AIR-004 | EP-009 |

### UX Requirements (26 total)

| IDs | Epic |
|-----|------|
| UXR-201, UXR-202, UXR-301, UXR-401 | EP-TECH |
| UXR-203, UXR-204, UXR-205, UXR-503 | EP-001 |
| UXR-101, UXR-102, UXR-302, UXR-501, UXR-504, UXR-601, UXR-602 | EP-002 |
| UXR-103, UXR-502 | EP-004 |
| UXR-604 | EP-005 |
| UXR-104 | EP-006 |
| UXR-105 | EP-007 |
| UXR-603 | EP-008 |
| UXR-106, UXR-402, UXR-403, UXR-404 | EP-009 |
| UXR-107 | EP-010 |

---

## Quality Gate Log

### T1 — Requirement Coverage

| Check | Result |
|-------|--------|
| Total requirement IDs in source (spec.md + design.md + figma_spec.md) | 136 |
| Total requirement IDs mapped to epics | 136 |
| Unmapped IDs | 0 |
| Duplicated IDs (same ID in two epics) | 0 |
| **T1 PASS** | All 136 requirement IDs assigned to exactly one epic |

### T2 — Dependency Integrity

| Check | Result |
|-------|--------|
| EP-TECH has no dependencies | ✓ |
| EP-DATA depends on EP-TECH only (Foundational) | ✓ |
| All feature epics (EP-001–EP-010) depend only on EP-TECH and EP-DATA | ✓ |
| Feature-to-feature dependencies | 0 (none) |
| Decomposed dependencies (EP-XXX-I → EP-XXX-II) | 0 (no split epics required; EP-002 and EP-DATA noted as over ~12 but retained as cohesive) |
| **T2 PASS** | No prohibited feature-to-feature dependencies; dependency graph is valid |

### T3 — Parallelisation

| Check | Result |
|-------|--------|
| Epics executable in parallel after EP-DATA | EP-001, EP-002, EP-003, EP-004, EP-005, EP-006, EP-007, EP-008, EP-009, EP-010 |
| Count of parallel epics | 10 (≥3 required) |
| **T3 PASS** | All 10 feature epics are independently executable after EP-DATA completes |

### T4 — Green-Field Signal Compliance

| Check | Result |
|-------|--------|
| EP-TECH created (first, no deps) | ✓ |
| EP-DATA created (second, depends EP-TECH only) | ✓ |
| codeanalysis.md absent — logged and skipped | ✓ (expected for GREEN-FIELD) |
| No requirements sourced from codeanalysis.md | ✓ |
| **T4 PASS** | Green-field foundational epic rules satisfied |

### T5 — Epic Size Advisory

| Epic | Item Count | Advisory |
|------|-----------|---------|
| EP-TECH | 16 | Exceeds ~12 soft limit; retained — all cross-cutting infrastructure, no unrelated outcomes |
| EP-DATA | 24 | Exceeds ~12 soft limit; retained — all HIPAA-compliance infrastructure with hard interdependencies; splitting would produce no independently deployable value |
| EP-001 | 11 | Within limit ✓ |
| EP-002 | 20 | Exceeds ~12 soft limit; retained — end-to-end booking flow (slot → insurance → risk → booking → PDF → email) is a single deployable outcome |
| EP-003–EP-010 | 4–10 each | Within limit ✓ |

---

*Generated by create-epics workflow v1.0 · Source: BRD Consolidated v1.0 · Date: 2026-05-14*
