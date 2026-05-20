import LoginForm from '../components/auth/LoginForm';
import styles from './LoginPage.module.css';
import { useLocation, useSearchParams } from 'react-router-dom';

interface LoginPageLocationState {
  registrationSuccess?: boolean;
}

function LoginPage() {
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const isSessionExpired = searchParams.get('reason') === 'expired';
  const isRegistrationSuccess = Boolean((location.state as LoginPageLocationState | null)?.registrationSuccess);

  return (
    <main className={styles.page}>
      <section className={styles.card} aria-labelledby="login-heading">
        <div className={styles.logo} aria-label="Unified Patient Access Platform">
          <div className={styles.logoMark} aria-hidden="true">
            P
          </div>
          <div>
            <div className={styles.logoText}>Propel Patient Portal</div>
            <div className={styles.logoSubtext}>Unified Patient Access &amp; Clinical Intelligence</div>
          </div>
        </div>

        <header className={styles.header}>
          <h1 id="login-heading" className={styles.title}>
            Sign in to your account
          </h1>
          <p className={styles.subtitle}>Welcome back. Enter your credentials to continue.</p>

          {isSessionExpired && (
            <p className={styles.sessionExpiredNotice} role="status" aria-live="polite">
              Session expired. Please log in again.
            </p>
          )}

          {isRegistrationSuccess && (
            <p className={styles.registrationSuccessNotice} role="status" aria-live="polite">
              Account created successfully. Please sign in.
            </p>
          )}
        </header>

        <LoginForm />
      </section>
    </main>
  );
}

export default LoginPage;
