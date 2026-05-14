# Business Requirements Document (BRD)

# Unified Patient Access & Clinical Intelligence Platform

**Version:** Consolidated v1.0
**Date:** May 13, 2026

---

## 1. Executive Summary

The Unified Patient Access & Clinical Intelligence Platform is a unified, standalone healthcare solution that bridges the gap between patient scheduling and clinical data management. By combining a modern, patient-centric appointment booking system with a "Trust-First" clinical intelligence engine, the platform simplifies scheduling, reduces no-show rates, and eliminates the manual extraction of patient data from unstructured reports.

The platform aims to:

- Simplify appointment booking
- Reduce no-show rates
- Automate clinical data extraction from medical documents
- Improve operational efficiency and clinical accuracy

The system serves **Patients**, **Staff**, and **Admin** users, providing a seamless end-to-end data lifecycle from initial booking to post-visit data consolidation — reducing clinical preparation from a 20-minute manual task to a 2-minute verification process.

---

## 2. Business Problem & Market Opportunity

Healthcare organizations currently face major operational challenges due to disconnected systems and manual workflows, creating inefficiencies at multiple stages.

### Key Challenges

- **High No-Show Rates:** Providers experience up to a 15% no-show rate due to complex booking processes and lack of smart reminders, leading to revenue loss and underutilized schedules.
- **Manual Clinical Preparation:** Clinical staff spend 20+ minutes manually reading multi-format PDF reports to gather required patient data (vitals, history, medications), which is a primary bottleneck in clinical prep.
- **Fragmented Systems:** Scheduling tools, intake systems, and clinical coding platforms operate independently, creating data silos and workflow inefficiencies.
- **Trust Deficit in AI Tools:** Existing AI solutions often operate as "Black Box" systems where users must manually verify unlinked data, reducing trust and adoption.

### Market Opportunity

There is a strong opportunity to build a unified platform that connects:

- Scheduling
- Patient intake
- Clinical intelligence
- Medical coding

into a single, scalable ecosystem — addressing the gap left by fragmented booking tools that lack clinical data context and AI coding tools that lack transparency.

---

## 3. Proposed Solution

The platform is an intelligent, integration-ready aggregator designed to improve both operational scheduling and clinical preparation.

### Front-End Capabilities

- Intelligent appointment booking with dynamic preferred slot swapping
- Rule-based no-show risk assessment
- Flexible digital intake: AI conversational or manual fallback
- Automated multi-channel reminders and notifications
- Calendar synchronization (Google/Outlook via free APIs)

### Back-End Intelligence

- Clinical document ingestion (multi-format PDFs and reports)
- Patient data extraction (vitals, history, medications)
- 360° patient profile generation
- ICD-10 and CPT code suggestions
- Data conflict detection and verification

The solution transforms a 20-minute manual search task into a 2-minute verification action.

---

## 4. Core Features & Differentiators

### 4.1 Flexible Patient Intake

Patients can freely choose between:

- **AI conversational intake** — guided, intelligent data collection
- **Traditional manual forms** — standard structured input

Patients can switch between both methods at any time, with edits easily handled without requiring human assistance or staff intervention.

### 4.2 Dynamic Preferred Slot Swap

Patients can:

- Book an available appointment slot
- Simultaneously select a preferred (currently unavailable) slot

If the preferred slot opens:

- The system automatically swaps the appointment
- Releases the originally booked slot for others
- Sends notifications to the patient confirming the change

### 4.3 Centralized Staff Control

Only **staff users** can:

- Handle walk-in bookings (optionally creating an account for the patient post-booking)
- Manage same-day queues
- Mark patients as "Arrived"

> **Patients cannot self-check in** via apps, web portals, or QR codes.

### 4.4 Clinical Data Consolidation & Conflict Resolution

The platform:

- Aggregates multiple uploaded clinical documents
- Removes duplicate information to produce a de-duplicated patient view
- Explicitly highlights critical data conflicts (e.g., conflicting medications)
- Builds a unified, verified patient summary (360° Patient View)

### 4.5 AI-Assisted Medical Coding

The system provides:

- ICD-10 code suggestions
- CPT code suggestions

with human verification support, ensuring clinical accuracy and audit traceability.

---

## 5. Technology Stack & Infrastructure

To ensure a scalable, cost-effective, and maintainable platform, the system architecture will be built utilizing the following technologies:

| Layer | Technology | Notes |
|---|---|---|
| **UI (Frontend)** | React | |
| **Frontend Hosting** | InfinityFree | Free hosting for React app |
| **API (Backend)** | .NET | |
| **Backend Hosting** | MonsterASP | Free .NET hosting |
| **Data Layer** | PostgreSQL (via Supabase) | Includes pgvector for AI/embeddings |
| **Backend Webjobs** | Hangfire | Background job processing |
| **Caching** | Upstash Redis | |
| **Dev Environment** | GitHub Codespaces | Free, open-source-friendly |
| **Secrets / Key Vault** | GitHub Secrets | Secure secrets management |
| **CI/CD** | GitHub Actions | Automated build and deployment |
| **AI Engine** | Gemini | Clinical intelligence and coding suggestions |

