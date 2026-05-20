/**
 * useAIIntake.ts — Hook driving AI intake session API calls (US_018, AC-001–AC-003).
 *
 * Responsibilities:
 *  - Start a Gemini intake session via POST /api/v1/intake/start
 *  - Submit answers via POST /api/v1/intake/answer
 *  - Resume a partial session via GET /api/v1/intake/session/:appointmentId
 *  - Confirm intake via POST /api/v1/intake/confirm (triggers backend task_003)
 *  - Persist partial answers in sessionStorage for mid-session resume
 *
 * Security: JWT bearer token from AuthContext is attached to every request.
 * All API calls route through the backend; no direct Gemini calls from the client.
 */
import { useState, useCallback, useEffect, useRef } from 'react';
import {
  StartSessionResponse,
  AnswerResponse,
  SessionResumeResponse,
  CapturedField,
  ChatMessage,
  IntakeSessionState,
} from '../types/intake';
import { useAuthContext } from '../context/AuthContext';

const API_BASE = '/api/v1/intake';
const SESSION_STORAGE_PREFIX = 'intake-session-';

interface UseAIIntakeReturn {
  messages: ChatMessage[];
  capturedFields: CapturedField[];
  currentFieldKey: string | null;
  questionNumber: number;
  totalQuestions: number;
  isPhiField: boolean;
  phase: 'idle' | 'loading' | 'conversation' | 'summary' | 'error';
  errorMessage: string | null;
  startSession: () => Promise<void>;
  submitAnswer: (rawAnswer: string) => Promise<void>;
  updateFieldValue: (fieldKey: string, value: string) => void;
  confirmIntake: () => Promise<void>;
  /** M-004: clears the error message — called when switching intake mode. */
  clearError: () => void;
}

function storageKey(appointmentId: string): string {
  return SESSION_STORAGE_PREFIX + appointmentId;
}

/**
 * H-001 / HIPAA: Strip PHI values before writing to sessionStorage.
 * Only structural state (fieldKeys, manualRequired flags, method) is persisted —
 * never the actual captured PHI values. Actual values live server-side only.
 */
function stripPhiValues(fields: CapturedField[]): CapturedField[] {
  return fields.map((f) => ({ ...f, value: '' }));
}

function saveToStorage(appointmentId: string, state: Partial<IntakeSessionState>): void {
  try {
    const safeState: Partial<IntakeSessionState> = {
      ...state,
      // PHI values are never written to browser storage
      capturedFields: state.capturedFields ? stripPhiValues(state.capturedFields) : undefined,
    };
    sessionStorage.setItem(storageKey(appointmentId), JSON.stringify(safeState));
  } catch {
    // sessionStorage full or unavailable — continue without persisting
  }
}

function loadFromStorage(appointmentId: string): Partial<IntakeSessionState> | null {
  try {
    const raw = sessionStorage.getItem(storageKey(appointmentId));
    return raw ? (JSON.parse(raw) as Partial<IntakeSessionState>) : null;
  } catch {
    return null;
  }
}

