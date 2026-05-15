# Navigation Map

> **Project:** UPACIP Hi-Fi Wireframe Set · 16 screens
> **Date:** 2026-05-20

---

## Flow Index

| Flow ID | Name | Entry screen | Exit screen | Trigger |
|---|---|---|---|---|
| FL-001 | Authentication | SCR-001 | SCR-003 / SCR-011 / SCR-015 (role-based) | Sign in, Register |
| FL-002 | Appointment booking | SCR-003 | SCR-003 (success) or SCR-006 (waitlist) | "Book appointment" CTA |
| FL-003 | Appointment management | SCR-003 | SCR-003 | Card → Detail → Cancel / Reschedule |
| FL-004 | AI intake | SCR-003 | SCR-003 | "Complete intake" alert CTA |
| FL-005 | Manual intake | SCR-003 | SCR-003 | "Complete intake" alert CTA or method toggle |
| FL-006 | Calendar sync | SCR-003 | SCR-003 | Nav "Calendar" or dashboard tile |
| FL-007 | Staff walk-in booking | SCR-011 | SCR-011 | "New Walk-in" button |
| FL-008 | Admin user management | SCR-015 | SCR-015 | Self-contained |
| FL-009 | Document upload | SCR-003 / SCR-009 | SCR-009 | "Upload document" CTA |
| FL-010 | Staff conflict resolution | SCR-011 / SCR-013 | SCR-011 | "Profile" → conflict drawer |
| FL-011 | Staff code verification | SCR-011 | SCR-011 | "Verify codes" button |
| FL-012 | Admin audit log | SCR-015 | SCR-017 | Nav "Audit Log" link |

---

## Screen-to-Screen Links

### SCR-001 — Login
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Create account" link | → | SCR-002 | Always |
| "Sign in" button (patient role) | → | SCR-003 | Credentials valid |
| "Sign in" button (staff role) | → | SCR-011 | Credentials valid |
| "Sign in" button (admin role) | → | SCR-015 | Credentials valid |
| Error state | — | SCR-001 (same page) | Invalid credentials |

### SCR-002 — Registration
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Sign in" link | → | SCR-001 | Always |
| "Create account" button (success) | → | SCR-003 | Validation passes |
| Inline validation error | — | SCR-002 (same page) | Field invalid |

### SCR-003 — Patient Dashboard
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Book appointment" tile | → | SCR-004 | Always |
| Appointment card (view detail) | → | SCR-005 | Always |
| "Complete intake" alert CTA | → | SCR-007 (AI default) | Intake incomplete |
| "Calendar sync" tile | → | SCR-016 | Always |
| "Upload document" tile | → | SCR-010 | Always |
| Nav "Book" | → | SCR-004 | Always |
| Nav "My Profile" | → | SCR-009 | Always |
| Nav "Documents" | → | SCR-010 | Always |
| Nav "Calendar" | → | SCR-016 | Always |
| Sign out | → | SCR-001 | Always |

### SCR-004 — Appointment Booking
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav back / breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Continue" (slot selected, no waitlist) | → | SCR-003 (confirmation) | Slot available |
| "Join waitlist" path | → | SCR-006 | Slot fully taken, waitlist option selected |
| Conflict toast (slot taken) | — | SCR-004 (same page) | Race condition |

### SCR-005 — Appointment Detail
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav "Dashboard" | → | SCR-003 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Cancel appointment" → confirm modal → "Confirm cancel" | → | SCR-003 | Post-cancel |
| Reschedule drawer → new slot confirmed | → | SCR-003 | Post-reschedule |

### SCR-006 — Preferred Slot Confirmation
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Back to dashboard" button | → | SCR-003 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| Nav links | → | See SCR-003 nav | Always |

### SCR-007 — AI Conversational Intake
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Switch to manual form" toggle | → | SCR-008 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Submit intake" (after step 8) | → | SCR-003 | Intake complete |

### SCR-008 — Manual Intake Form
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Switch to AI" toggle | → | SCR-007 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Submit intake" button | → | SCR-003 | Validation passes |

### SCR-009 — Patient 360° Profile
| Element | Direction | Target | Condition |
|---|---|---|---|
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Upload document" CTA | → | SCR-010 | Always |
| Nav links | → | See SCR-003 nav | Always |

### SCR-010 — Document Upload
| Element | Direction | Target | Condition |
|---|---|---|---|
| "View Profile" button | → | SCR-009 | Always |
| Breadcrumb "Profile" | → | SCR-009 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |

