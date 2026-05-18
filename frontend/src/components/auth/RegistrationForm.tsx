import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useRegistration } from '../../hooks/useRegistration';
import { evaluatePassword } from '../../utils/passwordValidator';
import styles from './RegistrationForm.module.css';

interface RegistrationFormValues {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phone: string;
}

function RegistrationForm() {
  const navigate = useNavigate();
  const { isSubmitting, submissionError, registerUser } = useRegistration();
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false);
  const [showSuccessState, setShowSuccessState] = useState(false);
  const redirectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const {
    register,
    watch,
    handleSubmit,
    setError,
    clearErrors,
    setFocus,
    formState: { errors },
  } = useForm<RegistrationFormValues>({
    mode: 'onBlur',
    reValidateMode: 'onBlur',
  });

  const passwordValue = watch('password', '');
  const passwordErrorMessage = 'Password does not meet requirements.';
  const passwordHint = 'Minimum 8 characters, at least one uppercase letter and one number.';
  const passwordStrength = [
    passwordValue.length >= 8,
    /[A-Z]/.test(passwordValue),
    /[0-9]/.test(passwordValue),
    /[^A-Za-z0-9]/.test(passwordValue),
  ].filter(Boolean).length;
  const passwordStrengthColors = [
    'transparent',
    'var(--color-danger)',
    'var(--color-warning)',
    'var(--color-success)',
    'var(--color-success)',
  ];

  const firstNameRegistration = register('firstName', {
    required: 'First name is required.',
  });
  const lastNameRegistration = register('lastName', {
    required: 'Last name is required.',
  });
  const emailRegistration = register('email', {
    required: 'Email address is required.',
    pattern: {
      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
      message: 'Please enter a valid email address.',
    },
  });
  const passwordRegistration = register('password', {
    required: 'Password is required.',
  });
  const dateOfBirthRegistration = register('dateOfBirth', {
    required: 'Date of birth is required.',
  });
  const phoneRegistration = register('phone');
  const confirmPasswordRegistration = register('confirmPassword', {
    required: 'Confirm password is required.',
    validate: (value) => value === passwordValue || 'Passwords must match.',
  });

  useEffect(() => {
    return () => {
      if (redirectTimeoutRef.current) {
        clearTimeout(redirectTimeoutRef.current);
      }
    };
  }, []);

  const onSubmit = async (values: RegistrationFormValues) => {
    const passwordResult = evaluatePassword(values.password);

    if (!passwordResult.isValid) {
      setError('password', {
        type: 'manual',
        message: passwordErrorMessage,
      });
      setFocus('password');
      return;
    }

    clearErrors('password');

    const wasSuccessful = await registerUser({
      email: values.email,
      password: values.password,
      firstName: values.firstName,
      lastName: values.lastName,
    });

    if (!wasSuccessful) {
      return;
    }

    setShowSuccessState(true);
    redirectTimeoutRef.current = setTimeout(() => {
      navigate('/login', {
        replace: true,
        state: {
          registrationSuccess: true,
        },
      });
    }, 1200);
  };

  const onInvalid = () => {
    if (errors.password) {
      setFocus('password');
    }
  };

  const renderFieldError = (message?: string) => {
    if (!message) {
      return null;
    }

    return (
      <>
        <span className={`${styles.fieldErrorIcon} ${styles.fieldErrorIconTriangle}`} aria-hidden="true">
          !
        </span>
        <span>{message}</span>
      </>
    );
  };

  return (
    <form className={styles.form} onSubmit={handleSubmit(onSubmit, onInvalid)} noValidate>
      {showSuccessState && (
        <div className={styles.formSuccess} role="status" aria-live="polite">
          <span className={styles.alertIcon} aria-hidden="true">
            ✓
          </span>
          <span>Account created successfully. Redirecting to login...</span>
        </div>
      )}

      {submissionError && (
        <div className={styles.formAlert} role="alert" aria-live="assertive">
          <span className={styles.alertIcon} aria-hidden="true">
            !
          </span>
          <span>{submissionError}</span>
        </div>
      )}

      <div className={styles.nameRow}>
        <div className={styles.fieldGroup}>
          <label htmlFor="firstName" className={styles.fieldLabel}>
            First name
          </label>
          <input
            id="firstName"
            type="text"
            className={styles.fieldInput}
            autoComplete="given-name"
            placeholder="Sarah"
            aria-required="true"
            aria-invalid={Boolean(errors.firstName)}
            aria-describedby="firstName-error"
            {...firstNameRegistration}
          />
          <p id="firstName-error" className={styles.fieldError} role={errors.firstName ? 'alert' : undefined}>
            {renderFieldError(errors.firstName?.message)}
          </p>
        </div>

        <div className={styles.fieldGroup}>
          <label htmlFor="lastName" className={styles.fieldLabel}>
            Last name
          </label>
          <input
            id="lastName"
            type="text"
            className={styles.fieldInput}
            autoComplete="family-name"
            placeholder="Johnson"
            aria-required="true"
            aria-invalid={Boolean(errors.lastName)}
            aria-describedby="lastName-error"
            {...lastNameRegistration}
          />
          <p id="lastName-error" className={styles.fieldError} role={errors.lastName ? 'alert' : undefined}>
            {renderFieldError(errors.lastName?.message)}
          </p>
        </div>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="email" className={styles.fieldLabel}>
          Email address
        </label>
        <input
          id="email"
          type="email"
          className={styles.fieldInput}
          autoComplete="email"
          placeholder="e.g. sarah.johnson@email.com"
          aria-required="true"
          aria-invalid={Boolean(errors.email)}
          aria-describedby="email-error"
          {...emailRegistration}
        />
        <p id="email-error" className={styles.fieldError} role={errors.email ? 'alert' : undefined}>
          {renderFieldError(errors.email?.message)}
        </p>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="dateOfBirth" className={styles.fieldLabel}>
          Date of birth
        </label>
        <input
          id="dateOfBirth"
          type="date"
          className={styles.fieldInput}
          autoComplete="bday"
          aria-required="true"
          aria-invalid={Boolean(errors.dateOfBirth)}
          aria-describedby="dateOfBirth-error"
          {...dateOfBirthRegistration}
        />
        <p
          id="dateOfBirth-error"
          className={styles.fieldError}
          role={errors.dateOfBirth ? 'alert' : undefined}
        >
          {renderFieldError(errors.dateOfBirth?.message)}
        </p>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="phone" className={styles.fieldLabel}>
          Phone number
        </label>
        <input
          id="phone"
          type="tel"
          className={styles.fieldInput}
          autoComplete="tel"
          placeholder="(555) 214-8832"
          aria-invalid={Boolean(errors.phone)}
          aria-describedby="phone-hint"
          {...phoneRegistration}
        />
        <p id="phone-hint" className={styles.fieldHint}>
          Optional - used for appointment reminders
        </p>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="password" className={styles.fieldLabel}>
          Password
        </label>
        <div className={styles.passwordWrapper}>
          <input
            id="password"
            type={isPasswordVisible ? 'text' : 'password'}
            className={styles.fieldInput}
            autoComplete="new-password"
            placeholder="Create a strong password"
            aria-required="true"
            aria-invalid={Boolean(errors.password)}
            aria-describedby="password-hint password-error"
            {...passwordRegistration}
            onBlur={(event) => {
              passwordRegistration.onBlur(event);

              const result = evaluatePassword(event.target.value);
              if (!result.isValid) {
                setError('password', {
                  type: 'manual',
                  message: passwordErrorMessage,
                });
                return;
              }

              clearErrors('password');
            }}
          />
          <button
            type="button"
            className={styles.passwordToggle}
            onClick={() => setIsPasswordVisible((currentValue) => !currentValue)}
            aria-label={isPasswordVisible ? 'Hide password' : 'Show password'}
            aria-pressed={isPasswordVisible}
          >
            {isPasswordVisible ? '🙈' : '👁'}
          </button>
        </div>

        <div className={styles.passwordStrength} data-testid="password-strength-track" aria-hidden="true">
          <div
            className={styles.passwordStrengthBar}
            style={{
              width: `${passwordStrength * 25}%`,
              backgroundColor: passwordStrengthColors[passwordStrength],
            }}
          />
        </div>

        <p id="password-hint" className={styles.fieldHint}>
          {passwordHint}
        </p>

        <p id="password-error" className={styles.fieldError} role={errors.password ? 'alert' : undefined}>
          {renderFieldError(errors.password?.message)}
        </p>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="confirmPassword" className={styles.fieldLabel}>
          Confirm password
        </label>
        <div className={styles.passwordWrapper}>
          <input
            id="confirmPassword"
            type={isConfirmPasswordVisible ? 'text' : 'password'}
            className={styles.fieldInput}
            autoComplete="new-password"
            placeholder="Re-enter your password"
            aria-required="true"
            aria-invalid={Boolean(errors.confirmPassword)}
            aria-describedby="confirmPassword-error"
            {...confirmPasswordRegistration}
          />
          <button
            type="button"
            className={styles.passwordToggle}
            onClick={() => setIsConfirmPasswordVisible((currentValue) => !currentValue)}
            aria-label={isConfirmPasswordVisible ? 'Hide confirm password' : 'Show confirm password'}
            aria-pressed={isConfirmPasswordVisible}
          >
            {isConfirmPasswordVisible ? '🙈' : '👁'}
          </button>
        </div>
        <p
          id="confirmPassword-error"
          className={styles.fieldError}
          role={errors.confirmPassword ? 'alert' : undefined}
        >
          {renderFieldError(errors.confirmPassword?.message)}
        </p>
      </div>

      <button type="submit" className={styles.submitButton} disabled={isSubmitting}>
        {isSubmitting ? 'Creating account...' : 'Create account'}
      </button>

      <p className={styles.termsNote}>
        By creating an account, you agree to our{' '}
        <a href="#" className={styles.inlineLink}>
          Terms of Service
        </a>{' '}
        and{' '}
        <a href="#" className={styles.inlineLink}>
          Privacy Policy
        </a>
      </p>

      <p className={styles.hipaaNote}>Your health data is protected under HIPAA.</p>

      <div className={styles.divider}>already have an account?</div>

      <Link to="/login" className={styles.secondaryActionLink}>
        Sign in instead
      </Link>
    </form>
  );
}

export default RegistrationForm;
