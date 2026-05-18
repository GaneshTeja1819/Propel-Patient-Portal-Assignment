import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { useLogin } from '../../hooks/useLogin';
import styles from './LoginForm.module.css';

interface LoginFormValues {
  email: string;
  password: string;
}

const LOGIN_FAILURE_ALERT_COPY = '⚠ Your email or password is incorrect. Please try again.';

function LoginForm() {
  const { loginWithRedirect, cookiesRequired } = useLogin();
  const [submitMessage, setSubmitMessage] = useState<string | null>(null);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    mode: 'onBlur',
    reValidateMode: 'onBlur',
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

  const onSubmit = async (values: LoginFormValues) => {
    setSubmitMessage(null);

    const result = await loginWithRedirect(values);
    if (!result.ok) {
      setSubmitMessage(LOGIN_FAILURE_ALERT_COPY);
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
    <form className={styles.form} onSubmit={handleSubmit(onSubmit)} noValidate>
      {cookiesRequired && (
        <p className={styles.cookiesBanner} role="alert">
          Cookies required
        </p>
      )}

      {submitMessage && (
        <div className={styles.formAlert} role="alert" aria-live="assertive">
          <span>{submitMessage}</span>
        </div>
      )}

      <div className={styles.fieldGroup}>
        <label htmlFor="login-email" className={styles.fieldLabel}>
          Email address
        </label>
        <input
          id="login-email"
          type="email"
          autoComplete="email"
          placeholder="e.g. sarah.johnson@email.com"
          className={styles.fieldInput}
          aria-required="true"
          aria-invalid={Boolean(errors.email)}
          aria-describedby="login-email-error"
          {...emailRegistration}
        />
        <p id="login-email-error" className={styles.fieldError} role={errors.email ? 'alert' : undefined}>
          {renderFieldError(errors.email?.message)}
        </p>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="login-password" className={styles.fieldLabel}>
          Password
        </label>
        <div className={styles.passwordWrapper}>
          <input
            id="login-password"
            type={isPasswordVisible ? 'text' : 'password'}
            autoComplete="current-password"
            placeholder="Enter your password"
            className={styles.fieldInput}
            aria-required="true"
            aria-invalid={Boolean(errors.password)}
            aria-describedby="login-password-error"
            {...passwordRegistration}
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
        <p
          id="login-password-error"
          className={styles.fieldError}
          role={errors.password ? 'alert' : undefined}
        >
          {renderFieldError(errors.password?.message)}
        </p>
      </div>

      <div className={styles.checkboxRow}>
        <input type="checkbox" id="remember-me" name="remember-me" aria-label="Keep me signed in for 30 days" />
        <label htmlFor="remember-me" className={styles.checkboxLabel}>
          Keep me signed in
        </label>
      </div>

      <button type="submit" className={styles.submitButton} disabled={isSubmitting}>
        {isSubmitting ? 'Signing in...' : 'Sign in'}
      </button>

      <div className={styles.divider}>or</div>

      <Link to="/register" className={styles.secondaryActionButton}>
        Create an account
      </Link>

      <div className={styles.demoShortcuts} aria-label="Wireframe demo navigation">
        <div className={styles.demoLabel}>Wireframe shortcuts</div>
        <div className={styles.demoRow}>
          <Link to="/dashboard" className={styles.demoButton} aria-label="Demo: Login as patient Sarah Johnson">
            Patient (Sarah J.)
          </Link>
          <Link to="/staff/queue" className={styles.demoButton} aria-label="Demo: Login as staff">
            Staff (Alex T.)
          </Link>
          <Link to="/admin/users" className={styles.demoButton} aria-label="Demo: Login as admin">
            Admin (Jennifer P.)
          </Link>
        </div>
      </div>

      <div className={styles.authFooter}>
        <p>
          Don't have an account?{' '}
          <Link to="/register" className={styles.inlineLink}>
            Create one here
          </Link>
        </p>
        <p>
          <a href="#" aria-label="Reset your password" className={styles.inlineLink}>
            Forgot your password?
          </a>
        </p>
      </div>
    </form>
  );
}

export default LoginForm;