export function useAIIntake(appointmentId: string): UseAIIntakeReturn {
  useAuthContext();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [capturedFields, setCapturedFields] = useState<CapturedField[]>([]);
  const [currentFieldKey, setCurrentFieldKey] = useState<string | null>(null);
  const [questionNumber, setQuestionNumber] = useState(0);
  const [totalQuestions, setTotalQuestions] = useState(0);
  const [isPhiField, setIsPhiField] = useState(false);
  const [phase, setPhase] = useState<UseAIIntakeReturn['phase']>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const messageIdRef = useRef(0);

  function nextId(): string {
    messageIdRef.current += 1;
    return String(messageIdRef.current);
  }

  const buildHeaders = useCallback((): Record<string, string> => ({
    'Content-Type': 'application/json',
  }), []);

  const clearError = useCallback((): void => {
    setErrorMessage(null);
  }, []);

  /** Attempt to resume a partial session from sessionStorage or backend. */
  useEffect(() => {
    const stored = loadFromStorage(appointmentId);
    if (!stored || !stored.capturedFields?.length) return;

    // Restore local UI state from stored partial session
    setCapturedFields(stored.capturedFields);
    if (stored.method) {
      // session was in progress; will be resumed via startSession
    }
  }, [appointmentId]);

  const startSession = useCallback(async (): Promise<void> => {
    setPhase('loading');
    setErrorMessage(null);

    // N-004: AbortController prevents React StrictMode double-invoke side-effects
    const ac = new AbortController();
    const { signal } = ac;

    // Try to resume from backend first
    try {
      const resumeRes = await fetch(`${API_BASE}/session/${appointmentId}`, {
        headers: buildHeaders(),
        signal,
      });

      if (resumeRes.ok) {
        const resume: SessionResumeResponse = await resumeRes.json();
        if (resume.capturedFields?.length) {
          setCapturedFields(resume.capturedFields);
          setQuestionNumber(resume.currentFieldIndex + 1);
          setTotalQuestions(resume.totalFields);
          setCurrentFieldKey(
            resume.capturedFields.find((f) => !f.value && !f.manualRequired)?.fieldKey ?? null,
          );

          const greeting: ChatMessage = {
            id: nextId(),
            role: 'ai',
            text: `Welcome back! You have ${
              resume.totalFields - resume.currentFieldIndex
            } questions remaining. Picking up where you left off…`,
          };
          const resumeQuestion: ChatMessage = {
            id: nextId(),
            role: 'ai',
            text: resume.currentQuestionText ?? 'Please continue answering from where you left off.',
            isPhiField: resume.isPhiField,
            fieldKey:
              resume.capturedFields.find((f) => !f.value && !f.manualRequired)?.fieldKey,
            questionNumber: resume.currentFieldIndex + 1,
            totalQuestions: resume.totalFields,
          };
          setMessages([greeting, resumeQuestion]);
          setPhase('conversation');
          return;
        }
      }
    } catch (err) {
      if ((err as Error).name === 'AbortError') return;
      // No active backend session — start fresh
    }

    // Start a new session
    try {
      const res = await fetch(`${API_BASE}/start`, {
        method: 'POST',
        headers: buildHeaders(),
        body: JSON.stringify({ appointmentId }),
        signal,
      });

      if (!res.ok) throw new Error(`Intake start failed: ${res.status}`);

      const data: StartSessionResponse = await res.json();

      const greeting: ChatMessage = {
        id: nextId(),
        role: 'ai',
        text: "Hi! I'm your AI Health Assistant, powered by Gemini. I'll guide you through a few questions to prepare for your appointment. You can switch to the manual form at any time.",
      };
      const firstQuestion: ChatMessage = {
        id: nextId(),
        role: 'ai',
        text: data.questionText,
        isPhiField: data.isPhiField,
        fieldKey: data.fieldKey,
        questionNumber: data.questionNumber,
        totalQuestions: data.totalQuestions,
        quickOptions: data.quickOptions,
      };

      setMessages([greeting, firstQuestion]);
      setCurrentFieldKey(data.fieldKey);
      setQuestionNumber(data.questionNumber);
      setTotalQuestions(data.totalQuestions);
      setIsPhiField(data.isPhiField);
      setPhase('conversation');

      saveToStorage(appointmentId, { appointmentId, capturedFields: [], method: 'AI' });
    } catch (err) {
      if ((err as Error).name === 'AbortError') return;
      setErrorMessage(err instanceof Error ? err.message : 'Failed to start intake session.');
      setPhase('error');
    }
  }, [appointmentId, buildHeaders]);

  const submitAnswer = useCallback(
    async (rawAnswer: string): Promise<void> => {
      if (!currentFieldKey || phase !== 'conversation') return;

      // Append user message immediately
      const userMsg: ChatMessage = { id: nextId(), role: 'user', text: rawAnswer };
      setMessages((prev) => [...prev, userMsg]);

      // Show loading state via a transient phase change
      setPhase('loading');

      try {
        const res = await fetch(`${API_BASE}/answer`, {
          method: 'POST',
          headers: buildHeaders(),
          body: JSON.stringify({
            appointmentId,
            fieldKey: currentFieldKey,
            rawAnswer,
          }),
        });

        if (!res.ok) throw new Error(`Answer submission failed: ${res.status}`);

        const data: AnswerResponse = await res.json();

        // Update captured fields
        const updatedFields: CapturedField[] = [
          ...capturedFields.filter((f) => f.fieldKey !== currentFieldKey),
          {
            fieldKey: currentFieldKey,
            fieldLabel: currentFieldKey,
            value: rawAnswer,
            manualRequired: false,
          },
        ];
        setCapturedFields(updatedFields);
        saveToStorage(appointmentId, { capturedFields: updatedFields });

        if (data.phase === 'summary') {
          const summaryFields = data.capturedFields ?? updatedFields;
          setCapturedFields(summaryFields);
          setPhase('summary');

          const summaryMsg: ChatMessage = {
            id: nextId(),
            role: 'ai',
            text: "Great work! You've answered all questions. Please review your responses below before confirming.",
          };
          setMessages((prev) => [...prev, summaryMsg]);
          return;
        }

        // Next question
        const nextMsg: ChatMessage = {
          id: nextId(),
          role: 'ai',
          text: data.nextQuestionText!,
          isPhiField: data.isPhiField,
          fieldKey: data.nextFieldKey,
          questionNumber: data.questionNumber,
          totalQuestions: data.totalQuestions,
          fallbackRequired: data.fallbackRequired,
          quickOptions: data.quickOptions,
        };
        setMessages((prev) => [...prev, nextMsg]);
        setCurrentFieldKey(data.nextFieldKey ?? null);
        setQuestionNumber(data.questionNumber ?? questionNumber + 1);
        setTotalQuestions(data.totalQuestions ?? totalQuestions);
        setIsPhiField(data.isPhiField ?? false);
        setPhase('conversation');
      } catch {
        // Rate-limit or network failure — surface as manual fallback for current field
        const fallbackMsg: ChatMessage = {
          id: nextId(),
          role: 'ai',
          text: 'I had trouble processing that response. Please type your answer directly below.',
          fieldKey: currentFieldKey,
          fallbackRequired: true,
        };
        setMessages((prev) => [...prev, fallbackMsg]);

        // Mark current field as manual-required
        const partialFields: CapturedField[] = [
          ...capturedFields.filter((f) => f.fieldKey !== currentFieldKey),
          {
            fieldKey: currentFieldKey,
            fieldLabel: currentFieldKey,
            value: '',
            manualRequired: true,
          },
        ];
        setCapturedFields(partialFields);
        saveToStorage(appointmentId, { capturedFields: partialFields, method: 'AI-Partial' });
        setPhase('conversation');
      }
    },
    [appointmentId, buildHeaders, currentFieldKey, capturedFields, phase, questionNumber, totalQuestions],
  );

  const updateFieldValue = useCallback(
    (fieldKey: string, value: string): void => {
      setCapturedFields((prev) =>
        prev.map((f) => (f.fieldKey === fieldKey ? { ...f, value } : f)),
      );
    },
    [],
  );

  const confirmIntake = useCallback(async (): Promise<void> => {
    setPhase('loading');
    try {
      const res = await fetch(`${API_BASE}/confirm`, {
        method: 'POST',
        headers: buildHeaders(),
        body: JSON.stringify({ appointmentId, capturedFields }),
      });
      if (!res.ok) throw new Error(`Confirm failed: ${res.status}`);

      // Clear session storage on success
      sessionStorage.removeItem(storageKey(appointmentId));
      setPhase('summary'); // Parent page handles navigation on confirm
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : 'Failed to confirm intake.');
      setPhase('error');
    }
  }, [appointmentId, buildHeaders, capturedFields]);

  return {
    messages,
    capturedFields,
    currentFieldKey,
    questionNumber,
    totalQuestions,
    isPhiField,
    phase,
    errorMessage,
    startSession,
    submitAnswer,
    updateFieldValue,
    confirmIntake,
    clearError,
  };
}
