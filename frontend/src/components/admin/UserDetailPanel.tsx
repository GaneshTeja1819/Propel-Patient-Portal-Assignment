import { useState } from 'react';
import type { AuthRole } from '../../context/AuthContext';
import type { AdminUser } from '../../hooks/useAdminUsers';
import { RoleDowngradeConfirmModal } from './RoleDowngradeConfirmModal';
import styles from './UserDetailPanel.module.css';

interface Props {
  user: AdminUser;
  onDeactivate: (id: string) => Promise<void>;
  onReactivate: (id: string) => Promise<void>;
  onRoleChange: (id: string, newRole: AuthRole) => Promise<void>;
  onActionComplete: () => void;
}

export function UserDetailPanel({
  user,
  onDeactivate,
  onReactivate,
  onRoleChange,
  onActionComplete,
}: Props) {
  const [pendingDowngradeRole, setPendingDowngradeRole] = useState<
    Exclude<AuthRole, 'Admin'> | null
  >(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const handleRoleChange = async (newRole: AuthRole) => {
    if (newRole === user.role) return;
    if (user.role === 'Admin' && newRole !== 'Admin') {
      setPendingDowngradeRole(newRole as Exclude<AuthRole, 'Admin'>);
      return;
    }
    setBusy(true);
    setActionError(null);
    try {
      await onRoleChange(user.id, newRole);
      onActionComplete();
    } catch {
      setActionError('Role change failed. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  const handleConfirmDowngrade = async () => {
    if (!pendingDowngradeRole) return;
    const role = pendingDowngradeRole;
    setPendingDowngradeRole(null);
    setBusy(true);
    setActionError(null);
    try {
      await onRoleChange(user.id, role);
      onActionComplete();
    } catch {
      setActionError('Role change failed. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  const handleDeactivateOrReactivate = async () => {
    setBusy(true);
    setActionError(null);
    try {
      if (user.status === 'Inactive') {
        await onReactivate(user.id);
      } else {
        await onDeactivate(user.id);
      }
      onActionComplete();
    } catch {
      setActionError(
        user.status === 'Inactive'
          ? 'Reactivation failed. Please try again.'
          : 'Deactivation failed. Please try again.',
      );
    } finally {
      setBusy(false);
    }
  };

  const isSelf = user.isSelf === true;
  const isInactive = user.status === 'Inactive';

  return (
    <>
      <div className={styles.wrap}>
        <div className={styles.actions}>
          {!isInactive && (
            <div className={styles.roleField}>
              <label htmlFor={`role-select-${user.id}`} className={styles.roleLabel}>
                Change role
              </label>
              <select
                id={`role-select-${user.id}`}
                className={styles.roleSelect}
                value={user.role}
                onChange={e => void handleRoleChange(e.target.value as AuthRole)}
                disabled={busy}
                aria-label={`Change role for ${user.name}`}
              >
                <option value="Patient">Patient</option>
                <option value="Staff">Staff</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
          )}

          {isInactive ? (
            <button
              type="button"
              className={styles.btnReactivate}
              onClick={() => void handleDeactivateOrReactivate()}
              disabled={busy}
            >
              Reactivate
            </button>
          ) : (
            <button
              type="button"
              className={styles.btnDeactivate}
              onClick={() => void handleDeactivateOrReactivate()}
              disabled={isSelf || busy}
              title={isSelf ? 'You cannot deactivate your own account' : undefined}
              aria-disabled={isSelf}
            >
              Deactivate
            </button>
          )}
        </div>

        {isSelf && (
          <p className={styles.selfDeactError} role="alert">
            You cannot deactivate your own account. Ask another administrator to perform this
            action.
          </p>
        )}

        {actionError && (
          <p className={styles.actionError} role="alert">
            {actionError}
          </p>
        )}
      </div>

      {pendingDowngradeRole !== null && (
        <RoleDowngradeConfirmModal
          username={user.name}
          newRole={pendingDowngradeRole}
          onConfirm={() => void handleConfirmDowngrade()}
          onCancel={() => setPendingDowngradeRole(null)}
        />
      )}
    </>
  );
}
