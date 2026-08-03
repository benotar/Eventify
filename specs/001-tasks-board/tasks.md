---

description: "Task list for 001-tasks-board — TASKS.md Kanban Board"
---

# Tasks: TASKS.md Kanban Board

**Input**: Design documents from `/specs/001-tasks-board/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: **Included.** The spec and plan explicitly require a built-in self-test mode
(`research.md` §6, `plan.md` Technical Context). Per **Constitution Principle VII (TDD is
rejected)**, every test task is placed **after** the implementation it covers, within the same
story phase — to be completed before the story is considered done, not before the code is written.

**Organization**: Tasks are grouped by user story so each story is independently implementable and
demoable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story the task belongs to (US1–US5)
- Exact file paths are included in every task

## Path Conventions

Per `plan.md` → **Structure Decision**: a single self-contained file at
`tools/tasks-board/index.html`, plus `tools/tasks-board/README.md`. No `src/`, no `tests/`, no
build output. `tools/` does not exist yet and is created in Phase 1.

> ⚠️ **Parallelism is near-zero in this feature, by design.** Almost every task edits the *same
> file* (`index.html`), so `[P]` appears only where a genuinely different file is touched. This is
> the accepted cost of the single-file decision — see "Parallel Opportunities" below. Do not add
> `[P]` markers to same-file tasks to make the list look more parallel than it is.

---

## Phase 1: Setup

**Purpose**: Create the tool's home and prove it is isolated from every build.

- [X] T001 Create directory `tools/tasks-board/` and `tools/tasks-board/index.html` containing only the skeleton: `<!doctype html>`, `<html lang="en">`, `<head>` with `<meta charset="utf-8">`, `<meta name="viewport">`, `<title>Eventify Backlog Board</title>`, an empty `<style>` block, and an empty classic `<script>` block (not `type="module"` — no code here uses `import`/`export`, and staying classic keeps top-level functions inspectable from devtools). **No `<link>` and no `<script src>` may ever be added to this file** (FR-018)
- [X] T002 [P] Create `tools/tasks-board/README.md` with: one-line purpose, the `file://` open command, the `npx --yes serve tools/tasks-board -l 4321` fallback, a placeholder line "Verified working via: TBD" to be filled in T050, and a link to `specs/001-tasks-board/spec.md`
- [X] T003 Verify build isolation (FR-017): confirm `tools` appears nowhere in `Eventify.slnx`, is not globbed by `Directory.Build.props`, and is outside the Vite root of `src/Web/EventifySpa` — record the three checks in `tools/tasks-board/README.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shell, the design tokens, and the plumbing that gets file text into memory.
Everything below is required by **every** user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Add CSS custom properties to the `<style>` block of `tools/tasks-board/index.html`, copied literally from the locked design system (FR-011): `--bg: #08080F`, `--brand: #6366F1`, `--brand-2: #8B5CF6`, `--surface: rgba(255,255,255,0.055)`, `--surface-2: rgba(255,255,255,0.09)`, `--fg: #FFFFFF`, `--fg-muted: rgba(255,255,255,0.45)`, `--border: rgba(255,255,255,0.09)`. No second brand hue
- [X] T005 Add font stacks to `tools/tasks-board/index.html` using local-first fallbacks only — `--font-mono: ui-monospace, "JetBrains Mono", Consolas, monospace` and `--font-sans: system-ui, "Plus Jakarta Sans", sans-serif`. **No `@import`, no Google Fonts** (FR-018, and the documented Principle VIII deviation in `plan.md` Complexity Tracking)
- [X] T006 Add the layout CSS to `tools/tasks-board/index.html`: sticky header bar, three-column CSS grid (`grid-template-columns: repeat(3, 1fr)`, collapsing to one column under ~900px), card surface with `background: var(--surface)` + `backdrop-blur`, monospace treatment for IDs/counts, and a `@media (prefers-reduced-motion: reduce) { animation: none }` guard
- [X] T007 Add the static HTML shell to `tools/tasks-board/index.html`: header containing an **Open TASKS.md** button and a mode banner element; a collapsible diagnostics panel with a count badge; three labelled column containers (**To Do**, **In Progress**, **Done**) each with a count element; and an empty-state message shown before a file is loaded
- [X] T008 Implement capability detection in the `<script>` block of `tools/tasks-board/index.html` per `contracts/persistence.md` §1: test `'showOpenFilePicker' in window`; attempt a guarded IndexedDB open inside `try/catch`; set a module-level mode of `'read-write'` or `'read-only'`; render the corresponding banner text. A browser that cannot write MUST say so explicitly (FR-016)
- [X] T009 Implement `openTasksFile()` in `tools/tasks-board/index.html` per `contracts/persistence.md` §2: call `showOpenFilePicker` **inside the button's click handler** (transient activation is required), read via `arrayBuffer()`, and decode with `new TextDecoder('utf-8', { ignoreBOM: true })`. Treat a cancelled picker as a no-op, not an error
- [X] T010 Implement `splitLinesKeepingSeparators(text)` in `tools/tasks-board/index.html` using `text.split(/(?<=\n)/)`, and assert `lines.join('') === text` immediately after splitting. This assertion is the foundation of every CRLF and byte-preservation guarantee (FR-014)
- [X] T011 Implement the shared UI feedback helpers in `tools/tasks-board/index.html` — `showBanner(kind, message)` and `showError(message)` covering the seven conditions in `contracts/persistence.md` §7. Errors must be displayed verbatim, never swallowed

