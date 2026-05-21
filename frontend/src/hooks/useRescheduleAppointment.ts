import { useCallback, useState } from 'react';

export interface RescheduleAppointmentResult {
  status: 'success' | 'conflict' | 'error';
  message?: string;
}

export function useRescheduleAppointment() {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const rescheduleAppointment = useCallback(
    async (appointmentId: string, newSlotId: string): Promise<RescheduleAppointmentResult> => {
      setIsSubmitting(true);

      try {
        const response = await fetch(
          `/api/v1/appointments/${encodeURIComponent(appointmentId)}/reschedule`,
          {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ newSlotId }),
          }
        );

        if (response.status === 409) {
          return {
            status: 'conflict',
            message: 'Slot no longer available',
          };
        }

        if (!response.ok) {
          return {
            status: 'error',
            message: 'Unable to reschedule appointment. Please try again.',
          };
        }

        return { status: 'success' };
      } catch {
        return {
          status: 'error',
          message: 'Unable to reschedule appointment. Please check your connection and retry.',
        };
      } finally {
        setIsSubmitting(false);
      }
    },
    []
  );

  return { rescheduleAppointment, isSubmitting };
}
