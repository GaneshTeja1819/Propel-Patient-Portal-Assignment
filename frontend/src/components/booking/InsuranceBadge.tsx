import styles from './InsuranceBadge.module.css';

export interface InsuranceBadgeProps {
  status: 'validated' | 'not-recognised' | null;
}

/**
 * Inline insurance soft-validation badge.
 *
 * AC-003: Shows "Validated" (green) or "Not Recognised" (amber) depending on validation result;
 * renders nothing when fields are blank; never blocks the booking CTA.
 */
export function InsuranceBadge({ status }: InsuranceBadgeProps) {
  if (status === null) {
    return null;
  }

  const isValid = status === 'validated';
  const badgeClass = isValid ? styles.validated : styles.notRecognised;
  const badgeText = isValid ? '✓ Validated' : '⚠ Not Recognised';

  return (
    <div className={`${styles.badge} ${badgeClass}`} role="status">
      {badgeText}
    </div>
  );
}
