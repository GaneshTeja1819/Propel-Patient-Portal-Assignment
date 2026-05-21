/**
 * DocumentUploadPage.tsx — SCR-010 Clinical Documents page (US_025).
 *
 * Renders the document upload form and upload status feedback.
 * Matches wireframe-SCR-010-document-upload.html layout:
 * - Two-column grid on wide screens (upload card left, doc list right)
 * - Progress bar while uploading
 * - Success banner on HTTP 201
 * - Storage error / generic error banner on failure
 */
import { useCallback } from 'react';
import { Link } from 'react-router-dom';
import { DocumentUploadForm } from '../components/documents/DocumentUploadForm';
import { ExtractionStatusBanner } from '../components/documents/ExtractionStatusBanner';
import { useDocumentUpload } from '../hooks/useDocumentUpload';
import { useExtractionStatus } from '../hooks/useExtractionStatus';

export function DocumentUploadPage(): JSX.Element {
  const { upload, progress, status, errorMessage, documentId, reset } = useDocumentUpload();
  // Poll extraction status once documentId is available after a successful upload.
  // documentId is null until upload succeeds, so the hook stays idle until then.
  const { status: extractionStatus, failureNote, retry } = useExtractionStatus(documentId);

  const handleUpload = useCallback(
    (file: File, documentType: string) => {
      reset();
      upload(file, documentType);
    },
    [upload, reset],
  );

  const isUploading = status === 'uploading';

  return (
    <div
      style={{
        minHeight: '100vh',
        background: 'var(--color-surface-muted)',
        fontFamily: 'var(--font-family-default)',
        color: 'var(--color-text-primary)',
      }}
    >
      {/* Top nav */}
      <nav
        aria-label="Main navigation"
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
                background: 'var(--color-brand-primary)',
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
              P
            </div>
            <span>Patient Portal</span>
          </Link>
          <nav aria-label="Page navigation" style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-1)' }}>
            <Link to="/" style={navLinkStyle}>🏠 Dashboard</Link>
            <span style={{ ...navLinkStyle, fontWeight: 600, background: 'var(--color-brand-primary-light)', color: 'var(--color-brand-primary)' }} aria-current="page">
              📄 Documents
            </span>
          </nav>
        </div>
      </nav>

      <main
        role="main"
        style={{ maxWidth: '1280px', margin: '0 auto', padding: 'var(--space-8)' }}
      >
        {/* Breadcrumb */}
        <nav aria-label="Breadcrumb" style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', fontSize: 'var(--font-size-body-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-6)' }}>
          <Link to="/" style={{ color: 'var(--color-text-secondary)', textDecoration: 'none' }}>Dashboard</Link>
          <span aria-hidden="true" style={{ color: 'var(--color-text-disabled)' }}>/</span>
          <span style={{ color: 'var(--color-text-primary)', fontWeight: 600 }} aria-current="page">Documents</span>
        </nav>

        {/* Page header */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 'var(--space-6)', flexWrap: 'wrap', gap: 'var(--space-4)' }}>
          <div>
            <h1 style={{ fontSize: 'var(--font-size-heading-xl)', fontWeight: 600, color: 'var(--color-text-primary)' }}>
              Clinical Documents
            </h1>
            <p style={{ fontSize: 'var(--font-size-body-sm)', color: 'var(--color-text-secondary)', marginTop: 'var(--space-1)' }}>
              Upload PDFs of your medical records, lab results, or post-visit summaries. AI will extract clinical data automatically.
            </p>
          </div>
        </div>

        {/* Two-column grid: upload card + doc list */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: 'var(--space-8)', alignItems: 'flex-start' }}>
          {/* Upload card */}
          <div>
            <div style={cardStyle}>
              <div style={cardHeaderStyle}>
                <span aria-hidden="true">📤</span>
                <h2 style={{ fontSize: 'var(--font-size-heading-sm)', fontWeight: 600, color: 'var(--color-text-primary)' }}>
                  Upload document
                </h2>
              </div>
              <div style={{ padding: 'var(--space-6)' }}>
                <DocumentUploadForm onUpload={handleUpload} isUploading={isUploading} />

                {/* Upload progress bar */}
                {isUploading && progress !== null && (
                  <div style={{ marginTop: 'var(--space-4)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 'var(--font-size-body-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-1)' }}>
                      <span>Uploading…</span>
                      <span aria-live="polite">{progress}%</span>
                    </div>
                    <div
                      role="progressbar"
                      aria-valuenow={progress}
                      aria-valuemin={0}
                      aria-valuemax={100}
                      aria-label="Upload progress"
                      style={{ height: '4px', background: 'var(--color-border-default)', borderRadius: 'var(--radius-full)', overflow: 'hidden' }}
                    >
                      <div
                        style={{
                          height: '100%',
                          background: 'var(--color-brand-primary)',
                          borderRadius: 'var(--radius-full)',
                          transition: 'width 0.3s',
                          width: `${progress}%`,
                        }}
                      />
                    </div>
                  </div>
                )}

                {/* Success banner (AC-001) */}
                {status === 'success' && (
                  <div
                    role="status"
                    aria-live="polite"
                    style={{
                      marginTop: 'var(--space-4)',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 'var(--space-2)',
                      background: 'var(--color-success-bg)',
                      border: '1px solid var(--color-success)',
                      borderRadius: 'var(--radius-sm)',
                      padding: 'var(--space-3)',
                      fontSize: 'var(--font-size-body-sm)',
                      color: 'var(--color-success)',
                      fontWeight: 600,
                    }}
                  >
                    <span aria-hidden="true">✓</span> Document uploaded successfully.
                    {documentId && (
                      <span style={{ fontWeight: 400, color: 'var(--color-text-secondary)', marginLeft: 'var(--space-1)' }}>
                        AI extraction queued.
                      </span>
                    )}
                  </div>
                )}

                {/* Storage error banner */}
                {status === 'storage-error' && (
                  <div
                    role="alert"
                    aria-live="assertive"
                    style={errorBannerStyle}
                  >
                    <span aria-hidden="true">⚠</span> {errorMessage}
                  </div>
                )}

                {/* Generic error banner */}
                {status === 'error' && (
                  <div
                    role="alert"
                    aria-live="assertive"
                    style={errorBannerStyle}
                  >
                    <span aria-hidden="true">✕</span> {errorMessage}
                  </div>
                )}

                {/* AI extraction status (AC-004, UXR-603) — shown after successful upload */}
                {documentId !== null && (
                  <ExtractionStatusBanner
                    status={extractionStatus}
                    failureNote={failureNote}
                    onRetry={retry}
                  />
                )}
              </div>
            </div>
          </div>

          {/* Document list placeholder — populated by backend in production */}
          <div>
            <div style={cardStyle}>
              <div style={cardHeaderStyle}>
                <span aria-hidden="true">📁</span>
                <h2 style={{ fontSize: 'var(--font-size-heading-sm)', fontWeight: 600, color: 'var(--color-text-primary)' }}>
                  Your documents
                </h2>
              </div>
              <div style={{ padding: 'var(--space-6)', color: 'var(--color-text-secondary)', fontSize: 'var(--font-size-body-sm)' }}>
                <p>Your uploaded documents will appear here once the document list endpoint (US_025 task_002) is connected.</p>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

// ── Shared styles ──────────────────────────────────────────────────────────────

const navLinkStyle: React.CSSProperties = {
  padding: 'var(--space-2) var(--space-3)',
  borderRadius: 'var(--radius-sm)',
  fontSize: 'var(--font-size-body-md)',
  color: 'var(--color-text-secondary)',
  fontWeight: 500,
  textDecoration: 'none',
};

const cardStyle: React.CSSProperties = {
  background: 'var(--color-surface-default)',
  border: '1px solid var(--color-border-default)',
  borderRadius: 'var(--radius-md)',
  boxShadow: 'var(--shadow-sm)',
};

const cardHeaderStyle: React.CSSProperties = {
  padding: 'var(--space-4) var(--space-6)',
  borderBottom: '1px solid var(--color-border-default)',
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-3)',
};

const errorBannerStyle: React.CSSProperties = {
  marginTop: 'var(--space-4)',
  display: 'flex',
  alignItems: 'center',
  gap: 'var(--space-2)',
  background: 'var(--color-danger-bg)',
  border: '1px solid var(--color-danger)',
  borderRadius: 'var(--radius-sm)',
  padding: 'var(--space-3)',
  fontSize: 'var(--font-size-body-sm)',
  color: 'var(--color-danger-text)',
  fontWeight: 600,
};
