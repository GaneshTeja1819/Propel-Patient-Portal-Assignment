import styles from './SlotGridSkeleton.module.css';

/**
 * Loading skeleton for appointment slot grid.
 * Renders 4 placeholder cards with CSS animation pulse.
 *
 * Edge case: Slow connection → loading skeleton shown on mount before data arrives
 */
export function SlotGridSkeleton() {
  return (
    <div className={styles.grid} role="status" aria-label="Loading appointment slots">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className={styles.skeletonCard}>
          <div className={styles.skeletonText} />
          <div className={styles.skeletonText} style={{ width: '80%' }} />
        </div>
      ))}
    </div>
  );
}
