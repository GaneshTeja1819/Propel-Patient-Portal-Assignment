import { useEffect, useRef } from 'react';
import styles from './CancelConfirmModal.module.css';

interface CancelConfirmModalProps {
  appointmentTitle: string;
  isSubmitting: boolean;
  onConfirm: () => void;
  onDismiss: () => void;
}

export function CancelConfirmModal({
  appointmentTitle,
  isSubmitting,
  onConfirm,
  onDismiss,
}: CancelConfirmModalProps) {
  const dismissButtonRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    dismissButtonRef.current?.focus();

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isSubmitting) {
        onDismiss();
      }
    };

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [isSubmitting, onDismiss]);

  return (
    <div className={styles.overlay}>
      <section
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="cancel-confirm-title"
      >
        <h2 id="cancel-confirm-title" className={styles.title}>
          Cancel appointment?
        </h2>
        <p className={styles.body}>
          This will cancel <strong>{appointmentTitle}</strong> and release the slot for other patients.
        </p>
        <div className={styles.actions}>
          <button
            ref={dismissButtonRef}
            type="button"
            className={styles.keepButton}
            onClick={onDismiss}
            disabled={isSubmitting}
          >
            Keep appointment
          </button>
          <button
            type="button"
            className={styles.confirmButton}
            onClick={onConfirm}
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Cancelling...' : 'Yes, cancel'}
          </button>
        </div>
      </section>
    </div>
  );
}
