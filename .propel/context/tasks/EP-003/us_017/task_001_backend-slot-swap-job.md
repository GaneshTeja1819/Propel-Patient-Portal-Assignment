# Task - TASK_001

## Requirement Reference
- **User Story:** us_017
- **Story Location:** .propel/context/tasks/EP-003/us_017/us_017.md
- **Acceptance Criteria:**
  - AC-001: Swap job triggered within 5 s of slot release; evaluates WaitlistEntry records ordered by registeredAt
  - AC-002: First eligible patient atomically moved to preferred slot in single DB transaction; old slot released; WaitlistEntry deleted
  - AC-004: Concurrency conflict → retry once; second failure → skip entry; evaluate next eligible entry; failure logged
  - AC-005: No WaitlistEntry for released slot → job no-ops; slot enters available pool
- **Edge Cases:**
  - Preferred slot rebooked between job pickup and swap attempt → `DbUpdateConcurrencyException`; swap abandoned; patient's original appointment unchanged; WaitlistEntry retained
  - Multiple concurrent releases for same slot → Hangfire distributed lock key `slot-swap-{slotId}` prevents double-evaluation
  - Hangfire job fires after preferred slot is rebooked → handled by concurrency check; WaitlistEntry kept for future

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
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008, TR-004 — async slot-swap job |
| Database | PostgreSQL via Supabase | 15 | TR-003 — WaitlistEntry, Appointment, AppointmentSlot; EF Core transactions |

---

## Task Overview
Implement `SlotSwapJob`, a Hangfire background job enqueued by `CancelAppointmentHandler` (US_014) whenever a slot is released. The job acquires a Hangfire distributed lock keyed on `slot-swap-{slotId}` to prevent concurrent processing. It loads all `WaitlistEntry` records for the released slot ordered by `registeredAt` (FIFO), then iterates: for each entry it opens an EF Core transaction to atomically release the patient's current slot and reserve the preferred slot. On `DbUpdateConcurrencyException` the transaction is rolled back, the failure is written to the audit log, and the next entry is evaluated. On success the `WaitlistEntry` is deleted and the job's notification step (task_002) is invoked.

## Dependent Tasks
- `task_001_backend-hangfire.md` (US_003) — Hangfire server running
- `task_002_backend-waitlist-upsert.md` (US_016) — WaitlistEntry table and repository must exist
- `task_002_backend-cancel-reschedule.md` (US_014) — `SlotSwapJob` is enqueued from `CancelAppointmentHandler` (replacing `WaitlistNotificationJob`)

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/SlotSwapJob.cs` — new Hangfire job
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/WaitlistNotificationJob.cs` — update: delegate slot-release trigger to `SlotSwapJob` instead (or chain from `SlotSwapJob`)
- `backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs` — enqueue `SlotSwapJob` instead of (or in addition to) `WaitlistNotificationJob`

## Implementation Plan
1. Create `SlotSwapJob.cs` accepting `Guid releasedSlotId`
2. Acquire Hangfire distributed lock: `using (var distributedLock = connection.AcquireDistributedLock($"slot-swap-{releasedSlotId}", TimeSpan.FromSeconds(30)))` — skip job if lock cannot be acquired within 5 s (prevents double evaluation on concurrent releases)
3. Query `WaitlistEntry` ordered by `registeredAt` for `preferredSlotId == releasedSlotId`; if none → log "No waitlist entries for slot {id}"; return (AC-005)
4. For each `WaitlistEntry`:
   a. Open EF Core transaction
   b. Load `releasedSlot` and `appointment.currentSlot`; check `releasedSlot.isBooked == false` (re-verify availability)
   c. Set `appointment.slotId = releasedSlotId`; set `releasedSlot.isBooked = true`; set `oldSlot.isBooked = false`
   d. Delete `WaitlistEntry`
   e. Call `SaveChangesAsync`; catch `DbUpdateConcurrencyException` → rollback; write `SLOT_SWAP_FAILED` audit entry with patientId, slotId, error; if this is the first attempt → retry once (re-enter inner loop); if second failure → skip entry; evaluate next
   f. On success → commit; enqueue `SlotSwapNotificationJob(appointmentId, newSlotId)` (task_002); break (one swap per released slot)
5. Wire `SlotSwapJob` to be enqueued from `CancelAppointmentHandler` and `RescheduleAppointmentHandler` after slot release

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/WaitlistNotificationJob.cs  (from US_014)
    UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs
    UPACIP.Domain/Entities/WaitlistEntry.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/SlotSwapJob.cs | Atomic slot-swap Hangfire job with FIFO WaitlistEntry evaluation |
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs | Enqueue SlotSwapJob after slot release |
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs | Enqueue SlotSwapJob after old slot release |

## External References
- [Hangfire — Distributed Locks](https://docs.hangfire.io/en/latest/configuration/using-redis.html#distributed-locks)
- [EF Core Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Cancel appointment → `SlotSwapJob` enqueued within 5 s; WaitlistEntry patient swapped atomically; old WaitlistEntry deleted
- [ ] Force concurrency conflict on swap → retry once; second failure → skip to next WaitlistEntry; `SLOT_SWAP_FAILED` audit written
- [ ] No WaitlistEntry for released slot → job no-ops; no audit entry written; slot available

## Implementation Checklist
- [ ] Implement `SlotSwapJob` with Hangfire distributed lock to prevent double-evaluation (AC-001, edge case)
- [ ] Load WaitlistEntries ordered by `registeredAt` (FIFO); no entries → return (AC-001, AC-005)
- [ ] Atomic EF Core transaction: swap slots + delete WaitlistEntry + enqueue notification (AC-002)
- [ ] On `DbUpdateConcurrencyException` → rollback → retry once; second failure → log `SLOT_SWAP_FAILED` audit; skip entry; evaluate next (AC-004)
- [ ] Enqueue `SlotSwapJob` from `CancelAppointmentHandler` after slot release (AC-001)
- [ ] Enqueue `SlotSwapJob` from `RescheduleAppointmentCommand` after old slot release (AC-001)
