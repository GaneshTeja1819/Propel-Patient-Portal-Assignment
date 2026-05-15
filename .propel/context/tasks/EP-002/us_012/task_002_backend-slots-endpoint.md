# Task - TASK_002

## Requirement Reference
- **User Story:** us_012
- **Story Location:** .propel/context/tasks/EP-002/us_012/us_012.md
- **Acceptance Criteria:**
  - AC-001: Slot data served from Redis cache within 100 ms; falls through to PostgreSQL if Redis unavailable
  - AC-002: Slot state changes reflected in cache within one 5-second TTL cycle
- **Edge Cases:**
  - Redis unavailable → fall through to direct PostgreSQL query; log warning; response tagged as non-cached

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

## Applicable Technology Stack

| Layer | Technology | Version | Justification |
|-------|------------|---------|---------------|
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — GET /api/v1/slots endpoint |
| Caching | Upstash Redis (StackExchange.Redis) | Serverless | NFR-002, TR-005 — slot availability cache; ≤ 5 s TTL |
| Database | PostgreSQL via Supabase | 15 | TR-003 — AppointmentSlot table fallback query |

---

## Task Overview
Implement `GET /api/v1/slots?date={date}` in the API layer. The handler reads slot availability from `ISlotCacheService` (Redis, 5 s TTL); on a cache miss or Redis unavailability it falls through to a PostgreSQL query against `AppointmentSlot`. The response includes a `cached: true/false` flag. When a slot is booked (in US_013), its Redis key is invalidated via `ISlotCacheService.InvalidateSlotAsync`, ensuring the 5-second freshness window.

## Dependent Tasks
- `task_002_backend-redis.md` (US_003) — `ISlotCacheService` must be registered
- `task_001_backend-migrations.md` (US_005) — `AppointmentSlot` table must exist

## Impacted Components
- `backend/src/UPACIP.Application/Queries/Slots/GetSlotsQuery.cs` — new query
- `backend/src/UPACIP.Application/Handlers/Slots/GetSlotsHandler.cs` — new handler
- `backend/src/UPACIP.API/Controllers/SlotsController.cs` — new controller

## Implementation Plan
1. Define `GetSlotsQuery(DateTime date)` record in Application layer
2. Implement `GetSlotsHandler`: try `ISlotCacheService.GetSlotAsync(cacheKey)` — if hit, deserialise and return with `cached = true`; if miss or exception, query `AppointmentSlot` filtered by date and `isBooked = false`, serialise to DTO, write to cache with 5-second TTL, return with `cached = false`
3. Create `SlotsController` with `[Authorize(Policy = "PatientPolicy")]` on `GET /api/v1/slots?date={date}`; validate date param
4. Response DTO: `{ slots: [{ id, startDateTime, endDateTime, status }], cached: bool }`
5. Ensure `ISlotCacheService.InvalidateSlotAsync(slotId)` is called from the booking handler (US_013) to keep cache consistent within the 5-second window

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/Caching/SlotCacheService.cs  (from US_003)
    UPACIP.Domain/Entities/AppointmentSlot.cs           (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Queries/Slots/GetSlotsQuery.cs | Slots query record |
| CREATE | backend/src/UPACIP.Application/Handlers/Slots/GetSlotsHandler.cs | Redis → DB fallback slot read |
| CREATE | backend/src/UPACIP.API/Controllers/SlotsController.cs | GET /api/v1/slots?date= endpoint |

## External References
- [ASP.NET Core Controller Actions](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions)
- [StackExchange.Redis — String Get/Set](https://stackexchange.github.io/StackExchange.Redis/Basics)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `GET /api/v1/slots?date=today` returns slot list within 100 ms from Redis on second call
- [ ] Disable Redis; confirm slot list still returned from PostgreSQL; response `cached: false`
- [ ] Book a slot (US_013); within 5 s subsequent `GET /api/v1/slots` shows slot as `status: "Unavailable"`

## Implementation Checklist
- [ ] Implement `GetSlotsHandler`: Redis cache read → DB fallback on miss/error; 5-second TTL write (AC-001)
- [ ] Return `cached: bool` flag in response DTO; log warning on Redis fallback (AC-001 edge case)
- [ ] Create `SlotsController` with `[Authorize(Policy = "PatientPolicy")]`; validate `date` query param (AC-001)
- [ ] Slot state changes propagate within 5-second TTL cycle via cache invalidation in booking handler (AC-002)

## Mobile References [CONDITIONAL: Mobile Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |
