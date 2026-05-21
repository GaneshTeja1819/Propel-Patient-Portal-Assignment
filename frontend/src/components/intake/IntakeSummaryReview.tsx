/**
 * IntakeSummaryReview.tsx — AC-003: display captured intake fields for
 * patient review and inline editing before final confirmation (US_018).
 *
 * No IntakeRecord is persisted until the patient clicks "Confirm".
 * PHI fields are highlighted with `color-surface-phi` and a 🔒 icon (UXR-402).
 * AI-captured content is annotated with an `color-ai-accent` badge.
 */
import { useState, useId, useRef, useEffect } from 'react';
import { CapturedField } from '../../types/intake';
import styles from './IntakeSummaryReview.module.css';

/** Known PHI field keys — must match IntakeQuestions.json on the backend. */
const PHI_FIELD_KEYS = new Set([
  'chiefComplaint',
  'allergies',
  'currentMedications',
  'smokingStatus',
  'alcoholUse',
  'exerciseHabits',
  'additionalSymptoms',
  'notes',
]);

function toLabel(fieldKey: string): string {
  return fieldKey
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (c) => c.toUpperCase())
    .trim();
}

interface FieldCardProps {
  field: CapturedField;
  onSave: (fieldKey: string, value: string) => void;
}

function FieldCard({ field, onSave }: FieldCardProps): JSX.Element {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(field.value);
  const inputId = useId();
  const editRef = useRef<HTMLTextAreaElement>(null);
  const isPhi = PHI_FIELD_KEYS.has(field.fieldKey);
  const label = field.fieldLabel !== field.fieldKey ? field.fieldLabel : toLabel(field.fieldKey);

  // Focus the edit textarea when entering edit mode (WCAG 2.1 SC 3.2.2)
  useEffect(() => {
    if (editing) {
      editRef.current?.focus();
    }
  }, [editing]);

  function handleSave(): void {
    onSave(field.fieldKey, draft);
    setEditing(false);
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSave();
    }
    if (e.key === 'Escape') {
      setDraft(field.value);
      setEditing(false);
    }
  }

  return (
    <div
      className={`${styles.fieldCard}${isPhi ? ` ${styles.phiField}` : ''}`}
      data-uxr="UXR-402"
    >
      <div className={styles.fieldInfo}>
        <div className={styles.fieldLabel}>
          {isPhi && (
            <span className={styles.phiIcon} aria-label="Protected health information">
              🔒
            </span>
          )}
          <label htmlFor={editing ? inputId : undefined}>{label}</label>
        </div>

        {editing ? (
          <textarea
            ref={editRef}
            id={inputId}
            className={styles.editInput}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={handleKeyDown}
            rows={3}
            aria-label={`Edit ${label}`}
          />
        ) : (
          <p className={`${styles.fieldValue}${!field.value ? ` ${styles.empty}` : ''}`}>
            {field.value || 'Not provided'}
          </p>
        )}
      </div>

      {editing ? (
        <button
          className={styles.btnEdit}
          onClick={handleSave}
          aria-label={`Save ${label}`}
        >
          Save
        </button>
      ) : (
        <button
          className={styles.btnEdit}
          onClick={() => {
            setDraft(field.value);
            setEditing(true);
          }}
          aria-label={`Edit ${label}`}
        >
          Edit
        </button>
      )}
    </div>
  );
}

interface IntakeSummaryReviewProps {
  fields: CapturedField[];
  isConfirming: boolean;
  onFieldSave: (fieldKey: string, value: string) => void;
  onConfirm: () => void;
}

export function IntakeSummaryReview({
  fields,
  isConfirming,
  onFieldSave,
  onConfirm,
}: IntakeSummaryReviewProps): JSX.Element {
  return (
    <div className={styles.wrap} aria-labelledby="summary-heading">
      <div className={styles.aiBadge} data-uxr="UXR-402">
        <span aria-hidden="true">🤖</span> AI-captured
      </div>
      <h1 id="summary-heading" className={styles.heading}>
        Review Your Intake Responses
      </h1>
      <p className={styles.subheading}>
        Please review the information below. You can edit any field before confirming.
        <br />
        <strong>Your intake will not be submitted until you click Confirm.</strong>
      </p>

      <div className={styles.fieldList} role="list" aria-label="Captured intake fields">
        {fields.map((field) => (
          <div key={field.fieldKey} role="listitem">
            <FieldCard field={field} onSave={onFieldSave} />
          </div>
        ))}
      </div>

      <div className={styles.actions}>
        <button
          className={styles.btnConfirm}
          onClick={onConfirm}
          disabled={isConfirming}
          aria-busy={isConfirming}
          aria-label="Confirm and submit intake"
        >
          {isConfirming ? 'Submitting…' : 'Confirm & Submit'}
        </button>
      </div>
    </div>
  );
}
