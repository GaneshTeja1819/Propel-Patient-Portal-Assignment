import { useEffect } from 'react';
import { Link, Navigate } from 'react-router-dom';
import QueueTable from '../components/staff/QueueTable';
import { useAuthContext } from '../context/AuthContext';
import { QueueStatus, useQueue } from '../hooks/useQueue';
import styles from './StaffQueuePage.module.css';

function StaffQueuePage() {
  const { role } = useAuthContext();
  const {
    entries,
    isLoading,
    fetchError,
    arrivedConflictId,
    fetchQueue,
    markArrived,
    removeEntry,
    reorder,
  } = useQueue();

  useEffect(() => {
    void fetchQueue();
  }, [fetchQueue]);

  // Guard: Staff only — rendered after all hooks
  if (role !== null && role !== 'Staff') {
    return <Navigate to="/login" replace />;
  }

  const countByStatus = (status: QueueStatus) => entries.filter((e) => e.status === status).length;

  const today = new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  return (
    <div className={styles.page}>
      {/* Top navigation */}
      <nav className={styles.topnav} aria-label="Main navigation — Staff Portal">
        <div className={styles.topnavInner}>
          <Link to="/staff/queue" className={styles.navLogo} aria-label="Staff Portal home">
            <div className={styles.navLogoMark} aria-hidden="true">
              S
            </div>
            <span>Staff Portal</span>
          </Link>

          <div className={styles.navLinks}>
            <Link
              to="/staff/queue"
              className={`${styles.navLink} ${styles.navLinkActive}`}
              aria-current="page"
            >
              📋 Queue
            </Link>
            <Link to="/staff/walk-in" className={styles.navLink}>
              ➕ New Walk-in
            </Link>
          </div>

          <div className={styles.navRight}>
            <span className={styles.roleBadge} aria-label="Logged in as Staff role">
              👩‍⚕ Staff
            </span>
          </div>
        </div>
      </nav>

      <main className={styles.content} role="main">
        {/* Page header */}
        <div className={styles.pageHeader}>
          <div>
            <h1 className={styles.pageTitle}>Today's Queue</h1>
            <p className={styles.pageSubtitle}>{today}</p>
          </div>
          <Link to="/staff/walk-in" className={styles.btnPrimary} id="btn-new-walkin">
            ➕ New Walk-in
          </Link>
        </div>

        {/* Stats (UXR-104) */}
        <div className={styles.statsRow} role="region" aria-label="Queue summary">
          <div className={styles.statCard}>
            <div className={styles.statValue}>{entries.length}</div>
            <div className={styles.statLabel}>Total appointments</div>
          </div>
          <div className={styles.statCard}>
            <div className={`${styles.statValue} ${styles.statValueArrived}`}>
              {countByStatus('Arrived')}
            </div>
            <div className={styles.statLabel}>Arrived</div>
          </div>
          <div className={styles.statCard}>
            <div className={`${styles.statValue} ${styles.statValueBooked}`}>
              {countByStatus('Booked')}
            </div>
            <div className={styles.statLabel}>Booked</div>
          </div>
          <div className={styles.statCard}>
            <div className={`${styles.statValue} ${styles.statValueWalkin}`}>
              {countByStatus('WalkIn')}
            </div>
            <div className={styles.statLabel}>Walk-ins</div>
          </div>
        </div>

        {/* Queue card */}
        <div className={styles.card}>
          <div className={styles.cardHeader}>
            <h2 className={styles.cardTitle}>Appointments</h2>
            <span className={styles.lastUpdated}>Last updated: just now</span>
          </div>

          {isLoading && (
            <div className={styles.loadingMsg} role="status" aria-live="polite">
              Loading today's queue…
            </div>
          )}

          {fetchError && !isLoading && (
            <div className={styles.errorMsg} role="alert">
              {fetchError}
              <button type="button" className={styles.retryBtn} onClick={() => void fetchQueue()}>
                Retry
              </button>
            </div>
          )}

          {!isLoading && !fetchError && (
            <div className={styles.cardBody}>
              <QueueTable
                entries={entries}
                arrivedConflictId={arrivedConflictId}
                onMarkArrived={markArrived}
                onRemove={removeEntry}
                onReorder={reorder}
              />
            </div>
          )}
        </div>
      </main>
    </div>
  );
}

export default StaffQueuePage;