**Checkpoint**: The board opens, reads `docs/TASKS.md` into memory, and reports its mode. Nothing is
rendered yet.

---

## Phase 3: User Story 1 — See the whole backlog as a board (Priority: P1) 🎯 MVP

**Goal**: Every Epic, Story, and Task in `docs/TASKS.md` appears as a card in the correct column.

**Independent Test**: Load the file; confirm the board shows **11 Epics, 45 Stories, 104 Tasks
(3 done)** matching `quickstart.md` step 2, with no duplicates and nothing dropped.

### Implementation for User Story 1

- [X] T012 [US1] Implement the `parseTasksMd(text)` skeleton in `tools/tasks-board/index.html`: single pass over lines, tracking fenced-code state by toggling on any line starting with ``` at column 0, and returning the `BoardDocument` shape from `data-model.md`. **Every line inside a fence is skipped entirely** (FR-002) — 35 such blocks exist in the file
- [X] T013 [US1] Implement Epic heading parsing inside `parseTasksMd` in `tools/tasks-board/index.html` — `/^### Epic (\d+)\s*[—-]\s*(.+)$/` — plus the GitHub anchor slug derivation from `contracts/tasks-md-grammar.md` §2: lowercase → strip all characters except `a-z0-9`, space and `-` → spaces to hyphens. **Em dashes are removed, not replaced**, so `Epic 1 — Identity Service` must yield `epic-1--identity-service` with a double hyphen
- [X] T014 [US1] Implement Roadmap table parsing in `tools/tasks-board/index.html`: read rows between `## Roadmap` and the next `##`, locate the *Priority* and *Epics* columns **by header name rather than fixed index**, extract every `[E<n>](#anchor)` link from the Epics cell (a row may map to several Epics), and build an `anchor → { phase, priority }` lookup (FR-003)
- [X] T015 [US1] Implement Story heading parsing inside `parseTasksMd` in `tools/tasks-board/index.html` — `/^#### (US-\d+\.\d+)\s*[—-]\s*(.+)$/` — attaching each story to the most recent Epic, and capture the following italicised text as `persona` when present. **Personas may span multiple source lines** (`contracts/tasks-md-grammar.md` §5) — every persona in the real file wraps this way; a single-line-only capture silently returns `null` for all 45 stories, a real bug caught only by testing against production data (see `data-model.md` → Story.persona)
- [X] T016 [US1] Implement task checkbox parsing inside `parseTasksMd` in `tools/tasks-board/index.html` — `/^(\s*)([-*])\s\[([^\]])\]\s(.*)$/` — recording `lineIndex` and the **measured** `markerOffset` (never hard-coded to 3), with `done = marker !== ' '` so `[x]`, `[X]` and `[-]` all count as done without throwing
- [X] T017 [US1] Implement continuation-line folding inside `parseTasksMd` in `tools/tasks-board/index.html`: an indented, non-empty line that is neither a checkbox nor a heading folds into the preceding task's display `text` and increments `continuationLineCount`. Ten such lines exist today. Folding is **display-only** — these lines are never written
- [X] T018 [US1] Implement Epic-direct task attachment inside `parseTasksMd` in `tools/tasks-board/index.html`: a checkbox seen after an `### Epic` heading but before any `#### US-` heading attaches to the Epic with `ownerKind: 'epic'` (FR-004). This is Epic 0's shape — verify its three tasks survive
- [X] T019 [US1] Implement parse diagnostics in `tools/tasks-board/index.html` (FR-005): duplicate story IDs (render both, warn — never merge), a marker not found at the recorded offset (mark the task non-toggleable), and a missing `## Backlog` section. Unrecognised content is skipped and reported, never fatal
- [X] T020 [US1] Implement `deriveStatus(tasks)` in `tools/tasks-board/index.html`: `0 done → 'todo'`, `all done && length > 0 → 'done'`, otherwise `'in-progress'`. **The `length > 0` guard is load-bearing** — a zero-task story must land in To Do, never Done (`data-model.md` → Derived status)
- [X] T021 [US1] Implement card rendering in `tools/tasks-board/index.html`: one card per Story (plus one per Epic that has direct tasks), showing story ID and title in the mono font, parent Epic, inherited priority chip (🔴/🟡/🟢), and `n/m` progress; append each card to the column its derived status selects (FR-006, FR-007, FR-008)
- [X] T022 [US1] Implement the header stats and per-column counts in `tools/tasks-board/index.html` — overall `done/total` across the document and a live count in each column header (FR-009)
- [X] T023 [US1] Implement the diagnostics panel rendering in `tools/tasks-board/index.html`: collapsed by default with a count badge, expanding to the list of `Diagnostic` entries. The board must still render fully when diagnostics exist

