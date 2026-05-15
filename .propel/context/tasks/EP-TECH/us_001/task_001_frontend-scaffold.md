# Task - TASK_001

## Requirement Reference
- **User Story:** us_001
- **Story Location:** .propel/context/tasks/EP-TECH/us_001/us_001.md
- **Acceptance Criteria:**
  - AC-001: React SPA scaffold builds without errors and is accessible at the InfinityFree production URL after CI deploys
  - AC-002: All colour, spacing, typography, radius, and elevation values resolve from CSS token variables; zero raw hex or pixel violations
  - AC-003: Zero colour-contrast violations reported by axe-core; normal text ≥ 4.5:1; large text / UI components ≥ 3:1
  - AC-004: Tab navigation reaches all interactive elements in logical DOM order; focus ring ≥ 3 px offset with ≥ 3:1 contrast
  - AC-005: Breakpoints at 375 px, 768 px, 1280 px defined; no horizontal scroll at any viewport; touch targets ≥ 44 × 44 px at 375 px
- **Edge Cases:**
  - Token file missing → CI build step fails (absent from build output)
  - InfinityFree file size limit exceeded → CI gate rejects oversized artefact
  - axe-core false positive on third-party component → documented suppression list; project-owned components pass with zero violations

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | UXR-201, UXR-202, UXR-301, UXR-401 |
| **Design Tokens** | N/A |

---

## AI References [CONDITIONAL: AI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

---

## Mobile References [CONDITIONAL: Mobile Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

---

## Applicable Technology Stack

| Layer | Technology | Version | Justification |
|-------|------------|---------|---------------|
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — mandated by BRD §5; free/open-source |
| Frontend | Vite (build tool) | 5.x | NFR-010 — free/open-source bundler for React 18 SPA |
| Frontend | ESLint + Prettier | Latest stable | NFR-012 — code quality enforcement in CI |
| Frontend | axe-core | Latest stable | UXR-201 — automated WCAG 2.2 AA accessibility baseline |
| Frontend | CSS custom properties (variables.css) | — | UXR-401 — design token system; zero raw values outside primitive table |
| Testing | React Testing Library | Latest stable | NFR-012, TR-002 — unit/integration testing for React components |

---

## Task Overview
Scaffold the React 18 SPA that serves as the frontend foundation for the entire UPACIP platform. This task establishes the project structure (Vite + React 18), configures ESLint and Prettier, creates the CSS design token system (`variables.css`) as the single source of truth for all visual constants, and validates the WCAG 2.2 AA accessibility baseline using axe-core. Responsive breakpoints (375 px, 768 px, 1280 px) are defined globally. The completed scaffold must build successfully so that the CI/CD pipeline (US_004) can deploy it to InfinityFree.

## Dependent Tasks
- None (EP-TECH foundational task — no upstream dependencies)

## Impacted Components
- `frontend/` — new React 18 SPA root (Vite scaffold)
- `frontend/src/styles/variables.css` — new design token stylesheet
- `frontend/src/styles/global.css` — new global reset and base styles
- `frontend/src/components/` — new baseline component directory
- `frontend/.eslintrc.cjs` — new ESLint configuration
- `frontend/.prettierrc` — new Prettier configuration

## Implementation Plan
1. Initialise React 18 project with Vite (`npm create vite@latest frontend -- --template react-ts`)
2. Install and configure ESLint (react, react-hooks, jsx-a11y plugins) and Prettier
3. Create `src/styles/variables.css` with token groups: `--color-*`, `--spacing-*`, `--font-*`, `--radius-*`, `--elevation-*`; import in `main.tsx`
4. Create `src/styles/global.css` with CSS reset, focus ring rule (`:focus-visible` — 3 px offset, ≥ 3:1 contrast), and breakpoint media query variables
5. Install axe-core (`@axe-core/react`) and wire into development mode component rendering for automated violation reporting
6. Create a `src/components/BaselineDemo.tsx` page that exercises all token categories so CI axe scan has a target
7. Add a `ci:token-audit` npm script that greps `src/**/*.css` for raw hex values and raw `px` literals outside `variables.css`; exits non-zero on violations
8. Verify build output (`npm run build`) produces artefact under InfinityFree size threshold

## Current Project State
```
/ (greenfield — no existing project files)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/package.json | Vite + React 18 dependencies and npm scripts |
| CREATE | frontend/vite.config.ts | Vite configuration |
| CREATE | frontend/tsconfig.json | TypeScript configuration |
| CREATE | frontend/.eslintrc.cjs | ESLint rules with jsx-a11y plugin |
| CREATE | frontend/.prettierrc | Prettier formatting rules |
| CREATE | frontend/src/main.tsx | SPA entry point, imports variables.css and global.css |
| CREATE | frontend/src/App.tsx | Root application component |
| CREATE | frontend/src/styles/variables.css | Design token definitions (colour, spacing, typography, radius, elevation) |
| CREATE | frontend/src/styles/global.css | CSS reset, focus ring, breakpoint media queries |
| CREATE | frontend/src/components/BaselineDemo.tsx | Component page used by CI axe scan |
| CREATE | frontend/scripts/token-audit.sh | Script: grep for raw hex/px values outside variables.css |

## External References
- [React 18 Docs](https://react.dev)
- [Vite 5 Docs](https://vitejs.dev/guide/)
- [axe-core WCAG 2.2 Rules](https://dequeuniversity.com/rules/axe/4.9)
- [WCAG 2.2 Contrast Criteria (SC 1.4.3, 1.4.11)](https://www.w3.org/WAI/WCAG22/quickref/?showtechniques=143%2C1411)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `npm run build` exits with code 0; dist/ artefact is generated
- [ ] `npm run ci:token-audit` exits with code 0 (zero raw hex/px violations in src/)
- [ ] axe-core scan against BaselineDemo page reports zero violations in browser console
- [ ] Manual Tab-key navigation through BaselineDemo confirms focus ring is visible on all interactive elements

## Implementation Checklist
- [ ] Initialise React 18 + Vite + TypeScript scaffold; ESLint (jsx-a11y) and Prettier configured (AC-001)
- [ ] Create `variables.css` with all token groups (`--color-*`, `--spacing-*`, `--font-*`, `--radius-*`, `--elevation-*`); import globally in `main.tsx` (AC-002)
- [ ] Add `ci:token-audit` npm script; verify zero raw hex/px violations in project source (AC-002)
- [ ] Install `@axe-core/react`; wire into dev mode; run baseline scan against BaselineDemo — zero WCAG 2.2 AA colour-contrast violations (AC-003)
- [ ] Define `:focus-visible` focus ring in `global.css` — 3 px offset, ≥ 3:1 contrast ratio (AC-004)
- [ ] Add responsive breakpoints at 375 px, 768 px, 1280 px in `global.css`; verify no horizontal scroll at each viewport (AC-005)
- [ ] Set touch-target minimum (`min-width: 44px; min-height: 44px`) on all interactive base styles; verify at 375 px (AC-005)
- [ ] Confirm `npm run build` succeeds and dist/ artefact is within InfinityFree size threshold (AC-001)
