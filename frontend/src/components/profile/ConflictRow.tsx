/**
 * ConflictRow.tsx — Individual conflict alert card (US_028, UXR-404).
 *
 * Displays a single DataConflictDto with:
 *  - Severity badge (High → --color-severity-high red, Medium → --color-severity-medium amber)
 *  - "New" badge when conflict.isNew === true
 *  - Competing values evidence box
 *  - Inline resolve form (radio select + note) on "Review & Resolve"
 *  - "Mark Reviewed" secondary action for non-critical triage
 *  - HTTP 409 inline error display per conflict
 *
 * Resolved / Reviewed conflicts render in a condensed read-only state.
 */
import { useState } from 'react';
import { DataConflictDto, ResolveConflictPayload } from '../../types/conflict';
import styles from './ConflictRow.module.css';

interface ConflictRowProps {
  conflict: DataConflictDto;
  inlineError?: string;
  onResolve: (conflictId: string, payload: ResolveConflictPayload) => Promise<void>;
  onMarkReviewed: (conflictId: string) => Promise<void>;
}

export function ConflictRow({
  conflict,
  inlineError,
  onResolve,
  onMarkReviewed,
}: ConflictRowProps): JSX.Element {
  const [resolveOpen, setResolveOpen] = useState(false);
  const [selectedValue, setSelectedValue] = useState(
    conflict.conflictingValues[0]?.value ?? '',
  );
  const [selectedSourceDocumentId, setSelectedSourceDocumentId] = useState(
    conflict.conflictingValues[0]?.sourceDocumentId,
  );
  const [otherValue, setOtherValue] = useState('');
  const [resolutionNote, setResolutionNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const OTHER_SENTINEL = '__other__';

  const isActioned = conflict.status === 'Resolved' || conflict.status === 'ReviewedUnresolved';
  const severityClass =
    conflict.severity === 'High' ? styles.severityHigh : styles.severityMedium;
  const severityIcon = conflict.severity === 'High' ? '⛔' : '⚠️';

  function handleOpenResolve() {
    setResolveOpen(true);
  }

  function handleCancelResolve() {
    setResolveOpen(false);
    setResolutionNote('');
    setOtherValue('');
    setSelectedValue(conflict.conflictingValues[0]?.value ?? '');
    setSelectedSourceDocumentId(conflict.conflictingValues[0]?.sourceDocumentId);
  }

  function handleSelectValue(value: string, sourceDocumentId?: string) {
    setSelectedValue(value);
    setSelectedSourceDocumentId(sourceDocumentId);
  }

  async function handleConfirmResolve() {
    const resolvedValue = selectedValue === OTHER_SENTINEL ? otherValue.trim() : selectedValue;
    if (!resolvedValue || submitting) return;
    setSubmitting(true);
    try {
      await onResolve(conflict.id, {
        authoritativeValue: resolvedValue,
        sourceDocumentId: selectedValue === OTHER_SENTINEL ? undefined : selectedSourceDocumentId,
        resolutionNote: resolutionNote.trim() || undefined,
      });
      setResolveOpen(false);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleMarkReviewed() {
    if (submitting) return;
    setSubmitting(true);
    try {
      await onMarkReviewed(conflict.id);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className={`${styles.conflictRow} ${severityClass}`}
      id={`conflict-${conflict.id}`}
      data-uxr="UXR-404"
      data-conflict-status={conflict.status}
    >
      <span className={styles.icon} aria-hidden="true">
        {severityIcon}
      </span>

      <div className={styles.body}>
        {/* Title + badges */}
        <div className={styles.titleRow}>
          <h3 className={styles.title}>{conflict.fieldName}</h3>
          <span
            className={`${styles.badge} ${
              conflict.severity === 'High' ? styles.badgeSeverityHigh : styles.badgeSeverityMedium
            }`}
            aria-label={`${conflict.severity} severity`}
          >
            {conflict.severity}
          </span>
          {conflict.isNew && (
            <span className={`${styles.badge} ${styles.badgeNew}`} aria-label="New conflict">
              New
            </span>
          )}
        </div>

        {/* Competing values evidence */}
        <div className={styles.evidenceBox} aria-label="Conflicting source values">
          <div className={styles.evidenceLabel}>Conflicting sources</div>
          {conflict.conflictingValues.map((cv, idx) => (
            <div key={idx} className={styles.evidenceSourceRow}>
              <span>
                {cv.isAiExtracted && (
                  <span className={styles.aiBadge} aria-label="AI-extracted">
                    🤖 AI
                  </span>
                )}
                {cv.sourceLabel ?? 'Unknown source'}
              </span>
              <span className={styles.evidenceValue}>{cv.value}</span>
            </div>
          ))}
        </div>

        {/* Meta info */}
        <div className={styles.meta}>
          {conflict.id} · Detected {new Date(conflict.detectedAt).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })} ·{' '}
          {conflict.status}
        </div>

        {/* Resolved read-only state */}
        {conflict.status === 'Resolved' && conflict.canonicalValue && (
          <div className={styles.resolvedMeta} role="status">
            ✓ Resolved: <strong>{conflict.canonicalValue}</strong>
            {conflict.resolvedAt &&
              ` on ${new Date(conflict.resolvedAt).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}`}
          </div>
        )}

        {/* Reviewed read-only state */}
        {conflict.status === 'ReviewedUnresolved' && (
          <div className={styles.reviewedMeta} role="status">
            Reviewed — awaiting resolution
          </div>
        )}

        {/* Inline error (e.g. HTTP 409) */}
        {inlineError && (
          <p className={styles.inlineError} role="alert">
            {inlineError}
          </p>
        )}

        {/* Actions — only for unactioned conflicts */}
        {!isActioned && !resolveOpen && (
          <div className={styles.actions}>
            <button
              className={styles.btnResolve}
              onClick={handleOpenResolve}
              aria-label={`Review and resolve: ${conflict.fieldName}`}
              id={`btn-resolve-${conflict.id}`}
            >
              Review &amp; Resolve
            </button>
            <button
              className={styles.btnMarkReviewed}
              onClick={handleMarkReviewed}
              disabled={submitting}
              aria-label={`Mark ${conflict.fieldName} as reviewed`}
            >
              Mark Reviewed
            </button>
          </div>
        )}

        {/* Inline resolve form */}
        {!isActioned && resolveOpen && (
          <div className={styles.resolveForm} role="group" aria-label="Resolve conflict form">
            <label className={styles.resolveFormLabel} id={`resolve-label-${conflict.id}`}>
              Select correct value
            </label>
            <div
              className={styles.radioGroup}
              role="radiogroup"
              aria-labelledby={`resolve-label-${conflict.id}`}
            >
              {conflict.conflictingValues.map((cv, idx) => (
                <label key={idx} className={styles.radioOption}>
                  <input
                    type="radio"
                    name={`conflict-val-${conflict.id}`}
                    value={cv.value}
                    checked={selectedValue === cv.value}
                    onChange={() => handleSelectValue(cv.value, cv.sourceDocumentId)}
                  />
                  <span>
                    <strong>{cv.value}</strong>
                    {cv.sourceLabel && ` — ${cv.sourceLabel}`}
                  </span>
                </label>
              ))}
              {/* Other free-text option */}
              <label className={styles.radioOption}>
                <input
                  type="radio"
                  name={`conflict-val-${conflict.id}`}
                  value={OTHER_SENTINEL}
                  checked={selectedValue === OTHER_SENTINEL}
                  onChange={() => handleSelectValue(OTHER_SENTINEL, undefined)}
                />
                <span>Other</span>
              </label>
              {selectedValue === OTHER_SENTINEL && (
                <input
                  type="text"
                  className={styles.otherInput}
                  value={otherValue}
                  onChange={(e) => setOtherValue(e.target.value)}
                  placeholder="Enter correct value…"
                  aria-label="Other value"
                  autoFocus
                />
              )}
            </div>

            <label
              className={styles.resolveFormLabel}
              htmlFor={`resolve-note-${conflict.id}`}
            >
              Resolution note (optional)
            </label>
            <textarea
              id={`resolve-note-${conflict.id}`}
              className={styles.noteTextarea}
              value={resolutionNote}
              onChange={(e) => setResolutionNote(e.target.value)}
              placeholder="Describe why you chose this value…"
              aria-label="Resolution note"
            />

            <p className={styles.auditNote}>
              Your resolution decision will be audit-logged with your staff ID and timestamp.
            </p>

            <div className={styles.actions}>
              <button
                className={styles.btnCancel}
                onClick={handleCancelResolve}
                type="button"
              >
                Cancel
              </button>
              <button
                className={styles.btnConfirmResolve}
                onClick={handleConfirmResolve}
                disabled={(selectedValue === OTHER_SENTINEL ? !otherValue.trim() : !selectedValue) || submitting}
                type="button"
                aria-label={`Confirm resolution: ${conflict.fieldName}`}
              >
                {submitting ? 'Resolving…' : 'Resolve conflict'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
