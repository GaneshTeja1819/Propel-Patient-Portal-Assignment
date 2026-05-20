/**
 * useCodeVerification.ts — Hook for SCR-014 Medical Code Verification (US_030).
 *
 * Responsibilities:
 *   - Fetch MedicalCodeSuggestion records for an encounter (GET /api/v1/code-suggestions)
 *   - Post code verification decisions (POST /api/v1/codes/verify)
 *   - Validate modified codes against reference data (GET /api/v1/codes/validate)
 *   - Track per-row decision state and the all-rejected condition (AC-005)
 *   - Redirect to / on HTTP 403 (OWASP A01 — access control, edge case)
 *
 * Security: JWT bearer token from AuthContext attached to every request.
 */
import { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type {
  MedicalCodeSuggestionDto,
  VerifyDecision,
  CodeValidateResponse,
} from '../types/coding';

const API_SUGGESTIONS = '/api/v1/code-suggestions';
const API_VERIFY      = '/api/v1/codes/verify';
const API_VALIDATE    = '/api/v1/codes/validate';

export interface UseCodeVerificationReturn {
  suggestions: MedicalCodeSuggestionDto[];
  loading: boolean;
  error: string | null;
  /** Per-row decision state. Key = suggestionId. */
  verifiedRows: Record<string, VerifyDecision>;
  /** Per-row inline error messages (422 / 409 etc.). Key = suggestionId. */
  rowErrors: Record<string, string>;
  /** True when every suggestion row is Rejected (AC-005). */
  allRejected: boolean;
  /** True when the encounter has been finalised for billing (edge case). */
  finalized: boolean;
  verify: (suggestionId: string, decision: VerifyDecision, verifiedCode?: string) => Promise<void>;
  validateCode: (code: string, codeType: string) => Promise<CodeValidateResponse>;
}

export function useCodeVerification(encounterId: string): UseCodeVerificationReturn {
  const { token } = useAuth();
  const navigate  = useNavigate();

  const [suggestions,  setSuggestions]  = useState<MedicalCodeSuggestionDto[]>([]);
  const [loading,      setLoading]      = useState(true);
  const [error,        setError]        = useState<string | null>(null);
  const [verifiedRows, setVerifiedRows] = useState<Record<string, VerifyDecision>>({});
  const [rowErrors,    setRowErrors]    = useState<Record<string, string>>({});
  const [finalized,    setFinalized]    = useState(false);

  // Stable auth header derived from token (avoids auth header in dep array).
  const buildHeaders = useCallback(
    (): Record<string, string> =>
      token
        ? { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }
        : { 'Content-Type': 'application/json' },
    [token],
  );

  // ── Fetch suggestions on mount ──────────────────────────────────────────────
  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(
          `${API_SUGGESTIONS}?encounterId=${encodeURIComponent(encounterId)}`,
          { credentials: 'include', headers: buildHeaders() },
        );

        if (res.status === 403) {
          navigate('/');
          return;
        }

        if (!res.ok) {
          if (!cancelled) setError('Failed to load code suggestions. Please refresh.');
          return;
        }

        const data = (await res.json()) as MedicalCodeSuggestionDto[];
        if (cancelled) return;

        setSuggestions(data);

        // Pre-populate verifiedRows from already-decided suggestions so that
        // refreshing the page shows outcome badges immediately.
        const preDecided: Record<string, VerifyDecision> = {};
        let allAlreadyDecided = data.length > 0;

        for (const s of data) {
          if (s.verifiedCode) {
            preDecided[s.id] = s.verifiedCode.decision;
          } else {
            allAlreadyDecided = false;
          }
        }

        setVerifiedRows(preDecided);

        // If every suggestion already has a VerifiedCode the encounter is finalised.
        if (allAlreadyDecided) setFinalized(true);
      } catch {
        if (!cancelled) setError('Network error. Please try again.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    load();
    return () => { cancelled = true; };
  }, [encounterId, navigate, buildHeaders]);

  // ── Verify a single code ────────────────────────────────────────────────────
  const verify = useCallback(
    async (suggestionId: string, decision: VerifyDecision, verifiedCode?: string) => {
      // Clear any previous row-level error before each attempt.
      setRowErrors(prev => ({ ...prev, [suggestionId]: '' }));

      try {
        const res = await fetch(API_VERIFY, {
          method: 'POST',
          credentials: 'include',
          headers: buildHeaders(),
          body: JSON.stringify({ suggestionId, decision, verifiedCode }),
        });

        if (res.status === 422) {
          const body = await res.json().catch(() => ({ message: 'Unprocessable entity.' })) as { message?: string };
          const msg  = body?.message ?? 'Invalid request.';
          if (msg.toLowerCase().includes('finalised')) {
            setFinalized(true);
          } else {
            setRowErrors(prev => ({ ...prev, [suggestionId]: msg }));
          }
          return;
        }

        if (res.status === 409) {
          setRowErrors(prev => ({ ...prev, [suggestionId]: 'This code has already been verified.' }));
          return;
        }

        if (!res.ok) {
          setRowErrors(prev => ({ ...prev, [suggestionId]: 'Verification failed. Please try again.' }));
          return;
        }

        setVerifiedRows(prev => ({ ...prev, [suggestionId]: decision }));
      } catch {
        setRowErrors(prev => ({ ...prev, [suggestionId]: 'Network error. Please try again.' }));
      }
    },
    [buildHeaders],
  );

  // ── Validate a code against the reference codeset ──────────────────────────
  const validateCode = useCallback(
    async (code: string, codeType: string): Promise<CodeValidateResponse> => {
      try {
        const params = new URLSearchParams({ code, type: codeType });
        const res = await fetch(`${API_VALIDATE}?${params}`, {
          credentials: 'include',
          headers: buildHeaders(),
        });
        if (!res.ok) return { valid: false };
        return res.json() as Promise<CodeValidateResponse>;
      } catch {
        return { valid: false };
      }
    },
    [buildHeaders],
  );

  const allRejected =
    suggestions.length > 0 &&
    suggestions.every(s => verifiedRows[s.id] === 'Rejected');

  return {
    suggestions,
    loading,
    error,
    verifiedRows,
    rowErrors,
    allRejected,
    finalized,
    verify,
    validateCode,
  };
}
