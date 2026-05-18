import styles from './SessionTimeoutModal.module.css';

interface SessionTimeoutModalProps {
  secondsRemaining: number;
  onStayLoggedIn: () => void | Promise<void>;
  refreshFailed: boolean;
  expired: boolean;
}

function SessionTimeoutModal({
  secondsRemaining,
  onStayLoggedIn,
  refreshFailed,
  expired,
}: SessionTimeoutModalProps) {
  const title = expired ? 'Session expired' : 'Session expiring soon';

  return (
    <div className={styles.overlay}>
      <section
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="session-timeout-title"
      >
        <h2 id="session-timeout-title" className={styles.title}>
          {title}
        </h2>

        <p className={styles.message} aria-live="assertive" aria-atomic="true">
          {expired
            ? 'Session expired. Redirecting to login...'
            : `Session expiring in ${secondsRemaining} seconds`}
        </p>

        {!expired && (
          <p className={styles.message} aria-live="assertive" aria-atomic="true">
            Session expiring in 2 minutes
          </p>
        )}

        {refreshFailed && !expired && (
          <p className={styles.error} role="alert">
            Refresh failed - you will be logged out in {secondsRemaining} seconds
          </p>
        )}

        {!expired && (
          <button type="button" className={styles.ctaButton} onClick={onStayLoggedIn}>
            Stay logged in
          </button>
        )}
      </section>
    </div>
  );
}

export default SessionTimeoutModal;
