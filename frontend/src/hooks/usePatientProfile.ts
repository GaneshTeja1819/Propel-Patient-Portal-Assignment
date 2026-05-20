/**
 * usePatientProfile.ts — Hook for loading the 360° patient profile (US_027, AC-001–AC-005).
 *
 * Fetches from:
 *   GET /api/v1/profile/me              — Patient's own profile (PatientPolicy)
 *   GET /api/v1/profile/{patientId}     — Any patient profile (StaffPolicy)
 *
 * Pass patientId = 'me' for the self-view endpoint.
 *
 * Security: uses HttpOnly cookie credentials (credentials: 'include').
 *           Server enforces role-based access (HTTP 403 on policy mismatch).
 */
import { useState, useEffect, useCallback } from 'react';
import { PatientProfileData } from '../types/profile';

const API_BASE = '/api/v1/profile';

export type ProfileLoadState = 'idle' | 'loading' | 'success' | 'error' | 'forbidden';

interface UsePatientProfileReturn {
  data: PatientProfileData | null;
  loadState: ProfileLoadState;
  errorMessage: string | null;
  refetch: () => void;
}

export function usePatientProfile(patientId: string | 'me'): UsePatientProfileReturn {
  const [data, setData] = useState<PatientProfileData | null>(null);
  const [loadState, setLoadState] = useState<ProfileLoadState>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  const refetch = useCallback(() => setTick((t) => t + 1), []);

  useEffect(() => {
    if (!patientId) return;

    let cancelled = false;

    const endpoint = patientId === 'me'
      ? `${API_BASE}/me`
      : `${API_BASE}/${encodeURIComponent(patientId)}`;

    setLoadState('loading');
    setErrorMessage(null);

    fetch(endpoint, { credentials: 'include' })
      .then((res) => {
        if (cancelled) return;

        if (res.status === 403) {
          setLoadState('forbidden');
          setErrorMessage('You do not have permission to view this profile.');
          return;
        }
        if (!res.ok) {
          setLoadState('error');
          setErrorMessage('Unable to load profile. Please try again.');
          return;
        }
        return res.json() as Promise<PatientProfileData>;
      })
      .then((json) => {
        if (cancelled || !json) return;
        setData(json);
        setLoadState('success');
      })
      .catch(() => {
        if (cancelled) return;
        setLoadState('error');
        setErrorMessage('Network error — please check your connection.');
      });

    return () => {
      cancelled = true;
    };
  }, [patientId, tick]);

  return { data, loadState, errorMessage, refetch };
}
