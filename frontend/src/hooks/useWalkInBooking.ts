import { useCallback, useState } from 'react';

export interface WalkInBookingPayload {
  patientId?: string;
  anonymousPatient?: {
    firstName: string;
    lastName: string;
    dateOfBirth: string;
    sex?: string;
    phone?: string;
  };
  slotId?: string;
  appointmentType: string;
  providerId: string;
}

export interface WalkInBookingResult {
  appointmentId: string;
}

export interface CreatePatientAccountPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  appointmentId: string;
}

interface UseWalkInBookingState {
  isBooking: boolean;
  bookingError: string | null;
  bookingResult: WalkInBookingResult | null;
  isCreatingAccount: boolean;
  accountError: string | null;
  accountCreated: boolean;
  isDuplicateEmail: boolean;
}

const INITIAL_STATE: UseWalkInBookingState = {
  isBooking: false,
  bookingError: null,
  bookingResult: null,
  isCreatingAccount: false,
  accountError: null,
  accountCreated: false,
  isDuplicateEmail: false,
};

export function useWalkInBooking() {
  const [state, setState] = useState<UseWalkInBookingState>(INITIAL_STATE);

  const bookWalkIn = useCallback(
    async (payload: WalkInBookingPayload): Promise<WalkInBookingResult | null> => {
      setState((prev) => ({ ...prev, isBooking: true, bookingError: null }));

      try {
        const response = await fetch('/api/v1/appointments/walk-in', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify(payload),
        });

        if (response.ok) {
          const result = (await response.json()) as WalkInBookingResult;
          setState((prev) => ({ ...prev, isBooking: false, bookingResult: result }));
          return result;
        }

        if (response.status === 409) {
          setState((prev) => ({
            ...prev,
            isBooking: false,
            bookingError: 'Slot no longer available. Please select a different slot.',
          }));
          return null;
        }

        setState((prev) => ({
          ...prev,
          isBooking: false,
          bookingError: 'Booking failed. Please try again.',
        }));
        return null;
      } catch {
        setState((prev) => ({
          ...prev,
          isBooking: false,
          bookingError: 'Booking failed. Please try again.',
        }));
        return null;
      }
    },
    [],
  );

  const createAccount = useCallback(
    async (payload: CreatePatientAccountPayload): Promise<boolean> => {
      setState((prev) => ({
        ...prev,
        isCreatingAccount: true,
        accountError: null,
        isDuplicateEmail: false,
      }));

      try {
        const response = await fetch('/api/v1/auth/register', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            email: payload.email,
            password: payload.password,
            firstName: payload.firstName,
            lastName: payload.lastName,
            linkedAppointmentId: payload.appointmentId,
          }),
        });

        if (response.ok) {
          setState((prev) => ({ ...prev, isCreatingAccount: false, accountCreated: true }));
          return true;
        }

        if (response.status === 409) {
          setState((prev) => ({
            ...prev,
            isCreatingAccount: false,
            isDuplicateEmail: true,
            accountError: 'Email already registered — link to existing account?',
          }));
          return false;
        }

        setState((prev) => ({
          ...prev,
          isCreatingAccount: false,
          accountError: 'Account creation failed. Please try again.',
        }));
        return false;
      } catch {
        setState((prev) => ({
          ...prev,
          isCreatingAccount: false,
          accountError: 'Account creation failed. Please try again.',
        }));
        return false;
      }
    },
    [],
  );

  return {
    ...state,
    bookWalkIn,
    createAccount,
  };
}
