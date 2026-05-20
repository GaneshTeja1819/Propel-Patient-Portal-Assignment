import RegistrationForm from '../components/auth/RegistrationForm';
import styles from './RegistrationPage.module.css';

function RegistrationPage() {
  return (
    <main className={styles.page}>
      <section className={styles.card} aria-labelledby="registration-heading">
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
          <h1 id="registration-heading" className={styles.title}>
            Create your account
          </h1>
          <p className={styles.subtitle}>
            Register to book appointments and manage your health.
          </p>
        </header>

        <RegistrationForm />
      </section>
    </main>
  );
}

export default RegistrationPage;
