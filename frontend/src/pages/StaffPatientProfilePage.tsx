/**
 * StaffPatientProfilePage.tsx — SCR-013 360° Patient Profile (Staff view, US_027).
 *
 * Staff-specific profile view:
 * - Reads patientId from URL params (/staff/patients/:id)
 * - Calls GET /api/v1/profile/{patientId} (StaffPolicy)
 * - Renders ClinicalSections in staff mode (conflict slot visible for US_028)
 * - Dedup banner and no-documents empty state (same edge cases as patient view)
 * - Redirects non-Staff users (403 from server; handled in hook)
 *
 * Route: /staff/patients/:id
 */
import { useParams } from 'react-router-dom';
import { Link } from 'react-router-dom';
import { useAuthContext } from '../context/AuthContext';
import { usePatientProfile } from '../hooks/usePatientProfile';
import { ClinicalSections } from '../components/profile/ClinicalSections';
import { ConflictsSection } from '../components/profile/ConflictsSection';
import styles from './PatientProfilePage.module.css';

export function StaffPatientProfilePage(): JSX.Element {
  const { id: patientId = '' } = useParams<{ id: string }>();
  const { role } = useAuthContext();
  const { data, loadState, errorMessage, refetch: refetchProfile } = usePatientProfile(patientId);

  const isLoading = loadState === 'loading' || loadState === 'idle';

  return (
    <div className={styles.page}>
      {/* ── Top navigation (Staff variant — green logo mark) ── */}
      <nav className={styles.topNav} aria-label="Main navigation">
        <div className={styles.topNavInner}>
          <Link
            to="/staff/queue"
            className={styles.navLogo}
            aria-label="Staff Portal home"
            id="nav-queue"
            style={{ '--logo-bg': 'var(--color-success)' } as React.CSSProperties}
          >
            <div
              className={styles.navLogoMark}
              aria-hidden="true"
              style={{ background: 'var(--color-success)' }}
            >
              P
            </div>
            <span>Staff Portal</span>
          </Link>
          <nav className={styles.navLinks} aria-label="Site navigation">
            <Link to="/staff/queue" className={styles.navLink} id="nav-queue-link">
              📋 Queue
            </Link>
            <Link to="/staff/patients" className={`${styles.navLink} ${styles.navLinkActive}`} aria-current="page">
              👤 Patients
            </Link>
          </nav>
          {/* Staff role badge */}
          <div
            className={styles.staffRoleBadge}
            aria-label="Logged in as Staff"
          >
            {role ?? 'Staff'} · Staff
          </div>
        </div>
      </nav>

      {/* ── Main content ── */}
      <main className={styles.main} id="main-profile" role="main">
        {/* Breadcrumb */}
        <nav className={styles.breadcrumb} aria-label="Breadcrumb">
          <Link to="/staff/queue">Queue</Link>
          <span className={styles.breadcrumbSep} aria-hidden="true">/</span>
          <Link to="/staff/patients">Patients</Link>
          <span className={styles.breadcrumbSep} aria-hidden="true">/</span>
          <span
            style={{ color: 'var(--color-text-primary)', fontWeight: 600 }}
            aria-current="page"
          >
            {data?.displayName ?? 'Patient Profile'}
          </span>
        </nav>

        {/* Error / Forbidden state */}
        {(loadState === 'error' || loadState === 'forbidden') && errorMessage && (
          <div className={styles.errorBanner} role="alert">
            {errorMessage}
          </div>
        )}

        {/* Profile header */}
        <div className={styles.profileHeader}>
          <div className={styles.profileIdentity}>
            <div className={styles.avatarLg} aria-hidden="true">
              {data?.initials ?? '??'}
            </div>
            <div>
              <div className={styles.profileName}>
                {data?.displayName ?? 'Loading…'}
              </div>
              {data && (
                <div className={styles.profileMeta}>
                  DOB: {data.dateOfBirth} · {data.sex} · {data.contact}
                </div>
              )}
              <div className={styles.profileBadges}>
                <span className={`${styles.badge} ${styles.badgePhi}`} data-uxr="UXR-402">
                  <span aria-hidden="true">🔒</span> PHI Protected
                </span>
                {data?.vitals.some((v) => v.isAiExtracted) && (
                  <span className={`${styles.badge} ${styles.badgeAi}`} data-uxr="UXR-403">
                    <span aria-hidden="true">🤖</span> AI-enriched
                  </span>
                )}
              </div>
            </div>
          </div>
          <div className={styles.profileActions}>
            <Link
              to={`/staff/patients/${patientId}/codes`}
              className={styles.btnPrimary}
              id="btn-verify-codes"
            >
              🔎 Verify codes
            </Link>
            <Link to={`/documents/upload?patientId=${patientId}`} className={styles.btnSecondary}>
              📄 Upload documents
            </Link>
          </div>
        </div>

        {/* Dedup in-progress banner (edge case) */}
        {data?.deduplicationStatus === 'Processing' && (
          <div className={styles.dedupBanner} role="status" aria-live="polite">
            <span aria-hidden="true">⏳</span>
            <span>
              <strong>Profile consolidation in progress</strong> — de-duplication is running.
              Sections may show unmerged entries until complete.
            </span>
          </div>
        )}

        {/* No documents state (AC-004) */}
        {!isLoading && data && !data.hasDocuments && (
          <div className={styles.emptyProfile} role="status">
            <p className={styles.emptyTitle}>No clinical data available</p>
            <p className={styles.emptyDescription}>
              No documents have been processed for this patient yet.
            </p>
            <Link
              to={`/documents/upload?patientId=${patientId}`}
              className={styles.btnPrimary}
            >
              📄 Upload documents
            </Link>
          </div>
        )}

        {/* Conflict alerts — Staff only (US_028, UXR-404). Renders nothing when no conflicts. */}
        {patientId && <ConflictsSection patientId={patientId} onAfterResolve={refetchProfile} />}

        {/* Clinical sections — staff mode shows conflict slot (US_028) */}
        {data && data.hasDocuments && (
          <ClinicalSections data={data} mode="staff" isLoading={isLoading} />
        )}

        {/* Loading skeleton placeholder when no data yet */}
        {isLoading && !data && (
          <ClinicalSections
            data={{
              patientId: '',
              displayName: '',
              initials: '',
              dateOfBirth: '',
              sex: '',
              contact: '',
              vitals: [],
              medications: [],
              diagnoses: [],
              visitHistory: [],
              deduplicationStatus: 'Pending',
              hasDocuments: false,
            }}
            mode="staff"
            isLoading
          />
        )}
      </main>
    </div>
  );
}
