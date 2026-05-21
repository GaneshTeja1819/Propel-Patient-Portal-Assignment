/**
 * CodeVerificationRow.tsx — Per-row UI for SCR-014 (US_030, AC-001–AC-004).
 *
 * Renders a single MedicalCodeSuggestion with:
 *   - Code, description, AI badge, confidence bar + Low confidence badge (UXR-107 / UXR-403)
 *   - Accept / Modify / Reject action buttons
 *   - Inline edit field for Modify decision (codeset validation on blur)
 *   - Outcome badge after decision; all action buttons hidden (irreversible)
 *   - Finalized guard: all buttons disabled with tooltip (edge case)
 */
import { useState, useCallback } from 'react';
import type { MedicalCodeSuggestionDto, VerifyDecision, CodeValidateResponse } from '../../types/coding';

interface CodeVerificationRowProps {
  suggestion: MedicalCodeSuggestionDto;
  /** Current decision for this row, if any. */
  decision: VerifyDecision | undefined;
  /** Row-level API error (422/409). */
  rowError: string | undefined;
  /** When true all action buttons are disabled with a tooltip. */
  finalized: boolean;
  onVerify: (suggestionId: string, decision: VerifyDecision, verifiedCode?: string) => Promise<void>;
  onValidateCode: (code: string, codeType: string) => Promise<CodeValidateResponse>;
}

/** Shared button base style (avoids object literal repetition per DRY rule). */
const btnBase: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 'var(--space-2)',
  padding: '0 var(--space-4)',
  height: 'var(--touch-target-min)',   // 44px — WCAG 2.2 touch target
  borderRadius: 'var(--radius-sm)',
  fontFamily: 'var(--font-family-default)',
  fontSize: 'var(--font-size-body-sm)',
  fontWeight: 600,
  border: '1.5px solid transparent',
  cursor: 'pointer',
  transition: `background var(--duration-fast)`,
  whiteSpace: 'nowrap',
};

/** Inline badge style. */
const badgeBase: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: 'var(--space-1)',
  padding: '2px var(--space-2)',
  borderRadius: 'var(--radius-full)',
  fontSize: 'var(--font-size-label)',
  fontWeight: 700,
  whiteSpace: 'nowrap',
};

