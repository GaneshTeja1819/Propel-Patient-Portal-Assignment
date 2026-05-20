/**
 * profile.ts — Shared type definitions for the 360° Patient Profile (US_027).
 *
 * PatientProfileData is the shape returned by:
 *   GET /api/v1/profile/me         (Patient own view)
 *   GET /api/v1/profile/{patientId} (Staff view)
 */

export type DeduplicationStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

/**
 * A single clinical data item within a profile section.
 * PHI fields are decrypted server-side before transmission — never sent raw.
 */
export interface ClinicalDataItem {
  id: string;
  label: string;
  value: string;
  /** True when the value was extracted by the AI pipeline (Gemini). */
  isAiExtracted: boolean;
  /** True when the field contains PHI — renders 🔒 icon (UXR-402). */
  isPhiField: boolean;
  /** Document IDs from which this canonical entry was derived (post-dedup). */
  sourceDocumentIds?: string[];
  /** AI confidence score, 0–1. Present on AI-extracted items only. */
  confidence?: number;
  /** Human-readable source label, e.g. "Dr. S. Patel · Apr 15, 2026". */
  sourceLabel?: string;
}

/** All clinical sections returned by the profile API. */
export interface PatientProfileSections {
  vitals: ClinicalDataItem[];
  medications: ClinicalDataItem[];
  diagnoses: ClinicalDataItem[];
  visitHistory: ClinicalDataItem[];
}

/** Full profile DTO as returned by GET /api/v1/profile/me or /{patientId}. */
export interface PatientProfileData extends PatientProfileSections {
  patientId: string;
  displayName: string;
  initials: string;
  dateOfBirth: string;
  sex: string;
  contact: string;
  deduplicationStatus: DeduplicationStatus;
  /** False when no ClinicalDocument records exist for this patient. */
  hasDocuments: boolean;
}
