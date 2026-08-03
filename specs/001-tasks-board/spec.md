# Feature Specification: TASKS.md Kanban Board

**Feature Branch**: `001-tasks-board`

**Created**: 2026-07-27

**Status**: Draft

**Input**: User description: "в мене є файл docs/TASKS.md це мій беклог. Я хочу візуально бачити цей беклог, щоб там можна було відмічати зроблені задачі, щоб було типу як boards в Azure DevOps Server. Якщо я зміню файл TASKS.md запиши щоб це була нова спека, яка підганяє вигляд борду під файл"

## Context

`docs/TASKS.md` is the project's backlog: a Roadmap table (Phases → Epics) plus a Backlog of
**Epics → User Stories → Tasks**, where Tasks are markdown checkboxes (`- [ ]` / `- [x]`).
Reading and updating it today means scrolling ~850 lines of markdown.

This feature adds a **local, visual Kanban board** over that same file — Azure DevOps Boards in
feel — where progress is visible at a glance and a task can be ticked off by clicking it.

## Governing principle *(mandatory — from user instruction)*

**`docs/TASKS.md` is the single source of truth. The board conforms to the file; the file never
conforms to the board.**

Two consequences, deliberately separated:

1. **Content changes need no work.** Adding, renaming, reordering, or deleting Epics / User
   Stories / Tasks in `TASKS.md` MUST be picked up by the board automatically on reload. This is
   the normal case and is never a spec change.
2. **Structural changes trigger a new spec.** If the *format* of `TASKS.md` changes — new heading
   levels, a different Epic/Story ID scheme, new per-task metadata (assignee, estimate, due date),
   a new status vocabulary beyond the two checkbox states, a different Roadmap table shape — then
   a **new spec** is written (`/speckit-specify`) that adapts the board's parsing and appearance to
   the new file. The board is never patched ad hoc to chase the file, and the file is never
   reshaped to suit the board.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — See the whole backlog as a board (Priority: P1)

As the project owner, I open the board, point it at `docs/TASKS.md`, and immediately see every
User Story as a card laid out in **To Do / In Progress / Done** columns, so I know where the
project stands without reading markdown.

**Why this priority**: This is the core value — visibility. Even with zero editing capability the
board already replaces "scroll through 850 lines of markdown" with "glance at three columns".

**Independent Test**: Open the board, select `docs/TASKS.md`, confirm every `#### US-N.M` heading
in the file appears exactly once as a card in the correct column with the correct task count.

**Acceptance Scenarios**:

1. **Given** `TASKS.md` in its current state, **When** I load it into the board, **Then** every
   Epic and every User Story from the Backlog section is rendered, and no story is duplicated or
   dropped.
2. **Given** a story whose tasks are all unchecked, **When** the board renders, **Then** that card
   sits in **To Do**.
3. **Given** a story with some — but not all — tasks checked, **When** the board renders, **Then**
   that card sits in **In Progress** and shows a partial progress indicator (e.g. `1/2`).
4. **Given** a story whose tasks are all checked, **When** the board renders, **Then** that card
   sits in **Done**.
5. **Given** Epic 0, whose tasks sit directly under the Epic heading with no `#### US-` heading,
   **When** the board renders, **Then** those tasks still appear on the board under that Epic and
   are not silently lost.

---

### User Story 2 — Tick a task off and have the file updated (Priority: P1)

As the project owner, I click a task's checkbox on a card and the corresponding `- [ ]` in
`docs/TASKS.md` becomes `- [x]` on disk, so the board and the file never disagree and the change
lands in git alongside my code.

**Why this priority**: Equal-first with US-1. This is what makes it a board rather than a report —
and it is the only way the "single source of truth" principle survives contact with daily use.

**Independent Test**: Toggle one checkbox on the board, then open `docs/TASKS.md` in an editor and
confirm exactly that one line changed, and `git diff` shows a one-line diff.

**Acceptance Scenarios**:

1. **Given** an unchecked task on a card, **When** I click its checkbox, **Then** the file on disk
   has that task's marker changed from `[ ]` to `[x]`.
2. **Given** I toggle a task, **When** the file is written, **Then** **no other byte of the file
   changes** — headings, tables, gherkin blocks, blockquotes, indentation, trailing whitespace, and
   line endings are all preserved exactly.
3. **Given** I toggle the last remaining unchecked task of a story, **When** the write completes,
   **Then** the card moves from **In Progress** to **Done** without a page reload.
4. **Given** a checked task, **When** I click its checkbox, **Then** it reverts to `[ ]` in the
   file — the operation is symmetric.
