import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Toast } from '../components/common/Toast';
import { useCalendarSync } from '../hooks/useCalendarSync';
import { useToast } from '../hooks/useToast';
import styles from './CalendarSyncPage.module.css';

/**
 * SCR-016 Calendar Sync OAuth consent and callback screen.
 *
 * AC-001: Shows Google and Outlook OAuth consent buttons; redirects to provider.
 * AC-004: On sync failure, renders non-blocking amber Toast; appointment confirmation unobstructed.
 * AC-005: On denied consent, shows advisory "Calendar sync is optional"; no error Toast.
 */
export default function CalendarSyncPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { status, message, initiateOAuth, handleCallback } = useCalendarSync();
  const { toasts, addToast, removeToast } = useToast();

  // Appointment ID and provider may be passed via query params (from BookingPage CTA)
  const appointmentId = searchParams.get('appointmentId') ?? '';
  const provider      = searchParams.get('provider') ?? '';
  const code          = searchParams.get('code');
  const error         = searchParams.get('error');

  // Handle OAuth callback automatically when code/error params are present in the URL
  useEffect(() => {
    if (!provider || !appointmentId) return;

    // error=access_denied means user denied consent (AC-005)
    const authCode = error === 'access_denied' ? null : (code ?? null);

    if (authCode !== undefined && provider && appointmentId) {
      handleCallback(authCode, provider, appointmentId);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Show amber Toast when sync fails (AC-004, UXR-604)
  useEffect(() => {
    if (status === 'failed') {
      addToast(message, 'warning', 0); // duration=0: no auto-dismiss (dismissible by patient)
    }
  }, [status, message, addToast]);

  const handleConnect = (selectedProvider: 'Google' | 'Outlook') => {
    if (!appointmentId) return;
    initiateOAuth(selectedProvider, appointmentId);
  };

  return (
    <main className={styles.page}>
      <div className={styles.container}>
        <h1 className={styles.title}>Sync to Your Calendar</h1>
        <p className={styles.subtitle}>
          Add your appointment to Google or Outlook Calendar. This step is optional.
        </p>

        {/* Callback result states */}
        {status === 'synced' && (
          <div className={styles.successCard} role="status" aria-live="polite">
            <span className={styles.successIcon} aria-hidden="true">✓</span>
            <p className={styles.successMessage}>{message}</p>
          </div>
        )}

        {status === 'denied' && (
          <p className={styles.advisory} role="status" aria-live="polite">
            {message}
          </p>
        )}

        {/* OAuth consent buttons — shown when not yet in a terminal state */}
        {status !== 'synced' && status !== 'loading' && (
          <div className={styles.providerGrid}>
            <button
              className={styles.providerButton}
              onClick={() => handleConnect('Google')}
              disabled={!appointmentId}
              aria-label="Connect Google Calendar"
            >
              <svg className={styles.providerIcon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              Connect Google Calendar
            </button>

            <button
              className={styles.providerButton}
              onClick={() => handleConnect('Outlook')}
              disabled={!appointmentId}
              aria-label="Connect Outlook Calendar"
            >
              <svg className={styles.providerIcon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <path fill="#0078D4" d="M7 6h10v2H7V6zm0 4h10v2H7v-2zm0 4h7v2H7v-2zM3 4v16l4-2h14V4H3zm16 12H7.5L5 17.5V5h14v11z"/>
              </svg>
              Connect Outlook Calendar
            </button>
          </div>
        )}

        {status === 'loading' && (
          <p className={styles.loading} role="status" aria-live="polite">
            Connecting to calendar…
          </p>
        )}

        <div className={styles.actions}>
          {!appointmentId && (
            <p className={styles.missingId}>
              No appointment selected. Please complete a booking first.
            </p>
          )}
          <button
            className={styles.skipButton}
            onClick={() => navigate('/booking')}
            aria-label="Skip calendar sync and return to booking"
          >
            Skip for now
          </button>
        </div>
      </div>

      {/* Amber Toast for sync failure — non-blocking, appointment confirmation unobstructed (AC-004, UXR-604) */}
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          id={toast.id}
          message={toast.message}
          type={toast.type}
          duration={toast.duration}
          onDismiss={removeToast}
        />
      ))}
    </main>
  );
}
