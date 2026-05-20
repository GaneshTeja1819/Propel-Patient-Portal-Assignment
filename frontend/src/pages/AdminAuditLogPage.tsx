import { Navigate } from 'react-router-dom';
import { AdminPortalLayout } from '../components/admin/AdminPortalLayout';
import { AuditFilterBar } from '../components/admin/AuditFilterBar';
import { AuditLogTable } from '../components/admin/AuditLogTable';
import { AuditStatStrip } from '../components/admin/AuditStatStrip';
import { useAuthContext } from '../context/AuthContext';
import { useAuditLog } from '../hooks/useAuditLog';
import styles from './AdminAuditLogPage.module.css';

// ── Access guard ────────────────────────────────────────────────────────────

function RequireRole({
  role,
  children,
}: {
  role: string;
  children: React.ReactNode;
}): React.ReactElement | null {
  const { isAuthenticated, role: userRole } = useAuthContext();
  if (!isAuthenticated || userRole !== role) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

// ── Page ────────────────────────────────────────────────────────────────────

function AdminAuditLogContent() {
  const {
    filters,
    events,
    total,
    page,
    pageSize,
    stats,
    loading,
    exportLoading,
    error,
    toastMessage,
    updateFilter,
    resetFilters,
    goToPage,
    exportCSV,
  } = useAuditLog();

  return (
    <AdminPortalLayout>
      {/* HIPAA Notice — always visible, not dismissible */}
      <div className={styles.hipaaBanner} role="note" aria-label="HIPAA compliance notice">
        🔒 HIPAA Notice: This audit log contains protected health information (PHI). Access is
        restricted to authorised administrators only. All access to this log is itself audited and
        subject to compliance review.
      </div>

      {/* Page header */}
      <div className={styles.pageHeader}>
        <div className={styles.titleRow}>
          <h1 className={styles.title}>Audit Log</h1>
          <span className={styles.immutableBadge} aria-label="Immutable records">
            🔒 Immutable
          </span>
        </div>
        <div className={styles.headerActions}>
          <button
            type="button"
            className={`${styles.headerBtn} ${styles.btnSecondary}`}
            onClick={() => window.print()}
            aria-label="Print audit log"
          >
            🖨 Print
          </button>
          <button
            type="button"
            className={`${styles.headerBtn} ${styles.btnPrimary}`}
            onClick={() => void exportCSV()}
            disabled={exportLoading}
            aria-label={exportLoading ? 'Exporting…' : 'Export audit log to CSV'}
          >
            {exportLoading ? (
              <>
                <span className={styles.spinner} aria-hidden="true" />
                Exporting…
              </>
            ) : (
              '⬇ Export CSV'
            )}
          </button>
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className={styles.errorBanner} role="alert" aria-live="assertive">
          {error}
        </div>
      )}

      {/* Stats strip */}
      <AuditStatStrip stats={stats} loading={loading} />

      {/* Filter bar */}
      <AuditFilterBar filters={filters} onUpdate={updateFilter} onReset={resetFilters} />

      {/* Event table */}
      <AuditLogTable
        events={events}
        total={total}
        page={page}
        pageSize={pageSize}
        loading={loading}
        onPageChange={goToPage}
      />

      {/* Toast notification */}
      {toastMessage && (
        <div
          className={styles.toast}
          role="status"
          aria-live="polite"
          aria-atomic="true"
          aria-label={toastMessage}
        >
          {toastMessage}
        </div>
      )}
    </AdminPortalLayout>
  );
}

export default function AdminAuditLogPage() {
  return (
    <RequireRole role="Admin">
      <AdminAuditLogContent />
    </RequireRole>
  );
}
