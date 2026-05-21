/**
 * IntakePage.tsx — Route /intake/:appointmentId (US_018).
 *
 * Shared intake page with route-level mode state ('ai' | 'manual').
 * Mode is persisted to sessionStorage so a browser refresh restores the
 * patient's chosen method.
 *
 * AI path:  AIIntakeChat + IntakeProgressBar (this task)
 * Manual path: ManualIntakeForm placeholder (US_019 task_001)
 *
 * Dependencies:
 *   - AuthContext (US_010 stub provided)
 *   - useAIIntake (this task)
 *   - react-router-dom v6 useParams
 */
import { useEffect, useState, useCallback } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useAIIntake } from '../hooks/useAIIntake';
import { useAuthContext } from '../context/AuthContext';
import { AIIntakeChat } from '../components/intake/AIIntakeChat';
import { IntakeProgressBar } from '../components/intake/IntakeProgressBar';
import { IntakeSummaryReview } from '../components/intake/IntakeSummaryReview';
import { ManualIntakeForm } from '../components/intake/ManualIntakeForm';
import styles from './IntakePage.module.css';

/** Visually hidden but accessible to assistive technology. */
// NOTE: replaced by global .sr-only class — this const removed (F015)

type IntakeMode = 'ai' | 'manual';

function modeStorageKey(appointmentId: string): string {
  return `intake-mode-${appointmentId}`;
}

function loadMode(appointmentId: string): IntakeMode {
  try {
    const stored = sessionStorage.getItem(modeStorageKey(appointmentId));
    return stored === 'manual' ? 'manual' : 'ai';
  } catch {
    return 'ai';
  }
}

function saveMode(appointmentId: string, mode: IntakeMode): void {
  try {
    sessionStorage.setItem(modeStorageKey(appointmentId), mode);
  } catch {
    // sessionStorage unavailable — continue without persisting
  }
}

