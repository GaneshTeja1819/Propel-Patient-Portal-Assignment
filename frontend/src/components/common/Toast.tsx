import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import styles from './Toast.module.css';

export interface ToastProps {
  id: string;
  message: string;
  type: 'error' | 'warning' | 'success' | 'info';
  duration?: number; // milliseconds; 0 = no auto-dismiss
  onDismiss: (id: string) => void;
}

/**
 * Dismissible toast notification rendered via portal.
 *
 * AC-004: Shows error messages within 2 seconds; auto-dismisses after 5 seconds.
 * Accessibility: role="alert", aria-live="polite" for screen readers.
 */
export function Toast({ id, message, type, duration = 5000, onDismiss }: ToastProps) {
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (duration > 0) {
      timeoutRef.current = setTimeout(() => {
        onDismiss(id);
      }, duration);
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [duration, id, onDismiss]);

  const handleClose = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    onDismiss(id);
  };

  const typeClass = {
    error: styles.error,
    warning: styles.warning,
    success: styles.success,
    info: styles.info,
  }[type];

  const toastContent = (
    <div className={`${styles.toast} ${typeClass}`} role="alert" aria-live="polite">
      <div className={styles.message}>{message}</div>
      <button
        className={styles.closeButton}
        onClick={handleClose}
        aria-label="Dismiss notification"
        type="button"
      >
        ✕
      </button>
    </div>
  );

  // Render via portal to avoid z-index stacking issues
  const toastContainer = document.getElementById('toast-root');
  if (!toastContainer) {
    return null;
  }

  return createPortal(toastContent, toastContainer);
}
