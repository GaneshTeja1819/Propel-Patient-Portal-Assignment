/**
 * ManualIntakeForm.tsx — 5-section manual intake form (US_019 task_001).
 *
 * Wireframe: SCR-008 (wireframe-SCR-008-manual-intake.html)
 * Accessibility: WCAG 2.2 AA — aria-labelledby on radio groups (fieldset+legend),
 *   aria-describedby on required inputs, focus-first-invalid on submit failure.
 * Design tokens: all colours via CSS custom properties from variables.css.
 *
 * Does NOT use react-hook-form (not installed). Validation is native React.
 */
import { useCallback, useRef, useState } from 'react';
import { IntakeProgressBar } from './IntakeProgressBar';
import { useManualIntake } from '../../hooks/useManualIntake';
import type { CapturedField } from '../../types/intake';
import styles from './ManualIntakeForm.module.css';

// ── Types ──────────────────────────────────────────────────────────────────

type FormValues = {
  'chief-complaint': string;
  'symptom-duration': string;
  'pain-scale': string;
  medications: string;
  'med-types': string[];
  allergies: string;
  'allergy-sev': string;
  smoking: string;
  alcohol: string;
  exercise: string;
  notes: string;
};

export interface ManualIntakeFormProps {
  appointmentId: string;
  /** Pre-populated values from the AI session (AC-004). */
  defaultValues: Partial<Record<string, string>>;
  /** Fields captured by AI that have no matching manual form key. */
  unmappedAiFields?: CapturedField[];
  /** Called when the user switches back to AI mode; passes current form values (AC-005). */
  onSwitchToAI: (currentValues: Record<string, string>) => void;
  /** Called when the user chooses "Save & continue later". */
  onSaveLater: () => void;
  /** Called after a successful submission. */
  onSubmitSuccess: () => void;
}

// ── Constants ──────────────────────────────────────────────────────────────

const SECTIONS = [
  { id: 1, title: 'Chief Complaint', phi: true, icon: '🩺' },
  { id: 2, title: 'Medications', phi: true, icon: '💊' },
  { id: 3, title: 'Allergies', phi: true, icon: '⚠️' },
  { id: 4, title: 'Lifestyle', phi: false, icon: '🏃' },
  { id: 5, title: 'Additional Notes', phi: true, icon: '📝' },
] as const;

const REQUIRED_FIELDS: Array<keyof FormValues> = [
  'chief-complaint',
  'symptom-duration',
  'medications',
  'allergies',
];

const REQUIRED_MESSAGES: Partial<Record<keyof FormValues, string>> = {
  'chief-complaint': 'Please describe your chief complaint.',
  'symptom-duration': 'Please select symptom duration.',
  medications: 'Please list your current medications.',
  allergies: 'Please list any known allergies.',
};

function buildInitialValues(defaults: Partial<Record<string, string>>): FormValues {
  return {
    'chief-complaint': defaults['chief-complaint'] ?? '',
    'symptom-duration': defaults['symptom-duration'] ?? '',
    'pain-scale': defaults['pain-scale'] ?? '0',
    medications: defaults['medications'] ?? '',
    'med-types': defaults['med-types'] ? defaults['med-types'].split(',') : [],
    allergies: defaults['allergies'] ?? '',
    'allergy-sev': defaults['allergy-sev'] ?? '',
    smoking: defaults['smoking'] ?? '',
    alcohol: defaults['alcohol'] ?? '',
    exercise: defaults['exercise'] ?? '',
    notes: defaults['notes'] ?? '',
  };
}

function flattenValues(values: FormValues): Record<string, string> {
  return {
    ...values,
    'med-types': values['med-types'].join(','),
  };
}

function validate(values: FormValues): Partial<Record<keyof FormValues, string>> {
  const errors: Partial<Record<keyof FormValues, string>> = {};
  for (const key of REQUIRED_FIELDS) {
    const val = values[key];
    if (!val || (typeof val === 'string' && !val.trim())) {
      errors[key] = REQUIRED_MESSAGES[key] ?? `${key} is required.`;
    }
  }
  return errors;
}