### Infrastructure Requirements

- Free and open-source-friendly platforms only
- No paid cloud services (e.g., AWS, Azure) in Phase 1
- HIPAA-compliant architecture
- Role-based access control (RBAC)
- Immutable audit logging
- Native deployment capabilities using PostgreSQL for structured data and Upstash Redis for caching
- All auxiliary processing, background jobs, and data handling workflows must exclusively use free and open-source technology stacks and tools

---

## 6. Project Scope (Phase 1)

### In-Scope

- **User Roles:** Patients, Staff (front desk/call center), and Admin (user management)
- **Appointment Booking:** Full booking workflow with waitlist functionality
- **Preferred Slot Swapping:** Dynamic slot swap with automatic notification
- **Reminders:** Automated multi-channel reminders (SMS/Email)
- **Calendar Synchronization:** Google/Outlook calendar sync via free APIs
- **PDF Confirmations:** Appointment details sent as a PDF via email after booking
- **Insurance Pre-Check:** Soft validation of insurance name and ID against an internal predefined set of dummy records
- **Clinical Document Uploads:** Patient-uploaded historical documents and post-visit clinical notes
- **360° Patient Profile Generation:** Core data extraction from uploaded clinical documents to build the patient profile
- **ICD-10 and CPT Mapping:** Medical coding based on aggregated patient data

### Out-of-Scope

- Provider logins or provider-facing portals/actions
- Payment gateway integration (provisioning for future reservation fees only)
- Family member profile features
- Patient self-check-in (mobile app, web portal, or QR code)
- Direct, bi-directional EHR integration or full claims submission
- Use of paid cloud infrastructure (e.g., AWS, Azure)

---

## 7. Non-Functional Requirements (NFRs)

### 7.1 Security & Compliance

- 100% HIPAA-compliant data handling, transmission, and storage
- Encrypted data transmission and storage
- Strict role-based access control (RBAC)
- Immutable audit logging for all patient and staff actions

### 7.2 Reliability

- 99.9% uptime target
- 15-minute automatic session timeout
- Robust and secure session management

### 7.3 Scalability

The platform must support growth in:

- Increasing patient volume
- Additional clinics and providers
- Future third-party integrations

without requiring major architectural redesign.

### 7.4 Infrastructure

- Native deployment capabilities (Windows Services / IIS)
- PostgreSQL (via Supabase) for structured data storage
- Upstash Redis for caching
- Hangfire for background job processing

---

## 8. Success Criteria

### 8.1 Operational Efficiency

- Demonstrable reduction in the baseline no-show rate
- Decrease in staff administrative time per appointment
- Faster appointment booking and management workflows

### 8.2 Platform Adoption

- High volume of total patient dashboards created
- High volume of appointments successfully booked
- Increased patient registrations and improved staff utilization

### 8.3 Clinical Accuracy

- **AI-Human Agreement Rate of >98%** for:
  - Clinical data extraction
  - ICD-10 mapping
  - CPT mapping

### 8.4 Risk Prevention

- Quantifiable metric of "Critical Conflicts Identified" to track prevented safety risks and claim denials
- Identification of medication conflicts
- Reduced claim denials
- Improved patient safety outcomes

---

## 9. Agile Epics

### Epic 1: Patient Scheduling & Intake

- Appointment booking
- Preferred slot swapping
- Reminder workflows (SMS/Email)
- AI/manual intake
- PDF confirmation delivery

### Epic 2: Clinical Data Aggregation

- PDF and multi-format document ingestion
- Clinical data extraction (vitals, history, medications)
- Conflict detection and highlighting

### Epic 3: Medical Coding Automation

- ICD-10 mapping
- CPT mapping
- AI-assisted verification workflows (powered by Gemini)

### Epic 4: Staff & Admin Controls

- Walk-in management
- Same-day queue handling
- Patient arrival workflows
- Admin user management

### Epic 5: Infrastructure & Compliance

- HIPAA compliance implementation
- RBAC implementation
- Immutable audit logging
- CI/CD pipeline (GitHub Actions)
- Performance monitoring
- Secrets management (GitHub Secrets)

---

## 10. Conclusion

The Unified Patient Access & Clinical Intelligence Platform modernizes healthcare operations by combining intelligent scheduling, flexible patient intake, and AI-powered clinical intelligence into a single, cohesive ecosystem.

The platform improves:

- **Patient experience** — through flexible intake, smart reminders, and preferred slot swapping
- **Operational efficiency** — through reduced no-show rates and automated workflows
- **Clinical preparation** — from a 20-minute manual task to a 2-minute verification action
- **Coding accuracy** — through AI-assisted ICD-10 and CPT suggestions with >98% agreement
- **Healthcare workflow transparency** — through immutable audit logging and trust-first AI design

All of this is delivered while remaining **scalable**, **HIPAA-compliant**, and **cost-effective** by leveraging free and open-source infrastructure throughout Phase 1.
