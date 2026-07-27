# Implementation Plan: TASKS.md Kanban Board

**Branch**: `001-tasks-board` | **Date**: 2026-07-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-tasks-board/spec.md`

## Summary

Render `docs/TASKS.md` as an Azure-DevOps-style Kanban board (**To Do / In Progress / Done**) in
which ticking a card's checkbox writes `- [ ]` → `- [x]` straight back into the file.

The technical approach is deliberately narrow: **one self-contained `index.html`**, vanilla ES2022,
no build step, no dependencies, no network. It reads and writes the file through the browser's
**File System Access API**, and every write is a **single-character replacement on a line-indexed
snapshot** of the original text — the document is never re-serialised from the parsed model. That
one decision is what makes "toggling a task produces a one-line `git diff`" a structural property
rather than a thing to be careful about.

Column membership is **derived**, never stored: the file has exactly two states per task, so
*In Progress* means "some tasks done, not all". No status metadata is added to the file, and
drag-and-drop is out of scope because it would have nothing to write.

## Technical Context

**Language/Version**: HTML5 + CSS3 + vanilla JavaScript (ES2022 syntax, no transpilation). Ships as
a classic `<script>`, not `type="module"` — nothing here uses `import`/`export`, and staying
classic keeps every top-level function inspectable from devtools, which matters for a tool with no
build step and no bundler-provided debugging story

**Primary Dependencies**: **None.** No npm packages, no CDN, no fonts, no framework. Browser
platform APIs only: File System Access API, IndexedDB (optional, US-5 only), `TextDecoder` /
`TextEncoder`

**Storage**: The user's `docs/TASKS.md` itself is the entire data store. IndexedDB holds one
`FileSystemFileHandle` for convenience (US-5) and nothing else — no task state is ever persisted
outside the markdown file

**Testing**: Built-in self-test mode `index.html?selftest=1` — assertions over inline fixtures for
the two pure functions (`parseTasksMd`, `toggleTaskInLines`), plus the manual verification
sequence in [quickstart.md](./quickstart.md)

**Target Platform**: Chromium desktop (Chrome / Edge 86+) on Windows 11, opened from `file://` or
served at `http://localhost` (see [research.md](./research.md) §2). Non-Chromium browsers render
read-only

**Project Type**: Standalone local developer tool — outside the solution, outside every build

**Performance Goals**: Parse + render the full 855-line / 37 KB file in < 1 s (SC-003); toggle
round-trip (compare → write → re-render) feels instant, < 200 ms

**Constraints**: Zero network requests (FR-018); a single toggle must change exactly one line of
the file (SC-002); CRLF line endings preserved (FR-014 — confirmed present on all 855 lines);
never write when the file changed underneath (FR-013)

**Scale/Scope**: 11 Epics, 45 User Stories, 104 tasks today. One screen, one user, one file.
Estimated ~900 lines total across HTML + CSS + JS in a single file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Gate status: ✅ **PASS with one recorded deviation**

Evaluated against `.specify/memory/constitution.md` **v1.0.0** (ratified 2026-07-27).

> *Historical note*: this gate was originally recorded as DEFERRED because the constitution was
> still an unratified template. It was re-evaluated after ratification.

| Principle | Applies? | Verdict |
|---|---|---|
| **I. Learning Surface Over Production Scale** | Yes | ✅ Pass — no framework, no build, no server, no dependencies for a one-screen local tool. The simpler option won at every fork (see research.md alternatives) |
| **II. Bounded Context Autonomy** | No | N/A — not a service. Owns no data; `docs/TASKS.md` is the only store and the board is its sole reader/writer |
| **III. The Dependency Rule** | No | N/A — no .NET projects, no layers. Explicitly excluded from `Eventify.slnx` (FR-017), so NetArchTest never sees it |
| **IV. Explicit Over Implicit** | Yes | ✅ Pass — hand-written parser rather than a markdown library; source coordinates (`lineIndex`/`markerOffset`) stored explicitly rather than re-derived by convention |
| **V. Errors Are Values** | Partially | ✅ Pass in spirit — no C#, so no `ErrorOr`. The equivalent discipline is enforced: parse failures become `Diagnostic` values surfaced in the UI (FR-005), and write failures are reported rather than swallowed (FR-015) |
| **VI. Transactional Event Integrity** | No | N/A — no messaging, no outbox, no database. The analogous concern (never lose a concurrent external write) is handled by compare-before-write, FR-013 |
| **VII. Tests After Code, Before Merge** | Yes | ✅ Pass — self-test mode covers the two pure functions where all risk sits; written after implementation, shipped in the same change. No Testcontainers requirement, as there is no infrastructure |
| **VIII. One Visual Language** | Yes | ⚠️ **Deviation on fonts.** Brand tokens, dark surface, and glassmorphism are matched exactly (FR-011); the CDN prohibition is honoured absolutely (FR-018, no network at all). But the design-system *fonts* cannot be loaded without the network — justified in Complexity Tracking. Localization: N/A — an English-only local dev tool, not user-facing product surface |

