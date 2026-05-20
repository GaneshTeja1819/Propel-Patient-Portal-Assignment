import { useCallback, useState } from 'react';
import { useToast } from './useToast';

export interface BookingPayload {
  slotId: string;
  preferredSlotId?: string;
  insuranceProvider?: string;
  insuranceId?: string;
}

export interface BookingResult {
  appointmentId: string;
  insuranceValidationStatus: string;
  slotId: string;
}

/**
 * Handles appointment booking with error recovery.
 *
 * AC-004: On HTTP 409 → show error toast "Slot no longer available";
 * on HTTP 200 → return appointment ID.
 */
export function useBooking() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { addToast } = useToast();

  const book = useCallback(
    async (payload: BookingPayload): Promise<BookingResult | null> => {
      setIsSubmitting(true);
      try {
        const response = await fetch('/api/v1/appointments', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({
            slotId: payload.slotId,
            preferredSlotId: payload.preferredSlotId || null,
            insuranceProvider: payload.insuranceProvider || null,
            insuranceId: payload.insuranceId || null,
          }),
        });

        // AC-004: Handle slot conflict
        if (response.status === 409) {
          // Show error toast within 2 seconds
          addToast('Slot no longer available — try another time', 'error', 5000);

          // Force a refresh of slots by calling the hook's refetch
          // (Will be integrated with React Query once installed)
          return null;
        }

        if (!response.ok) {
          const error = await response.text();
          addToast(`Booking failed: ${error}`, 'error', 5000);
          return null;
        }

        const data = await response.json();
        addToast('Booking confirmed! Check your email for details.', 'success', 5000);

        return {
          appointmentId: data.appointmentId,
          insuranceValidationStatus: data.insuranceValidationStatus,
          slotId: data.slotId,
        };
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Booking failed';
        addToast(message, 'error', 5000);
        return null;
      } finally {
        setIsSubmitting(false);
      }
    },
    [addToast]
  );

  return { book, isSubmitting };
}
