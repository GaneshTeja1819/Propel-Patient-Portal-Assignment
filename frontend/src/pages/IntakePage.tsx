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
import { useAuth } from '../context/AuthContext';
import { AIIntakeChat } from '../components/intake/AIIntakeChat';
import { IntakeProgressBar } from '../components/intake/IntakeProgressBar';
import { IntakeSummaryReview } from '../components/intake/IntakeSummaryReview';
import styles from './IntakePage.module.css';

/** Visually hidden but accessible to assistive technology. */
const srOnly: React.CSSProperties = {
  position: 'absolute',
  width: '1px',
  height: '1px',
  padding: 0,
  margin: '-1px',
  overflow: 'hidden',
  clip: 'rect(0,0,0,0)',
  whiteSpace: 'nowrap',
  border: 0,
};

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

/** Minimal placeholder rendered until US_019 delivers ManualIntakeForm. */
function ManualIntakeFormPlaceholder(): JSX.Element {
  return (
    <div
      style={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 'var(--space-12)',
        flexDirection: 'column',
        gap: 'var(--space-4)',
      }}
    >
      <p style={{ color: 'var(--color-text-secondary)', fontSize: 'var(--font-size-body-md)' }}>
        Manual intake form will be available in US_019.
      </p>
    </div>
  );
}

export function IntakePage(): JSX.Element {
  const { appointmentId = '' } = useParams<{ appointmentId: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [mode, setMode] = useState<IntakeMode>(() => loadMode(appointmentId));
  const [confirmed, setConfirmed] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);

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

  const displayInitials = user?.displayName
    ? user.displayName
        .split(' ')
        .map((n) => n?.[0] ?? '')
        .filter(Boolean)
        .join('')
        .toUpperCase()
        .slice(0, 2)
    : 'P';

  if (confirmed) {
    return (
      <div className={styles.page}>
        {/* N-001: Screen-specific page title */}
        <title>Intake Submitted | Patient Portal</title>
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
        {/* A-002: page-level heading (screen reader visible only) */}
        <h1 style={srOnly}>Intake Submitted</h1>
        <main className={styles.confirmedBanner}>
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
      {/* N-001: Screen-specific page title */}
      <title>AI Intake | Patient Portal</title>

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
            aria-label={user?.displayName ?? 'Patient'}
            role="img"
          >
            {displayInitials}
          </div>
        </div>
      </nav>

      {/* A-002: visually-hidden h1 for screen-reader heading navigation */}
      <h1 style={srOnly}>AI Conversational Intake — complete your pre-visit questionnaire</h1>

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
      <main className={styles.mainContent}>
        {/* ── AI path ───────────────────────────────────────── */}
        {mode === 'ai' && (phase === 'conversation' || phase === 'loading' || phase === 'error') && (
          <>
            {totalQuestions > 0 && (
              <IntakeProgressBar
                current={questionNumber}
                total={totalQuestions}
                data-uxr="UXR-502"
              />
            )}
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

        {/* ── Manual path placeholder (US_019) ─────────────── */}
        {mode === 'manual' && <ManualIntakeFormPlaceholder />}
      </main>
    </div>
  );
}