**Post-Phase-1 re-check**: ✅ Unchanged. The Phase 1 design introduced no dependency, no build
step, no network call, and no new project in the solution. The single deviation (fonts) is
unchanged and remains documented below.

## Project Structure

### Documentation (this feature)

```text
specs/001-tasks-board/
├── spec.md              # Feature specification
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — measured baseline + 6 decisions
├── data-model.md        # Phase 1 output — parsed entities & derivation rules
├── quickstart.md        # Phase 1 output — run & verification guide
├── contracts/
│   ├── tasks-md-grammar.md    # THE contract: the markdown shapes the parser accepts
│   └── persistence.md         # IndexedDB handle store + write protocol
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
tools/
└── tasks-board/
    ├── index.html       # The entire tool: markup + <style> + classic <script> (no import/export)
    └── README.md        # How to open it; the file:// vs localhost note
```

**Structure Decision**: A new top-level `tools/` directory (does not exist yet) holding one
self-contained file.

`tools/` rather than `src/` or `docs/` because this is developer tooling, not product source and
not documentation. It is deliberately outside `Eventify.slnx`, outside `Directory.Build.props`
globbing, and outside the SPA's Vite build (FR-017) — nothing about it can break `dotnet build` or
`npm run build`, and it can never leak into a deployed artifact.

Everything in one file because the tool's premise is "double-click and it opens": splitting CSS and
JS into siblings would gain modularity that ~900 lines do not need, while adding module-resolution
concerns under `file://`.

### Implementation shape inside `index.html`

Four clearly separated concerns, top to bottom — no framework, but no spaghetti either:

| Concern | Responsibility | Purity |
|---|---|---|
| `parseTasksMd(text)` | markdown → Board Document (Epics → Stories → Tasks + source coordinates + diagnostics) | **Pure** — unit-tested |
| `toggleTaskInLines(lines, lineIndex, markerOffset)` | flips one marker character in one line | **Pure** — unit-tested |
| File layer | `showOpenFilePicker`, permission handling, compare-before-write, IndexedDB handle | Side-effecting, thin |
| Render layer | columns, cards, filters, counts, diagnostics panel | DOM-only, driven by the parsed model |

The two pure functions hold essentially all the risk; the two impure layers are thin enough to
verify by hand via `quickstart.md`.

## Phase sequencing

Follows the spec's own priorities — each stage is independently demoable:

1. **P1 — US-1**: parse + render read-only board (columns, cards, counts). Delivers the visibility
   win on its own.
2. **P1 — US-2**: write-back on toggle, including the self-test round-trip assertion. This is the
   stage that must not be rushed.
3. **P2 — US-3**: compare-before-write conflict detection + reload.
4. **P2 — US-4**: Epic/priority filters, card expansion, per-Epic counts.
5. **P3 — US-5**: IndexedDB handle persistence (degrades silently if unavailable).

## Complexity Tracking

> Filled because the Constitution Check records one deviation from a locked project decision.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| **Principle VIII** — design-system fonts (JetBrains Mono, Plus Jakarta Sans) referenced but not loaded | FR-018 forbids all network requests, and both faces ship via Google Fonts. A local-first stack (`ui-monospace, "JetBrains Mono", Consolas` / `system-ui, "Plus Jakarta Sans"`) uses the real faces when the developer already has them installed and degrades cleanly otherwise | Loading them from Google Fonts would break the offline guarantee (SC-006) and reintroduce exactly the CDN dependency that US-1.4 exists to remove. Bundling the font files as base64 would add ~200 KB to a single-file dev tool for a cosmetic gain |
