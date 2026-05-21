import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlotGrid } from '../components/booking/SlotGrid';
import { BookingForm } from '../components/booking/BookingForm';
import { PDFConfirmationStatus } from '../components/booking/PDFConfirmationStatus';
import { Toast } from '../components/common/Toast';
import { useSlots } from '../hooks/useSlots';
import { useBooking } from '../hooks/useBooking';
import { useToast } from '../hooks/useToast';
import styles from './BookingPage.module.css';

type BookingState = 'slot-selection' | 'form' | 'confirmation';

/**
 * SCR-004 Appointment Booking page.
 *
 * AC-005: Full flow from dashboard to confirmation in ≤ 3 screen transitions (1 navigation to /booking).
 * AC-004: Slot conflict shows error toast within 2s; grid refreshes.
 * AC-003: Insurance validation badges show status without blocking CTA.
 */
function BookingPage() {
  const today = new Date().toISOString().split('T')[0];
  const [selectedDate, setSelectedDate] = useState(today);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [preferredSlotId, setPreferredSlotId] = useState<string | null>(null);
  const [bookingState, setBookingState] = useState<BookingState>('slot-selection');
  const [confirmedAppointmentId, setConfirmedAppointmentId] = useState<string | null>(null);

  const { slots, isLoading, isError, isCachedData } = useSlots(selectedDate);
  const { book, isSubmitting } = useBooking();
  const { toasts, removeToast } = useToast();
  const navigate = useNavigate();

  const selectedSlot = slots.find((s) => s.id === selectedSlotId);

  const handleSelectSlot = (slotId: string) => {
    setSelectedSlotId(slotId);
    setBookingState('form');
  };

  const handleBookingSubmit = async (data: {
    slotId: string;
    preferredSlotId?: string;
    insuranceProvider?: string;
    insuranceId?: string;
  }) => {
    const result = await book(data);

    if (result) {
      setConfirmedAppointmentId(result.appointmentId);
      setBookingState('confirmation');
    } else {
      // On 409 or error, revert to form state but keep slot selected
      // (slot will be marked unavailable by grid refresh)
      setSelectedSlotId(null);
      setBookingState('slot-selection');
    }
  };

  return (
    <main className={styles.page}>
      <div className={styles.container}>
        <h1 className={styles.title}>Book an appointment</h1>
        <p className={styles.subtitle}>
          {bookingState === 'confirmation'
            ? 'Your appointment has been confirmed!'
            : 'Select a date and available slot to proceed with booking.'}
        </p>

        {bookingState === 'confirmation' && confirmedAppointmentId ? (
          // Confirmation view (0 transitions, all on same page)
          <div className={styles.confirmationCard}>
            <div className={styles.confirmationIcon}>✓</div>
            <h2 className={styles.confirmationTitle}>Booking Confirmed!</h2>
            <p className={styles.confirmationText}>
              Your appointment has been booked successfully.
            </p>
            <div className={styles.confirmationDetails}>
              <div className={styles.detailRow}>
                <span>Appointment ID:</span>
                <span className={styles.detailValue}>{confirmedAppointmentId}</span>
              </div>
            </div>
            {preferredSlotId && (
              <p className={styles.preferredAcknowledgement} role="status">
                Preferred slot registered - you&apos;ll be notified if it becomes available.
              </p>
            )}
            <PDFConfirmationStatus appointmentId={confirmedAppointmentId} />
            {/* US_021, AC-005: Optional calendar sync CTA — booking confirmed without it */}
            <button
              className={styles.secondaryButton}
              onClick={() => navigate(`/calendar-sync?appointmentId=${confirmedAppointmentId}`)}
              aria-label="Sync this appointment to your Google or Outlook calendar (optional)"
            >
              Sync to calendar (optional)
            </button>
            <button
              className={styles.primaryButton}
              onClick={() => {
                // Reset state to allow booking another appointment
                setBookingState('slot-selection');
                setSelectedSlotId(null);
                setPreferredSlotId(null);
                setConfirmedAppointmentId(null);
              }}
            >
              Book Another Appointment
            </button>
          </div>
        ) : (
          <div className={styles.bookingGrid}>
            {/* Left: Slot selection and form (1 transition total from dashboard) */}
            <div className={styles.formSection}>
              {bookingState === 'slot-selection' ? (
                <>
                  <div className={styles.card}>
                    <h2 className={styles.cardTitle}>Select date</h2>
                    <input
                      type="date"
                      value={selectedDate}
                      onChange={(e) => setSelectedDate(e.target.value)}
                      min={today}
                      className={styles.dateInput}
                    />
                  </div>

                  <div className={styles.card}>
                    <h2 className={styles.cardTitle}>Select a slot</h2>
                    {isError && (
                      <div className={styles.errorMessage} role="alert">
                        ⚠ Failed to load slots. Please try again.
                      </div>
                    )}
                    <SlotGrid
                      slots={slots}
                      isLoading={isLoading}
                      isCachedData={isCachedData}
                      selectedSlotId={selectedSlotId}
                      onSelectSlot={handleSelectSlot}
                      onPreferredSlotChange={setPreferredSlotId}
                    />
                  </div>
                </>
              ) : (
                <div className={styles.card}>
                  <BookingForm
                    selectedSlotId={selectedSlotId}
                    selectedSlotTime={selectedSlot?.time}
                    onSubmit={(formData) =>
                      handleBookingSubmit({
                        ...formData,
                        preferredSlotId: preferredSlotId ?? undefined,
                      })
                    }
                    isSubmitting={isSubmitting}
                  />
                </div>
              )}
            </div>

            {/* Right: Booking summary */}
            <aside className={styles.summary}>
              <div className={styles.summaryCard}>
                <h3 className={styles.summaryTitle}>📋 Booking summary</h3>
                <div className={styles.summaryRow}>
                  <span className={styles.summaryLabel}>Date</span>
                  <span className={styles.summaryValue}>{selectedDate}</span>
                </div>
                <div className={styles.summaryRow}>
                  <span className={styles.summaryLabel}>Time</span>
                  <span className={styles.summaryValue}>{selectedSlot?.time || '—'}</span>
                </div>
                {bookingState === 'form' && (
                  <button
                    className={styles.backButton}
                    onClick={() => setBookingState('slot-selection')}
                  >
                    ← Back to slots
                  </button>
                )}
              </div>
            </aside>
          </div>
        )}
      </div>

      {/* Toast container (renders via portal) */}
      <div id="toast-root" className={styles.toastContainer}>
        {toasts.map((toast) => (
          <Toast
            key={toast.id}
            id={toast.id}
            message={toast.message}
            type={toast.type}
            duration={toast.duration}
            onDismiss={removeToast}
          />
        ))}
      </div>
    </main>
  );
}

export default BookingPage;
