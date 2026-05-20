/**
 * useManualIntake.ts — Submit manual intake form to the backend (US_019).
 *
 * Calls POST /api/v1/intake/confirm with method="Manual".
 * Converts the flat form values map into the CapturedField array
 * shape expected by the backend.
 */
import { useCallback, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import type { CapturedField } from '../types/intake';

const API_BASE = '/api/v1/intake';

/** Human-readable labels for each field key (used as fieldLabel in the payload). */
const FIELD_LABELS: Record<string, string> = {
  'chief-complaint': 'Chief complaint',
  'symptom-duration': 'Symptom duration',
  'pain-scale': 'Pain / severity (0–10)',
  medications: 'Current medications',
  'med-types': 'Medication categories',
  allergies: 'Known allergies',
  'allergy-sev': 'Allergy severity',
  smoking: 'Smoking status',
  alcohol: 'Alcohol consumption',
  exercise: 'Exercise frequency',
  notes: 'Additional notes',
};

export interface UseManualIntakeReturn {
  isSubmitting: boolean;
  submitError: string | null;
  submitManualIntake: (appointmentId: string, values: Record<string, string>) => Promise<void>;
  clearSubmitError: () => void;
}

export function useManualIntake(): UseManualIntakeReturn {
  const { token } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const buildHeaders = useCallback((): Record<string, string> => {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
  }, [token]);

  const submitManualIntake = useCallback(
    async (appointmentId: string, values: Record<string, string>): Promise<void> => {
      setIsSubmitting(true);
      setSubmitError(null);

      const capturedFields: CapturedField[] = Object.entries(values)
        .filter(([, value]) => value !== '' && value !== undefined)
        .map(([fieldKey, value]) => ({
          fieldKey,
          fieldLabel: FIELD_LABELS[fieldKey] ?? fieldKey,
          value,
          manualRequired: false,
        }));

      try {
        const res = await fetch(`${API_BASE}/confirm`, {
          method: 'POST',
          headers: buildHeaders(),
          body: JSON.stringify({
            appointmentId,
            capturedFields,
            method: 'Manual',
          }),
        });

        if (!res.ok) {
          const text = await res.text().catch(() => '');
          throw new Error(text || `Submission failed (${res.status}). Please try again.`);
        }
      } catch (err) {
        const message =
          err instanceof Error ? err.message : 'Failed to submit intake. Please try again.';
        setSubmitError(message);
        throw err;
      } finally {
        setIsSubmitting(false);
      }
    },
    [buildHeaders],
  );

  const clearSubmitError = useCallback(() => setSubmitError(null), []);

  return { isSubmitting, submitError, submitManualIntake, clearSubmitError };
}
