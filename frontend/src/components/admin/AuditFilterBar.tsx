import type { ActionCategory, AuditFilters } from '../../hooks/useAuditLog';
import styles from './AuditFilterBar.module.css';

interface Props {
  filters: AuditFilters;
  onUpdate: <K extends keyof AuditFilters>(key: K, value: AuditFilters[K]) => void;
  onReset: () => void;
}

export function AuditFilterBar({ filters, onUpdate, onReset }: Props) {
  return (
    <div className={styles.bar} role="search" aria-label="Filter audit log">
      <div className={styles.row}>
        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-from">
            From
          </label>
          <input
            type="date"
            id="filter-from"
            className={styles.dateInput}
            value={filters.dateFrom}
            onChange={e => onUpdate('dateFrom', e.target.value)}
            aria-label="Filter from date"
          />
        </div>

        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-to">
            To
          </label>
          <input
            type="date"
            id="filter-to"
            className={styles.dateInput}
            value={filters.dateTo}
            onChange={e => onUpdate('dateTo', e.target.value)}
            aria-label="Filter to date"
          />
        </div>

        <div className={styles.divider} aria-hidden="true" />

        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-category">
            Action Category
          </label>
          <select
            id="filter-category"
            className={styles.select}
            value={filters.actionCategory}
            onChange={e => onUpdate('actionCategory', e.target.value as ActionCategory)}
            aria-label="Filter by action category"
          >
            <option value="">All actions</option>
            <option value="AUTH">Authentication</option>
            <option value="ACCOUNT">Account Management</option>
            <option value="SCHEDULING">Scheduling</option>
            <option value="CLINICAL">Clinical / PHI</option>
            <option value="INTEGRATION">Integration</option>
            <option value="SECURITY">Security</option>
          </select>
        </div>

        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-role">
            Role
          </label>
          <select
            id="filter-role"
            className={styles.select}
            value={filters.role}
            onChange={e => onUpdate('role', e.target.value as AuditFilters['role'])}
            aria-label="Filter by role"
          >
            <option value="">All roles</option>
            <option value="Admin">Admin</option>
            <option value="Staff">Staff</option>
            <option value="Patient">Patient</option>
            <option value="System">System</option>
          </select>
        </div>

        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-status">
            Status
          </label>
          <select
            id="filter-status"
            className={styles.select}
            value={filters.status}
            onChange={e => onUpdate('status', e.target.value as AuditFilters['status'])}
            aria-label="Filter by status"
          >
            <option value="">All status</option>
            <option value="SUCCESS">Success</option>
            <option value="FAILURE">Failure</option>
          </select>
        </div>

        <div className={styles.divider} aria-hidden="true" />

        <div className={styles.group}>
          <label className={styles.label} htmlFor="filter-search">
            Search
          </label>
          <input
            type="search"
            id="filter-search"
            className={styles.searchInput}
            value={filters.search}
            onChange={e => onUpdate('search', e.target.value)}
            placeholder="User, email, or resource…"
            aria-label="Search audit events"
          />
        </div>

        <button
          type="button"
          className={styles.resetBtn}
          onClick={onReset}
          aria-label="Reset all filters"
        >
          ✕ Reset
        </button>
      </div>
    </div>
  );
}
