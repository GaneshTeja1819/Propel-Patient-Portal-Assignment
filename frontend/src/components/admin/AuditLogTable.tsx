import { Fragment, useCallback, useEffect, useState } from 'react';
import type { AuditEvent } from '../../hooks/useAuditLog';
import { AuditEventDetailRow } from './AuditEventDetailRow';
import styles from './AuditLogTable.module.css';

type ActionVariant = 'auth' | 'create' | 'update' | 'delete' | 'view' | 'export' | 'failure';

function getActionVariant(actionType: string, status: 'Success' | 'Failure'): ActionVariant {
  if (status === 'Failure') return 'failure';
  const t = actionType.toUpperCase();
  if (t.includes('EXPORT') || t.includes('DOWNLOAD')) return 'export';
  if (t.includes('LOGIN') || t.includes('LOGOUT') || t.includes('AUTH') || t.includes('SESSION_'))
    return 'auth';
  if (
    t.endsWith('_CREATED') ||
    t.endsWith('_BOOKED') ||
    t.endsWith('_CONNECTED') ||
    t.endsWith('_UPLOADED') ||
    t.endsWith('_REGISTERED') ||
    t.endsWith('_SUBMITTED')
  )
    return 'create';
  if (
    t.endsWith('_DELETED') ||
    t.endsWith('_DEACTIVATED') ||
    t.endsWith('_REMOVED') ||
    t.endsWith('_CANCELLED')
  )
    return 'delete';
  if (
    t.endsWith('_CHANGED') ||
    t.endsWith('_UPDATED') ||
    t.endsWith('_MODIFIED') ||
    t.endsWith('_VERIFIED')
  )
    return 'update';
  return 'view';
}

const ACTION_CLASS: Record<ActionVariant, string> = {
  auth: styles.actionAuth,
  create: styles.actionCreate,
  update: styles.actionUpdate,
  delete: styles.actionDelete,
  view: styles.actionView,
  export: styles.actionExport,
  failure: styles.actionFailure,
};

const ROLE_CLASS: Record<string, string> = {
  Admin: styles.badgeAdmin,
  Staff: styles.badgeStaff,
  Patient: styles.badgePatient,
  System: styles.badgeSystem,
};

function formatDate(iso: string): { date: string; time: string } {
  try {
    const d = new Date(iso);
    return {
      date: d.toISOString().slice(0, 10),
      time: `${d.toISOString().slice(11, 19)} UTC`,
    };
  } catch {
    return { date: iso, time: '' };
  }
}

function buildPageNumbers(current: number, total: number): (number | '…')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages: (number | '…')[] = [1];
  const left = Math.max(2, current - 1);
  const right = Math.min(total - 1, current + 1);
  if (left > 2) pages.push('…');
  for (let i = left; i <= right; i++) pages.push(i);
  if (right < total - 1) pages.push('…');
  pages.push(total);
  return pages;
}

interface Props {
  events: AuditEvent[];
  total: number;
  page: number;
  pageSize: number;
  loading: boolean;
  onPageChange: (p: number) => void;
}