export function IntakePage(): JSX.Element {
  const { appointmentId = '' } = useParams<{ appointmentId: string }>();
  const { role } = useAuthContext();
  const navigate = useNavigate();
  const [mode, setMode] = useState<IntakeMode>(() => loadMode(appointmentId));
  const [confirmed, setConfirmed] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const [modeAnnouncement, setModeAnnouncement] = useState('');

  const displayInitials = (role ?? 'P').slice(0, 1).toUpperCase();

  // B-001: update document.title on mount and when submission or mode changes (WCAG 2.4.2)
  useEffect(() => {
    if (confirmed) {
      document.title = 'Intake Submitted | Patient Portal';
    } else if (mode === 'manual') {
      document.title = 'Manual Intake | Patient Portal';
    } else {
      document.title = 'AI Intake | Patient Portal';
    }
    return () => {
      document.title = 'Patient Portal';
    };
  }, [confirmed, mode]);

  const {
    messages,
    capturedFields,
    questionNumber,
    totalQuestions,
    isPhiField,
    phase,
    errorMessage,
    startSession,
    submitAnswer,
    updateFieldValue,
    confirmIntake,
    clearError,
  } = useAIIntake(appointmentId);

  // Start AI session on mount (or resume if partial state exists)
  useEffect(() => {
    if (mode === 'ai') {
      startSession();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleModeSwitch = useCallback(
    (next: IntakeMode): void => {
      if (next === mode) return;
      saveMode(appointmentId, next);
      setMode(next);
      setModeAnnouncement(
        next === 'manual'
          ? 'Switched to manual intake form. Your AI answers have been carried over.'
          : 'Switched to AI-assisted intake. Your entered answers have been carried over.',
      );
      // M-004: clear AI session error when switching away — it's not relevant in manual mode
      if (next === 'manual') clearError();
      // capturedFields are preserved in useAIIntake state — no data loss (UXR-103)
    },
    [mode, appointmentId, clearError],
  );

  const handleFallbackSubmit = useCallback(
    (fieldKey: string, value: string): void => {
      // F-002: persist locally only; do NOT also call submitAnswer to avoid double-dispatch
      updateFieldValue(fieldKey, value);
    },
    [updateFieldValue],
  );

  const handleConfirmIntake = useCallback(async (): Promise<void> => {
    setIsConfirming(true);
    try {
      await confirmIntake();
      setConfirmed(true);
    } finally {
      setIsConfirming(false);
    }
  }, [confirmIntake]);

  /** M-001: explicit save & exit affordance — data is already persisted in sessionStorage. */
  const handleSaveLater = useCallback((): void => {
    // sessionStorage is updated on every answer submission; navigate away is safe.
    navigate('/');
  }, [navigate]);

  /**
   * AC-004/AC-005: when switching manual → AI, sync any manually-entered values
   * back into the AI session's capturedFields state so no data is lost (UXR-103).
   */
  const handleSwitchToAI = useCallback(
    (currentManualValues: Record<string, string>): void => {
      Object.entries(currentManualValues).forEach(([key, value]) => {
        if (value) updateFieldValue(key, value);
      });
      handleModeSwitch('ai');
    },
    [updateFieldValue, handleModeSwitch],
  );

  /**
   * Convert capturedFields array to a flat Record<fieldKey, value> for ManualIntakeForm
   * defaultValues (AC-004 — AI answers pre-populate the manual form).
   */
  const capturedFieldsMap = capturedFields.reduce<Record<string, string>>((acc, f) => {
    acc[f.fieldKey] = f.value;
    return acc;
  }, {});

  if (confirmed) {
    return (
      <div className={styles.page}>
        {/* WCAG 2.4.1 — skip link to main content */}
        <a href="#main-intake" className={styles.skipLink}>
          Skip to main content
        </a>
        <nav className={styles.topnav} aria-label="Main navigation">
          <div className={styles.topnavInner}>
            <Link to="/" className={styles.navBack} aria-label="Back to dashboard">
              <div className={styles.navLogoMark} aria-hidden="true">
                P
              </div>
              <span>Patient Portal</span>
            </Link>
          </div>
        </nav>
        <main id="main-intake" className={styles.confirmedBanner}>
          {/* A-002: page-level heading (screen reader visible only) */}
          <h1 className="sr-only">Intake Submitted</h1>
          <p className={styles.confirmedHeading}>✓ Intake Submitted</p>
          <p className={styles.confirmedSub}>
            Your intake information has been securely submitted. Your care team will review it
            before your appointment.
          </p>
          <Link to="/" className={styles.btnDashboard}>
            Return to Dashboard
          </Link>
        </main>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      {/* WCAG 2.4.1 — skip navigation link */}
      <a href="#main-intake" className={styles.skipLink}>
        Skip to main content
      </a>
      {/* WCAG 4.1.3 — ARIA live region announces mode switch to screen readers */}
      <div role="status" aria-live="polite" aria-atomic="true" className="sr-only">
        {modeAnnouncement}
      </div>
      {/* ── Top nav ────────────────────────────────────────── */}
      <nav className={styles.topnav} aria-label="Main navigation">
        <div className={styles.topnavInner}>
          <Link
            to="/"
            className={styles.navBack}
            aria-label="Patient Portal — back to dashboard"
          >
            <div className={styles.navLogoMark} aria-hidden="true">
              P
            </div>
            <span>Intake Form</span>
          </Link>

          {/* Method switch — UXR-103: data preserved on switch */}
          <div
            className={styles.methodSwitch}
            role="group"
            aria-label="Intake method"
            data-uxr="UXR-103"
          >
            <span className={styles.methodSwitchLabel}>Method:</span>
            <button
              className={`${styles.methodBtn}${mode === 'ai' ? ` ${styles.active}` : ''}`}
              onClick={() => handleModeSwitch('ai')}
              aria-pressed={mode === 'ai'}
              aria-label="AI-assisted intake"
            >
              🤖 AI
            </button>
            <button
              className={`${styles.methodBtn}${mode === 'manual' ? ` ${styles.active}` : ''}`}
              onClick={() => handleModeSwitch('manual')}
              aria-pressed={mode === 'manual'}
              aria-label="Switch to manual form (your answers will be carried over)"
            >
              📝 Manual
            </button>
          </div>

          <div
            className={styles.userAvatar}
            aria-label="Patient"
            role="img"
          >
            {displayInitials}
          </div>
        </div>
      </nav>

      {/* ── Error banner ──────────────────────────────────── */}
      {errorMessage && (
        <div className={styles.errorBanner} role="alert" aria-live="assertive">
          <span aria-hidden="true">⚠</span> {errorMessage}
          {/* N-003: retry action so the patient can recover without a full refresh */}
          <button
            className={styles.btnRetry}
            onClick={startSession}
            aria-label="Retry starting the intake session"
          >
            Try again
          </button>
        </div>
      )}

      {/* A-003: <main> wraps the primary content at all phases */}
      <main id="main-intake" className={styles.mainContent}>
        {/* B-002: h1 inside main for correct landmark association (WCAG 1.3.1) */}
        <h1 className="sr-only">
          {mode === 'manual'
            ? 'Manual Intake — complete your pre-visit questionnaire'
            : 'AI Conversational Intake — complete your pre-visit questionnaire'}
        </h1>

        {/* ── AI path ───────────────────────────────────────── */}
        {mode === 'ai' && (phase === 'conversation' || phase === 'loading' || phase === 'error') && (
          <>
            {/* H-001: render progress bar even during error state using constant total (UXR-502) */}
            <IntakeProgressBar
              current={questionNumber}
              total={totalQuestions || 8}
              data-uxr="UXR-502"
            />
            <AIIntakeChat
              messages={messages}
              isLoading={phase === 'loading'}
              isPhiField={isPhiField}
              onSubmitAnswer={submitAnswer}
              onFallbackSubmit={handleFallbackSubmit}
              disabled={phase === 'loading' || phase === 'error'}
              onSaveLater={handleSaveLater}
            />
          </>
        )}

        {/* ── Summary / review (AC-003) ─────────────────────── */}
        {mode === 'ai' && (phase === 'summary' || isConfirming) && (
          <IntakeSummaryReview
            fields={capturedFields}
            isConfirming={isConfirming}
            onFieldSave={updateFieldValue}
            onConfirm={handleConfirmIntake}
          />
        )}

        {/* ── Manual path (US_019 task_001) ────────────────── */}
        {mode === 'manual' && (
          <ManualIntakeForm
            appointmentId={appointmentId}
            defaultValues={capturedFieldsMap}
            onSwitchToAI={handleSwitchToAI}
            onSaveLater={handleSaveLater}
            onSubmitSuccess={() => setConfirmed(true)}
          />
        )}
      </main>
    </div>
  );
}
