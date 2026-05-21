/**
 * conflict.ts — Shared type definitions for the Conflict Review feature (US_028, UXR-404).
 *
 * DataConflictDto is the shape returned by:
 *   GET /api/v1/patients/{patientId}/conflicts   (StaffPolicy)
 *
 * PATCH payloads are sent to:
 *   PATCH /api/v1/conflicts/{id}/resolve         (StaffPolicy)
 *   PATCH /api/v1/conflicts/{id}/mark-reviewed   (StaffPolicy)
 */

/** Severity of the detected conflict — drives alert colouring (UXR-404). */
export type ConflictSeverity = 'High' | 'Medium';

/** Lifecycle state of a conflict record. */
export type ConflictStatus = 'Unresolved' | 'ReviewedUnresolved' | 'Resolved';

/** One of the competing values contributing to a conflict. */
export interface ConflictingValue {
  /** The data value extracted from this source. */
  value: string;
  /** Human-readable source description e.g. "AI — Admission record (Mar 12 2026)". */
  sourceLabel?: string;
  /** ID of the originating ClinicalDocument, if applicable. */
  sourceDocumentId?: string;
  /** True when this value was produced by the AI extraction pipeline. */
  isAiExtracted: boolean;
}

/** Full conflict DTO as returned by GET /api/v1/patients/{patientId}/conflicts. */
export interface DataConflictDto {
  id: string;
  patientId: string;
  /** Human-readable field name in conflict e.g. "Medication Dosage — Metoprolol". */
  fieldName: string;
  severity: ConflictSeverity;
  status: ConflictStatus;
  /** All competing values; typically two entries (source vs. target). */
  conflictingValues: ConflictingValue[];
  /** The value chosen as authoritative after resolution (present when status = Resolved). */
  canonicalValue?: string;
  /** True when the conflict was created after the staff member's last visit. */
  isNew?: boolean;
  detectedAt: string;
  resolvedAt?: string;
  /** Staff ID who resolved the conflict. */
  resolvedById?: string;
}

/** Payload sent to PATCH /api/v1/conflicts/{id}/resolve. */
export interface ResolveConflictPayload {
  authoritativeValue: string;
  sourceDocumentId?: string;
  resolutionNote?: string;
}
