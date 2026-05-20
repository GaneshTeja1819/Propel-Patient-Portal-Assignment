import { useForm } from 'react-hook-form';
import { useInsuranceValidation } from '../../hooks/useInsuranceValidation';
import { InsuranceBadge } from './InsuranceBadge';
import styles from './BookingForm.module.css';

export interface BookingFormProps {
  selectedSlotId: string | null;
  selectedSlotTime?: string;
  onSubmit: (data: { slotId: string; insuranceProvider?: string; insuranceId?: string }) => void;
  isSubmitting: boolean;
}

/**
 * Booking form with insurance fields and inline validation badges.
 *
 * AC-003: Insurance badge updates on blur; CTA always enabled.
 * AC-004: On submission, triggers booking API call.
 */
export function BookingForm({
  selectedSlotId,
  selectedSlotTime,
  onSubmit,
  isSubmitting,
}: BookingFormProps) {
  const { register, handleSubmit, watch } = useForm({
    mode: 'onBlur',
    defaultValues: {
      insuranceProvider: '',
      insuranceId: '',
    },
  });

  const insuranceProvider = watch('insuranceProvider');
  const insuranceId = watch('insuranceId');

  const { status, validate: validateInsurance } = useInsuranceValidation();

  // Trigger insurance validation when ID field is blurred
  const handleInsuranceIdBlur = async () => {
    if (insuranceProvider && insuranceId) {
      await validateInsurance(insuranceProvider, insuranceId);
    }
  };

  const handleFormSubmit = (data: any) => {
    if (!selectedSlotId) {
      return;
    }

    onSubmit({
      slotId: selectedSlotId,
      insuranceProvider: data.insuranceProvider || undefined,
      insuranceId: data.insuranceId || undefined,
    });
  };

  return (
    <form className={styles.form} onSubmit={handleSubmit(handleFormSubmit)}>
      <h2 className={styles.formTitle}>Booking Details</h2>

      {/* Slot Summary */}
      {selectedSlotTime && (
        <div className={styles.slotSummary}>
          <span className={styles.summaryLabel}>Selected Time:</span>
          <span className={styles.summaryValue}>{selectedSlotTime}</span>
        </div>
      )}

      {/* Insurance Provider */}
      <div className={styles.formGroup}>
        <label htmlFor="insuranceProvider" className={styles.label}>
          Insurance Provider (Optional)
        </label>
        <input
          id="insuranceProvider"
          type="text"
          placeholder="e.g., BlueCross, Aetna"
          className={styles.input}
          {...register('insuranceProvider')}
        />
      </div>

      {/* Insurance ID */}
      <div className={styles.formGroup}>
        <label htmlFor="insuranceId" className={styles.label}>
          Insurance ID (Optional)
        </label>
        <div className={styles.insuranceIdWrapper}>
          <input
            id="insuranceId"
            type="text"
            placeholder="Your insurance ID"
            className={styles.input}
            {...register('insuranceId')}
            onBlur={handleInsuranceIdBlur}
          />
          <InsuranceBadge status={status} />
        </div>
      </div>

      {/* Confirm Booking CTA */}
      <button
        type="submit"
        className={styles.confirmButton}
        disabled={!selectedSlotId || isSubmitting}
        aria-label={!selectedSlotId ? 'Select a slot to continue' : 'Confirm appointment booking'}
      >
        {isSubmitting ? 'Confirming...' : 'Confirm Booking'}
      </button>
    </form>
  );
}
