import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { evaluatePassword } from '../../utils/passwordValidator';
import styles from './CreatePatientAccountForm.module.css';

interface CreatePatientAccountFormValues {
  email: string;
  password: string;
}

interface CreatePatientAccountFormProps {
  firstName: string;
  lastName: string;
  appointmentId: string;
  isSubmitting: boolean;
  submitError: string | null;
  isDuplicateEmail: boolean;
  accountCreated: boolean;
  onSubmit: (email: string, password: string) => void;
  onLinkExisting: () => void;
}

const PASSWORD_HINT = 'Minimum 8 characters, at least one uppercase letter and one number.';

function CreatePatientAccountForm({
  firstName,
  lastName,
  appointmentId: _appointmentId,
  isSubmitting,
  submitError,
  isDuplicateEmail,
  accountCreated,
  onSubmit,
  onLinkExisting,
}: CreatePatientAccountFormProps) {
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<CreatePatientAccountFormValues>({
    mode: 'onBlur',
    reValidateMode: 'onBlur',
  });

  const passwordValue = watch('password', '');
  const passwordStrength = [
    passwordValue.length >= 8,
    /[A-Z]/.test(passwordValue),
    /[0-9]/.test(passwordValue),
    /[^A-Za-z0-9]/.test(passwordValue),
  ].filter(Boolean).length;

  const strengthColors = [
    'transparent',
    'var(--color-danger)',
    'var(--color-warning)',
    'var(--color-success)',
    'var(--color-success)',
  ];

  const handleFormSubmit = (values: CreatePatientAccountFormValues) => {
    const result = evaluatePassword(values.password);
    if (!result.isValid) {
      setError('password', { type: 'manual', message: 'Password does not meet requirements.' });
      return;
    }
    onSubmit(values.email, values.password);
  };

  if (accountCreated) {
    return (
      <div className={styles.successBanner} role="status" aria-live="polite">
        Account created for {firstName} {lastName}. They can log in with their email.
      </div>
    );
  }

  return (
    <form
      className={styles.form}
      onSubmit={handleSubmit(handleFormSubmit)}
      noValidate
      aria-labelledby="create-account-heading"
    >
      <p id="create-account-heading" className={styles.description}>
        Create a portal account for <strong>{firstName} {lastName}</strong>. They can use it to manage future appointments.
      </p>

      {submitError && !isDuplicateEmail && (
        <div className={styles.errorBanner} role="alert" aria-live="assertive">
          {submitError}
        </div>
      )}

      {isDuplicateEmail && (
        <div className={styles.duplicateBanner} role="alert" aria-live="assertive">
          <span>Email already registered — link to existing account?</span>
          <button
            type="button"
            className={styles.linkBtn}
            onClick={onLinkExisting}
          >
            Link
          </button>
        </div>
      )}

      <div className={styles.fieldGroup}>
        <label htmlFor="create-email" className={styles.fieldLabel}>
          Email address <span className={styles.required} aria-hidden="true">*</span>
        </label>
        <input
          id="create-email"
          type="email"
          className={`${styles.input}${errors.email ? ` ${styles.inputError}` : ''}`}
          aria-required="true"
          aria-describedby={errors.email ? 'create-email-error' : undefined}
          autoComplete="off"
          {...register('email', {
            required: 'Email address is required.',
            pattern: {
              value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
              message: 'Please enter a valid email address.',
            },
          })}
        />
        {errors.email && (
          <span id="create-email-error" className={styles.fieldError} role="alert">
            {errors.email.message}
          </span>
        )}
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="create-password" className={styles.fieldLabel}>
          Password <span className={styles.required} aria-hidden="true">*</span>
        </label>
        <div className={styles.passwordWrapper}>
          <input
            id="create-password"
            type={isPasswordVisible ? 'text' : 'password'}
            className={`${styles.input}${errors.password ? ` ${styles.inputError}` : ''}`}
            aria-required="true"
            aria-describedby="create-password-hint create-password-strength"
            autoComplete="new-password"
            {...register('password', { required: 'Password is required.' })}
          />
          <button
            type="button"
            className={styles.revealBtn}
            aria-pressed={isPasswordVisible}
            aria-label={isPasswordVisible ? 'Hide password' : 'Show password'}
            onClick={() => setIsPasswordVisible((v) => !v)}
          >
            {isPasswordVisible ? 'Hide' : 'Show'}
          </button>
        </div>
        <div
          id="create-password-strength"
          className={styles.strengthBar}
          role="presentation"
          aria-hidden="true"
        >
          {[1, 2, 3, 4].map((segment) => (
            <div
              key={segment}
              className={styles.strengthSegment}
              style={{
                background: segment <= passwordStrength ? strengthColors[passwordStrength] : 'var(--color-border-default)',
              }}
            />
          ))}
        </div>
        <p id="create-password-hint" className={styles.fieldHint}>
          {PASSWORD_HINT}
        </p>
        {errors.password && (
          <span className={styles.fieldError} role="alert">
            {errors.password.message}
          </span>
        )}
      </div>

      <button
        type="submit"
        className={styles.submitBtn}
        disabled={isSubmitting}
        aria-busy={isSubmitting}
      >
        {isSubmitting ? 'Creating account…' : 'Create account'}
      </button>
    </form>
  );
}

export default CreatePatientAccountForm;
