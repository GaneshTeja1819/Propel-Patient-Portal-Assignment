/**
 * ExtractionStatusBanner.tsx — AI extraction status feedback (US_026, AC-004, UXR-603).
 *
 * Renders below the upload form when a document is being processed by Gemini.
 * Four visual states match the wireframe-SCR-010 UXR-603 contract:
 *   processing → amber "AI extraction in progress…"     (role="status")
 *   completed  → green "Extraction complete"            (role="status")
 *   failed     → amber "Extraction failed…" + Retry CTA (role="alert")
 *   timeout    → amber "Taking longer…"    + Retry CTA  (role="alert")
 *
 * Design tokens: --color-success* / --color-warning* / variables.css
 * ARIA: role="status" for polite updates; role="alert" for assertive failures.
 */
import type { ExtractionStatus } from '../../hooks/useExtractionStatus';

interface ExtractionStatusBannerProps {
  status: ExtractionStatus;
  failureNote: string | null;
  onRetry: () => void;
}

export function ExtractionStatusBanner({
  status,
  failureNote,
  onRetry,
}: ExtractionStatusBannerProps): JSX.Element | null {
  if (status === 'idle') return null;

  const isAlert = status === 'failed' || status === 'timeout';
  const showRetry = isAlert;

  const bannerColors: React.CSSProperties =
    status === 'completed'
      ? {
          background: 'var(--color-success-bg)',
          border: '1px solid var(--color-success)',
          color: 'var(--color-success)',
        }
      : {
          // processing / failed / timeout → amber (UXR-603)
          background: 'var(--color-warning-bg)',
          border: '1px solid var(--color-warning)',
          color: 'var(--color-warning-text)',
        };

  const icon = {
    processing: '⏳',
    completed: '✓',
    failed: '⚠',
    timeout: 'ℹ',
  }[status];

  const message = {
    processing: 'AI extraction in progress\u2026',
    completed: 'Extraction complete',
    failed: 'Extraction failed \u2014 contact staff or retry',
    timeout: 'Extraction is taking longer than expected \u2014 check back later',
  }[status];

  return (
    <div
      role={isAlert ? 'alert' : 'status'}
      data-uxr="UXR-603"
      style={{
        marginTop: 'var(--space-4)',
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        borderRadius: 'var(--radius-sm)',
        padding: 'var(--space-3)',
        fontSize: 'var(--font-size-body-sm)',
        fontWeight: 600,
        ...bannerColors,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', flexWrap: 'wrap' }}>
        <span aria-hidden="true">{icon}</span>
        <span>{message}</span>
        {showRetry && (
          <button
            type="button"
            onClick={onRetry}
            aria-label="Retry AI extraction"
            style={{
              height: '34px',
              padding: '0 var(--space-3)',
              borderRadius: 'var(--radius-sm)',
              fontFamily: 'var(--font-family-default)',
              fontSize: 'var(--font-size-body-sm)',
              fontWeight: 600,
              cursor: 'pointer',
              background: 'var(--color-warning-bg)',
              color: 'var(--color-warning-text)',
              border: '1.5px solid var(--color-warning)',
              display: 'inline-flex',
              alignItems: 'center',
              gap: 'var(--space-1)',
              marginLeft: 'auto',
            }}
          >
            ↺ Retry extraction
          </button>
        )}
      </div>
      {failureNote && (
        <p
          style={{
            margin: 0,
            fontWeight: 400,
            fontSize: 'var(--font-size-caption)',
            color: 'var(--color-warning-text)',
          }}
        >
          {failureNote}
        </p>
      )}
    </div>
  );
}
