import { useCallback, useState } from 'react';

export type QueueStatus = 'Booked' | 'Arrived' | 'WalkIn' | 'Cancelled';

export interface QueueEntry {
  id: string;
  scheduledTime: string;
  patientName: string;
  dateOfBirth: string;
  sex: string;
  phone: string;
  appointmentType: string;
  intakeComplete: boolean;
  status: QueueStatus;
  queueIndex: number;
}

interface UseQueueState {
  entries: QueueEntry[];
  isLoading: boolean;
  fetchError: string | null;
}

const INITIAL_STATE: UseQueueState = {
  entries: [],
  isLoading: false,
  fetchError: null,
};

export function useQueue() {
  const [state, setState] = useState<UseQueueState>(INITIAL_STATE);
  const [arrivedConflictId, setArrivedConflictId] = useState<string | null>(null);

  const fetchQueue = useCallback(async (): Promise<void> => {
    setState((prev) => ({ ...prev, isLoading: true, fetchError: null }));

    try {
      const response = await fetch('/api/v1/queue/today', {
        credentials: 'include',
      });

      if (!response.ok) {
        throw new Error('Failed to load queue');
      }

      const data = (await response.json()) as QueueEntry[];
      setState({ entries: data, isLoading: false, fetchError: null });
    } catch {
      setState((prev) => ({
        ...prev,
        isLoading: false,
        fetchError: "Unable to load today's queue. Please refresh.",
      }));
    }
  }, []);

  const markArrived = useCallback(async (id: string): Promise<void> => {
    // Optimistic update
    setState((prev) => ({
      ...prev,
      entries: prev.entries.map((e) =>
        e.id === id ? { ...e, status: 'Arrived' as QueueStatus } : e
      ),
    }));
    setArrivedConflictId(null);

    try {
      const response = await fetch(`/api/v1/queue/${encodeURIComponent(id)}/arrive`, {
        method: 'PATCH',
        credentials: 'include',
      });

      if (response.status === 409) {
        // Revert optimistic update — patient already arrived on another client
        setState((prev) => ({
          ...prev,
          entries: prev.entries.map((e) =>
            e.id === id ? { ...e, status: 'Booked' as QueueStatus } : e
          ),
        }));
        setArrivedConflictId(id);
        return;
      }

      if (!response.ok) {
        // Revert on server error
        setState((prev) => ({
          ...prev,
          entries: prev.entries.map((e) =>
            e.id === id ? { ...e, status: 'Booked' as QueueStatus } : e
          ),
        }));
      }
    } catch {
      // Revert on network error
      setState((prev) => ({
        ...prev,
        entries: prev.entries.map((e) =>
          e.id === id ? { ...e, status: 'Booked' as QueueStatus } : e
        ),
      }));
    }
  }, []);

  const removeEntry = useCallback(async (id: string, reason: string): Promise<boolean> => {
    try {
      const url = `/api/v1/queue/${encodeURIComponent(id)}?reason=${encodeURIComponent(reason)}`;
      const response = await fetch(url, {
        method: 'DELETE',
        credentials: 'include',
      });

      if (response.ok) {
        setState((prev) => ({
          ...prev,
          entries: prev.entries.filter((e) => e.id !== id),
        }));
        return true;
      }

      return false;
    } catch {
      return false;
    }
  }, []);

  const reorder = useCallback(async (id: string, newIndex: number): Promise<void> => {
    setState((prev) => {
      const entries = [...prev.entries];
      const fromIndex = entries.findIndex((e) => e.id === id);
      if (fromIndex === -1) return prev;
      const [moved] = entries.splice(fromIndex, 1);
      entries.splice(newIndex, 0, moved);
      return { ...prev, entries: entries.map((e, i) => ({ ...e, queueIndex: i })) };
    });

    try {
      await fetch(`/api/v1/queue/${encodeURIComponent(id)}/reorder`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ newIndex }),
      });
    } catch {
      // Reorder is best-effort; a subsequent fetchQueue will correct any drift
    }
  }, []);

  const clearArrivedConflict = useCallback(() => setArrivedConflictId(null), []);

  return {
    entries: state.entries,
    isLoading: state.isLoading,
    fetchError: state.fetchError,
    arrivedConflictId,
    fetchQueue,
    markArrived,
    removeEntry,
    reorder,
    clearArrivedConflict,
  };
}
