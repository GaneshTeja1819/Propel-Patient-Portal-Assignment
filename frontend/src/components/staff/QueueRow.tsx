import React from 'react';
import { QueueEntry, QueueStatus } from '../../hooks/useQueue';
import styles from './QueueRow.module.css';

interface QueueRowProps {
  entry: QueueEntry;
  arrivedConflict: boolean;
  onMarkArrived: (id: string) => void;
  onRemove: (id: string) => void;
  onDragStart: (e: React.DragEvent<HTMLTableRowElement>, id: string) => void;
  onDragOver: (e: React.DragEvent<HTMLTableRowElement>) => void;
  onDrop: (e: React.DragEvent<HTMLTableRowElement>, id: string) => void;
}

const STATUS_LABELS: Record<QueueStatus, string> = {
  Booked: 'Booked',
  Arrived: '✓ Arrived',
  WalkIn: 'Walk-in',
  Cancelled: 'Cancelled',
};

const STATUS_CLASS: Record<QueueStatus, string> = {
  Booked: styles.badgeBooked,
  Arrived: styles.badgeArrived,
  WalkIn: styles.badgeWalkIn,
  Cancelled: styles.badgeCancelled,
};

function QueueRow({
  entry,
  arrivedConflict,
  onMarkArrived,
  onRemove,
  onDragStart,
  onDragOver,
  onDrop,
}: QueueRowProps) {
  const canMarkArrived = entry.status === 'Booked' || entry.status === 'WalkIn';
  const canRemove = entry.status !== 'Cancelled';

  return (
    <tr
      data-status={entry.status.toLowerCase()}
      draggable
      onDragStart={(e) => onDragStart(e, entry.id)}
      onDragOver={onDragOver}
      onDrop={(e) => onDrop(e, entry.id)}
      className={styles.row}
    >
      <td className={styles.dragCell}>
        <span className={styles.dragHandle} aria-hidden="true" title="Drag to reorder">
          ⠿
        </span>
      </td>
      <td className={styles.timeCell}>
        <strong>{entry.scheduledTime}</strong>
      </td>
      <td>
        <div className={styles.patientName}>{entry.patientName}</div>
        <div className={styles.patientMeta}>
          DOB: {entry.dateOfBirth} · {entry.sex}
          {entry.phone ? ` · ${entry.phone}` : ''}
        </div>
      </td>
      <td className={styles.typeCell}>{entry.appointmentType}</td>
      <td>
        {entry.intakeComplete ? (
          <span className={styles.intakeComplete}>✓ Complete</span>
        ) : (
          <span className={styles.intakeMissing}>⚠ Missing</span>
        )}
      </td>
      <td>
        <span className={`${styles.badge} ${STATUS_CLASS[entry.status]}`} data-uxr="UXR-104">
          {STATUS_LABELS[entry.status]}
        </span>
        {arrivedConflict && (
          <div className={styles.conflictMsg} role="alert" aria-live="polite">
            Patient already marked as arrived
          </div>
        )}
      </td>
      <td>
        <div className={styles.rowActions}>
          <button
            type="button"
            className={`${styles.btn} ${styles.btnSuccess}`}
            onClick={() => onMarkArrived(entry.id)}
            disabled={!canMarkArrived}
            aria-label={`Mark ${entry.patientName} as arrived`}
          >
            Mark arrived
          </button>
          {canRemove && (
            <button
              type="button"
              className={`${styles.btn} ${styles.btnRemove}`}
              onClick={() => onRemove(entry.id)}
              aria-label={`Remove ${entry.patientName} from queue`}
            >
              Remove
            </button>
          )}
        </div>
      </td>
    </tr>
  );
}

export default QueueRow;
