import { useEffect, useRef, useState } from 'react';
import styles from './RemoveQueueModal.module.css';

const REMOVAL_REASONS = ['No-show', 'Error', 'Patient request', 'Other'] as const;
type RemovalReason = (typeof REMOVAL_REASONS)[number];

interface RemoveQueueModalProps {
  patientName: string;
  isAlreadyArrived: boolean;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

function RemoveQueueModal({
  patientName,
  isAlreadyArrived,
  onConfirm,
  onCancel,
}: RemoveQueueModalProps) {
  const [reason, setReason] = useState<RemovalReason | ''>('');
  const [submitAttempted, setSubmitAttempted] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const selectRef = useRef<HTMLSelectElement>(null);

  // Trap focus and announce modal on open
  useEffect(() => {
    selectRef.current?.focus();

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onCancel();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onCancel]);

  function handleSubmit() {
    setSubmitAttempted(true);
    if (!reason) return;
    onConfirm(reason);
  }

  const reasonError = submitAttempted && !reason;

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby="remove-modal-title"
      aria-describedby="remove-modal-desc"
      onClick={(e) => {
        if (e.target === e.currentTarget) onCancel();
      }}
      ref={dialogRef}
    >
      <div className={styles.modal}>
        <h2 className={styles.title} id="remove-modal-title">
          Remove from Queue
        </h2>

        {isAlreadyArrived && (
          <div className={styles.arrivedWarning} role="alert">
            ⚠ Patient has already arrived — confirm removal?
          </div>
        )}

        <p className={styles.description} id="remove-modal-desc">
          You are removing <strong>{patientName}</strong> from today's queue. This action cannot be
          undone.
        </p>

        <div className={styles.fieldGroup}>
          <label htmlFor="remove-reason" className={styles.label}>
            Reason for removal
            <span className={styles.required} aria-hidden="true">
              {' '}
              *
            </span>
          </label>
          <select
            id="remove-reason"
            ref={selectRef}
            className={`${styles.select} ${reasonError ? styles.selectError : ''}`}
            value={reason}
            onChange={(e) => setReason(e.target.value as RemovalReason | '')}
            aria-required="true"
            aria-invalid={reasonError}
            aria-describedby={reasonError ? 'remove-reason-error' : undefined}
          >
            <option value="">— Select a reason —</option>
            {REMOVAL_REASONS.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
          {reasonError && (
            <span id="remove-reason-error" className={styles.errorMsg} role="alert">
              Please select a reason before confirming removal.
            </span>
          )}
        </div>

        <div className={styles.actions}>
          <button type="button" className={styles.btnCancel} onClick={onCancel}>
            Cancel
          </button>
          <button
            type="button"
            className={styles.btnConfirm}
            onClick={handleSubmit}
            aria-disabled={!reason}
          >
            Confirm removal
          </button>
        </div>
      </div>
    </div>
  );
}

export default RemoveQueueModal;
