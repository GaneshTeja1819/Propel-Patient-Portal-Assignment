/**
 * useExtractionStatus.ts — AI extraction status polling hook (US_026, AC-004).
 *
 * Polls GET /api/v1/documents/{id}/extraction-status at a 5 s interval after
 * a document upload succeeds. Stops automatically on terminal state (completed
 * / failed) or after a 5-minute timeout. Exposes retry() for AC-004 retry CTA.
 *
 * Auth: relies on __Host-access HttpOnly cookie (credentials: 'include').
 *
 * Returns:
 *   status      — 'idle' | 'processing' | 'completed' | 'failed' | 'timeout'
 *   failureNote — human-readable retry-error string, null when no retry error
 *   retry       — calls POST …/retry-extraction; on success restarts polling
 */
import { useState, useEffect, useRef, useCallback } from 'react';

export type ExtractionStatus = 'idle' | 'processing' | 'completed' | 'failed' | 'timeout';

interface UseExtractionStatusReturn {
  status: ExtractionStatus;
  failureNote: string | null;
  retry: () => void;
}

const POLL_INTERVAL_MS = 5_000;
const POLL_TIMEOUT_MS = 300_000; // 5 min — AC-004 edge case

export function useExtractionStatus(documentId: string | null): UseExtractionStatusReturn {
  const [status, setStatus] = useState<ExtractionStatus>('idle');
  const [failureNote, setFailureNote] = useState<string | null>(null);
  const intervalRef = useRef<number | null>(null);
  const timeoutRef = useRef<number | null>(null);

  const stopPolling = useCallback(() => {
    if (intervalRef.current !== null) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    if (timeoutRef.current !== null) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
  }, []);

  // Accept id as parameter (not a closure) so this stays stable across renders.
  const startPolling = useCallback(
    (id: string) => {
      stopPolling();
      setStatus('processing');
      setFailureNote(null);

      const poll = () => {
        fetch(`/api/v1/documents/${id}/extraction-status`, { credentials: 'include' })
          .then((res) => {
            if (!res.ok) return; // transient server error — keep polling
            return res.json() as Promise<{ extractionStatus?: string }>;
          })
          .then((data) => {
            if (!data) return;
            const s = (data.extractionStatus ?? '').toLowerCase();
            if (s === 'completed') {
              setStatus('completed');
              stopPolling();
            } else if (s === 'failed') {
              setStatus('failed');
              stopPolling();
            }
            // 'processing' or unknown → continue polling
          })
          .catch(() => {
            // network error — keep polling; do not surface as a state change
          });
      };

      poll(); // immediate first poll on mount / retry
      intervalRef.current = window.setInterval(poll, POLL_INTERVAL_MS);

      // Graceful timeout after 5 min (AC-004 edge case: job stuck)
      timeoutRef.current = window.setTimeout(() => {
        stopPolling();
        setStatus((prev) => (prev === 'processing' ? 'timeout' : prev));
      }, POLL_TIMEOUT_MS);
    },
    [stopPolling],
  );

  const retry = useCallback(() => {
    if (documentId === null) return;
    setFailureNote(null);
    fetch(`/api/v1/documents/${documentId}/retry-extraction`, {
      method: 'POST',
      credentials: 'include',
    })
      .then((res) => {
        if (res.status === 409) return; // already processing — no-op (AC-004 edge case)
        if (!res.ok) {
          setFailureNote('Retry failed — contact support');
          return;
        }
        startPolling(documentId);
      })
      .catch(() => {
        setFailureNote('Retry failed — contact support');
      });
  }, [documentId, startPolling]);

  useEffect(() => {
    if (documentId === null) {
      setStatus('idle');
      stopPolling();
      return;
    }
    startPolling(documentId);
    return stopPolling; // cleanup: clear interval + timeout on unmount or documentId change
  }, [documentId, startPolling, stopPolling]);

  return { status, failureNote, retry };
}
