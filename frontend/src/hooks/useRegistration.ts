import { useCallback, useState } from 'react';

export interface RegistrationPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

interface UseRegistrationResult {
  isSubmitting: boolean;
  submissionError: string | null;
  registerUser: (payload: RegistrationPayload) => Promise<boolean>;
}

export function useRegistration(): UseRegistrationResult {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submissionError, setSubmissionError] = useState<string | null>(null);

  const registerUser = useCallback(async (payload: RegistrationPayload): Promise<boolean> => {
    setIsSubmitting(true);
    setSubmissionError(null);

    try {
      const response = await fetch('/api/v1/auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        return true;
      }

      if (response.status === 409) {
        setSubmissionError('Email address already in use');
        return false;
      }

      if (response.status >= 500) {
        setSubmissionError('Registration failed - please try again');
        return false;
      }

      setSubmissionError('Unable to complete registration. Please review your details.');
      return false;
    } catch {
      setSubmissionError('Registration failed - please try again');
      return false;
    } finally {
      setIsSubmitting(false);
    }
  }, []);

  return {
    isSubmitting,
    submissionError,
    registerUser,
  };
}
