import { useEffect, useRef } from 'react';
import type { AuthRole } from '../../context/AuthContext';
import styles from './RoleDowngradeConfirmModal.module.css';

interface Props {
  username: string;
  newRole: Exclude<AuthRole, 'Admin'>;
  onConfirm: () => void;
  onCancel: () => void;
}

export function RoleDowngradeConfirmModal({ username, newRole, onConfirm, onCancel }: Props) {
  const cancelRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    cancelRef.current?.focus();
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel();
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onCancel]);

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby="rdcm-title">
      <div className={styles.modal}>
        <h2 id="rdcm-title" className={styles.title}>
          Change user role
        </h2>
        <p className={styles.body}>
          This will revoke Admin access for <strong>{username}</strong> — confirm?
        </p>
        <p className={styles.detail}>
          New role: <strong>{newRole}</strong>
        </p>
        <div className={styles.actions}>
          <button ref={cancelRef} type="button" className={styles.btnCancel} onClick={onCancel}>
            Cancel
          </button>
          <button type="button" className={styles.btnConfirm} onClick={onConfirm}>
            Confirm change
          </button>
        </div>
      </div>
    </div>
  );
}
