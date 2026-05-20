import styles from './SlotCard.module.css';

export interface SlotCardProps {
  id: string;
  date: string;
  time: string;
  isBooked: boolean;
  isSelected: boolean;
  isPreferred: boolean;
  onSelect: (slotId: string) => void;
  onRegisterPreferred?: (slotId: string) => void;
}

/**
 * Individual appointment slot card with optimistic selection UX.
 *
 * AC-004: Clicked slot triggers CSS "Selected" state within 200ms before server response
 */
export function SlotCard({
  id,
  date,
  time,
  isBooked,
  isSelected,
  isPreferred,
  onSelect,
  onRegisterPreferred,
}: SlotCardProps) {
  const handleClick = () => {
    if (!isBooked) {
      onSelect(id);
    }
  };

  const statusClass = isSelected
    ? styles.selected
    : isPreferred
      ? styles.preferred
      : isBooked
        ? styles.unavailable
        : styles.available;

  const statusText = isSelected
    ? 'Selected ✓'
    : isPreferred
      ? 'Preferred ✓'
    : isBooked
      ? 'Taken'
      : 'Available';

  const handleRegisterPreferred = () => {
    if (onRegisterPreferred) {
      onRegisterPreferred(id);
    }
  };

  return (
    <article className={`${styles.card} ${statusClass}`} data-slot-id={id}>
      <button
        type="button"
        className={styles.primaryAction}
        onClick={handleClick}
        disabled={isBooked}
        aria-label={`${time} — ${statusText}`}
      >
        <div className={styles.time}>{time}</div>
        <div className={styles.status}>{statusText}</div>
      </button>

      {isBooked && onRegisterPreferred && (
        <button
          type="button"
          className={styles.preferredAction}
          onClick={handleRegisterPreferred}
          aria-label={`Register as preferred slot for ${date} ${time}`}
        >
          {isPreferred ? 'Preferred ✓' : 'Register Preferred'}
        </button>
      )}
    </article>
  );
}
