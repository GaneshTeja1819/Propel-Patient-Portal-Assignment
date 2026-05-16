import styles from './BaselineDemo.module.css';

/**
 * BaselineDemo — Token system showcase and axe-core scan target.
 *
 * Purpose: Exercises every design token category so the CI accessibility
 * audit (axe-core) has a rendered page to scan. All styling is done via
 * CSS Modules that reference tokens from variables.css — zero raw values.
 *
 * AC coverage: AC-002 (tokens), AC-003 (contrast), AC-004 (focus/keyboard),
 *              AC-005 (touch targets, breakpoints).
 */
function BaselineDemo() {
  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.pageTitle}>Patient Portal — Design System Baseline</h1>
        <p className={styles.pageSubtitle}>
          Token system showcase for WCAG 2.2 AA compliance validation.
        </p>
      </header>

      <section aria-labelledby="colours-heading" className={styles.section}>
        <h2 id="colours-heading" className={styles.sectionHeading}>
          Colour Tokens
        </h2>

        <div className={styles.swatchGrid} role="list" aria-label="Colour token swatches">
          <SwatchItem label="Brand Primary" cssVar="--color-brand-primary" role="listitem" />
          <SwatchItem
            label="Brand Primary Light"
            cssVar="--color-brand-primary-light"
            role="listitem"
            dark
          />
          <SwatchItem
            label="Surface Muted"
            cssVar="--color-surface-muted"
            role="listitem"
            dark
          />
          <SwatchItem
            label="Surface Subtle"
            cssVar="--color-surface-subtle"
            role="listitem"
            dark
          />
          <SwatchItem label="Success" cssVar="--color-success" role="listitem" />
          <SwatchItem label="Warning" cssVar="--color-warning" role="listitem" />
          <SwatchItem label="Danger" cssVar="--color-danger" role="listitem" />
          <SwatchItem label="AI Accent" cssVar="--color-ai-accent" role="listitem" />
        </div>
      </section>

      <section aria-labelledby="typography-heading" className={styles.section}>
        <h2 id="typography-heading" className={styles.sectionHeading}>
          Typography Scale
        </h2>
        <div className={styles.typeStack}>
          <p className={styles.typeHeadingXl}>Heading XL — 24 px / 600</p>
          <p className={styles.typeHeadingLg}>Heading LG — 20 px / 600</p>
          <p className={styles.typeHeadingMd}>Heading MD — 18 px / 600</p>
          <p className={styles.typeHeadingSm}>Heading SM — 16 px / 600</p>
          <p className={styles.typeBodyMd}>Body MD — 14 px / 400 — Normal body text</p>
          <p className={styles.typeBodySm}>Body SM — 13 px / 400 — Secondary text</p>
          <p className={styles.typeCaption}>Caption — 12 px / 400</p>
          <p className={styles.typeLabel}>Label — 12 px / 600 / Uppercase</p>
        </div>
      </section>

      <section aria-labelledby="interactive-heading" className={styles.section}>
        <h2 id="interactive-heading" className={styles.sectionHeading}>
          Interactive Elements
        </h2>
        <div className={styles.interactiveRow}>
          <button type="button" className={styles.btnPrimary}>
            Primary Action
          </button>
          <button type="button" className={styles.btnSecondary}>
            Secondary Action
          </button>
          <button type="button" className={styles.btnDanger}>
            Danger Action
          </button>
          <button type="button" className={styles.btnPrimary} disabled aria-disabled="true">
            Disabled
          </button>
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="demo-input" className={styles.formLabel}>
            Text Input
          </label>
          <input
            id="demo-input"
            type="text"
            className={styles.formInput}
            placeholder="Enter a value"
            aria-describedby="demo-input-hint"
          />
          <p id="demo-input-hint" className={styles.formHint}>
            Example hint text for screen readers.
          </p>
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="demo-select" className={styles.formLabel}>
            Select
          </label>
          <select id="demo-select" className={styles.formInput}>
            <option value="">Choose an option</option>
            <option value="a">Option A</option>
            <option value="b">Option B</option>
          </select>
        </div>

        <div className={styles.linkRow}>
          <a href="#interactive-heading">Internal anchor link</a>
          <a href="https://example.com" target="_blank" rel="noreferrer noopener">
            External link
            <span className={styles.srOnly}>(opens in new tab)</span>
          </a>
        </div>
      </section>

      <section aria-labelledby="spacing-heading" className={styles.section}>
        <h2 id="spacing-heading" className={styles.sectionHeading}>
          Spacing &amp; Radius Tokens
        </h2>
        <div className={styles.spacingGrid} role="list" aria-label="Spacing token samples">
          {(['--space-1', '--space-2', '--space-3', '--space-4', '--space-6', '--space-8'] as const).map(
            (token) => (
              <div key={token} className={styles.spacingSample} role="listitem">
                <div
                  className={styles.spacingBlock}
                  style={{ width: `var(${token})`, height: `var(${token})` }}
                  aria-hidden="true"
                />
                <span className={styles.typeCaption}>{token}</span>
              </div>
            )
          )}
        </div>
      </section>

      <section aria-labelledby="elevation-heading" className={styles.section}>
        <h2 id="elevation-heading" className={styles.sectionHeading}>
          Elevation / Shadow Tokens
        </h2>
        <div className={styles.elevationRow} role="list" aria-label="Shadow token samples">
          <div className={styles.shadowSm} role="listitem" aria-label="shadow-sm">
            shadow-sm
          </div>
          <div className={styles.shadowMd} role="listitem" aria-label="shadow-md">
            shadow-md
          </div>
          <div className={styles.shadowLg} role="listitem" aria-label="shadow-lg">
            shadow-lg
          </div>
        </div>
      </section>

      <section aria-labelledby="status-heading" className={styles.section}>
        <h2 id="status-heading" className={styles.sectionHeading}>
          Status Banners
        </h2>
        <div role="status" className={styles.bannerSuccess} aria-label="Success example">
          <strong>Success:</strong> Appointment confirmed.
        </div>
        <div role="alert" className={styles.bannerWarning} aria-label="Warning example">
          <strong>Warning:</strong> Your session expires in 5 minutes.
        </div>
        <div role="alert" className={styles.bannerDanger} aria-label="Error example">
          <strong>Error:</strong> Unable to load records.
        </div>
      </section>
    </div>
  );
}

interface SwatchItemProps {
  label: string;
  cssVar: string;
  role?: string;
  dark?: boolean;
}

function SwatchItem({ label, cssVar, role, dark = false }: SwatchItemProps) {
  return (
    <div className={styles.swatchItem} role={role}>
      <div
        className={styles.swatchBox}
        style={{ backgroundColor: `var(${cssVar})` }}
        aria-hidden="true"
      />
      <span className={`${styles.typeCaption} ${dark ? styles.swatchLabelDark : ''}`}>
        {label}
      </span>
      <span className={`${styles.typeCaption} ${dark ? styles.swatchLabelDark : ''}`}>
        {cssVar}
      </span>
    </div>
  );
}

export default BaselineDemo;