function isSectionComplete(sectionId: number, values: FormValues): boolean {
  switch (sectionId) {
    case 1:
      return Boolean(values['chief-complaint']?.trim() && values['symptom-duration']?.trim());
    case 2:
      return Boolean(values['medications']?.trim());
    case 3:
      return Boolean(values['allergies']?.trim());
    case 4:
      return Boolean(values.smoking || values.alcohol || values.exercise);
    case 5:
      return Boolean(values.notes?.trim());
    default:
      return false;
  }
}

// ── Component ──────────────────────────────────────────────────────────────

export function ManualIntakeForm({
  appointmentId,
  defaultValues,
  onSwitchToAI,
  onSaveLater,
  onSubmitSuccess,
}: ManualIntakeFormProps): JSX.Element {
  const [values, setValues] = useState<FormValues>(() => buildInitialValues(defaultValues));
  const [errors, setErrors] = useState<Partial<Record<keyof FormValues, string>>>({});
  const [openSections, setOpenSections] = useState<Set<number>>(new Set([1]));
  const fieldRefs = useRef<Partial<Record<keyof FormValues, HTMLElement | null>>>({});

  const { isSubmitting, submitError, submitManualIntake, clearSubmitError } = useManualIntake();

  const completedSections = SECTIONS.filter((s) => isSectionComplete(s.id, values)).length;
  // Active section = lowest-numbered open section (for Section label); default to 1
  const activeSectionNumber = openSections.size > 0 ? Math.min(...Array.from(openSections)) : 1;
  const completedPct = Math.round((completedSections / SECTIONS.length) * 100);

  // ── Field change handlers ────────────────────────────────────────────────

  const handleChange = useCallback(
    (key: keyof FormValues, value: string) => {
      setValues((prev) => ({ ...prev, [key]: value }));
      if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }));
    },
    [errors],
  );

  const handleCheckboxChange = useCallback((key: keyof FormValues, option: string, checked: boolean) => {
    setValues((prev) => {
      const current = prev[key] as string[];
      return {
        ...prev,
        [key]: checked ? [...current, option] : current.filter((v) => v !== option),
      };
    });
  }, []);

  // ── Section toggle ───────────────────────────────────────────────────────

  const toggleSection = useCallback((id: number) => {
    setOpenSections((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }, []);

  // ── Submit ───────────────────────────────────────────────────────────────

  const handleSubmit = useCallback(
    async (e: React.FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      clearSubmitError();
      const validationErrors = validate(values);

      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
        // Focus the first invalid field (AC-002)
        const firstKey = Object.keys(validationErrors)[0] as keyof FormValues;
        const el = fieldRefs.current[firstKey];
        if (el) {
          // Ensure the section containing the invalid field is open
          const fieldSection = { 'chief-complaint': 1, 'symptom-duration': 1, 'pain-scale': 1, medications: 2, 'med-types': 2, allergies: 3, 'allergy-sev': 3, smoking: 4, alcohol: 4, exercise: 4, notes: 5 } as const;
          const sectionId = fieldSection[firstKey];
          setOpenSections((prev) => new Set([...prev, sectionId]));
          // Delay focus to allow section expand to render
          setTimeout(() => el.focus(), 50);
        }
        return;
      }

      try {
        await submitManualIntake(appointmentId, flattenValues(values));
        onSubmitSuccess();
      } catch {
        // submitError state is set by the hook; error is displayed in the banner
      }
    },
    [values, appointmentId, submitManualIntake, clearSubmitError, onSubmitSuccess],
  );

  // ── Ref helper ───────────────────────────────────────────────────────────

  const setRef = useCallback(
    (key: keyof FormValues) => (el: HTMLElement | null) => {
      fieldRefs.current[key] = el;
    },
    [],
  );

  // ── Render helpers ───────────────────────────────────────────────────────

  function renderFieldError(key: keyof FormValues) {
    return errors[key] ? (
      <span id={`${key}-err`} className={styles.fieldError} role="alert">
        <span aria-hidden="true">⚠</span> {errors[key]}
      </span>
    ) : null;
  }

  function renderPhiNote() {
    return (
      <div className={styles.phiNote} aria-label="Protected health information">
        <span aria-hidden="true">🔒</span> Protected health information
      </div>
    );
  }

  // ── Sections ─────────────────────────────────────────────────────────────

  function renderSection1() {
    const isOpen = openSections.has(1);
    const complete = isSectionComplete(1, values);
    return (
      <div className={styles.sectionCard}>
        <button
          type="button"
          className={styles.sectionHeader}
          aria-expanded={isOpen}
          aria-controls="section-1-body"
          onClick={() => toggleSection(1)}
        >
          <div className={styles.sectionTitleGroup}>
            <span className={styles.sectionIcon} aria-hidden="true">🩺</span>
            <span className={styles.sectionTitle}>Chief Complaint</span>
          </div>
          <div className={styles.sectionMeta}>
            {complete && (
              <span className={styles.sectionComplete} id="sec-1-status" aria-label="Section complete">
                ✓ Complete
              </span>
            )}
            <span className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ''}`} aria-hidden="true">▼</span>
          </div>
        </button>
        <div id="section-1-body" className={`${styles.sectionBody}${!isOpen ? ` ${styles.sectionBodyHidden}` : ''}`} role="region" aria-labelledby="sec-1-heading">
            <span id="sec-1-heading" className="sr-only">Chief Complaint section</span>
            {renderPhiNote()}

            {/* Chief complaint */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="chief-complaint">
                Chief complaint <span className={styles.required} aria-hidden="true">*</span>
              </label>
              <textarea
                id="chief-complaint"
                className={`${styles.textarea} ${styles.phiField}${errors['chief-complaint'] ? ` ${styles.inputError}` : ''}`}
                aria-required="true"
                aria-describedby={errors['chief-complaint'] ? 'chief-complaint-err' : undefined}
                rows={4}
                placeholder="Describe your main symptom or reason for the visit…"
                value={values['chief-complaint']}
                onChange={(e) => handleChange('chief-complaint', e.target.value)}
                ref={setRef('chief-complaint') as React.RefCallback<HTMLTextAreaElement>}
              />
              {renderFieldError('chief-complaint')}
            </div>

            {/* Symptom duration */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="symptom-duration">
                How long have you had this symptom?{' '}
                <span className={styles.required} aria-hidden="true">*</span>
              </label>
              <select
                id="symptom-duration"
                className={`${styles.select} ${styles.phiField}${errors['symptom-duration'] ? ` ${styles.inputError}` : ''}`}
                aria-required="true"
                aria-describedby={errors['symptom-duration'] ? 'symptom-duration-err' : undefined}
                value={values['symptom-duration']}
                onChange={(e) => handleChange('symptom-duration', e.target.value)}
                ref={setRef('symptom-duration') as React.RefCallback<HTMLSelectElement>}
              >
                <option value="">— select —</option>
                <option value="days">A few days</option>
                <option value="1-2w">1–2 weeks</option>
                <option value="3-4w">3–4 weeks</option>
                <option value="1-3m">1–3 months</option>
                <option value="3m+">More than 3 months</option>
              </select>
              {renderFieldError('symptom-duration')}
            </div>

            {/* Pain scale */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="pain-scale">
                Pain / severity (0–10)
              </label>
              <div className={styles.rangeWrap}>
                <input
                  type="range"
                  id="pain-scale"
                  className={`${styles.range} ${styles.phiField}`}
                  min={0}
                  max={10}
                  step={1}
                  value={values['pain-scale']}
                  onChange={(e) => handleChange('pain-scale', e.target.value)}
                  aria-valuemin={0}
                  aria-valuemax={10}
                  aria-valuenow={Number(values['pain-scale'])}
                />
                <span className={styles.rangeValue} aria-live="polite" aria-atomic="true">
                  {values['pain-scale']}
                </span>
              </div>
              <div className={styles.rangeLabels} aria-hidden="true">
                <span>0 (None)</span>
                <span>10 (Severe)</span>
              </div>
            </div>
        </div>
      </div>
    );
  }

  function renderSection2() {
    const isOpen = openSections.has(2);
    const complete = isSectionComplete(2, values);
    const medTypeOptions = [
      { value: 'blood-thinners', label: 'Blood thinners' },
      { value: 'insulin', label: 'Insulin' },
      { value: 'heart', label: 'Heart medication' },
      { value: 'none', label: 'None of the above' },
    ];
    return (
      <div className={styles.sectionCard}>
        <button
          type="button"
          className={styles.sectionHeader}
          aria-expanded={isOpen}
          aria-controls="section-2-body"
          onClick={() => toggleSection(2)}
        >
          <div className={styles.sectionTitleGroup}>
            <span className={styles.sectionIcon} aria-hidden="true">💊</span>
            <span className={styles.sectionTitle}>Medications</span>
          </div>
          <div className={styles.sectionMeta}>
            {complete && (
              <span className={styles.sectionComplete} id="sec-2-status" aria-label="Section complete">
                ✓ Complete
              </span>
            )}
            <span className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ''}`} aria-hidden="true">▼</span>
          </div>
        </button>
        <div id="section-2-body" className={`${styles.sectionBody}${!isOpen ? ` ${styles.sectionBodyHidden}` : ''}`} role="region" aria-labelledby="sec-2-heading">
            <span id="sec-2-heading" className="sr-only">Medications section</span>
            {renderPhiNote()}

            {/* Medications textarea */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="medications">
                Current medications{' '}
                <span className={styles.required} aria-hidden="true">*</span>
              </label>
              <textarea
                id="medications"
                className={`${styles.textarea} ${styles.phiField}${errors['medications'] ? ` ${styles.inputError}` : ''}`}
                aria-required="true"
                aria-describedby={errors['medications'] ? 'medications-err' : undefined}
                rows={3}
                placeholder="List all current medications, dosages, and frequency…"
                value={values['medications']}
                onChange={(e) => handleChange('medications', e.target.value)}
                ref={setRef('medications') as React.RefCallback<HTMLTextAreaElement>}
              />
              {renderFieldError('medications')}
            </div>

            {/* Medication type checkboxes */}
            <fieldset className={styles.fieldset}>
              <legend className={styles.fieldGroupLegend}>Medication categories (optional)</legend>
              <div className={styles.optionGrid} role="group" aria-label="Medication categories">
                {medTypeOptions.map(({ value, label }) => (
                  <label key={value} className={styles.optionLabel}>
                    <input
                      type="checkbox"
                      value={value}
                      checked={values['med-types'].includes(value)}
                      onChange={(e) => handleCheckboxChange('med-types', value, e.target.checked)}
                    />
                    {label}
                  </label>
                ))}
              </div>
            </fieldset>
        </div>
      </div>
    );
  }

  function renderSection3() {
    const isOpen = openSections.has(3);
    const complete = isSectionComplete(3, values);
    const severityOptions = [
      { value: 'mild', label: 'Mild' },
      { value: 'moderate', label: 'Moderate' },
      { value: 'severe', label: 'Severe' },
    ];
    return (
      <div className={styles.sectionCard}>
        <button
          type="button"
          className={styles.sectionHeader}
          aria-expanded={isOpen}
          aria-controls="section-3-body"
          onClick={() => toggleSection(3)}
        >
          <div className={styles.sectionTitleGroup}>
            <span className={styles.sectionIcon} aria-hidden="true">⚠️</span>
            <span className={styles.sectionTitle}>Allergies</span>
          </div>
          <div className={styles.sectionMeta}>
            {complete && (
              <span className={styles.sectionComplete} id="sec-3-status" aria-label="Section complete">
                ✓ Complete
              </span>
            )}
            <span className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ''}`} aria-hidden="true">▼</span>
          </div>
        </button>
        <div id="section-3-body" className={`${styles.sectionBody}${!isOpen ? ` ${styles.sectionBodyHidden}` : ''}`} role="region" aria-labelledby="sec-3-heading">
            <span id="sec-3-heading" className="sr-only">Allergies section</span>
            {renderPhiNote()}

            {/* Allergies textarea */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="allergies">
                Known allergies{' '}
                <span className={styles.required} aria-hidden="true">*</span>
              </label>
              <textarea
                id="allergies"
                className={`${styles.textarea} ${styles.phiField}${errors['allergies'] ? ` ${styles.inputError}` : ''}`}
                aria-required="true"
                aria-describedby={errors['allergies'] ? 'allergies-err' : undefined}
                rows={3}
                placeholder="List known allergies and reactions…"
                value={values['allergies']}
                onChange={(e) => handleChange('allergies', e.target.value)}
                ref={setRef('allergies') as React.RefCallback<HTMLTextAreaElement>}
              />
              {renderFieldError('allergies')}
            </div>

            {/* Allergy severity — aria-labelledby on fieldset legend (WCAG 1.3.1) */}
            <fieldset className={styles.fieldset} aria-labelledby="allergy-sev-label">
              <legend id="allergy-sev-label" className={styles.fieldGroupLegend}>
                Allergy severity (optional)
              </legend>
              <div className={styles.optionGrid}>
                {severityOptions.map(({ value, label }) => (
                  <label key={value} className={styles.optionLabel}>
                    <input
                      type="radio"
                      name="allergy-sev"
                      value={value}
                      checked={values['allergy-sev'] === value}
                      onChange={() => handleChange('allergy-sev', value)}
                    />
                    {label}
                  </label>
                ))}
              </div>
            </fieldset>
        </div>
      </div>
    );
  }

  function renderSection4() {
    const isOpen = openSections.has(4);
    const complete = isSectionComplete(4, values);
    const smokingOptions = [
      { value: 'never', label: 'Never' },
      { value: 'former', label: 'Former smoker' },
      { value: 'current', label: 'Current smoker' },
    ];
    return (
      <div className={styles.sectionCard}>
        <button
          type="button"
          className={styles.sectionHeader}
          aria-expanded={isOpen}
          aria-controls="section-4-body"
          onClick={() => toggleSection(4)}
        >
          <div className={styles.sectionTitleGroup}>
            <span className={styles.sectionIcon} aria-hidden="true">🏃</span>
            <span className={styles.sectionTitle}>Lifestyle</span>
          </div>
          <div className={styles.sectionMeta}>
            {complete && (
              <span className={styles.sectionComplete} id="sec-4-status" aria-label="Section complete">
                ✓ Complete
              </span>
            )}
            <span className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ''}`} aria-hidden="true">▼</span>
          </div>
        </button>
        <div id="section-4-body" className={`${styles.sectionBody}${!isOpen ? ` ${styles.sectionBodyHidden}` : ''}`} role="region" aria-labelledby="sec-4-heading">
            <span id="sec-4-heading" className="sr-only">Lifestyle section</span>

            {/* Smoking status — aria-labelledby on fieldset (UXR-203) */}
            <fieldset className={styles.fieldset} aria-labelledby="smoking-label">
              <legend id="smoking-label" className={styles.fieldGroupLegend}>
                Smoking status (optional)
              </legend>
              <div className={styles.optionGrid}>
                {smokingOptions.map(({ value, label }) => (
                  <label key={value} className={styles.optionLabel}>
                    <input
                      type="radio"
                      name="smoking"
                      value={value}
                      checked={values.smoking === value}
                      onChange={() => handleChange('smoking', value)}
                    />
                    {label}
                  </label>
                ))}
              </div>
            </fieldset>

            {/* Alcohol */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="alcohol">
                Alcohol consumption (optional)
              </label>
              <select
                id="alcohol"
                className={styles.select}
                value={values.alcohol}
                onChange={(e) => handleChange('alcohol', e.target.value)}
              >
                <option value="">— select —</option>
                <option value="none">None</option>
                <option value="occasional">Occasional (1–2 drinks/week)</option>
                <option value="moderate">Moderate (3–7 drinks/week)</option>
                <option value="heavy">Heavy (8+ drinks/week)</option>
              </select>
            </div>

            {/* Exercise */}
            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="exercise">
                Exercise frequency (optional)
              </label>
              <select
                id="exercise"
                className={styles.select}
                value={values.exercise}
                onChange={(e) => handleChange('exercise', e.target.value)}
              >
                <option value="">— select —</option>
                <option value="sedentary">Sedentary (little/no exercise)</option>
                <option value="1-2">1–2 days/week</option>
                <option value="3-4">3–4 days/week</option>
                <option value="5+">5+ days/week</option>
              </select>
            </div>
        </div>
      </div>
    );
  }

  function renderSection5() {
    const isOpen = openSections.has(5);
    const complete = isSectionComplete(5, values);
    return (
      <div className={styles.sectionCard}>
        <button
          type="button"
          className={styles.sectionHeader}
          aria-expanded={isOpen}
          aria-controls="section-5-body"
          onClick={() => toggleSection(5)}
        >
          <div className={styles.sectionTitleGroup}>
            <span className={styles.sectionIcon} aria-hidden="true">📝</span>
            <span className={styles.sectionTitle}>Additional Notes</span>
          </div>
          <div className={styles.sectionMeta}>
            {complete && (
              <span className={styles.sectionComplete} id="sec-5-status" aria-label="Section complete">
                ✓ Complete
              </span>
            )}
            <span className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ''}`} aria-hidden="true">▼</span>
          </div>
        </button>
        <div id="section-5-body" className={`${styles.sectionBody}${!isOpen ? ` ${styles.sectionBodyHidden}` : ''}`} role="region" aria-labelledby="sec-5-heading">
            <span id="sec-5-heading" className="sr-only">Additional notes section</span>
            {renderPhiNote()}

            <div className={styles.fieldWrap}>
              <label className={styles.fieldLabel} htmlFor="notes">
                Anything else your doctor should know? (optional)
              </label>
              <textarea
                id="notes"
                className={`${styles.textarea} ${styles.phiField}`}
                rows={4}
                placeholder="Any additional information that may be relevant to your visit…"
                value={values['notes']}
                onChange={(e) => handleChange('notes', e.target.value)}
              />
            </div>
        </div>
      </div>
    );
  }

  // ── Main render ───────────────────────────────────────────────────────────

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label="Manual intake form"
      data-uxr="UXR-203"
      style={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}
    >
      {/* Progress bar: section-based for manual mode (UXR-502) */}
      <div className={styles.progressWrap}>
        <IntakeProgressBar
          current={activeSectionNumber}
          total={SECTIONS.length}
          progressValue={completedPct}
          sessionTitle="Manual Intake"
          labelPrefix="Section"
          data-uxr="UXR-502"
        />
      </div>

      <div className={styles.formScroll}>
        <div className={styles.formInner}>
          <p className={styles.formSubtitle}>
            Complete all sections. Required fields are marked{' '}
            <span aria-hidden="true">*</span>. Your information is encrypted.
          </p>
          {renderSection1()}
          {renderSection2()}
          {renderSection3()}
          {renderSection4()}
          {renderSection5()}
        </div>
      </div>

      {/* Submit error banner */}
      {submitError && (
        <div className={styles.submitErrorBanner} role="alert" aria-live="assertive">
          <span aria-hidden="true">⚠</span> {submitError}
        </div>
      )}

      {/* Form footer */}
      <div className={styles.formFooter}>
        <button
          type="button"
          className={styles.btnGhost}
          onClick={() => onSwitchToAI(flattenValues(values))}
          aria-label="Switch to AI-assisted intake (your answers will be carried over)"
          data-uxr="UXR-103"
        >
          ← Switch to AI
        </button>
        <div style={{ display: 'flex', gap: 'var(--space-3)', alignItems: 'center' }}>
          <button
            type="button"
            className={styles.btnGhost}
            onClick={onSaveLater}
            aria-label="Save progress and continue later"
          >
            Save &amp; continue later
          </button>
          <button
            type="submit"
            className={styles.btnPrimary}
            disabled={isSubmitting}
            aria-busy={isSubmitting}
          >
            {isSubmitting ? (
              <>
                <span className={styles.btnSpinner} aria-hidden="true" />
                Submitting…
              </>
            ) : 'Submit intake →'}
          </button>
        </div>
      </div>
    </form>
  );
}
