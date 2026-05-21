import { useCallback, useState } from 'react';

export interface InsuranceValidationResult {
  status: 'validated' | 'not-recognised' | null;
  isLoading: boolean;
}

/**
 * Validates insurance provider and ID against backend.
 * Errors default to null (no badge) without blocking booking (AC-003).
 */
export function useInsuranceValidation() {
  const [status, setStatus] = useState<'validated' | 'not-recognised' | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const validate = useCallback(async (provider: string, id: string) => {
    if (!provider || !id) {
      setStatus(null);
      return;
    }

    setIsLoading(true);
    try {
      const response = await fetch('/api/v1/insurance/validate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ insuranceProvider: provider, insuranceId: id }),
      });

      if (!response.ok) {
        // Default to null (no badge) on error
        setStatus(null);
        return;
      }

      const data = await response.json();
      setStatus(data.isValid ? 'validated' : 'not-recognised');
    } catch (error) {
      // Default to null on exception (AC-003)
      setStatus(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { status, isLoading, validate };
}