export function CodeVerificationRow({
  suggestion,
  decision,
  rowError,
  finalized,
  onVerify,
  onValidateCode,
}: CodeVerificationRowProps): JSX.Element {
  const [modifyOpen,       setModifyOpen]       = useState(false);
  const [modifyCode,       setModifyCode]       = useState('');
  const [modifyError,      setModifyError]      = useState('');
  const [modifyValid,      setModifyValid]      = useState(false);
  const [modifyDesc,       setModifyDesc]       = useState('');
  const [validating,       setValidating]       = useState(false);
  const [submitting,       setSubmitting]       = useState(false);

  const isLowConfidence    = suggestion.confidenceScore < 0.5;
  const confidencePercent  = Math.round(suggestion.confidenceScore * 100);

  // Confidence bar colour (wireframe thresholds: ≥75% green, 50–74% amber, <50% red)
  let barColor: string;
  if (suggestion.confidenceScore >= 0.75)      barColor = 'var(--color-success)';
  else if (suggestion.confidenceScore >= 0.5)  barColor = 'var(--color-warning)';
  else                                          barColor = 'var(--color-danger)';

  // Left-border accent mirrors wireframe .code-row.accepted / .modified / .rejected
  let borderLeft = '4px solid transparent';
  if (decision === 'Accepted')  borderLeft = '4px solid var(--color-success)';
  else if (decision === 'Modified') borderLeft = '4px solid var(--color-brand-primary)';
  else if (decision === 'Rejected') borderLeft = '4px solid var(--color-text-disabled)';

  const actionsDisabled = finalized || !!decision || submitting;
  const finalizedTitle  = finalized ? 'This encounter has been finalised' : undefined;

  // ── Handlers ────────────────────────────────────────────────────────────────

  const handleAccept = useCallback(async () => {
    setSubmitting(true);
    await onVerify(suggestion.id, 'Accepted');
    setSubmitting(false);
  }, [suggestion.id, onVerify]);

  const handleReject = useCallback(async () => {
    setSubmitting(true);
    await onVerify(suggestion.id, 'Rejected');
    setSubmitting(false);
  }, [suggestion.id, onVerify]);

  const handleModifyOpen = useCallback(() => {
    setModifyCode('');
    setModifyError('');
    setModifyValid(false);
    setModifyDesc('');
    setModifyOpen(true);
  }, []);

  const handleModifyCancel = useCallback(() => {
    setModifyOpen(false);
    setModifyCode('');
    setModifyError('');
    setModifyValid(false);
  }, []);

  const handleModifyBlur = useCallback(async () => {
    const trimmed = modifyCode.trim();
    if (!trimmed) return;
    setValidating(true);
    setModifyError('');
    setModifyValid(false);
    const result = await onValidateCode(trimmed, suggestion.codeSystem);
    setValidating(false);
    if (result.valid) {
      setModifyValid(true);
      setModifyDesc(result.description ?? '');
    } else {
      setModifyError('Code not found in codeset');
    }
  }, [modifyCode, suggestion.codeSystem, onValidateCode]);

  const handleModifyConfirm = useCallback(async () => {
    if (!modifyValid || !modifyCode.trim()) return;
    setSubmitting(true);
    await onVerify(suggestion.id, 'Modified', modifyCode.trim());
    setModifyOpen(false);
    setSubmitting(false);
  }, [modifyValid, modifyCode, suggestion.id, onVerify]);

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div
      style={{
        background: 'var(--color-surface-default)',
        border: '1px solid var(--color-border-default)',
        borderLeft,
        borderRadius: 'var(--radius-md)',
        padding: 'var(--space-4) var(--space-6)',
        marginBottom: 'var(--space-4)',
        opacity: decision === 'Rejected' ? 0.6 : 1,
        transition: `border-color var(--duration-normal)`,
      }}
      data-uxr="UXR-107 UXR-403"
      data-testid={`code-row-${suggestion.id}`}
    >
      {/* ── Code info ─────────────────────────────────────────────────────── */}
      <div
        style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          gap: 'var(--space-4)',
          marginBottom: 'var(--space-3)',
          flexWrap: 'wrap',
        }}
      >
        <div>
          {/* Code + description + AI badge + decision badge */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', flexWrap: 'wrap' }}>
            <span style={{ fontFamily: 'monospace', fontSize: 'var(--font-size-heading-lg)', fontWeight: 700 }}>
              {suggestion.suggestedCode}
            </span>
            <span style={{ fontSize: 'var(--font-size-body-md)', fontWeight: 600 }}>
              {suggestion.description}
            </span>

            {/* AI badge (UXR-403) */}
            <span
              style={{ ...badgeBase, background: 'var(--color-ai-accent-bg)', color: 'var(--color-ai-accent)', border: '1px solid var(--color-border-ai)' }}
              aria-label="AI suggested"
            >
              <span aria-hidden="true">🤖</span> AI
            </span>

            {/* Decision outcome badge — replaces "Pending" once decided */}
            {!decision && (
              <span style={{ ...badgeBase, background: 'var(--color-warning-bg)', color: 'var(--color-warning-text)' }} role="status" aria-live="polite">
                ⏳ Pending
              </span>
            )}
            {decision === 'Accepted' && (
              <span style={{ ...badgeBase, background: 'var(--color-success-bg)', color: 'var(--color-success-text)' }} role="status">
                ✓ Accepted
              </span>
            )}
            {decision === 'Modified' && (
              <span style={{ ...badgeBase, background: 'var(--color-brand-primary-light)', color: 'var(--color-brand-primary)' }} role="status">
                ✏ Modified
              </span>
            )}
            {decision === 'Rejected' && (
              <span style={{ ...badgeBase, background: 'var(--color-surface-subtle)', color: 'var(--color-text-disabled)', textDecoration: 'line-through' }} role="status">
                ✕ Rejected
              </span>
            )}
          </div>

          {/* Meta row: code system, rank, confidence bar, low-confidence badge */}
          <div
            style={{
              fontSize: 'var(--font-size-body-sm)',
              color: 'var(--color-text-secondary)',
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-3)',
              marginTop: 'var(--space-2)',
              flexWrap: 'wrap',
            }}
          >
            <span>{suggestion.codeSystem}</span>
            <span>Rank: {suggestion.rank}</span>
            <span>Confidence:</span>
            <div
              style={{ width: '80px', height: '8px', background: 'var(--color-border-default)', borderRadius: 'var(--radius-full)', overflow: 'hidden' }}
              role="img"
              aria-label={`Confidence: ${confidencePercent}%`}
            >
              <div style={{ height: '100%', width: `${confidencePercent}%`, background: barColor, borderRadius: 'var(--radius-full)' }} />
            </div>
            <strong>{confidencePercent}%</strong>

            {/* Low confidence badge — AC-001 / UXR-107 */}
            {isLowConfidence && (
              <span
                style={{ ...badgeBase, background: 'var(--color-warning-bg)', color: 'var(--color-warning-text)' }}
                role="img"
                aria-label="Low confidence — verify carefully"
                data-uxr="UXR-107"
              >
                ⚠ Low confidence
              </span>
            )}
          </div>
        </div>
      </div>

      {/* ── Inline modify edit (AC-003) ───────────────────────────────────── */}
      {modifyOpen && !decision && (
        <div
          style={{
            background: 'var(--color-surface-muted)',
            border: '1px solid var(--color-border-default)',
            borderRadius: 'var(--radius-sm)',
            padding: 'var(--space-3)',
            marginBottom: 'var(--space-3)',
          }}
        >
          <label
            htmlFor={`modify-input-${suggestion.id}`}
            style={{
              display: 'block',
              fontSize: 'var(--font-size-label)',
              fontWeight: 600,
              color: 'var(--color-text-secondary)',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
              marginBottom: 'var(--space-1)',
            }}
          >
            Enter {suggestion.codeSystem} code{' '}
            <span style={{ color: 'var(--color-danger)' }} aria-hidden="true">*</span>
          </label>
          <input
            id={`modify-input-${suggestion.id}`}
            type="text"
            value={modifyCode}
            placeholder={`e.g. ${suggestion.suggestedCode}`}
            aria-required="true"
            aria-invalid={!!modifyError}
            aria-describedby={modifyError ? `modify-error-${suggestion.id}` : undefined}
            onChange={e => {
              setModifyCode(e.target.value);
              setModifyValid(false);
              setModifyError('');
              setModifyDesc('');
            }}
            onBlur={handleModifyBlur}
            style={{
              width: '100%',
              maxWidth: '280px',
              padding: 'var(--space-2) var(--space-3)',
              border: `1.5px solid ${modifyError ? 'var(--color-danger)' : 'var(--color-border-default)'}`,
              borderRadius: 'var(--radius-sm)',
              fontFamily: 'monospace',
              fontSize: 'var(--font-size-body-md)',
              height: 'var(--touch-target-min)',
            }}
          />

          {validating && (
            <p style={{ fontSize: 'var(--font-size-caption)', color: 'var(--color-text-secondary)', marginTop: 'var(--space-1)' }}>
              Validating…
            </p>
          )}

          {/* WCAG 3.3.3 — inline error suggestion */}
          {modifyError && !validating && (
            <p
              id={`modify-error-${suggestion.id}`}
              role="alert"
              style={{ fontSize: 'var(--font-size-caption)', color: 'var(--color-danger-text)', marginTop: 'var(--space-1)' }}
            >
              {modifyError}
            </p>
          )}

          {modifyValid && modifyDesc && (
            <p style={{ fontSize: 'var(--font-size-caption)', color: 'var(--color-success-text)', marginTop: 'var(--space-1)' }}>
              ✓ {modifyDesc}
            </p>
          )}

          <div style={{ display: 'flex', gap: 'var(--space-2)', marginTop: 'var(--space-3)' }}>
            {modifyValid && (
              <button
                onClick={handleModifyConfirm}
                disabled={submitting}
                aria-label={`Confirm modified code ${modifyCode} for ${suggestion.suggestedCode}`}
                style={{
                  ...btnBase,
                  background: 'var(--color-brand-primary)',
                  color: 'var(--color-text-on-primary)',
                  borderColor: 'var(--color-brand-primary)',
                  opacity: submitting ? 0.5 : 1,
                  cursor: submitting ? 'not-allowed' : 'pointer',
                }}
              >
                {submitting ? 'Saving…' : 'Confirm'}
              </button>
            )}
            <button
              onClick={handleModifyCancel}
              style={{
                ...btnBase,
                background: 'transparent',
                color: 'var(--color-brand-primary)',
                borderColor: 'var(--color-border-default)',
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* ── Row-level API error ───────────────────────────────────────────── */}
      {rowError && (
        <p
          role="alert"
          style={{ fontSize: 'var(--font-size-caption)', color: 'var(--color-danger-text)', marginBottom: 'var(--space-2)' }}
        >
          {rowError}
        </p>
      )}

      {/* ── Action buttons — hidden once a decision is recorded (irreversible) */}
      {!decision && (
        <div style={{ display: 'flex', gap: 'var(--space-2)', flexWrap: 'wrap' }}>
          <button
            onClick={handleAccept}
            disabled={actionsDisabled}
            title={finalizedTitle}
            aria-label={`Accept ${suggestion.suggestedCode} — ${suggestion.description}`}
            style={{
              ...btnBase,
              background: 'var(--color-success-bg)',
              color: 'var(--color-success-text)',
              borderColor: 'var(--color-success)',
              opacity: actionsDisabled ? 0.5 : 1,
              cursor: actionsDisabled ? 'not-allowed' : 'pointer',
            }}
          >
            ✓ Accept
          </button>

          {!modifyOpen && (
            <button
              onClick={handleModifyOpen}
              disabled={actionsDisabled}
              title={finalizedTitle}
              aria-label={`Modify ${suggestion.suggestedCode}`}
              style={{
                ...btnBase,
                background: 'var(--color-brand-primary-light)',
                color: 'var(--color-brand-primary)',
                borderColor: 'var(--color-brand-primary)',
                opacity: actionsDisabled ? 0.5 : 1,
                cursor: actionsDisabled ? 'not-allowed' : 'pointer',
              }}
            >
              ✏ Modify
            </button>
          )}

          <button
            onClick={handleReject}
            disabled={actionsDisabled}
            title={finalizedTitle}
            aria-label={`Reject ${suggestion.suggestedCode}`}
            style={{
              ...btnBase,
              background: 'var(--color-danger-bg)',
              color: 'var(--color-danger-text)',
              borderColor: 'var(--color-danger)',
              opacity: actionsDisabled ? 0.5 : 1,
              cursor: actionsDisabled ? 'not-allowed' : 'pointer',
            }}
          >
            ✕ Reject
          </button>
        </div>
      )}
    </div>
  );
}
