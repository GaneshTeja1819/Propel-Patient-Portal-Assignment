import { useEffect, useState } from 'react';
import { AppointmentSlot } from '../../hooks/useSlots';
import { SlotCard } from './SlotCard';
import { SlotGridSkeleton } from './SlotGridSkeleton';
import styles from './SlotGrid.module.css';

export interface SlotGridProps {
  slots: AppointmentSlot[];
  isLoading: boolean;
  isCachedData: boolean;
  selectedSlotId: string | null;
  onSelectSlot: (slotId: string) => void;
  onPreferredSlotChange?: (slotId: string | null) => void;
}

/**
 * Responsive CSS Grid layout for appointment slots (4/2/1 columns at 1280/768/375 px).
 *
 * AC-001: Available slots rendered within 500ms from Redis cache
 * AC-003: Responsive grid layout with no horizontal scroll
 * AC-005: Empty state when no slots available
 */
export function SlotGrid({
  slots,
  isLoading,
  isCachedData,
  selectedSlotId,
  onSelectSlot,
  onPreferredSlotChange,
}: SlotGridProps) {
  const [preferredSlotId, setPreferredSlotId] = useState<string | null>(null);
  const [validationMessage, setValidationMessage] = useState<string | null>(null);

  useEffect(() => {
    onPreferredSlotChange?.(preferredSlotId);
  }, [onPreferredSlotChange, preferredSlotId]);

  useEffect(() => {
    if (preferredSlotId && selectedSlotId && preferredSlotId === selectedSlotId) {
      setPreferredSlotId(null);
      setValidationMessage('Preferred slot must differ from your booked slot');
      return;
    }

    setValidationMessage(null);
  }, [preferredSlotId, selectedSlotId]);

  const handleRegisterPreferred = (slotId: string) => {
    setPreferredSlotId(slotId);
  };

  if (isLoading) {
    return <SlotGridSkeleton />;
  }

  if (slots.length === 0) {
    return (
      <div className={styles.emptyState} role="status" aria-live="polite">
        <p>No available slots — try another date</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {isCachedData && (
        <div className={styles.badge} role="status" aria-live="polite">
          📡 Showing live data
        </div>
      )}
      <div className={styles.grid} role="group" aria-label="Available appointment slots">
        {slots.map((slot) => (
          <SlotCard
            key={slot.id}
            id={slot.id}
            date={slot.date}
            time={slot.time}
            isBooked={slot.isBooked}
            isSelected={selectedSlotId === slot.id}
            isPreferred={preferredSlotId === slot.id}
            onSelect={onSelectSlot}
            onRegisterPreferred={handleRegisterPreferred}
          />
        ))}
      </div>
      {validationMessage && (
        <p className={styles.validationMessage} role="alert">
          {validationMessage}
        </p>
      )}
    </div>
  );
}