### Tests for User Story 1 ⚠️ *(after implementation, same PR — Constitution VII)*

- [X] T024 [US1] Add the self-test harness to `tools/tasks-board/index.html`: when `location.search` includes `selftest=1`, run all registered assertions against inline fixture strings and render a pass/fail list instead of the board. No test runner, no dependency (`research.md` §6)
- [X] T025 [US1] Add parser self-tests to `tools/tasks-board/index.html` covering: a `- [ ]` inside a ```gherkin fence is **not** parsed as a task; a 6-space continuation line folds into its task; Epic-direct tasks are retained; a zero-task story derives `todo` not `done`; `[X]` and `[-]` parse as done; a duplicate story ID produces two cards plus a diagnostic; the `epic-1--identity-service` double-hyphen anchor is derived correctly

**Checkpoint**: US1 is independently demoable — a read-only board that faithfully mirrors the file.

---

## Phase 4: User Story 2 — Tick a task off and update the file (Priority: P1)

**Goal**: Clicking a checkbox rewrites exactly one character in `docs/TASKS.md`.

**Independent Test**: Toggle one task, then `git diff docs/TASKS.md` shows **exactly one changed
line** (`quickstart.md` step 3), and the byte-level check in step 3b shows 855 CRLF and 0 bare LF.

### Implementation for User Story 2

- [X] T026 [US2] Implement the pure function `toggleTaskInLines(lines, lineIndex, markerOffset)` in `tools/tasks-board/index.html`: return a new array in which only `lines[lineIndex]` differs, with the single character at `markerOffset` flipped between `' '` and `'x'`. It must not re-serialise the document, touch any other line, or alter line separators (FR-012)
- [X] T027 [US2] Render each card's task checklist with interactive `<input type="checkbox">` elements in `tools/tasks-board/index.html`, each bound to its Task's `lineIndex` and `markerOffset`. Tasks flagged non-toggleable by T019 render disabled with an explanatory title
- [X] T028 [US2] Implement the permission step in `tools/tasks-board/index.html` per `contracts/persistence.md` §3: on the first toggle, call `queryPermission({ mode: 'readwrite' })` and, if not granted, `requestPermission` **from within the click handler** so the user gesture is still live. Denial switches the board to read-only mode
- [X] T029 [US2] Implement the one-character guard in `tools/tasks-board/index.html` (`contracts/persistence.md` §4 step 5): before writing, assert that `newText` differs from `originalText` in exactly one character position. **On failure, abort the write and report a bug** — a failed invariant means the parser's coordinates are wrong, and writing anyway risks the backlog file
- [X] T030 [US2] Implement the write itself in `tools/tasks-board/index.html`: `handle.createWritable()` → `write(new TextEncoder().encode(newText))` → `close()`. `createWritable` truncates, so the full document is written — byte-identical except the one character
- [X] T031 [US2] Implement post-write commit and re-render in `tools/tasks-board/index.html`: update `originalText` and `sourceLines`, recompute the owning story's derived status, and move its card between columns without a page reload (FR-015, US2 acceptance §3)
- [X] T032 [US2] Implement write-failure handling in `tools/tasks-board/index.html`: revert the checkbox to its prior state and surface the error verbatim. **The UI must never update optimistically** — showing a task as done when the write failed produces exactly the board/file disagreement this feature exists to prevent

### Tests for User Story 2 ⚠️ *(after implementation, same PR — Constitution VII)*

- [X] T033 [US2] Add write-path self-tests to `tools/tasks-board/index.html`: `lines.join('') === originalText` round-trip; a CRLF fixture survives parse → toggle → reassemble with its `\r\n` intact; **one toggle changes exactly one character**; toggling twice returns the text to byte-identical original
- [X] T034 [US2] Execute `quickstart.md` steps 3 and 3b against the real `docs/TASKS.md` and record the outcome: `git diff --stat` shows `1 insertion(+), 1 deletion(-)`, and the byte-level check reports 855 CRLF, 0 bare LF, no BOM. Restore the file with `git checkout -- docs/TASKS.md` afterwards. Manually verified by the user through the real OS file picker (not automatable — see research.md §2)

**Checkpoint**: The board is now a board, not a report. US1 + US2 together are a shippable MVP.

---

## Phase 5: User Story 3 — Don't clobber external edits (Priority: P2)

**Goal**: An edit made in the IDE is never silently overwritten by the board.

**Independent Test**: Edit `docs/TASKS.md` externally while the board is open, then toggle a
checkbox — a conflict warning appears and **no write occurs** (`quickstart.md` step 4).

### Implementation for User Story 3

- [X] T035 [US3] Implement the compare-before-write step in `tools/tasks-board/index.html` (`contracts/persistence.md` §4 steps 2–3): on **every** toggle, re-read the file through the handle, decode it, and compare against `originalText`. On mismatch, abort before writing (FR-013)
- [X] T036 [US3] Implement the conflict UI in `tools/tasks-board/index.html`: a warning bar reading "TASKS.md changed on disk. Reload to continue." with a **Reload** action. The attempted toggle is **discarded, not queued** — re-applying it after reload could re-tick a task the user deliberately unticked
- [X] T037 [US3] Implement the reload path in `tools/tasks-board/index.html`: re-read, re-parse, and re-render from the current file contents, resetting `originalText` and `sourceLines` and clearing the warning

### Tests for User Story 3 ⚠️ *(after implementation, same PR — Constitution VII)*

- [X] T038 [US3] Execute `quickstart.md` step 4 against the real file and record the outcome: external edit → toggle → warning shown, no write performed, IDE edit intact. Restore with `git checkout -- docs/TASKS.md`. Manually verified by the user through the real OS file picker (not automatable — see research.md §2)

**Checkpoint**: Safe to keep open all day alongside an editor.

---

## Phase 6: User Story 4 — Filter and navigate (Priority: P2)

**Goal**: A 10-Epic backlog stays workable.

**Independent Test**: Filter to Epic 1 — only Epic 1 cards remain across all three columns and the
header counts follow the filtered set (`quickstart.md` step 5).

### Implementation for User Story 4

- [X] T039 [US4] Add the filter controls to the header in `tools/tasks-board/index.html`: an Epic dropdown populated from the parsed Epics, and a priority filter offering 🔴/🟡/🟢/all (FR-010)
- [X] T040 [US4] Implement filter application in `tools/tasks-board/index.html`: re-render the three columns from the filtered story set and update **both** the header stats and the per-column counts to reflect the filtered set, not the whole document
- [X] T041 [US4] Implement card expansion in `tools/tasks-board/index.html`: collapsed cards show ID, title, Epic, priority chip and `n/m`; expanding reveals the persona line and the full task checklist (FR-008). Expansion state lives in memory only and is intentionally lost on reload
- [X] T042 [US4] Add per-Epic completion counts to `tools/tasks-board/index.html`, rendered on the Epic filter options or as an Epic summary strip (FR-009)

### Tests for User Story 4 ⚠️ *(after implementation, same PR — Constitution VII)*

- [X] T043 [US4] Execute `quickstart.md` step 5 and confirm: Epic-1 filtering isolates the right cards; the 🔴 priority filter yields exactly E0–E3 (Phases 0–3); and **every card shows a priority chip** — universally missing chips indicate the T013 anchor derivation is wrong

**Checkpoint**: Usable at the current backlog's scale.

---

## Phase 7: User Story 5 — Reopen without re-picking (Priority: P3)

**Goal**: Reopening the board is one click.

**Independent Test**: Reload the page — a **Reopen docs/TASKS.md** button appears and one click
restores the board (`quickstart.md` step 6).

### Implementation for User Story 5

- [X] T044 [US5] Implement the IndexedDB handle store in `tools/tasks-board/index.html` per `contracts/persistence.md` §5: database `eventify-tasks-board` v1, object store `handles` keyed on `id`, storing `{ id: 'tasks-md', handle, savedAt }`. `FileSystemFileHandle` is structured-cloneable and stores directly
- [X] T045 [US5] Implement handle restoration in `tools/tasks-board/index.html`: on load, read the stored record and render a **Reopen `<handle.name>`** button; clicking it calls `requestPermission` (permission never survives a reload), then reads and parses. Display only `handle.name` — the API exposes no full path
- [X] T046 [US5] Ensure graceful degradation in `tools/tasks-board/index.html`: every IndexedDB call is wrapped so that a failure disables only the reopen button. **US5 degrading must never block US1–US4** — a live possibility under `file://` (`research.md` §2)