export function AuditLogTable({ events, total, page, pageSize, loading, onPageChange }: Props) {
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const toggleExpand = useCallback((id: string) => {
    setExpandedId(prev => (prev === id ? null : id));
  }, []);

  // Escape key closes all expanded rows
  useEffect(() => {
    if (!expandedId) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') setExpandedId(null);
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [expandedId]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  return (
    <div className={styles.card} role="region" aria-label="Audit log events">
      <div className={styles.cardHeader}>
        <span className={styles.cardTitle}>
          Audit Events{' '}
          <span className={styles.eventCount}>
            {loading ? 'Loading…' : `Showing ${start}–${end} of ${total}`}
          </span>
        </span>
        <span className={styles.sortNote}>
          Sorted by: <strong>Newest first</strong>
        </span>
      </div>

      <div className={styles.tableWrap}>
        <table className={styles.table} aria-label="Audit log entries">
          <thead>
            <tr>
              <th className={styles.colTs} scope="col">
                Timestamp
              </th>
              <th className={styles.colUser} scope="col">
                User
              </th>
              <th className={styles.colRole} scope="col">
                Role
              </th>
              <th className={styles.colAction} scope="col">
                Action
              </th>
              <th className={styles.colResource} scope="col">
                Resource / Target
              </th>
              <th className={styles.colIp} scope="col">
                IP Address
              </th>
              <th className={styles.colStatus} scope="col">
                Status
              </th>
              <th className={styles.colActions} scope="col">
                <span className={styles.srOnly}>Detail</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {!loading && events.length === 0 && (
              <tr>
                <td colSpan={8} className={styles.emptyState}>
                  No events match the selected filters
                </td>
              </tr>
            )}
            {events.map(event => {
              const isExpanded = expandedId === event.id;
              const { date, time } = formatDate(event.timestamp);
              const variant = getActionVariant(event.actionType, event.status);

              return (
                <Fragment key={event.id}>
                  <tr className={isExpanded ? styles.rowExpanded : undefined}>
                    <td className={styles.colTs}>
                      <div className={styles.tsDate}>{date}</div>
                      <div className={styles.tsTime}>{time}</div>
                    </td>
                    <td className={styles.colUser}>
                      <div className={styles.userName}>{event.actorName || 'Unknown'}</div>
                      <div className={styles.userEmail}>{event.actorEmail}</div>
                    </td>
                    <td className={styles.colRole}>
                      <span
                        className={`${styles.badge} ${ROLE_CLASS[event.actorRole] ?? styles.badgeSystem}`}
                      >
                        {event.actorRole}
                      </span>
                    </td>
                    <td className={styles.colAction}>
                      <span className={`${styles.actionBadge} ${ACTION_CLASS[variant]}`}>
                        {event.actionType}
                        {event.phiAccess && (
                          <span className={styles.phiIndicator} aria-label="PHI access event">
                            🔒 PHI
                          </span>
                        )}
                      </span>
                    </td>
                    <td className={styles.colResource}>{event.resource}</td>
                    <td className={styles.colIp}>{event.ipAddress}</td>
                    <td className={styles.colStatus}>
                      {event.status === 'Success' ? (
                        <span className={`${styles.badge} ${styles.badgeSuccess}`}>● Success</span>
                      ) : (
                        <span className={`${styles.badge} ${styles.badgeFailure}`}>✕ Failure</span>
                      )}
                    </td>
                    <td className={styles.colActions}>
                      <button
                        type="button"
                        className={styles.expandBtn}
                        onClick={() => toggleExpand(event.id)}
                        aria-expanded={isExpanded}
                        aria-controls={`detail-${event.id}`}
                        aria-label={`${isExpanded ? 'Collapse' : 'Expand'} details for ${event.id}`}
                      >
                        {isExpanded ? 'Detail ▴' : 'Detail ▾'}
                      </button>
                    </td>
                  </tr>
                  {isExpanded && (
                    <AuditEventDetailRow event={event} colSpan={8} />
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pagination footer */}
      <div className={styles.pagination}>
        <span className={styles.paginationInfo}>
          {total === 0 ? 'No events' : `Showing ${start}–${end} of ${total} events`}
        </span>
        <div className={styles.paginationControls} role="navigation" aria-label="Pagination">
          <button
            type="button"
            className={styles.pageBtn}
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
            aria-label="Previous page"
          >
            ‹
          </button>
          {buildPageNumbers(page, totalPages).map((p, idx) =>
            p === '…' ? (
              <span key={`ellipsis-${idx}`} className={styles.pageDots} aria-hidden="true">
                …
              </span>
            ) : (
              <button
                key={p}
                type="button"
                className={p === page ? `${styles.pageBtn} ${styles.pageBtnActive}` : styles.pageBtn}
                onClick={() => onPageChange(p)}
                aria-current={p === page ? 'page' : undefined}
                aria-label={`Page ${p}`}
              >
                {p}
              </button>
            ),
          )}
          <button
            type="button"
            className={styles.pageBtn}
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages}
            aria-label="Next page"
          >
            ›
          </button>
        </div>
      </div>
    </div>
  );
}