5. **Given** the browser has not yet been granted write access to the file, **When** I attempt the
   first toggle, **Then** I am prompted for permission and the toggle completes once granted.

---

### User Story 3 — Don't clobber edits I made outside the board (Priority: P2)

As the project owner who also edits `TASKS.md` by hand in the IDE, I want the board to detect that
the file changed underneath it and refuse to overwrite my edits blindly.

**Why this priority**: Directly implied by the governing principle — the user is explicitly
expected to keep editing the file. Silent data loss here would poison trust in the whole tool.
It is P2 only because US-1 and US-2 are usable alone for a single-editor session.

**Independent Test**: Load the board, edit `TASKS.md` in an editor and save, then toggle a checkbox
on the board and confirm a conflict warning appears instead of a write.

**Acceptance Scenarios**:

1. **Given** the board has the file loaded, **When** the file is modified externally and I then
   toggle a checkbox, **Then** the board detects the mismatch, does **not** write, and offers to
   reload.
2. **Given** the conflict warning, **When** I choose reload, **Then** the board re-parses the
   current file contents and my toggle is discarded (not silently re-applied).

---

### User Story 4 — Filter and navigate like a real board (Priority: P2)

As the project owner, I want to filter the board by Epic and by priority and see a card's full
task checklist inline, so a 10-Epic backlog stays workable.

**Why this priority**: The current backlog already has 10 Epics and 40+ stories — an unfiltered
three-column wall is hard to act on. Valuable, but the board is still useful without it.

**Independent Test**: Select a single Epic in the filter and confirm only that Epic's cards remain
in all three columns.

**Acceptance Scenarios**:

1. **Given** the full board, **When** I filter to Epic 1, **Then** only Epic 1 cards are visible
   across all three columns and the header counts reflect the filtered set.
2. **Given** a card, **When** I view it, **Then** it shows its Epic, its story ID (`US-1.1`), its
   inherited Phase priority (🔴/🟡/🟢), and its progress (`n/m`).
3. **Given** a card, **When** I expand it, **Then** I see that story's full task checklist and its
   persona line.

---

### User Story 5 — Reopen the board without re-picking the file (Priority: P3)

As the project owner, I want the board to remember which file I chose so reopening it is one click,
not a file-picker dance every time.

**Why this priority**: Pure convenience. The board is fully functional without it.

**Acceptance Scenarios**:

1. **Given** I previously loaded `docs/TASKS.md`, **When** I reopen the board, **Then** it offers
   to reopen that same file and needs at most one click to re-grant access.

---

### Edge Cases