**Checkpoint**: All five stories complete.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T047 Run the full self-test suite at `tools/tasks-board/index.html?selftest=1` and confirm every assertion from T025 and T033 passes (`quickstart.md` step 7)
- [X] T048 Verify the no-network guarantee (FR-018, SC-006): `Select-String -Path tools\tasks-board\index.html -Pattern 'https?://|cdn\.|fonts\.googleapis'` must return **no matches**; then disconnect the network, reload the board, and confirm it works unchanged
- [X] T049 Verify SC-005 — add a throwaway Epic and Story to `docs/TASKS.md`, reload the board, confirm both appear with **zero code changes**, then revert with `git checkout -- docs/TASKS.md`
- [X] T050 Resolve the one open assumption from `research.md` §2 and record the answer in `tools/tasks-board/README.md`: does `file://` support the picker and IndexedDB in this Chrome build, or is `npx serve` required? Replace the "Verified working via: TBD" placeholder from T002
- [X] T051 Confirm accessibility basics in `tools/tasks-board/index.html`: checkboxes reachable and toggleable by keyboard, visible focus rings against the dark surface, and column headers marked up as headings
- [X] T052 Final pass over `quickstart.md` "Definition of done" — walk the SC-001 … SC-006 table and confirm each row's stated verification actually passed
- [X] T053 Verify FR-019 (no create/delete/reorder/re-word affordance for Epics, Stories, or Tasks) — added during `/speckit-analyze` as a coverage-gap fix, since no prior task referenced FR-019 directly. Confirmed via static inspection of `tools/tasks-board/index.html`: no add/delete/reorder/drag/contenteditable control exists anywhere in the file; `toggleTaskInLines` — the only function that ever mutates task/story/epic content — flips a single marker character and nothing else

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (Setup)
   ↓
