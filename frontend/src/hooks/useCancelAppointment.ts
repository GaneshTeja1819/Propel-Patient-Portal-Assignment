import { useCallback, useState } from 'react';

export interface CancelAppointmentResult {
  success: boolean;
  message?: string;
}

export function useCancelAppointment() {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const cancelAppointment = useCallback(async (appointmentId: string): Promise<CancelAppointmentResult> => {
    setIsSubmitting(true);

    try {
      const response = await fetch(`/api/v1/appointments/${encodeURIComponent(appointmentId)}/cancel`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
      });

      if (!response.ok) {
        return {
          success: false,
          message: 'Unable to cancel appointment. Please try again.',
        };
      }

      return { success: true };
    } catch {
      return {
        success: false,
        message: 'Unable to cancel appointment. Please check your connection and retry.',
      };
    } finally {
      setIsSubmitting(false);
    }
  }, []);

  return { cancelAppointment, isSubmitting };
}
