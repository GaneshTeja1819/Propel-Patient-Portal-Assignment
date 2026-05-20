/**
 * CodeVerificationPage.tsx — SCR-014 Medical Code Verification (US_030).
 *
 * Route: /staff/coding/:encounterId
 * Access: Staff only. HTTP 403 from API → redirect to / (OWASP A01).
 *
 * Renders the progress summary bar and CodeVerificationTable.
 * Layout mirrors wireframe-SCR-014-code-verification.html.
 */
import { Link, useParams } from 'react-router-dom';
import { CodeVerificationTable } from '../components/coding/CodeVerificationTable';
import { useCodeVerification } from '../hooks/useCodeVerification';

export function CodeVerificationPage(): JSX.Element {
  const { encounterId = '' } = useParams<{ encounterId: string }>();

  const {
    suggestions,
    loading,
    error,
    verifiedRows,
    rowErrors,
    allRejected,
    finalized,
    verify,
    validateCode,
  } = useCodeVerification(encounterId);

  // ── Progress counters ───────────────────────────────────────────────────────
  const totalCount    = suggestions.length;
  const pendingCount  = suggestions.filter(s => !verifiedRows[s.id]).length;
  const acceptedCount = suggestions.filter(s => verifiedRows[s.id] === 'Accepted').length;
  const modifiedCount = suggestions.filter(s => verifiedRows[s.id] === 'Modified').length;
  const rejectedCount = suggestions.filter(s => verifiedRows[s.id] === 'Rejected').length;

  return (
    <div
      style={{
        minHeight: '100vh',
        background: 'var(--color-surface-muted)',
        fontFamily: 'var(--font-family-default)',
        color: 'var(--color-text-primary)',
      }}
    >
      {/* ── Top navigation ───────────────────────────────────────────────── */}
      <nav
        aria-label="Main navigation — Staff Portal"
        style={{
          height: '64px',
          background: 'var(--color-surface-default)',
          borderBottom: '1px solid var(--color-border-default)',
          position: 'sticky',
          top: 0,
          zIndex: 100,
          boxShadow: 'var(--shadow-sm)',
        }}
      >
        <div
          style={{
            maxWidth: '1280px',
            margin: '0 auto',
            padding: '0 var(--space-8)',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 'var(--space-4)',
          }}
        >
          <Link
            to="/"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-2)',
              fontWeight: 700,
              color: 'var(--color-text-primary)',
              textDecoration: 'none',
              fontSize: 'var(--font-size-heading-sm)',
            }}
          >
            <div
              style={{
                width: '32px',
                height: '32px',
                background: 'var(--color-success)',
                borderRadius: 'var(--radius-md)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--color-text-on-primary)',
                fontWeight: 700,
                fontSize: 'var(--font-size-body-md)',
              }}
              aria-hidden="true"
            >
              S
            </div>
            <span>Staff Portal</span>
          </Link>
          <span
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-2)',
              padding: 'var(--space-1) var(--space-3)',
              background: 'var(--color-success-bg)',
              borderRadius: 'var(--radius-full)',
              fontSize: 'var(--font-size-label)',
              fontWeight: 700,
              color: 'var(--color-success-text)',
            }}
            aria-label="Staff role"
          >
            👩‍⚕ Staff
          </span>
        </div>
      </nav>

      {/* ── Main content ─────────────────────────────────────────────────── */}
      <main
        style={{ maxWidth: '1280px', margin: '0 auto', padding: 'var(--space-8)' }}
        role="main"
      >
        {/* Breadcrumb */}
        <nav
          aria-label="Breadcrumb"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            fontSize: 'var(--font-size-body-sm)',
            color: 'var(--color-text-secondary)',
            marginBottom: 'var(--space-6)',
          }}
        >
          <Link to="/" style={{ color: 'var(--color-text-secondary)', textDecoration: 'none' }}>
            Queue
          </Link>
          <span aria-hidden="true" style={{ color: 'var(--color-text-disabled)' }}>/</span>
          <span style={{ color: 'var(--color-text-primary)', fontWeight: 600 }} aria-current="page">
            Code Verification
          </span>
        </nav>

        {/* Page header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 'var(--space-6)',
            flexWrap: 'wrap',
            gap: 'var(--space-4)',
          }}
        >
          <h1
            style={{
              fontSize: 'var(--font-size-heading-xl)',
              fontWeight: 600,
              color: 'var(--color-text-primary)',
              margin: 0,
            }}
          >
            Medical Code Verification
          </h1>
          <Link
            to="/"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 'var(--space-2)',
              padding: '0 var(--space-4)',
              height: 'var(--touch-target-min)',
              borderRadius: 'var(--radius-sm)',
              fontFamily: 'var(--font-family-default)',
              fontSize: 'var(--font-size-body-sm)',
              fontWeight: 600,
              border: '1.5px solid var(--color-border-default)',
              background: 'transparent',
              color: 'var(--color-brand-primary)',
              textDecoration: 'none',
            }}
          >
            ← Back to queue
          </Link>
        </div>

        {/* Finalized notice */}
        {finalized && (
          <div
            role="alert"
            style={{
              background: 'var(--color-warning-bg)',
              border: '1px solid var(--color-warning)',
              borderRadius: 'var(--radius-md)',
              padding: 'var(--space-4) var(--space-6)',
              marginBottom: 'var(--space-6)',
              fontSize: 'var(--font-size-body-md)',
              fontWeight: 600,
              color: 'var(--color-warning-text)',
            }}
          >
            ⚠ This encounter has been finalised. No further code changes are permitted.
          </div>
        )}

        {/* Progress summary */}
        {!loading && !error && (
          <div
            style={{
              background: 'var(--color-surface-default)',
              border: '1px solid var(--color-border-default)',
              borderRadius: 'var(--radius-md)',
              padding: 'var(--space-4) var(--space-6)',
              marginBottom: 'var(--space-6)',
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-6)',
              flexWrap: 'wrap',
            }}
            role="status"
            aria-live="polite"
            aria-label="Code review progress"
          >
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 'var(--font-size-heading-lg)', fontWeight: 700, color: 'var(--color-warning)' }}>
                {pendingCount}
              </div>
              <div style={{ fontSize: 'var(--font-size-label)', color: 'var(--color-text-secondary)' }}>Pending</div>
            </div>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 'var(--font-size-heading-lg)', fontWeight: 700, color: 'var(--color-success)' }}>
                {acceptedCount}
              </div>
              <div style={{ fontSize: 'var(--font-size-label)', color: 'var(--color-text-secondary)' }}>Accepted</div>
            </div>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 'var(--font-size-heading-lg)', fontWeight: 700, color: 'var(--color-brand-primary)' }}>
                {modifiedCount}
              </div>
              <div style={{ fontSize: 'var(--font-size-label)', color: 'var(--color-text-secondary)' }}>Modified</div>
            </div>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: 'var(--font-size-heading-lg)', fontWeight: 700, color: 'var(--color-danger)' }}>
                {rejectedCount}
              </div>
              <div style={{ fontSize: 'var(--font-size-label)', color: 'var(--color-text-secondary)' }}>Rejected</div>
            </div>
            <div style={{ marginLeft: 'auto', fontSize: 'var(--font-size-body-sm)', color: 'var(--color-text-secondary)' }}>
              {totalCount} code{totalCount !== 1 ? 's' : ''} · AI suggested
            </div>
          </div>
        )}

        {/* Loading state */}
        {loading && (
          <p
            role="status"
            aria-live="polite"
            style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--font-size-body-md)' }}
          >
            Loading code suggestions…
          </p>
        )}

        {/* Fetch error */}
        {error && (
          <div
            role="alert"
            style={{
              background: 'var(--color-danger-bg)',
              border: '1px solid var(--color-danger)',
              borderRadius: 'var(--radius-md)',
              padding: 'var(--space-4) var(--space-6)',
              marginBottom: 'var(--space-6)',
              fontSize: 'var(--font-size-body-md)',
              color: 'var(--color-danger-text)',
            }}
          >
            {error}
          </div>
        )}

        {/* Code verification table */}
        {!loading && !error && (
          <CodeVerificationTable
            encounterId={encounterId}
            suggestions={suggestions}
            verifiedRows={verifiedRows}
            rowErrors={rowErrors}
            allRejected={allRejected}
            finalized={finalized}
            onVerify={verify}
            onValidateCode={validateCode}
          />
        )}
      </main>
    </div>
  );
}
