import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import styles from './AnonymousPatientForm.module.css';

export interface AnonymousPatientData {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  sex?: string;
  phone?: string;
}

interface AnonymousPatientFormProps {
  onChange: (data: AnonymousPatientData) => void;
}

function AnonymousPatientForm({ onChange }: AnonymousPatientFormProps) {
  const {
    register,
    watch,
    formState: { errors },
  } = useForm<AnonymousPatientData>({
    mode: 'onBlur',
    reValidateMode: 'onBlur',
    defaultValues: { sex: '' },
  });

  const formValues = watch();

  useEffect(() => {
    if (formValues.firstName && formValues.lastName && formValues.dateOfBirth) {
      onChange(formValues);
    }
  }, [formValues, onChange]);

  return (
    <div className={styles.form} role="group" aria-labelledby="anon-patient-heading">
      <h2 id="anon-patient-heading" className={styles.heading}>
        New patient details
      </h2>

      <div className={styles.fieldRow}>
        <div className={styles.fieldGroup}>
          <label htmlFor="anon-first" className={styles.fieldLabel}>
            First name <span className={styles.required} aria-hidden="true">*</span>
          </label>
          <input
            id="anon-first"
            type="text"
            className={`${styles.input}${errors.firstName ? ` ${styles.inputError}` : ''}`}
            placeholder="First name"
            aria-required="true"
            aria-describedby={errors.firstName ? 'anon-first-error' : undefined}
            {...register('firstName', { required: 'First name is required.' })}
          />
          {errors.firstName && (
            <span id="anon-first-error" className={styles.fieldError} role="alert">
              {errors.firstName.message}
            </span>
          )}
        </div>

        <div className={styles.fieldGroup}>
          <label htmlFor="anon-last" className={styles.fieldLabel}>
            Last name <span className={styles.required} aria-hidden="true">*</span>
          </label>
          <input
            id="anon-last"
            type="text"
            className={`${styles.input}${errors.lastName ? ` ${styles.inputError}` : ''}`}
            placeholder="Last name"
            aria-required="true"
            aria-describedby={errors.lastName ? 'anon-last-error' : undefined}
            {...register('lastName', { required: 'Last name is required.' })}
          />
          {errors.lastName && (
            <span id="anon-last-error" className={styles.fieldError} role="alert">
              {errors.lastName.message}
            </span>
          )}
        </div>
      </div>

      <div className={styles.fieldRow}>
        <div className={styles.fieldGroup}>
          <label htmlFor="anon-dob" className={styles.fieldLabel}>
            Date of birth <span className={styles.required} aria-hidden="true">*</span>
          </label>
          <input
            id="anon-dob"
            type="date"
            className={`${styles.input}${errors.dateOfBirth ? ` ${styles.inputError}` : ''}`}
            aria-required="true"
            aria-describedby={errors.dateOfBirth ? 'anon-dob-error' : undefined}
            {...register('dateOfBirth', { required: 'Date of birth is required.' })}
          />
          {errors.dateOfBirth && (
            <span id="anon-dob-error" className={styles.fieldError} role="alert">
              {errors.dateOfBirth.message}
            </span>
          )}
        </div>

        <div className={styles.fieldGroup}>
          <label htmlFor="anon-sex" className={styles.fieldLabel}>
            Sex assigned at birth
          </label>
          <select
            id="anon-sex"
            className={styles.input}
            {...register('sex')}
          >
            <option value="">— select —</option>
            <option value="Female">Female</option>
            <option value="Male">Male</option>
            <option value="Intersex">Intersex</option>
            <option value="Prefer not to say">Prefer not to say</option>
          </select>
        </div>
      </div>

      <div className={styles.fieldGroup}>
        <label htmlFor="anon-phone" className={styles.fieldLabel}>
          Phone
        </label>
        <input
          id="anon-phone"
          type="tel"
          className={styles.input}
          placeholder="(555) 000-0000"
          autoComplete="off"
          {...register('phone')}
        />
      </div>
    </div>
  );
}

export default AnonymousPatientForm;
