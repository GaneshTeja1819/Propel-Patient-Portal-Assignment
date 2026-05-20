import { useMemo, useState } from 'react';
import { useSlots } from '../../hooks/useSlots';
import { useRescheduleAppointment } from '../../hooks/useRescheduleAppointment';
import { SlotGrid } from '../booking/SlotGrid';
import styles from './RescheduleFlow.module.css';

interface RescheduleFlowProps {
  appointmentId: string;
  onSuccess: (newSlotId: string) => void;
  onConflict: (message: string) => void;
  onError: (message: string) => void;
  onClose: () => void;
}

export function RescheduleFlow({
  appointmentId,
  onSuccess,
  onConflict,
  onError,
  onClose,
}: RescheduleFlowProps) {
  const [selectedDate, setSelectedDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [inlineError, setInlineError] = useState<string | null>(null);

  const { slots, isLoading, isError, isCachedData, refetch } = useSlots(selectedDate);
  const { rescheduleAppointment, isSubmitting } = useRescheduleAppointment();

  const canSubmit = useMemo(() => !!selectedSlotId && !isSubmitting, [selectedSlotId, isSubmitting]);

  const handleSubmit = async () => {
    if (!selectedSlotId) {
      return;
    }

    const result = await rescheduleAppointment(appointmentId, selectedSlotId);

    if (result.status === 'success') {
      onSuccess(selectedSlotId);
      onClose();
      return;
    }

    if (result.status === 'conflict') {
      const message = result.message ?? 'Slot no longer available';
      setInlineError(message);
      onConflict(message);
      await refetch();
      setSelectedSlotId(null);
      return;
    }

    const fallbackMessage = result.message ?? 'Reschedule failed. Please try again.';
    setInlineError(fallbackMessage);
    onError(fallbackMessage);
  };

  return (
    <section className={styles.panel} aria-label="Reschedule appointment panel">
      <header className={styles.header}>
        <h3 className={styles.title}>Reschedule appointment</h3>
        <button type="button" className={styles.closeButton} onClick={onClose}>
          Close
        </button>
      </header>

      <label className={styles.dateLabel} htmlFor={`reschedule-date-${appointmentId}`}>
        Choose date
      </label>
      <input
        id={`reschedule-date-${appointmentId}`}
        type="date"
        className={styles.dateInput}
        value={selectedDate}
        min={new Date().toISOString().split('T')[0]}
        onChange={(event) => setSelectedDate(event.target.value)}
      />

      {isError && (
        <p className={styles.errorText} role="alert">
          Failed to load slots. Please choose another date.
        </p>
      )}
      {inlineError && (
        <p className={styles.errorText} role="alert">
          {inlineError}
        </p>
      )}

      <SlotGrid
        slots={slots}
        isLoading={isLoading}
        isCachedData={isCachedData}
        selectedSlotId={selectedSlotId}
        onSelectSlot={(slotId) => {
          setInlineError(null);
          setSelectedSlotId(slotId);
        }}
      />

      <div className={styles.actions}>
        <button type="button" className={styles.secondaryButton} onClick={onClose}>
          Cancel
        </button>
        <button
          type="button"
          className={styles.primaryButton}
          onClick={handleSubmit}
          disabled={!canSubmit}
        >
          {isSubmitting ? 'Submitting...' : 'Confirm reschedule'}
        </button>
      </div>
    </section>
  );
}