- **Checkbox syntax inside a fenced code block** (e.g. a ```gherkin example containing `- [ ]`)
  MUST NOT be parsed as a task.
- **Multi-line tasks** — a checkbox whose text wraps onto indented continuation lines (present
  today, e.g. US-1.3 and US-2.5) MUST be treated as one task, and its continuation lines MUST
  survive a write untouched.
- **Multi-line personas** — a story's `*As a …, I want …*` persona text wraps across two source
  lines for **every story in the current file** (not an edge case in the statistical sense — it is
  the norm). It MUST be captured as one continuous string; a parser that only recognises a persona
  fully contained on a single line will silently return no persona at all for the entire backlog.
- **A story with zero tasks** (e.g. US-4.1 style stubs) — the card MUST still render; it counts as
  To Do, not Done. Zero of zero is not completion.
- **Epic 0**, whose tasks are attached directly to the Epic with no story heading — see US-1 §5.
- **Duplicate story IDs** in the file — the board MUST render both rather than collapse them, and
  surface a visible warning.
- **A checkbox marker other than `[ ]` / `[x]`** (e.g. `[-]`, `[X]`) — the board MUST NOT crash;
  it renders the task and treats any non-space marker as done.
- **The file is empty, unreadable, or contains no Backlog section** — the board shows a clear
  message, not a blank screen or a stack trace.
- **The browser does not support the file-access capability** — the board MUST say so explicitly
  and fall back to read-only rather than appearing to save.
- **The user's file has CRLF line endings** — writes MUST NOT convert them to LF (that would turn
  a one-task toggle into an 850-line git diff).

## Requirements *(mandatory)*

### Functional Requirements

**Parsing**

- **FR-001**: The board MUST parse `docs/TASKS.md` into Epics → User Stories → Tasks, deriving the
  hierarchy from `### Epic N — …` and `#### US-N.M — …` headings and `- [ ]` / `- [x]` list items.
- **FR-002**: The board MUST ignore checkbox-looking lines inside fenced code blocks.
- **FR-003**: The board MUST derive each Epic's priority (🔴/🟡/🟢) from the Roadmap table by
  following the Phase → Epic anchor links, and display it on that Epic's cards.
- **FR-004**: The board MUST attach tasks that appear under an Epic before any story heading to
  that Epic directly, without inventing a fake story.
- **FR-005**: Parsing MUST be tolerant: content it does not recognise is skipped and reported in a
  visible diagnostics area, never thrown away silently and never fatal.

**Board rendering**

- **FR-006**: The board MUST present three columns — **To Do**, **In Progress**, **Done** — with
  each User Story as a card.
- **FR-007**: Column membership MUST be derived, not stored: 0 tasks done → To Do; some done → In
  Progress; all done (and at least one task exists) → Done.
- **FR-008**: Each card MUST show story ID, title, parent Epic, inherited priority, and `n/m`
  progress; expanding a card MUST reveal its persona line and full task checklist.
- **FR-009**: The board MUST show per-Epic and overall completion counts.
- **FR-010**: The board MUST offer filtering by Epic and by priority.
- **FR-011**: The board MUST visually match the project's locked design system — dark background
  `#08080F`, indigo `#6366F1` → violet `#8B5CF6` brand pair, glass surfaces, monospace for IDs,
  counts and headings — and MUST NOT introduce a second brand hue.

**Writing back**

- **FR-012**: Toggling a task's checkbox MUST rewrite only that task's marker characters on that
  one line of the file. The board MUST NOT re-serialise the document from its parsed model.
- **FR-013**: Before writing, the board MUST verify the on-disk content still matches the content
  it parsed; on mismatch it MUST abort the write and offer to reload.
- **FR-014**: The board MUST preserve the file's original line endings and final-newline state.
- **FR-015**: The board MUST reflect a successful write in the UI immediately, and MUST visibly
  report a failed write rather than leaving the UI optimistically updated.
- **FR-016**: Where the environment cannot grant write access, the board MUST degrade to read-only
  and say so, rather than silently discarding toggles.

**Scope discipline**

- **FR-017**: The board MUST be a standalone local developer tool. It MUST NOT be added to
  `Eventify.slnx`, MUST NOT be part of any .NET build or the SPA build, and MUST NOT ship in any
  deployed artifact.
- **FR-018**: The board MUST make zero network requests. No CDN, no web fonts, no analytics — it
  must work offline and must not repeat the Tailwind-CDN tech debt already tracked for Identity
  Server (US-1.4).
- **FR-019**: The board MUST NOT create, delete, reorder, or re-word Epics, Stories, or Tasks.
  Toggling a checkbox is the only mutation it is permitted to make.

### Key Entities

- **Board Document**: the parsed representation of one `TASKS.md`, holding the original text
  verbatim, the ordered Epic list, and any parse diagnostics.
- **Epic**: number, title, anchor slug, derived priority, branch/depends-on metadata, direct tasks,
  and ordered stories.
- **User Story**: ID (`US-1.1`), title, persona line, ordered tasks, and a *derived* status.
- **Task**: display text, done flag, and — critically — its exact source coordinates (line index
  and marker character offset) that make a surgical write possible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every Epic, User Story, and Task present in `docs/TASKS.md` appears on the board —
  100% round-trip, zero dropped items, verified by count against the file.
- **SC-002**: Toggling any single checkbox produces a `git diff` of **exactly one changed line**.
- **SC-003**: A full parse-and-render of the current ~850-line file completes in under 1 second on
  the developer's machine.
- **SC-004**: The project owner can determine "what is in progress right now" in under 5 seconds of
  looking at the board, without scrolling markdown.
- **SC-005**: Adding a new Epic or Story to `TASKS.md` in the existing format requires **zero**
  changes to the board code — it appears on next reload.
- **SC-006**: The board loads and functions with the network disconnected.

## Assumptions

- Single user, single machine, local development only. No multi-user concurrency, no auth, no
  hosting, no server-side component.
- The user's browser is Chromium-based (Chrome/Edge), consistent with the existing
  Claude-in-Chrome workflow. Non-Chromium browsers get read-only degradation (FR-016), not parity.
- `docs/TASKS.md` keeps its current structural conventions; per the governing principle, changing
  those conventions is a **new spec**, not a bug in this one.
- The two checkbox states are the entire status vocabulary. "In Progress" is *derived* from partial
  completion — the file has no explicit in-progress marker, and this feature does not add one.
- Priority lives on Phases in the Roadmap table and is *inherited* by Epics and their Stories.
  Per-story priority does not exist in the file and will not be invented by the board.
- Drag-and-drop between columns is explicitly **out of scope**: column membership is derived from
  task completion (FR-007), so dragging a card would have no representable meaning in the file.
