import { useCallback, useState } from 'react';

type SyncStatus = 'idle' | 'loading' | 'synced' | 'failed' | 'denied';

interface UseCalendarSyncReturn {
  status: SyncStatus;
  message: string;
  initiateOAuth: (provider: 'Google' | 'Outlook', appointmentId: string) => Promise<void>;
  handleCallback: (code: string | null, provider: string, appointmentId: string) => Promise<void>;
  reset: () => void;
}

const API_BASE = '/api/v1';

/**
 * Manages calendar OAuth initiation and callback for Google and Outlook.
 *
 * AC-001: initiateOAuth fetches the OAuth URL and redirects the patient.
 * AC-004: handleCallback surfaces a failed sync without blocking the appointment.
 * AC-005: handleCallback with null/empty code shows advisory and does not error.
 */
export function useCalendarSync(): UseCalendarSyncReturn {
  const [status, setStatus] = useState<SyncStatus>('idle');
  const [message, setMessage] = useState('');

  const initiateOAuth = useCallback(
    async (provider: 'Google' | 'Outlook', appointmentId: string) => {
      setStatus('loading');
      try {
        const redirectUri = `${window.location.origin}/calendar-sync?provider=${provider}&appointmentId=${appointmentId}`;
        const params = new URLSearchParams({ provider, redirectUri });
        const response = await fetch(`${API_BASE}/calendar-sync/oauth-url?${params.toString()}`, {
          credentials: 'include',
        });

        if (!response.ok) {
          throw new Error(`Failed to get OAuth URL: ${response.statusText}`);
        }

        const data: { url: string } = await response.json();
        // Redirect to OAuth consent page
        window.location.href = data.url;
      } catch (err) {
        setStatus('failed');
        setMessage("Calendar sync failed — your appointment is still confirmed.");
      }
    },
    []
  );

  const handleCallback = useCallback(
    async (code: string | null, provider: string, appointmentId: string) => {
      setStatus('loading');

      // AC-005: denied consent — show advisory without error
      if (!code) {
        setStatus('denied');
        setMessage('Calendar sync is optional — you can enable it later in Profile Settings.');
        return;
      }

      try {
        const response = await fetch(`${API_BASE}/calendar-sync/callback`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ code, provider, appointmentId }),
        });

        if (!response.ok) {
          throw new Error(`Callback failed: ${response.statusText}`);
        }

        const data: { synced: boolean; message: string } = await response.json();

        if (data.synced) {
          setStatus('synced');
          setMessage(data.message);
        } else {
          // AC-004: backend sync failure — amber Toast shown by page component
          setStatus('failed');
          setMessage(data.message);
        }
      } catch {
        setStatus('failed');
        setMessage("Calendar sync failed — your appointment is still confirmed.");
      }
    },
    []
  );

  const reset = useCallback(() => {
    setStatus('idle');
    setMessage('');
  }, []);

  return { status, message, initiateOAuth, handleCallback, reset };
}
