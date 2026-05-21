import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import AnonymousPatientForm, { AnonymousPatientData } from '../components/staff/AnonymousPatientForm';
import CreatePatientAccountForm from '../components/staff/CreatePatientAccountForm';
import PatientSearchInput, { PatientSearchResult } from '../components/staff/PatientSearchInput';
import { useAuthContext } from '../context/AuthContext';
import { useWalkInBooking } from '../hooks/useWalkInBooking';
import styles from './WalkInBookingPage.module.css';

type PatientMode = 'existing' | 'anonymous';
type AppointmentType = 'new' | 'followup' | 'urgent' | 'consult';

interface SlotOption {
  id: string;
  label: string;
  available: boolean;
}

interface AppointmentDetails {
  type: AppointmentType;
  providerId: string;
  slotId: string | null;
}

const APPOINTMENT_TYPES: { value: AppointmentType; label: string }[] = [
  { value: 'new', label: 'New Patient Visit' },
  { value: 'followup', label: 'Follow-up' },
  { value: 'urgent', label: 'Urgent Care' },
  { value: 'consult', label: 'Consultation' },
];

const PROVIDERS = [
  { value: 'okafor', label: 'Dr. M. Okafor' },
  { value: 'patel', label: 'Dr. S. Patel' },
];

// Placeholder slots — in production these come from the SlotGrid component (US_012)
const DEMO_SLOTS: SlotOption[] = [
  { id: 'slot-1330', label: '1:30 PM', available: true },
  { id: 'slot-1430', label: '2:30 PM', available: true },
  { id: 'slot-0900', label: '9:00 AM', available: false },
  { id: 'slot-1000', label: '10:00 AM', available: false },
];

function WalkInBookingPage() {
  const { role } = useAuthContext();

  // Redirect non-Staff users
  if (role !== null && role !== 'Staff') {
    return <Navigate to="/login" replace />;
  }

  return <WalkInBookingContent />;
}

