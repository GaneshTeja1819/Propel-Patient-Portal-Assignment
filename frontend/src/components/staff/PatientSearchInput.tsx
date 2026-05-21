import { useCallback, useEffect, useRef, useState } from 'react';
import styles from './PatientSearchInput.module.css';

export interface PatientSearchResult {
  id: string;
  name: string;
  dateOfBirth: string;
  sex?: string;
  phone?: string;
}

interface PatientSearchInputProps {
  onPatientSelect: (patient: PatientSearchResult) => void;
  onNotFound: () => void;
}

const DEBOUNCE_MS = 300;
const MIN_QUERY_LENGTH = 2;

async function fetchPatients(query: string): Promise<PatientSearchResult[]> {
  const response = await fetch(
    `/api/v1/patients/search?q=${encodeURIComponent(query)}`,
    { credentials: 'include' },
  );

  if (!response.ok) {
    return [];
  }

  return (await response.json()) as PatientSearchResult[];
}

function PatientSearchInput({ onPatientSelect, onNotFound }: PatientSearchInputProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<PatientSearchResult[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [selected, setSelected] = useState<PatientSearchResult | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const search = useCallback(async (value: string) => {
    if (value.length < MIN_QUERY_LENGTH) {
      setResults([]);
      setIsOpen(false);
      return;
    }

    setIsLoading(true);
    try {
      const found = await fetchPatients(value);
      setResults(found);
      setIsOpen(true);
    } catch {
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setQuery(value);
    setSelected(null);

    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    debounceRef.current = setTimeout(() => {
      void search(value);
    }, DEBOUNCE_MS);
  };

  const handleSelect = (patient: PatientSearchResult) => {
    setSelected(patient);
    setQuery(patient.name);
    setIsOpen(false);
    onPatientSelect(patient);
  };

  const handleClear = () => {
    setSelected(null);
    setQuery('');
    setResults([]);
    setIsOpen(false);
  };

  const handleNotFound = () => {
    setIsOpen(false);
    onNotFound();
  };

  // Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Cleanup debounce on unmount
  useEffect(() => {
    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, []);

  return (
    <div className={styles.container} ref={containerRef}>
      <div className={styles.fieldGroup}>
        <label htmlFor="patient-search" className={styles.fieldLabel}>
          Name or date of birth <span className={styles.required} aria-hidden="true">*</span>
        </label>
        <input
          id="patient-search"
          type="text"
          className={styles.input}
          value={query}
          onChange={handleInputChange}
          placeholder="e.g. Michael Chen or 07/08/1978"
          aria-required="true"
          aria-label="Search patient by name or date of birth"
          aria-expanded={isOpen}
          aria-haspopup="listbox"
          aria-autocomplete="list"
          autoComplete="off"
          disabled={selected !== null}
        />
      </div>

      {isLoading && (
        <p className={styles.loadingHint} aria-live="polite">Searching…</p>
      )}

      {isOpen && !isLoading && (
        <ul
          className={styles.dropdown}
          role="listbox"
          aria-label="Patient search results"
          id="patient-search-results"
        >
          {results.map((patient) => (
            <li key={patient.id} role="option" aria-selected="false">
              <button
                type="button"
                className={styles.resultRow}
                onClick={() => handleSelect(patient)}
              >
                <span className={styles.resultName}>{patient.name}</span>
                <span className={styles.resultMeta}>
                  DOB: {patient.dateOfBirth}
                  {patient.sex ? ` · ${patient.sex}` : ''}
                  {patient.phone ? ` · ${patient.phone}` : ''}
                </span>
                <span className={styles.resultCta} aria-hidden="true">Select ›</span>
              </button>
            </li>
          ))}
          <li role="option" aria-selected="false">
            <button
              type="button"
              className={styles.notFoundRow}
              onClick={handleNotFound}
            >
              Patient not found — proceed as anonymous walk-in
            </button>
          </li>
        </ul>
      )}

      {selected && (
        <div className={styles.selectedBanner} role="status" aria-live="polite">
          <span>
            <strong>{selected.name}</strong> — DOB: {selected.dateOfBirth}
            {selected.sex ? ` · ${selected.sex}` : ''}
          </span>
          <button
            type="button"
            className={styles.clearBtn}
            onClick={handleClear}
            aria-label="Clear selected patient"
          >
            ✕ Clear
          </button>
        </div>
      )}
    </div>
  );
}

export default PatientSearchInput;
