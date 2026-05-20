import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { CancelConfirmModal } from '../components/appointments/CancelConfirmModal';
import { RescheduleFlow } from '../components/appointments/RescheduleFlow';
import { Toast } from '../components/common/Toast';
import { useCancelAppointment } from '../hooks/useCancelAppointment';
import { useToast } from '../hooks/useToast';
import styles from './DashboardPage.module.css';

interface AppointmentItem {
  id: string;
  title: string;
  providerName: string;
  startDateTime: string;
  status: 'Booked' | 'Cancelled';
  slotId: string;
}

const createSeedAppointments = (): AppointmentItem[] => {
  const now = new Date();

  const future = new Date(now);
  future.setDate(now.getDate() + 2);
  future.setHours(10, 0, 0, 0);

  const past = new Date(now);
  past.setDate(now.getDate() - 3);
  past.setHours(9, 30, 0, 0);

  return [
    {
      id: 'appt-future-001',
      title: 'Annual Physical',
      providerName: 'Dr. M. Okafor',
      startDateTime: future.toISOString(),
      status: 'Booked',
      slotId: 'slot-future-001',
    },
    {
      id: 'appt-past-001',
      title: 'Follow-up Review',
      providerName: 'Dr. M. Okafor',
      startDateTime: past.toISOString(),
      status: 'Booked',
      slotId: 'slot-past-001',
    },
  ];
};

function DashboardPage() {
  const [appointments, setAppointments] = useState<AppointmentItem[]>(() => createSeedAppointments());
  const [cancelTarget, setCancelTarget] = useState<AppointmentItem | null>(null);
  const [rescheduleTargetId, setRescheduleTargetId] = useState<string | null>(null);

  const { cancelAppointment, isSubmitting } = useCancelAppointment();
  const { toasts, addToast, removeToast } = useToast();

  const upcomingCount = useMemo(
    () =>
      appointments.filter(
        (appointment) =>
          appointment.status === 'Booked' && new Date(appointment.startDateTime) >= new Date()
      ).length,
    [appointments]
  );

  const handleConfirmCancel = async () => {
    if (!cancelTarget) {
      return;
    }

    const isPast = new Date(cancelTarget.startDateTime) < new Date();
    if (isPast) {
      addToast('Past appointments cannot be cancelled.', 'warning');
      setCancelTarget(null);
      return;
    }

    const result = await cancelAppointment(cancelTarget.id);
    if (!result.success) {
      addToast(result.message ?? 'Unable to cancel appointment.', 'error');
      return;
    }

    setAppointments((current) =>
      current.map((item) => (item.id === cancelTarget.id ? { ...item, status: 'Cancelled' } : item))
    );

    addToast('Appointment cancelled successfully.', 'success');
    setCancelTarget(null);
    setRescheduleTargetId(null);
  };

  return (
    <main className={styles.page}>
      <section className={styles.header}>
        <div>
          <h1 className={styles.title}>Patient Dashboard</h1>
          <p className={styles.subtitle}>You have {upcomingCount} upcoming appointments.</p>
        </div>
        <Link to="/booking" className={styles.bookButton}>
          Book appointment
        </Link>
      </section>

      <section className={styles.listSection} aria-label="Appointment list">
        {appointments.map((appointment) => {
          const isPast = new Date(appointment.startDateTime) < new Date();
          const canManage = !isPast && appointment.status === 'Booked';

          return (
            <article key={appointment.id} className={styles.card}>
              <div className={styles.cardHeaderRow}>
                <div>
                  <h2 className={styles.cardTitle}>{appointment.title}</h2>
                  <p className={styles.cardMeta}>
                    {new Date(appointment.startDateTime).toLocaleString()} · {appointment.providerName}
                  </p>
                </div>
                <span className={appointment.status === 'Booked' ? styles.bookedBadge : styles.cancelledBadge}>
                  {appointment.status}
                </span>
              </div>

              <div className={styles.actions}>
                <Link
                  to={`/appointments/${appointment.id}`}
                  state={{ appointment }}
                  className={styles.secondaryAction}
                >
                  View details
                </Link>

                <button
                  type="button"
                  className={styles.secondaryAction}
                  onClick={() => setRescheduleTargetId(appointment.id)}
                  disabled={!canManage}
                  aria-disabled={!canManage}
                >
                  Reschedule
                </button>

                <button
                  type="button"
                  className={styles.dangerAction}
                  onClick={() => setCancelTarget(appointment)}
                  disabled={!canManage}
                  aria-disabled={!canManage}
                >
                  Cancel
                </button>
              </div>

              {rescheduleTargetId === appointment.id && canManage && (
                <RescheduleFlow
                  appointmentId={appointment.id}
                  onClose={() => setRescheduleTargetId(null)}
                  onSuccess={(newSlotId) => {
                    setAppointments((current) =>
                      current.map((item) =>
                        item.id === appointment.id ? { ...item, slotId: newSlotId } : item
                      )
                    );
                    addToast('Appointment rescheduled successfully.', 'success');
                  }}
                  onConflict={(message) => {
                    addToast(message, 'error');
                  }}
                  onError={(message) => {
                    addToast(message, 'error');
                  }}
                />
              )}
            </article>
          );
        })}
      </section>

      {cancelTarget && (
        <CancelConfirmModal
          appointmentTitle={cancelTarget.title}
          isSubmitting={isSubmitting}
          onConfirm={handleConfirmCancel}
          onDismiss={() => setCancelTarget(null)}
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

export default DashboardPage;
