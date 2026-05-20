/**
 * useConflicts.ts — Hook for loading and resolving data conflicts (US_028, UXR-404).
 *
 * Fetches from:
 *   GET  /api/v1/patients/{patientId}/conflicts       — list all conflicts (StaffPolicy)
 *   PATCH /api/v1/conflicts/{id}/resolve              — resolve with authoritative value (StaffPolicy)
 *   PATCH /api/v1/conflicts/{id}/mark-reviewed        — mark as reviewed without resolving (StaffPolicy)
 *
 * Security: uses HttpOnly cookie credentials (credentials: 'include').
 *           Server enforces StaffPolicy (HTTP 403 for non-Staff).
 *           HTTP 409 on resolve/mark-reviewed means the conflict was already actioned concurrently.
 */
import { useState, useEffect, useCallback } from 'react';
import { DataConflictDto, ResolveConflictPayload } from '../types/conflict';

const API_CONFLICTS_BASE = '/api/v1/conflicts';

export type ConflictLoadState = 'idle' | 'loading' | 'success' | 'error' | 'forbidden';

interface UseConflictsReturn {
  conflicts: DataConflictDto[];
  loadState: ConflictLoadState;
  errorMessage: string | null;
  /** Per-conflict inline error keyed by conflict ID (e.g. HTTP 409). */
  conflictErrors: Record<string, string>;
  resolveConflict: (conflictId: string, payload: ResolveConflictPayload) => Promise<void>;
  markReviewed: (conflictId: string) => Promise<void>;
  refetch: () => void;
}

export function useConflicts(patientId: string): UseConflictsReturn {
  const [conflicts, setConflicts] = useState<DataConflictDto[]>([]);
  const [loadState, setLoadState] = useState<ConflictLoadState>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [conflictErrors, setConflictErrors] = useState<Record<string, string>>({});
  const [tick, setTick] = useState(0);

  const refetch = useCallback(() => setTick((t) => t + 1), []);

  function setConflictError(conflictId: string, message: string) {
    setConflictErrors((prev) => ({ ...prev, [conflictId]: message }));
  }

  function clearConflictError(conflictId: string) {
    setConflictErrors((prev) => {
      const next = { ...prev };
      delete next[conflictId];
      return next;
    });
  }

  useEffect(() => {
    if (!patientId) return;

    let cancelled = false;

    setLoadState('loading');
    setErrorMessage(null);

    fetch(`/api/v1/patients/${encodeURIComponent(patientId)}/conflicts`, {
      credentials: 'include',
    })
      .then((res) => {
        if (cancelled) return;

        if (res.status === 403) {
          setLoadState('forbidden');
          setErrorMessage('You do not have permission to view conflicts for this patient.');
          return;
        }
        if (!res.ok) {
          setLoadState('error');
          setErrorMessage('Unable to load conflicts. Please try again.');
          return;
        }

        return res.json();
      })
      .then((data: DataConflictDto[] | undefined) => {
        if (cancelled || data === undefined) return;
        setConflicts(data);
        setLoadState('success');
      })
      .catch(() => {
        if (cancelled) return;
        setLoadState('error');
        setErrorMessage('Network error loading conflicts.');
      });

    return () => {
      cancelled = true;
    };
  }, [patientId, tick]);

  const resolveConflict = useCallback(
    async (conflictId: string, payload: ResolveConflictPayload): Promise<void> => {
      clearConflictError(conflictId);

      const res = await fetch(`${API_CONFLICTS_BASE}/${encodeURIComponent(conflictId)}/resolve`, {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (res.status === 409) {
        setConflictError(conflictId, 'Conflict already resolved by another user.');
        refetch();
        return;
      }
      if (res.status === 403) {
        setConflictError(conflictId, 'You do not have permission to resolve this conflict.');
        return;
      }
      if (!res.ok) {
        setConflictError(conflictId, 'Failed to resolve conflict. Please try again.');
        return;
      }

      refetch();
    },
    [refetch],
  );

  const markReviewed = useCallback(
    async (conflictId: string): Promise<void> => {
      clearConflictError(conflictId);

      const res = await fetch(
        `${API_CONFLICTS_BASE}/${encodeURIComponent(conflictId)}/mark-reviewed`,
        {
          method: 'PATCH',
          credentials: 'include',
        },
      );

      if (res.status === 409) {
        setConflictError(conflictId, 'Conflict already reviewed.');
        refetch();
        return;
      }
      if (res.status === 403) {
        setConflictError(conflictId, 'You do not have permission to mark this conflict as reviewed.');
        return;
      }
      if (!res.ok) {
        setConflictError(conflictId, 'Failed to mark conflict as reviewed. Please try again.');
        return;
      }

      refetch();
    },
    [refetch],
  );

  return {
    conflicts,
    loadState,
    errorMessage,
    conflictErrors,
    resolveConflict,
    markReviewed,
    refetch,
  };
}