### SCR-011 — Staff Queue
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav "New Walk-in" | → | SCR-012 | Always |
| "New Walk-in" page button | → | SCR-012 | Always |
| "Profile" row action | → | SCR-013 | Always |
| "Verify codes" row action | → | SCR-014 | Always |
| "Mark Arrived" row action | — | SCR-011 (status update) | Always |
| Sign out | → | SCR-001 | Always |

### SCR-012 — Staff Walk-in Booking
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Back to Queue" breadcrumb | → | SCR-011 | Always |
| Nav "Queue" | → | SCR-011 | Always |
| "Confirm & add to queue" button | → | SCR-011 | Booking confirmed |
| "Add to queue without slot" | → | SCR-011 | No slot available (UC-014 fallback) |

### SCR-013 — Staff Patient Profile (Conflicts)
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Back to Queue" breadcrumb | → | SCR-011 | Always |
| Nav "Queue" | → | SCR-011 | Always |
| "Review & Resolve" CTA on ConflictAlert | — | SCR-013 (opens inline drawer) | Alert present |
| "Verify codes" button | → | SCR-014 | Always |
| Conflict drawer resolved → close | — | SCR-013 (drawer dismissed) | Post-resolution |

### SCR-014 — Medical Code Verification
| Element | Direction | Target | Condition |
|---|---|---|---|
| "Back to Queue" breadcrumb | → | SCR-011 | Always |
| Nav "Queue" | → | SCR-011 | Always |
| "Submit all decisions" button | → | SCR-011 | All codes decided |
| "Modify" → code edit modal → Save | — | SCR-014 (modal dismissed, row updated) | Always |

### SCR-015 — Admin User Management
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav "Users" (active) | — | SCR-015 | Always |
| Nav "Audit Log" | → | SCR-017 | Always |
| Role change modal → confirm | — | SCR-015 (role updated inline) | Always |
| Deactivate modal → confirm | — | SCR-015 (row updated to Inactive) | User is not self |
| Self-deactivation attempt | — | SCR-015 (error inside modal) | `data-self="true"` |
| Reactivate button (inactive user) | — | SCR-015 (status → Active) | User inactive |
| Sign out | → | SCR-001 | Always |

### SCR-016 — Calendar OAuth Consent
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav "Dashboard" | → | SCR-003 | Always |
| Breadcrumb "Dashboard" | → | SCR-003 | Always |
| "Connect calendar" (provider selected) | — | SCR-016 (OAuth flow simulation → connected state) | Provider selected |
| "Disconnect" button | — | SCR-016 (disconnected state + provider selector shown) | Currently connected |
| "Retry sync now" (advisory) | — | SCR-016 (advisory dismissed, retry spinner) | Sync failed |
| "Dismiss" advisory | — | SCR-016 (advisory hidden) | Always |

### SCR-017 — Admin Audit Log
| Element | Direction | Target | Condition |
|---|---|---|---|
| Nav logo / "Admin Portal" | → | SCR-015 | Always |
| Nav "Users" | → | SCR-015 | Always |
| Nav "Audit Log" (active) | — | SCR-017 | Always |
| "Export CSV" button | — | SCR-017 (toast shown, file download triggered) | Always |
| "🖨 Print" button | — | SCR-017 (browser print dialog) | Always |
| Row "Detail ▾" button | — | SCR-017 (expand row shown inline) | Always |
| Row "Detail ▴" button (close) | — | SCR-017 (expand row hidden) | Row expanded |
| Filter controls (date, action, role, status, search) | — | SCR-017 (filtered rows) | Always |
| "✕ Reset" filter button | — | SCR-017 (filters cleared) | Any filter active |
| Pagination page buttons | — | SCR-017 (demo alert, pages 2–17 not wired) | Always |
| Sign out | → | SCR-001 | Always |

---

## Dead Ends and Exceptions

| Screen | Dead-end scenario | Resolution |
|---|---|---|
| SCR-006 | Patient closes browser after waitlist join | No back-navigate issue; link → SCR-003 always shown |
| SCR-015 | Admin only user — cannot deactivate self | UC-017 ext 3a: confirm button disabled + inline error |
| SCR-014 | Staff navigates away before submitting decisions | `beforeunload` warning text noted (not yet wired in wireframe) |
| SCR-013 | Conflict drawer open + nav link clicked | Drawer closes on navigation; no data loss (drawer is read-only resolution form) |
| SCR-016 | OAuth provider cancels consent | Simulated: page returns to disconnected state (no provider connected) |
| SCR-017 | Admin reads immutable log — no mutations | UI is fully read-only; Export and Print are output-only |
| SCR-007/008 | User navigates away mid-intake | Session continues; intake alert remains on SCR-003 until submitted |
