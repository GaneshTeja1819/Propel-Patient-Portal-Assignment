import React, { useState } from 'react';
import { QueueEntry, QueueStatus } from '../../hooks/useQueue';
import QueueRow from './QueueRow';
import RemoveQueueModal from './RemoveQueueModal';
import styles from './QueueTable.module.css';

type FilterStatus = 'all' | QueueStatus;

interface QueueTableProps {
  entries: QueueEntry[];
  arrivedConflictId: string | null;
  onMarkArrived: (id: string) => void;
  onRemove: (id: string, reason: string) => Promise<boolean>;
  onReorder: (id: string, newIndex: number) => void;
}

interface RemoveTarget {
  id: string;
  patientName: string;
  isAlreadyArrived: boolean;
}

const FILTER_OPTIONS: { value: FilterStatus; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'Booked', label: 'Booked' },
  { value: 'Arrived', label: 'Arrived' },
  { value: 'WalkIn', label: 'Walk-in' },
];

function QueueTable({
  entries,
  arrivedConflictId,
  onMarkArrived,
  onRemove,
  onReorder,
}: QueueTableProps) {
  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterStatus>('all');
  const [removeTarget, setRemoveTarget] = useState<RemoveTarget | null>(null);
  const [conflictToast, setConflictToast] = useState(false);
  const [removeConfirmed, setRemoveConfirmed] = useState<string | null>(null);

  function handleDragStart(e: React.DragEvent<HTMLTableRowElement>, id: string) {
    setDraggedId(id);
    e.dataTransfer.effectAllowed = 'move';
  }

  function handleDragOver(e: React.DragEvent<HTMLTableRowElement>) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }

  function handleDrop(e: React.DragEvent<HTMLTableRowElement>, targetId: string) {
    e.preventDefault();
    if (!draggedId || draggedId === targetId) {
      setDraggedId(null);
      return;
    }

    const fromIndex = entries.findIndex((e) => e.id === draggedId);
    const toIndex = entries.findIndex((e) => e.id === targetId);

    if (fromIndex === -1 || toIndex === -1) {
      setDraggedId(null);
      return;
    }

    // Conflict detection: check whether drop position results in two entries at the same time
    const reordered = [...entries];
    const [moved] = reordered.splice(fromIndex, 1);
    reordered.splice(toIndex, 0, moved);

    const targetEntry = reordered[toIndex];
    const neighbours = [reordered[toIndex - 1], reordered[toIndex + 1]].filter(Boolean);
    const hasTimeConflict = neighbours.some((n) => n.scheduledTime === targetEntry.scheduledTime);

    if (hasTimeConflict) {
      setConflictToast(true);
      setTimeout(() => setConflictToast(false), 4000);
    }

    onReorder(draggedId, toIndex);
    setDraggedId(null);
  }

  function handleRemoveClick(entry: QueueEntry) {
    setRemoveTarget({
      id: entry.id,
      patientName: entry.patientName,
      isAlreadyArrived: entry.status === 'Arrived',
    });
  }

  async function handleRemoveConfirm(reason: string) {
    if (!removeTarget) return;
    const id = removeTarget.id;
    setRemoveTarget(null);
    const ok = await onRemove(id, reason);
    if (ok) {
      setRemoveConfirmed(id);
      setTimeout(() => setRemoveConfirmed(null), 3000);
    }
  }

  const filteredEntries =
    filter === 'all' ? entries : entries.filter((e) => e.status === filter);

  const countFor = (status: QueueStatus) => entries.filter((e) => e.status === status).length;

  return (
    <div className={styles.wrapper}>
      {/* Filter bar (UXR-104) */}
      <div
        className={styles.filterBar}
        role="group"
        aria-label="Filter queue by status"
        data-uxr="UXR-104"
      >
        {FILTER_OPTIONS.map((opt) => {
          const count =
            opt.value === 'all' ? entries.length : countFor(opt.value as QueueStatus);
          return (
            <button
              key={opt.value}
              type="button"
              className={`${styles.filterBtn} ${filter === opt.value ? styles.filterBtnActive : ''}`}
              onClick={() => setFilter(opt.value)}
              aria-pressed={filter === opt.value}
            >
              {opt.label} ({count})
            </button>
          );
        })}
      </div>

      {/* Conflict toast */}
      {conflictToast && (
        <div className={styles.conflictToast} role="status" aria-live="polite">
          ⚠ Position conflicts with another appointment — override applied
        </div>
      )}

      {/* Remove confirmation banner */}
      {removeConfirmed && (
        <div className={styles.removeConfirmation} role="status" aria-live="polite">
          ✓ Patient removed from queue
        </div>
      )}

      <div className={styles.tableWrapper}>
        <table className={styles.table} aria-label="Today's appointment queue">
          <thead>
            <tr>
              <th className={styles.thDrag} aria-label="Drag handle" />
              <th className={styles.th}>Time</th>
              <th className={styles.th}>Patient</th>
              <th className={styles.th}>Type</th>
              <th className={styles.th}>Intake</th>
              <th className={styles.th}>Status</th>
              <th className={styles.th}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredEntries.length === 0 ? (
              <tr>
                <td colSpan={7} className={styles.emptyCell}>
                  No appointments match this filter.
                </td>
              </tr>
            ) : (
              filteredEntries.map((entry) => (
                <QueueRow
                  key={entry.id}
                  entry={entry}
                  arrivedConflict={arrivedConflictId === entry.id}
                  onMarkArrived={onMarkArrived}
                  onRemove={() => handleRemoveClick(entry)}
                  onDragStart={handleDragStart}
                  onDragOver={handleDragOver}
                  onDrop={handleDrop}
                />
              ))
            )}
          </tbody>
        </table>
      </div>

      {removeTarget && (
        <RemoveQueueModal
          patientName={removeTarget.patientName}
          isAlreadyArrived={removeTarget.isAlreadyArrived}
          onConfirm={handleRemoveConfirm}
          onCancel={() => setRemoveTarget(null)}
        />
      )}
    </div>
  );
}

export default QueueTable;
