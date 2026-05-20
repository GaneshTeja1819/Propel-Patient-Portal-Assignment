/**
 * ClinicalSection.tsx — Collapsible clinical data section for the 360° profile (US_027).
 *
 * Features:
 *  - Collapsible (aria-expanded / aria-controls) per wireframe-SCR-009 .ps-header pattern
 *  - "No data available" empty state when items is empty (AC-001)
 *  - Client-side pagination at ITEMS_PER_PAGE = 20 rows (edge case: > 50 entries)
 *  - AI-extracted rows: left border accent + AI badge (UXR-403)
 *  - PHI fields: 🔒 prefix icon with aria-label="PHI" (UXR-402)
 *  - In patient mode: AI-extracted badge shown; in staff mode: badge hidden (cleaner view)
 */
import { useState } from 'react';
import { ClinicalDataItem } from '../../types/profile';
import styles from './ClinicalSection.module.css';

const ITEMS_PER_PAGE = 20;

interface ClinicalSectionProps {
  sectionId: string;
  title: string;
  icon: string;
  items: ClinicalDataItem[];
  mode: 'patient' | 'staff';
  defaultExpanded?: boolean;
  isLoading?: boolean;
}

export function ClinicalSection({
  sectionId,
  title,
  icon,
  items,
  mode,
  defaultExpanded = false,
  isLoading = false,
}: ClinicalSectionProps): JSX.Element {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [page, setPage] = useState(1);

  const bodyId = `${sectionId}-body`;
  const totalPages = Math.ceil(items.length / ITEMS_PER_PAGE);
  const pageItems = items.slice((page - 1) * ITEMS_PER_PAGE, page * ITEMS_PER_PAGE);

  const hasAiItems = items.some((item) => item.isAiExtracted);

  function handleToggle() {
    setExpanded((prev) => !prev);
  }

  return (
    <div className={styles.section} id={sectionId} data-uxr="UXR-106">
      <button
        type="button"
        className={styles.sectionHeader}
        onClick={handleToggle}
        aria-expanded={expanded}
        aria-controls={bodyId}
      >
        <div className={styles.titleRow}>
          <span className={styles.sectionIcon} aria-hidden="true">{icon}</span>
          <span className={styles.sectionTitle}>{title}</span>
        </div>
        <div className={styles.badgeRow}>
          {hasAiItems && (
            <span className={styles.badgeAi} aria-label="Contains AI-extracted data">
              <span aria-hidden="true">🤖</span> AI
            </span>
          )}
          <span
            className={`${styles.chevron} ${expanded ? styles.chevronExpanded : ''}`}
            aria-hidden="true"
          >
            ›
          </span>
        </div>
      </button>

      {expanded && (
        <div className={styles.sectionBody} id={bodyId}>
          {isLoading && (
            <div className={styles.dataRow} aria-label="Loading…">
              <span className={styles.dataLabel} style={{ color: 'var(--color-text-disabled)' }}>
                Loading…
              </span>
              <span className={styles.dataValue}>
                <span className={styles.skeleton} aria-hidden="true">&nbsp;</span>
              </span>
            </div>
          )}

          {!isLoading && items.length === 0 && (
            <p className={styles.emptyState} role="status">
              No data available
            </p>
          )}

          {!isLoading && pageItems.map((item) => (
            <DataRow key={item.id} item={item} mode={mode} />
          ))}

          {!isLoading && totalPages > 1 && (
            <div className={styles.pagination} role="navigation" aria-label={`${title} pagination`}>
              <button
                type="button"
                className={styles.pageBtn}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                aria-label="Previous page"
              >
                ‹ Prev
              </button>
              <span aria-live="polite" aria-atomic="true">
                Page {page} of {totalPages}
              </span>
              <button
                type="button"
                className={styles.pageBtn}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                aria-label="Next page"
              >
                Next ›
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

interface DataRowProps {
  item: ClinicalDataItem;
  mode: 'patient' | 'staff';
}

function DataRow({ item, mode }: DataRowProps): JSX.Element {
  const rowClass = item.isAiExtracted
    ? `${styles.dataRow} ${styles.aiAccentRow}`
    : styles.dataRow;

  return (
    <div className={rowClass}>
      <span className={styles.dataLabel}>
        {item.isPhiField && (
          <span className={styles.phiIcon} aria-label="PHI">🔒</span>
        )}
        {item.label}
      </span>
      <span className={styles.dataValue}>
        {item.value}
        {item.isAiExtracted && mode === 'patient' && (
          <span className={styles.badgeAi} data-uxr="UXR-403">
            <span aria-hidden="true">🤖</span> AI-extracted
          </span>
        )}
        {!item.isAiExtracted && (
          <span className={styles.badgeVerified}>
            ✓ Verified
          </span>
        )}
      </span>
    </div>
  );
}
