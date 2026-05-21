import type { AuditStats } from '../../hooks/useAuditLog';
import styles from './AuditStatStrip.module.css';

interface Props {
  stats: AuditStats | null;
  loading: boolean;
}

export function AuditStatStrip({ stats, loading }: Props) {
  return (
    <div className={styles.strip} role="region" aria-label="Audit summary statistics">
      <StatCard
        label="Events Today"
        value={loading || !stats ? '—' : stats.eventsToday.toLocaleString()}
        meta="Since 12:00 AM"
      />
      <StatCard
        label="Unique Users"
        value={loading || !stats ? '—' : stats.uniqueUsers.toLocaleString()}
        meta="Active sessions"
      />
      <StatCard
        label="PHI Access Events"
        value={loading || !stats ? '—' : stats.phiAccessEvents.toLocaleString()}
        meta="Patient profile views"
      />
      <StatCard
        label="Failed Attempts"
        value={loading || !stats ? '—' : stats.failedAttempts.toLocaleString()}
        meta="Login failures today"
        valueVariant={stats && stats.failedAttempts > 0 ? 'err' : undefined}
      />
    </div>
  );
}

interface StatCardProps {
  label: string;
  value: string;
  meta: string;
  valueVariant?: 'err';
}

function StatCard({ label, value, meta, valueVariant }: StatCardProps) {
  return (
    <div className={styles.card}>
      <div className={styles.label}>{label}</div>
      <div className={valueVariant === 'err' ? `${styles.value} ${styles.valueErr}` : styles.value}>
        {value}
      </div>
      <div className={styles.meta}>{meta}</div>
    </div>
  );
}
