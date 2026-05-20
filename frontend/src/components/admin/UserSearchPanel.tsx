import { Fragment, useCallback, useRef, useState } from 'react';
import type { AuthRole } from '../../context/AuthContext';
import type { AdminUser } from '../../hooks/useAdminUsers';
import { UserDetailPanel } from './UserDetailPanel';
import styles from './UserSearchPanel.module.css';

type RoleFilter = '' | AuthRole;
type StatusFilter = '' | 'Active' | 'Inactive';

interface Props {
  users: AdminUser[];
  loading: boolean;
  error: string | null;
  onSearch: (q: string) => Promise<void>;
  onDeactivate: (id: string) => Promise<void>;
  onReactivate: (id: string) => Promise<void>;
  onRoleChange: (id: string, newRole: AuthRole) => Promise<void>;
}

function RoleBadge({ role }: { role: AuthRole }) {
  const cls =
    role === 'Admin'
      ? styles.badgeAdmin
      : role === 'Staff'
        ? styles.badgeStaff
        : styles.badgePatient;
  return <span className={`${styles.badge} ${cls}`}>{role}</span>;
}

function StatusBadge({ status }: { status: 'Active' | 'Inactive' }) {
  const cls = status === 'Active' ? styles.badgeActive : styles.badgeInactive;
  const prefix = status === 'Active' ? '●' : '○';
  return (
    <span className={`${styles.badge} ${cls}`}>
      {prefix} {status}
    </span>
  );
}

export function UserSearchPanel({
  users,
  loading,
  error,
  onSearch,
  onDeactivate,
  onReactivate,
  onRoleChange,
}: Props) {
  const [query, setQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState<RoleFilter>('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('');
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleQueryChange = useCallback(
    (value: string) => {
      setQuery(value);
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        void onSearch(value);
      }, 300);
    },
    [onSearch],
  );

  const toggleExpand = useCallback((id: string) => {
    setExpandedId(prev => (prev === id ? null : id));
  }, []);

  const handleActionComplete = useCallback(() => {
    setExpandedId(null);
  }, []);

  const filtered = users.filter(u => {
    const matchRole = !roleFilter || u.role === roleFilter;
    const matchStatus = !statusFilter || u.status === statusFilter;
    return matchRole && matchStatus;
  });

  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <span className={styles.cardTitle}>
          Users <span className={styles.userCount}>{filtered.length} total</span>
        </span>
        <div className={styles.searchWrap}>
          <input
            type="search"
            className={styles.searchInput}
            placeholder="Search by name or email…"
            aria-label="Search users"
            value={query}
            onChange={e => handleQueryChange(e.target.value)}
          />
          <select
            className={styles.filterSelect}
            aria-label="Filter by role"
            value={roleFilter}
            onChange={e => setRoleFilter(e.target.value as RoleFilter)}
          >
            <option value="">All roles</option>
            <option value="Admin">Admin</option>
            <option value="Staff">Staff</option>
            <option value="Patient">Patient</option>
          </select>
          <select
            className={styles.filterSelect}
            aria-label="Filter by status"
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value as StatusFilter)}
          >
            <option value="">All status</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
        </div>
      </div>

      {loading && (
        <p className={styles.statusMsg} aria-live="polite">
          Loading users…
        </p>
      )}

      {error && (
        <p className={styles.errorMsg} role="alert">
          {error}
        </p>
      )}

      {!loading && !error && (
        <div className={styles.tableWrap}>
          <table className={styles.table} aria-label="User accounts">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Role</th>
                <th scope="col">Status</th>
                <th scope="col">Last login</th>
                <th scope="col">
                  <span className={styles.srOnly}>Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={5} className={styles.emptyState}>
                    No users found matching your search
                  </td>
                </tr>
              ) : (
                filtered.map(user => (
                  <Fragment key={user.id}>
                    <tr
                      className={expandedId === user.id ? styles.rowExpanded : undefined}
                      style={user.status === 'Inactive' ? { opacity: 0.7 } : undefined}
                    >
                      <td>
                        <div className={styles.userName}>
                          {user.name}
                          {user.isSelf && <span className={styles.youBadge}>You</span>}
                        </div>
                        <div className={styles.userEmail}>{user.email}</div>
                      </td>
                      <td>
                        <RoleBadge role={user.role} />
                      </td>
                      <td>
                        <StatusBadge status={user.status} />
                      </td>
                      <td className={styles.lastLogin}>{user.lastLogin ?? '—'}</td>
                      <td>
                        <button
                          type="button"
                          className={styles.btnExpand}
                          onClick={() => toggleExpand(user.id)}
                          aria-expanded={expandedId === user.id}
                          aria-controls={`actions-row-${user.id}`}
                        >
                          Actions {expandedId === user.id ? '▴' : '▾'}
                        </button>
                      </td>
                    </tr>
                    {expandedId === user.id && (
                      <tr id={`actions-row-${user.id}`} className={styles.expandRow}>
                        <td colSpan={5}>
                          <UserDetailPanel
                            user={user}
                            onDeactivate={onDeactivate}
                            onReactivate={onReactivate}
                            onRoleChange={onRoleChange}
                            onActionComplete={handleActionComplete}
                          />
                        </td>
                      </tr>
                    )}
                  </Fragment>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
