/**
 * ConflictsSection.tsx — Conflict alert region for the Staff Patient Profile (US_028, UXR-404).
 *
 * AC-004: Renders nothing (null) when there are no conflicts — the section is invisible to staff
 *         if the patient has a clean record.
 *
 * Behaviour:
 *  - Calls useConflicts(patientId) internally — data ownership stays in this tree
 *  - Renders a <ConflictRow> for every DataConflictDto returned
 *  - Loading: silent (no spinner) to avoid layout shift; errors surfaced via alert banner
 *  - Forbidden (403): renders nothing — staff without access should not see a red banner here
 *
 * Accessibility: wraps all cards in a landmark region labelled "Data conflicts requiring review"
 *   matching the wireframe aria-label (SCR-013, UXR-404).
 */
import { useConflicts } from '../../hooks/useConflicts';
import type { ResolveConflictPayload } from '../../types/conflict';
import { ConflictRow } from './ConflictRow';

interface ConflictsSectionProps {
  patientId: string;
  onAfterResolve?: () => void;
}

export function ConflictsSection({ patientId, onAfterResolve }: ConflictsSectionProps): JSX.Element | null {
  const { conflicts, loadState, errorMessage, conflictErrors, resolveConflict, markReviewed } =
    useConflicts(patientId);

  async function handleResolve(conflictId: string, payload: ResolveConflictPayload): Promise<void> {
    await resolveConflict(conflictId, payload);
    onAfterResolve?.();
  }

  async function handleMarkReviewed(conflictId: string): Promise<void> {
    await markReviewed(conflictId);
    onAfterResolve?.();
  }

  // AC-004: render nothing when no conflicts exist
  if (loadState === 'success' && conflicts.length === 0) return null;

  // Silent on idle/loading — avoids layout shift before data arrives
  if (loadState === 'idle' || loadState === 'loading') return null;

  // Forbidden — do not surface an error for access-denied; fail silently
  if (loadState === 'forbidden') return null;

  if (loadState === 'error') {
    return (
      <div
        role="alert"
        style={{
          padding: 'var(--space-3)',
          marginBottom: 'var(--space-4)',
          background: 'var(--color-danger-bg)',
          border: '1px solid var(--color-danger)',
          borderRadius: '6px',
          fontSize: 'var(--font-size-body-sm)',
          color: 'var(--color-danger-text)',
        }}
      >
        {errorMessage ?? 'Unable to load conflict data.'}
      </div>
    );
  }

  return (
    <section
      role="region"
      aria-label="Data conflicts requiring review"
      data-uxr="UXR-404"
      style={{ marginBottom: 'var(--space-4)' }}
    >
      {conflicts.map((conflict) => (
        <ConflictRow
          key={conflict.id}
          conflict={conflict}
          inlineError={conflictErrors[conflict.id]}
          onResolve={handleResolve}
          onMarkReviewed={handleMarkReviewed}
        />
      ))}
    </section>
  );
}
