/**
 * IntakeProgressBar.tsx — UXR-502 step progress indicator for AI intake (US_018).
 *
 * Renders "Question N of M" label alongside a CSS progress bar that advances
 * with each answered intake question. Fully accessible per WCAG 2.2 SC 4.1.3.
 */
import styles from './IntakeProgressBar.module.css';

interface IntakeProgressBarProps {
  /** 1-based current step number shown in the label. */
  current: number;
  /** Total number of steps. */
  total: number;
  /** Override the progress bar fill percentage (0-100). Defaults to current/total*100. */
  progressValue?: number;
  /** Label prefix before "N of M" — e.g. "Question" or "Section". Defaults to "Question". */
  labelPrefix?: string;
  /** Optional title for the intake session (e.g. appointment description). */
  sessionTitle?: string;
  /** F-003: forward data-uxr attribute to the root element for UXR compliance tooling. */
  'data-uxr'?: string;
}

export function IntakeProgressBar({
  current,
  total,
  progressValue,
  labelPrefix = 'Question',
  sessionTitle,
  'data-uxr': dataUxr,
}: IntakeProgressBarProps): JSX.Element {
  const pct = progressValue !== undefined ? progressValue : (total > 0 ? Math.round((current / total) * 100) : 0);

  return (
    <div className={styles.wrap} role="region" aria-label="Intake progress" data-uxr={dataUxr}>
      <div className={styles.header}>
        {sessionTitle ? (
          <span className={styles.label}>{sessionTitle}</span>
        ) : (
          <span className={styles.label}>Intake</span>
        )}
        <span className={styles.count} aria-label={`${labelPrefix} ${current} of ${total}`}>
          {labelPrefix} {current} of {total}
        </span>
      </div>
      <div
        className={styles.track}
        role="progressbar"
        aria-valuenow={pct}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${pct}% complete`}
      >
        <div className={styles.fill} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}