Phase 2 (Foundational) ── blocks everything
   ↓
Phase 3 (US1, P1) ── parser + render          ← MVP starts here
   ↓
Phase 4 (US2, P1) ── write-back               ← MVP complete
   ↓
Phase 5 (US3, P2) ── conflict detection
   ↓
Phase 6 (US4, P2) ── filters
   ↓
Phase 7 (US5, P3) ── remembered handle
   ↓
Phase 8 (Polish)
```

### User Story Dependencies

Unlike a typical multi-service feature, these stories are **genuinely sequential**, and the tasks
list should not pretend otherwise:

- **US1** depends only on Phase 2. It is the only story that stands completely alone.
- **US2** depends on US1 — there is no checkbox to click until cards render.
- **US3** depends on US2 — there is no write to guard until writing exists.
- **US4** depends on US1 only (it filters rendered cards). It *could* be built before US2, but US2
  is higher value and equal priority, so it goes first.
- **US5** depends on Phase 2's file layer only. It is the most independent story after US1 and can
  be deferred indefinitely.

### Within Each Story

Implementation first; tests follow in the same PR before merge (Constitution Principle VII).

---

## Parallel Opportunities

**There are almost none, and that is a design consequence, not an oversight.**

51 of the 52 tasks edit `tools/tasks-board/index.html`. The single-file decision (`plan.md` →
Structure Decision) traded parallelism for zero build tooling and a double-click launch — a correct
trade for a one-screen personal tool, but it means the work is inherently serial.

Genuine `[P]` opportunities:

- **T002** (`README.md`) runs parallel to **T001** (`index.html`) — different files.

Everything else within a phase must be sequenced. Attempting parallel edits to `index.html` would
produce conflicts, not speed.

---

## Implementation Strategy

### MVP scope

**Phases 1 → 4 (T001–T034)** — Setup, Foundational, US1, US2.

That is the smallest thing that satisfies the original request: *see the backlog visually and tick
tasks off*. US3–US5 are quality and convenience layered on a working board.

### Incremental delivery

1. **Stop after Phase 3** for a working read-only board — already replaces scrolling 855 lines of
   markdown, and it is safe because it cannot write.
2. **Stop after Phase 4** for the full MVP. Do not rush T029/T033 — the one-character guard and the
   round-trip test are what stand between this tool and a corrupted backlog file.
3. **Phases 5–7** in priority order as the tool proves itself in daily use.

### Risk concentration

Effectively all risk sits in two pure functions — `parseTasksMd` (T012–T019) and
`toggleTaskInLines` (T026). Both are string-in/string-out and fully covered by T025 and T033. The
file and render layers are thin enough to verify by hand via `quickstart.md`.

The highest-consequence single task is **T029** (the one-character guard). It is the last line of
defence against a coordinate bug rewriting the wrong part of `docs/TASKS.md`.

---

## Notes

- `[P]` = different file, no dependency on incomplete work. Same-file tasks are never `[P]`.
- Write tests after the code they cover, in the same PR before merge (Constitution VII).
- Commit after each task or logical group.
- `docs/TASKS.md` is used as live test data throughout. Several tasks modify it deliberately —
  each says so and each ends with `git checkout -- docs/TASKS.md`. Keep the file committed so that
  restore always works.
