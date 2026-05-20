/**
 * CodeVerificationTable.tsx — Suggestion table for SCR-014 (US_030, AC-001–AC-005).
 *
 * Renders code suggestion rows in rank order. Handles three states:
 *   - Normal: one CodeVerificationRow per suggestion
 *   - Empty (US_029 failure): "No codes suggested" notice + Regenerate CTA
 *   - All rejected (AC-005): amber banner above the rows
 */
import { useCallback } from 'react';
import { CodeVerificationRow } from './CodeVerificationRow';
import type { MedicalCodeSuggestionDto, VerifyDecision, CodeValidateResponse } from '../../types/coding';

interface CodeVerificationTableProps {
  encounterId: string;
  suggestions: MedicalCodeSuggestionDto[];
  verifiedRows: Record<string, VerifyDecision>;
  rowErrors: Record<string, string>;
  allRejected: boolean;
  finalized: boolean;
  onVerify: (suggestionId: string, decision: VerifyDecision, verifiedCode?: string) => Promise<void>;
  onValidateCode: (code: string, codeType: string) => Promise<CodeValidateResponse>;
}

export function CodeVerificationTable({
  encounterId,
  suggestions,
  verifiedRows,
  rowErrors,
  allRejected,
  finalized,
  onVerify,
  onValidateCode,
}: CodeVerificationTableProps): JSX.Element {
  // Regenerate fires POST /api/v1/code-suggestions/generate (US_029 CTA)
  const handleRegenerate = useCallback(async () => {
    try {
      await fetch('/api/v1/code-suggestions/generate', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ encounterId }),
      });
      // Page reload lets the hook re-fetch the new suggestions.
      window.location.reload();
    } catch {
      // Non-critical; user can retry.
    }
  }, [encounterId]);

  // ── Empty state (US_029 failure) ───────────────────────────────────────────
  if (suggestions.length === 0) {
    return (
      <div
        style={{
          background: 'var(--color-surface-default)',
          border: '1px solid var(--color-border-default)',
          borderRadius: 'var(--radius-md)',
          padding: 'var(--space-8)',
          textAlign: 'center',
        }}
        role="status"
        aria-label="No code suggestions available"
      >
        <p style={{ fontSize: 'var(--font-size-body-md)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-4)' }}>
          No codes suggested — manual coding required.
        </p>
        <button
          onClick={handleRegenerate}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 'var(--space-2)',
            padding: '0 var(--space-6)',
            height: 'var(--touch-target-min)',
            borderRadius: 'var(--radius-sm)',
            fontFamily: 'var(--font-family-default)',
            fontSize: 'var(--font-size-body-md)',
            fontWeight: 600,
            border: '1.5px solid var(--color-brand-primary)',
            background: 'var(--color-brand-primary)',
            color: 'var(--color-text-on-primary)',
            cursor: 'pointer',
          }}
          aria-label="Regenerate AI code suggestions"
        >
          ↺ Regenerate suggestions
        </button>
      </div>
    );
  }

  // Suggestions are already sorted by rank from the API (GET endpoint orders by Rank).
  // Defensive client-sort ensures correct order even if API changes.
  const sorted = [...suggestions].sort((a, b) => a.rank - b.rank);

  return (
    <div role="list" aria-label="Medical code suggestions">
      {/* All-rejected banner (AC-005) */}
      {allRejected && (
        <div
          style={{
            background: 'var(--color-warning-bg)',
            border: '1px solid var(--color-warning)',
            borderRadius: 'var(--radius-md)',
            padding: 'var(--space-4) var(--space-6)',
            marginBottom: 'var(--space-4)',
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
          }}
          role="alert"
          aria-live="polite"
          data-uxr="UXR-107"
        >
          <span aria-hidden="true" style={{ fontSize: '20px' }}>⚠</span>
          <p style={{ fontSize: 'var(--font-size-body-md)', fontWeight: 600, color: 'var(--color-warning-text)' }}>
            All suggestions rejected — manual coding required. Encounter flagged; no billing will occur.
          </p>
        </div>
      )}

      {sorted.map(suggestion => (
        <div key={suggestion.id} role="listitem">
          <CodeVerificationRow
            suggestion={suggestion}
            decision={verifiedRows[suggestion.id]}
            rowError={rowErrors[suggestion.id]}
            finalized={finalized}
            onVerify={onVerify}
            onValidateCode={onValidateCode}
          />
        </div>
      ))}
    </div>
  );
}