function WalkInBookingContent() {
  const [patientMode, setPatientMode] = useState<PatientMode>('existing');
  const [selectedPatient, setSelectedPatient] = useState<PatientSearchResult | null>(null);
  const [anonymousData, setAnonymousData] = useState<AnonymousPatientData | null>(null);
  const [appointmentDetails, setAppointmentDetails] = useState<AppointmentDetails>({
    type: 'new',
    providerId: 'okafor',
    slotId: null,
  });
  const [showCreateAccount, setShowCreateAccount] = useState(false);
  const successRef = useRef<HTMLDivElement>(null);

  const {
    isBooking,
    bookingError,
    bookingResult,
    isCreatingAccount,
    accountError,
    accountCreated,
    isDuplicateEmail,
    bookWalkIn,
    createAccount,
  } = useWalkInBooking();

  const isPatientReady =
    patientMode === 'existing'
      ? selectedPatient !== null
      : anonymousData !== null &&
        anonymousData.firstName.trim() !== '' &&
        anonymousData.lastName.trim() !== '' &&
        anonymousData.dateOfBirth !== '';

  const hasAvailableSlots = DEMO_SLOTS.some((s) => s.available);

  const handlePatientModeSwitch = (mode: PatientMode) => {
    setPatientMode(mode);
    setSelectedPatient(null);
    setAnonymousData(null);
  };

  const handlePatientSelect = useCallback((patient: PatientSearchResult) => {
    setSelectedPatient(patient);
    setPatientMode('existing');
  }, []);

  const handleNotFound = useCallback(() => {
    setPatientMode('anonymous');
    setSelectedPatient(null);
  }, []);

  const handleAnonymousChange = useCallback((data: AnonymousPatientData) => {
    setAnonymousData(data);
  }, []);

  const handleSlotSelect = (slotId: string) => {
    setAppointmentDetails((prev) => ({
      ...prev,
      slotId: prev.slotId === slotId ? null : slotId,
    }));
  };

  const handleConfirm = async () => {
    const payload = {
      patientId: selectedPatient?.id,
      anonymousPatient:
        patientMode === 'anonymous' && anonymousData
          ? {
              firstName: anonymousData.firstName,
              lastName: anonymousData.lastName,
              dateOfBirth: anonymousData.dateOfBirth,
              sex: anonymousData.sex,
              phone: anonymousData.phone,
            }
          : undefined,
      slotId: appointmentDetails.slotId ?? undefined,
      appointmentType: appointmentDetails.type,
      providerId: appointmentDetails.providerId,
    };

    await bookWalkIn(payload);
  };

  const handleCreateAccount = async (email: string, password: string) => {
    if (!bookingResult) {
      return;
    }

    const patientFirstName =
      patientMode === 'existing'
        ? (selectedPatient?.name.split(' ')[0] ?? '')
        : (anonymousData?.firstName ?? '');
    const patientLastName =
      patientMode === 'existing'
        ? (selectedPatient?.name.split(' ').slice(1).join(' ') ?? '')
        : (anonymousData?.lastName ?? '');

    await createAccount({
      email,
      password,
      firstName: patientFirstName,
      lastName: patientLastName,
      appointmentId: bookingResult.appointmentId,
    });
  };

  // Focus success region after booking
  useEffect(() => {
    if (bookingResult && successRef.current) {
      successRef.current.focus();
    }
  }, [bookingResult]);

  const patientDisplayName =
    patientMode === 'existing' && selectedPatient
      ? selectedPatient.name
      : anonymousData
        ? `${anonymousData.firstName} ${anonymousData.lastName}`
        : '';

  const patientFirstName =
    patientMode === 'existing'
      ? (selectedPatient?.name.split(' ')[0] ?? '')
      : (anonymousData?.firstName ?? '');

  const patientLastName =
    patientMode === 'existing'
      ? (selectedPatient?.name.split(' ').slice(1).join(' ') ?? '')
      : (anonymousData?.lastName ?? '');

  if (bookingResult) {
    return (
      <main className={styles.page}>
        <nav className={styles.topnav} aria-label="Staff Portal navigation">
          <div className={styles.topnavInner}>
            <Link to="/staff/queue" className={styles.navLogo} aria-label="Staff Portal">
              <div className={styles.navLogoMark} aria-hidden="true">S</div>
              <span>Staff Portal</span>
            </Link>
            <div className={styles.navLinks}>
              <Link to="/staff/queue" className={styles.navLink}>Queue</Link>
              <span className={`${styles.navLink} ${styles.navLinkActive}`} aria-current="page">
                New Walk-in
              </span>
            </div>
            <span className={styles.roleBadge} aria-label="Staff role">Staff</span>
          </div>
        </nav>

        <div className={styles.content}>
          <div
            className={styles.successCard}
            role="region"
            aria-label="Booking confirmed"
            tabIndex={-1}
            ref={successRef}
          >
            <h1 className={styles.successTitle}>Walk-in registered</h1>
            <p className={styles.successBody}>
              {patientDisplayName || 'Patient'} has been added
              {appointmentDetails.slotId
                ? ` with slot ${DEMO_SLOTS.find((s) => s.id === appointmentDetails.slotId)?.label ?? ''}`
                : ' to the queue without a slot'}
              .
            </p>

            {patientMode === 'anonymous' && (
              <div className={styles.createAccountSection}>
                <button
                  type="button"
                  className={styles.accordionToggle}
                  aria-expanded={showCreateAccount}
                  onClick={() => setShowCreateAccount((v) => !v)}
                >
                  {showCreateAccount ? '▲ Hide' : '▼ Create patient account'}
                </button>

                {showCreateAccount && (
                  <div className={styles.accordionBody}>
                    <CreatePatientAccountForm
                      firstName={patientFirstName}
                      lastName={patientLastName}
                      appointmentId={bookingResult.appointmentId}
                      isSubmitting={isCreatingAccount}
                      submitError={accountError}
                      isDuplicateEmail={isDuplicateEmail}
                      accountCreated={accountCreated}
                      onSubmit={handleCreateAccount}
                      onLinkExisting={() => {/* US_023 placeholder */}}
                    />
                  </div>
                )}
              </div>
            )}

            <div className={styles.formFooter}>
              <Link to="/staff/queue" className={styles.btnPrimary}>
                Back to queue
              </Link>
            </div>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className={styles.page}>
      <nav className={styles.topnav} aria-label="Staff Portal navigation">
        <div className={styles.topnavInner}>
          <Link to="/staff/queue" className={styles.navLogo} aria-label="Staff Portal">
            <div className={styles.navLogoMark} aria-hidden="true">S</div>
            <span>Staff Portal</span>
          </Link>
          <div className={styles.navLinks}>
            <Link to="/staff/queue" className={styles.navLink}>Queue</Link>
            <span className={`${styles.navLink} ${styles.navLinkActive}`} aria-current="page">
              New Walk-in
            </span>
          </div>
          <span className={styles.roleBadge} aria-label="Staff role">Staff</span>
        </div>
      </nav>

      <div className={styles.content}>
        <nav className={styles.breadcrumb} aria-label="Breadcrumb">
          <Link to="/staff/queue" className={styles.breadcrumbLink}>Queue</Link>
          <span className={styles.breadcrumbSep} aria-hidden="true">/</span>
          <span aria-current="page">New Walk-in</span>
        </nav>

        <h1 className={styles.pageTitle}>Add Walk-in Patient</h1>
        <p className={styles.pageSubtitle}>
          Register a walk-in patient for today's queue. You can assign a slot or add them to the
          queue without one.
        </p>

        {/* Patient type toggle */}
        <div className={styles.patientToggle} role="group" aria-label="Patient type">
          <button
            type="button"
            className={`${styles.toggleBtn}${patientMode === 'existing' ? ` ${styles.toggleBtnActive}` : ''}`}
            aria-pressed={patientMode === 'existing'}
            onClick={() => handlePatientModeSwitch('existing')}
          >
            Existing patient
          </button>
          <button
            type="button"
            className={`${styles.toggleBtn}${patientMode === 'anonymous' ? ` ${styles.toggleBtnActive}` : ''}`}
            aria-pressed={patientMode === 'anonymous'}
            onClick={() => handlePatientModeSwitch('anonymous')}
          >
            New patient
          </button>
        </div>

        {/* Step 1: Patient identification */}
        {patientMode === 'existing' ? (
          <section className={styles.card} aria-labelledby="search-heading">
            <div className={styles.cardHeader}>
              <h2 id="search-heading" className={styles.cardTitle}>Search patient</h2>
            </div>
            <div className={styles.cardBody}>
              <PatientSearchInput
                onPatientSelect={handlePatientSelect}
                onNotFound={handleNotFound}
              />
            </div>
          </section>
        ) : (
          <section className={styles.card} aria-labelledby="anon-section-heading">
            <div className={styles.cardHeader}>
              <h2 id="anon-section-heading" className={styles.cardTitle}>New patient details</h2>
            </div>
            <div className={styles.cardBody}>
              <AnonymousPatientForm onChange={handleAnonymousChange} />
            </div>
          </section>
        )}

        {/* Step 2: Appointment details */}
        <section className={styles.card} aria-labelledby="appt-heading">
          <div className={styles.cardHeader}>
            <h2 id="appt-heading" className={styles.cardTitle}>Appointment details</h2>
          </div>
          <div className={styles.cardBody}>
            <div className={styles.fieldRow}>
              <div className={styles.fieldGroup}>
                <label htmlFor="walkin-type" className={styles.fieldLabel}>
                  Type <span className={styles.required} aria-hidden="true">*</span>
                </label>
                <select
                  id="walkin-type"
                  className={styles.select}
                  aria-required="true"
                  value={appointmentDetails.type}
                  onChange={(e) =>
                    setAppointmentDetails((prev) => ({
                      ...prev,
                      type: e.target.value as AppointmentType,
                    }))
                  }
                >
                  {APPOINTMENT_TYPES.map(({ value, label }) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
              </div>

              <div className={styles.fieldGroup}>
                <label htmlFor="walkin-provider" className={styles.fieldLabel}>
                  Provider
                </label>
                <select
                  id="walkin-provider"
                  className={styles.select}
                  value={appointmentDetails.providerId}
                  onChange={(e) =>
                    setAppointmentDetails((prev) => ({ ...prev, providerId: e.target.value }))
                  }
                >
                  {PROVIDERS.map(({ value, label }) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className={styles.fieldGroup}>
              <span className={styles.fieldLabel}>
                Assign slot — Today{' '}
                <span className={styles.fieldLabelOptional}>(optional)</span>
              </span>

              {hasAvailableSlots ? (
                <>
                  <div
                    className={styles.slotGrid}
                    role="group"
                    aria-label="Available appointment slots"
                  >
                    {DEMO_SLOTS.map((slot) => (
                      <button
                        key={slot.id}
                        type="button"
                        className={`${styles.slot}${
                          !slot.available
                            ? ` ${styles.slotTaken}`
                            : appointmentDetails.slotId === slot.id
                              ? ` ${styles.slotSelected}`
                              : ` ${styles.slotAvailable}`
                        }`}
                        disabled={!slot.available}
                        aria-disabled={!slot.available}
                        aria-pressed={slot.available && appointmentDetails.slotId === slot.id}
                        onClick={() => slot.available && handleSlotSelect(slot.id)}
                      >
                        {slot.label}
                      </button>
                    ))}
                  </div>
                  <p className={styles.slotNote} aria-live="polite">
                    {appointmentDetails.slotId
                      ? `Slot selected: ${DEMO_SLOTS.find((s) => s.id === appointmentDetails.slotId)?.label ?? ''}`
                      : 'No slot selected — you can add the patient to the queue without a slot.'}
                  </p>
                </>
              ) : (
                <div className={styles.noSlotBanner} role="region" aria-label="No available slots">
                  <p className={styles.noSlotTitle}>No available slots</p>
                  <p className={styles.noSlotBody}>
                    There are no open slots for today. Add the patient to the same-day queue
                    instead.
                  </p>
                  <Link to="/staff/queue" className={styles.btnWarning}>
                    Add to same-day queue
                  </Link>
                </div>
              )}
            </div>
          </div>
        </section>

        {/* Booking error */}
        {bookingError && (
          <div className={styles.errorBanner} role="alert" aria-live="assertive">
            {bookingError}
          </div>
        )}

        {/* Form footer */}
        <div className={styles.formFooter}>
          <Link to="/staff/queue" className={styles.btnGhost}>
            Cancel
          </Link>
          <button
            type="button"
            id="btn-confirm-walkin"
            className={styles.btnPrimary}
            disabled={!isPatientReady || isBooking}
            aria-busy={isBooking}
            onClick={handleConfirm}
          >
            {isBooking ? 'Registering…' : 'Confirm walk-in'}
          </button>
        </div>
      </div>
    </main>
  );
}

export default WalkInBookingPage;
