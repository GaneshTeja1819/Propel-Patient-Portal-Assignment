/**
 * PatientProfilePage.tsx — SCR-009 360° Patient Profile (Patient self-view, US_027).
 *
 * Renders the patient's own clinical profile in read-only mode.
 * - "Patient view — read-only" indicator badge (AC-002)
 * - AI-extracted field badges on AI-sourced data (AC-002, UXR-403)
 * - No Staff conflict resolution controls (AC-002)
 * - Empty state with upload CTA when no documents exist (AC-004)
 * - "Profile consolidation in progress" amber banner (edge case)
 *
 * Route: /profile  (PatientPolicy — accessible to Patient role only)
 */
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { usePatientProfile } from '../hooks/usePatientProfile';
import { ClinicalSections } from '../components/profile/ClinicalSections';
import styles from './PatientProfilePage.module.css';

export function PatientProfilePage(): JSX.Element {
  const { user } = useAuth();
  const { data, loadState, errorMessage } = usePatientProfile('me');

  const isLoading = loadState === 'loading' || loadState === 'idle';

  return (
    <div className={styles.page}>
      {/* ── Top navigation ── */}
      <nav className={styles.topNav} aria-label="Main navigation">
        <div className={styles.topNavInner}>
          <Link to="/" className={styles.navLogo} aria-label="Patient Portal home">
            <div className={styles.navLogoMark} aria-hidden="true">P</div>
            <span>Patient Portal</span>
          </Link>
          <nav className={styles.navLinks} aria-label="Site navigation">
            <Link to="/" className={styles.navLink}>🏠 Dashboard</Link>
            <Link to="/appointments" className={styles.navLink}>📅 Book</Link>
            <Link to="/profile" className={`${styles.navLink} ${styles.navLinkActive}`} aria-current="page">
              👤 My Profile
            </Link>
            <Link to="/documents/upload" className={styles.navLink}>📄 Documents</Link>
          </nav>
          <div className={styles.avatarSm} aria-label={user?.displayName ?? 'User'} aria-hidden="true">
            {user?.displayName.slice(0, 2).toUpperCase() ?? 'U'}
          </div>
        </div>
      </nav>

      {/* ── Main content ── */}
      <main className={styles.main} id="main-profile" role="main">
        {/* Breadcrumb */}
        <nav className={styles.breadcrumb} aria-label="Breadcrumb">
          <Link to="/">Dashboard</Link>
          <span className={styles.breadcrumbSep} aria-hidden="true">/</span>
          <span style={{ color: 'var(--color-text-primary)', fontWeight: 600 }} aria-current="page">
            My Profile
          </span>
        </nav>

        {/* Error state */}
        {(loadState === 'error' || loadState === 'forbidden') && errorMessage && (
          <div className={styles.errorBanner} role="alert">
            {errorMessage}
          </div>
        )}

        {/* Profile header */}
        <div className={styles.profileHeader}>
          <div className={styles.profileIdentity}>
            <div className={styles.avatarLg} aria-hidden="true">
              {data?.initials ?? user?.displayName.slice(0, 2).toUpperCase() ?? 'U'}
            </div>
            <div>
              <div className={styles.profileName}>
                {data?.displayName ?? user?.displayName ?? 'Loading…'}
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
                {/* AC-002: "Patient view" indicator — no Staff controls visible */}
                <span className={`${styles.badge} ${styles.badgePatientView}`} data-testid="patient-view-badge">
                  👤 Patient view — read-only
                </span>
              </div>
            </div>
          </div>
          <div className={styles.profileActions}>
            <Link to="/documents/upload" className={styles.btnPrimary}>
              📄 Upload documents
            </Link>
            <Link to="/intake/new" className={styles.btnSecondary}>
              📋 Update intake
            </Link>
          </div>
        </div>

        {/* Dedup in-progress banner (edge case) */}
        {data?.deduplicationStatus === 'Processing' && (
          <div className={styles.dedupBanner} role="status" aria-live="polite">
            <span aria-hidden="true">⏳</span>
            <span>
              <strong>Profile consolidation in progress</strong> — your documents are being analysed.
              Some sections may show unmerged entries until complete.
            </span>
          </div>
        )}

        {/* No documents state (AC-004) */}
        {!isLoading && data && !data.hasDocuments && (
          <div className={styles.emptyProfile} role="status">
            <p className={styles.emptyTitle}>No clinical data available</p>
            <p className={styles.emptyDescription}>
              Upload your first document to generate your 360° profile.
            </p>
            <Link to="/documents/upload" className={styles.btnPrimary}>
              📄 Upload documents
            </Link>
          </div>
        )}

        {/* Clinical sections (AC-001, AC-002) */}
        {data && data.hasDocuments && (
          <ClinicalSections data={data} mode="patient" isLoading={isLoading} />
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
            mode="patient"
            isLoading
          />
        )}
      </main>
    </div>
  );
}
