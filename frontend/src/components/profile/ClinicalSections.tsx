/**
 * ClinicalSections.tsx — Shared container for all 360° profile clinical sections (US_027).
 *
 * Renders: Vitals, Medications, Conditions & Diagnoses, Visit History.
 *
 * mode='patient' — Patient read-only view (SCR-009): AI-extracted badges shown (AC-002, UXR-403)
 * mode='staff'   — Staff view (SCR-013): conflict section slot rendered; AI badges hidden inline
 *
 * Conflict section is a placeholder — populated by US_028.
 */
import { PatientProfileData } from '../../types/profile';
import { ClinicalSection } from './ClinicalSection';

interface ClinicalSectionsProps {
  data: PatientProfileData;
  mode: 'patient' | 'staff';
  isLoading?: boolean;
}

export function ClinicalSections({
  data,
  mode,
  isLoading = false,
}: ClinicalSectionsProps): JSX.Element {
  return (
    <div data-uxr="UXR-106" aria-label="Clinical data sections">
      {/* Conflicts slot — US_028 (Staff only) */}
      {mode === 'staff' && (
        <div
          id="conflicts-section"
          data-uxr="UXR-404"
          aria-label="Conflict alerts"
          style={{ marginBottom: 'var(--space-4)' }}
        >
          {/* Populated by US_028 ConflictAlerts component */}
        </div>
      )}

      <ClinicalSection
        sectionId="ps-vitals"
        title="Vitals"
        icon="❤"
        items={data.vitals}
        mode={mode}
        defaultExpanded
        isLoading={isLoading}
      />
      <ClinicalSection
        sectionId="ps-medications"
        title="Medications"
        icon="💊"
        items={data.medications}
        mode={mode}
        isLoading={isLoading}
      />
      <ClinicalSection
        sectionId="ps-diagnoses"
        title="Conditions &amp; Diagnoses"
        icon="🏥"
        items={data.diagnoses}
        mode={mode}
        isLoading={isLoading}
      />
      <ClinicalSection
        sectionId="ps-visit-history"
        title="Visit History"
        icon="📋"
        items={data.visitHistory}
        mode={mode}
        isLoading={isLoading}
      />
    </div>
  );
}
