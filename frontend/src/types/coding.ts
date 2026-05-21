/**
 * coding.ts — Shared types for the Medical Code Verification flow (US_030).
 * API contract shapes and component props for SCR-014.
 * Mirrors backend DTOs: MedicalCodeSuggestionDto, VerifyCodeResponse, CodeValidateResponse.
 */

/** Decision values for code verification (AC-002, AC-003, AC-004). */
export type VerifyDecision = 'Accepted' | 'Modified' | 'Rejected';

/** Summary of a verified code attached to a suggestion (backend: VerifiedCodeSummaryDto). */
export interface VerifiedCodeSummaryDto {
  decision: VerifyDecision;
  code: string;
}

/**
 * A single AI-suggested medical code (backend: MedicalCodeSuggestionDto).
 * Returned by GET /api/v1/code-suggestions?encounterId=
 */
export interface MedicalCodeSuggestionDto {
  id: string;
  codeSystem: string;         // 'ICD10' | 'CPT' | 'SNOMED'
  suggestedCode: string;
  description: string;
  confidenceScore: number;    // 0.0–1.0
  rank: number;               // 1 = highest confidence (sort order)
  status: string;             // 'Pending' | 'Accepted' | 'Modified' | 'Rejected'
  verifiedCode?: VerifiedCodeSummaryDto;
}

/** POST /api/v1/codes/verify — request body. */
export interface VerifyCodeRequest {
  suggestionId: string;
  decision: VerifyDecision;
  verifiedCode?: string;
}

/** POST /api/v1/codes/verify — response body. */
export interface VerifyCodeResponse {
  verifiedMedicalCodeId: string;
  codingStatusUpdated: boolean;
}

/** GET /api/v1/codes/validate?code=&type= — response body. */
export interface CodeValidateResponse {
  valid: boolean;
  description?: string;
}
