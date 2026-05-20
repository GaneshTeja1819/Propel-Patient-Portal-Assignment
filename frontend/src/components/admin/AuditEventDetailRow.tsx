import type { AuditEvent } from '../../hooks/useAuditLog';
import styles from './AuditEventDetailRow.module.css';

interface Props {
  event: AuditEvent;
  colSpan: number;
}

export function AuditEventDetailRow({ event, colSpan }: Props) {
  return (
    <tr role="region" aria-label={`Event detail for ${event.id}`}>
      <td colSpan={colSpan} className={styles.cell}>
        <div className={styles.grid}>
          {/* Column A: Event Identity */}
          <section>
            <h3 className={styles.sectionTitle}>Event Identity</h3>
            <Field label="Event ID" value={event.id} />
            {event.correlationId && <Field label="Correlation ID" value={event.correlationId} />}
            {event.sessionId && <Field label="Session ID" value={event.sessionId} />}
            <Field label="Timestamp (ISO 8601)" value={event.timestamp} />
          </section>

          {/* Column B: Actor */}
          <section>
            <h3 className={styles.sectionTitle}>Actor</h3>
            <Field label="User ID" value={event.actorId} />
            {event.actorName && <Field label="Name" value={event.actorName} />}
            <Field label="Role" value={event.actorRole} />
            <Field
              label="IP / User Agent"
              value={
                event.userAgent
                  ? `${event.ipAddress} · ${event.userAgent}`
                  : event.ipAddress
              }
            />
          </section>

          {/* Column C: Payload */}
          <section>
            <h3 className={styles.sectionTitle}>Payload</h3>
            <Field label="Resource" value={event.resource} phiMarker={event.phiAccess} />
            {event.metadata && <MonoField label="Parameters" value={prettyJson(event.metadata)} />}
            <div className={styles.immutableNote} aria-label="Immutable record notice">
              🔒 This record is immutable and cannot be modified or deleted.
            </div>
          </section>
        </div>
      </td>
    </tr>
  );
}

// ── Helpers ─────────────────────────────────────────────────────────────────

function prettyJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function Field({
  label,
  value,
  phiMarker,
}: {
  label: string;
  value: string;
  phiMarker?: boolean;
}) {
  return (
    <div className={styles.field}>
      <div className={styles.fieldLabel}>{label}</div>
      <div className={styles.fieldValue}>
        {phiMarker && <span className={styles.phiIndicator}>🔒 PHI</span>}
        {value}
      </div>
    </div>
  );
}

function MonoField({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.field}>
      <div className={styles.fieldLabel}>{label}</div>
      <pre className={styles.fieldValueMono}>{value}</pre>
    </div>
  );
}
