import { useMemo, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { CancelConfirmModal } from '../components/appointments/CancelConfirmModal';
import { RescheduleFlow } from '../components/appointments/RescheduleFlow';
import { Toast } from '../components/common/Toast';
import { useCancelAppointment } from '../hooks/useCancelAppointment';
import { useToast } from '../hooks/useToast';
import styles from './AppointmentDetailPage.module.css';

interface AppointmentItem {
  id: string;
  title: string;
  providerName: string;
  startDateTime: string;
  status: 'Booked' | 'Cancelled';
  slotId: string;
}

interface AppointmentLocationState {
  appointment?: AppointmentItem;
}

function AppointmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const state = (location.state as AppointmentLocationState | null) ?? null;

  const seededAppointment = useMemo<AppointmentItem>(() => {
    const future = new Date();
    future.setDate(future.getDate() + 1);
    future.setHours(11, 0, 0, 0);

    return (
      state?.appointment ?? {
        id: id ?? 'unknown-appointment',
        title: 'Appointment Detail',
        providerName: 'Assigned Provider',
        startDateTime: future.toISOString(),
        status: 'Booked',
        slotId: 'slot-detail-001',
      }
    );
  }, [id, state?.appointment]);

  const [appointment, setAppointment] = useState<AppointmentItem>(seededAppointment);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [showReschedule, setShowReschedule] = useState(false);

  const { cancelAppointment, isSubmitting } = useCancelAppointment();
  const { toasts, addToast, removeToast } = useToast();

  const isPast = new Date(appointment.startDateTime) < new Date();
  const canManage = !isPast && appointment.status === 'Booked';

  const handleConfirmCancel = async () => {
    if (!canManage) {
      return;
    }

    const result = await cancelAppointment(appointment.id);
    if (!result.success) {
      addToast(result.message ?? 'Unable to cancel appointment.', 'error');
      return;
    }

    setAppointment((current) => ({ ...current, status: 'Cancelled' }));
    setShowCancelModal(false);
    addToast('Appointment cancelled successfully.', 'success');
  };

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <Link to="/dashboard" className={styles.backLink}>
          ← Back to dashboard
        </Link>
        <h1 className={styles.title}>Appointment Detail</h1>
      </header>

      <article className={styles.card}>
        <div className={styles.row}>
          <span className={styles.label}>Type</span>
          <span className={styles.value}>{appointment.title}</span>
        </div>
        <div className={styles.row}>
          <span className={styles.label}>When</span>
          <span className={styles.value}>{new Date(appointment.startDateTime).toLocaleString()}</span>
        </div>
        <div className={styles.row}>
          <span className={styles.label}>Provider</span>
          <span className={styles.value}>{appointment.providerName}</span>
        </div>
        <div className={styles.row}>
          <span className={styles.label}>Status</span>
          <span className={styles.value}>{appointment.status}</span>
        </div>
      </article>

      <section className={styles.actions}>
        <button
          type="button"
          className={styles.secondaryAction}
          onClick={() => setShowReschedule((current) => !current)}
          disabled={!canManage}
          aria-disabled={!canManage}
        >
          Reschedule
        </button>
        <button
          type="button"
          className={styles.dangerAction}
          onClick={() => setShowCancelModal(true)}
          disabled={!canManage}
          aria-disabled={!canManage}
        >
          Cancel
        </button>
      </section>

      {showReschedule && canManage && (
        <RescheduleFlow
          appointmentId={appointment.id}
          onClose={() => setShowReschedule(false)}
          onSuccess={(newSlotId) => {
            setAppointment((current) => ({ ...current, slotId: newSlotId }));
            addToast('Appointment rescheduled successfully.', 'success');
          }}
          onConflict={(message) => addToast(message, 'error')}
          onError={(message) => addToast(message, 'error')}
        />
      )}

      {showCancelModal && (
        <CancelConfirmModal
          appointmentTitle={appointment.title}
          isSubmitting={isSubmitting}
          onConfirm={handleConfirmCancel}
          onDismiss={() => setShowCancelModal(false)}
        />
      )}

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

export default AppointmentDetailPage;
