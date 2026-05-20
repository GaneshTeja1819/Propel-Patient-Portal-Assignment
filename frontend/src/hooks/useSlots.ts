import { useCallback, useEffect, useState } from 'react';

export interface AppointmentSlot {
  id: string;
  date: string;
  time: string;
  providerId: string;
  isBooked: boolean;
  isPreferred: boolean;
}

export interface UseSlotsResult {
  slots: AppointmentSlot[];
  isLoading: boolean;
  isError: boolean;
  isCachedData: boolean;
  refetch: () => Promise<void>;
}

/**
 * Hook to fetch available appointment slots with 5-second auto-refetch.
 * Polls the /api/v1/slots endpoint matching Redis TTL for real-time updates.
 *
 * AC-001: data within 500ms from cache; slot cards show date, time, status
 * AC-002: state changes reflected within 5s without full page reload
 */
export function useSlots(date: string): UseSlotsResult {
  const [slots, setSlots] = useState<AppointmentSlot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);
  const [isCachedData, setIsCachedData] = useState(false);

  const fetchSlots = useCallback(async () => {
    try {
      setIsLoading(true);
      const response = await fetch(`/api/v1/slots?date=${encodeURIComponent(date)}`, {
        credentials: 'include',
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch slots: ${response.statusText}`);
      }

      const json = (await response.json()) as { data?: AppointmentSlot[]; cached?: boolean };
      setSlots(json.data ?? []);
      setIsCachedData(json.cached ?? false);
      setIsError(false);
    } catch {
      setIsError(true);
    } finally {
      setIsLoading(false);
    }
  }, [date]);

  useEffect(() => {
    fetchSlots();
    const intervalId = setInterval(fetchSlots, 5000); // 5s polling per TR-005
    return () => clearInterval(intervalId);
  }, [fetchSlots]);

  return {
    slots,
    isLoading,
    isError,
    isCachedData,
    refetch: fetchSlots,
  };
}
