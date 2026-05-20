import { useRef, useState, useEffect } from 'react';
import styles from './PDFConfirmationStatus.module.css';

type ConfirmationStatus = 'Queued' | 'Processing' | 'Sent';

interface PDFConfirmationStatusProps {
  appointmentId: string;
}

interface ConfirmationStatusResponse {
  status?: string;
}

/**
 * UXR-504 inline progress indicator for confirmation PDF generation.
 * Polls status every 3s and stops automatically once Sent.
 */
export function PDFConfirmationStatus({ appointmentId }: PDFConfirmationStatusProps) {
  const startedAtRef = useRef<number>(Date.now());
  const [elapsedMs, setElapsedMs] = useState(0);
  const [status, setStatus] = useState<ConfirmationStatus>('Queued');
  const [hasPollingError, setHasPollingError] = useState(false);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setElapsedMs(Date.now() - startedAtRef.current);
    }, 1000);

    return () => window.clearInterval(intervalId);
  }, []);

  const isSent = status === 'Sent';
  const showAdvisory = !isSent && elapsedMs > 60000;

  useEffect(() => {
    if (appointmentId.length === 0 || isSent) {
      return;
    }

    let isActive = true;

    const fetchStatus = async () => {
      try {
        const response = await fetch(
          `/api/v1/appointments/${encodeURIComponent(appointmentId)}/confirmation-status`,
          {
            credentials: 'include',
          }
        );

        if (!response.ok) {
          throw new Error(`Failed to fetch confirmation status: ${response.status}`);
        }

        const data = (await response.json()) as ConfirmationStatusResponse;
        if (isActive) {
          setStatus(normalizeStatus(data.status));
          setHasPollingError(false);
        }
      } catch {
        if (isActive) {
          // Keep spinner visible and retry on next cycle for transient errors/timeouts.
          setHasPollingError(true);
        }
      }
    };

    fetchStatus();
    const intervalId = window.setInterval(fetchStatus, 3000);

    return () => {
      isActive = false;
      window.clearInterval(intervalId);
    };
  }, [appointmentId, isSent]);

  return (
    <section className={styles.container} aria-live="polite" data-uxr="UXR-504">
      {isSent ? (
        <p className={styles.success} role="status">
          <span className={styles.successIcon} aria-hidden="true">
            ✓
          </span>
          Confirmation emailed ✓
        </p>
      ) : (
        <>
          <p className={styles.progress} role="status">
            <span className={styles.spinner} aria-hidden="true" />
            Generating confirmation...
          </p>
          {showAdvisory && (
            <p className={styles.advisory}>Confirmation is taking longer than expected</p>
          )}
          {hasPollingError && <span className={styles.srOnly}>Retrying confirmation status check.</span>}
        </>
      )}
    </section>
  );
}

function normalizeStatus(status: string | undefined): ConfirmationStatus {
  if (status === 'Sent' || status === 'Processing') {
    return status;
  }

  return 'Queued';
}
