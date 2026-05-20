/**
 * intake.ts — Shared types for the AI conversational intake flow (US_018).
 * All API contract shapes, session state, and component props are defined here
 * to enforce a single source of truth (DRY, NFR-012).
 */

/** A single captured intake field, keyed by field name. */
export interface CapturedField {
  fieldKey: string;
  fieldLabel: string;
  value: string;
  /** True when Gemini failed to parse this field and manual input is required. */
  manualRequired: boolean;
}

/** Mirrors the backend IntakeSessionState. */
export interface IntakeSessionState {
  appointmentId: string;
  currentFieldIndex: number;
  totalFields: number;
  capturedFields: CapturedField[];
  method: 'AI' | 'AI-Partial';
  phase: 'conversation' | 'summary';
}

/** Response from POST /api/v1/intake/start */
export interface StartSessionResponse {
  fieldKey: string;
  questionText: string;
  questionNumber: number;
  totalQuestions: number;
  isPhiField: boolean;
  /** Optional quick-select answers for common responses (H-001). */
  quickOptions?: string[];
}

/** Response from POST /api/v1/intake/answer */
export interface AnswerResponse {
  phase: 'conversation' | 'summary';
  nextFieldKey?: string;
  nextQuestionText?: string;
  questionNumber?: number;
  totalQuestions?: number;
  isPhiField?: boolean;
  /** Set when Gemini failed to parse; manual input required for this field. */
  fallbackRequired?: boolean;
  capturedFields?: CapturedField[];
  /** Optional quick-select answers for the next question (H-001). */
  quickOptions?: string[];
}

/** Response from GET /api/v1/intake/session/:appointmentId (resume) */
export interface SessionResumeResponse extends IntakeSessionState {
  currentQuestionText?: string;
  isPhiField?: boolean;
}

/** A single message in the chat thread. */
export interface ChatMessage {
  id: string;
  role: 'ai' | 'user';
  text: string;
  isPhiField?: boolean;
  fieldKey?: string;
  questionNumber?: number;
  totalQuestions?: number;
  /** When true, renders a ManualFieldFallback instead of the text input. */
  fallbackRequired?: boolean;
  /** Quick-select answer chips shown below the AI question (H-001). */
  quickOptions?: string[];
}

/**
 * Field keys and value shape for the manual intake form (US_019).
 * Keys match the wireframe SCR-008 field IDs.
 */
export interface ManualIntakeFormValues {
  'chief-complaint': string;
  'symptom-duration': string;
  'pain-scale': string;
  medications: string;
  'med-types': string[];
  allergies: string;
  'allergy-sev': string;
  smoking: string;
  alcohol: string;
  exercise: string;
  notes: string;
}
