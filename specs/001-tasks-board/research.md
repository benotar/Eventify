# Phase 0 — Research: TASKS.md Kanban Board

**Feature**: `001-tasks-board` | **Date**: 2026-07-27

## 0. Measured baseline of the target file

Everything below was measured against `docs/TASKS.md` as of 2026-07-27, not assumed:

| Property | Measured value | Consequence for the design |
|---|---|---|
| Size | 37 211 bytes | Trivially small — parse the whole file in memory, no streaming |
| Lines | 855 | Full parse is O(n) over 855 lines; SC-003 (<1s) is not at risk |
| Line endings | **CRLF on all 855 lines** | CRLF preservation is a *confirmed* requirement, not hypothetical (FR-014) |
| BOM | **None** (`23 20 45` = `# E`) | No BOM round-trip problem — but read/write must not *introduce* one |
| Final newline | Present | Must be preserved |
| Checkboxes | **104** — 101 `- [ ]` + 3 `- [x]` | Small enough to render all cards at once, no virtualisation |
| Checkbox indentation | **All at column 0**, form `- [ ]` | Marker char is at index 3 today — but store the *measured* offset (FR-005) |
| Epic headings (`### Epic`) | 11 (Epic 0 … Epic 10) | |
| Story headings (`#### US-`) | 45 | 45 cards across 3 columns |
| Fenced blocks | 70 fence lines = **35 ```gherkin blocks**, all at column 0 | Fence-skipping is mandatory, and a column-0 fence check is sufficient |
| Continuation lines | 6-space indented (10 occurrences) | Multi-line task text is real and must be handled |

**Why this mattered**: two of these (CRLF everywhere, 35 fenced blocks) turn "nice-to-have
robustness" into hard requirements.

### ⚠️ A trap that makes CRLF harder, not easier, to get right

The repository is configured `core.autocrlf=true` with **no `.gitattributes`** (verified). Git
therefore stores LF in the index and converts to CRLF in the working tree — and, critically,
**`git diff` normalises line endings before comparing.**

Consequence: if the board wrote the file back with LF endings, **`git diff` would still show a
clean one-line change.** The regression would be invisible to the very check meant to catch it,
while the working-tree file quietly diverged from every other file in the checkout — until Git
"touched" it again and rewrote the endings, producing churn with no author.

So CRLF preservation cannot be verified through `git diff`. It must be asserted at the byte level
(`quickstart.md` step 3b) and in the self-test. This is exactly the class of assumption that would
have shipped silently had the file not been measured.

**Note on the file's current state**: `docs/TASKS.md` is staged but not yet committed (`git status`
shows `AM`), so `git diff` compares against the staged copy. The verification steps work the same
way, but the file should ideally be committed before relying on them.

---

## 1. How does the board write to a file the user picked?

**Decision**: **File System Access API** — `window.showOpenFilePicker()` → `FileSystemFileHandle`
→ `handle.createWritable()`.

**Rationale**: It is the only browser API that grants persistent read **and write** access to a
user-chosen file on disk with no server. That is precisely the shape of this problem: one local
user, one local file, no hosting.

**Confirmed API behaviour** (stable Chromium, documented):

- `showOpenFilePicker()` is Chromium-only (Chrome/Edge 86+). Firefox and Safari do not implement
  it — hence the read-only degradation in FR-016.
- It requires a **secure context** and **transient user activation** (must run inside a click
  handler; cannot be called on page load).
- `FileSystemFileHandle` is structured-cloneable, so it can be stored in IndexedDB and retrieved on
  a later visit — this is what makes US-5 possible.
- Permission does **not** survive a page reload: a restored handle reports
  `queryPermission({mode:'readwrite'}) === 'prompt'`, and `requestPermission()` must again be
  called from a user gesture. So US-5 is "one click to re-grant", never "zero clicks".
- `createWritable()` **truncates the file by default**. The board therefore always writes the full
  document text — the "surgical edit" of FR-012 happens in the in-memory string, not via a partial
  file write.

**Alternatives considered**:

- *`<input type="file">` + download-a-copy* — read works, but "saving" means dropping a new file in
  Downloads that the user must move over the original by hand. Fails US-2 outright.
- *Small local Node/.NET server exposing read/write endpoints* — works everywhere, but adds a
  process to start, a project to maintain, and a port to remember, for a personal tool. Rejected as
  over-engineering; kept in reserve if the `file://` question below goes badly.
- *Electron/Tauri wrapper* — vastly disproportionate.

---

## 2. Will it work when opened as `file://index.html`? ⚠️ Unverified

**Status**: **Open question — must be verified by the user in one minute, not guessed at.**

**What is certain**: the API works from `https://` and from `http://localhost`.

**What is not certain**: whether `showOpenFilePicker()` and, separately, **IndexedDB** are
available to a page loaded from a `file://` URL in the user's Chrome build. `file://` pages get an
opaque-ish origin, and storage APIs have historically been restricted there. I am not going to
assert either way — the honest answer is that it depends on the browser build and must be tested.

**Decision**: design so that **the answer doesn't block the feature**.

- The board detects capability at startup (`'showOpenFilePicker' in window`, and a guarded
  IndexedDB open) and tells the user plainly which mode it is in.
- **Primary path**: try `file:///D:/Programming/Eventify/tools/tasks-board/index.html` first. If
  the picker opens and a write succeeds, done — zero infrastructure, exactly as scoped.
- **Guaranteed fallback**, if `file://` is restricted: serve the folder over `http://localhost`,
  which is unambiguously a secure context:

  ```powershell
  npx --yes serve tools/tasks-board -l 4321   # or: python -m http.server 4321 -d tools/tasks-board
  ```

  Still zero build, still one HTML file, one command.
- If IndexedDB is unavailable, US-5 (remembered file) silently degrades to "pick the file each
  time". US-1–US-4 are unaffected.

**Verification step** (belongs in `quickstart.md`, step 1): open the file, click *Open TASKS.md*,
toggle one checkbox, run `git diff docs/TASKS.md`, expect exactly one changed line.

---

## 3. How is a checkbox toggled without disturbing the rest of the file?

**Decision**: **Surgical single-character replacement on a line-indexed snapshot.** Never
re-serialise the document from the parsed model.

Mechanism:

1. Read bytes → decode once to a string → split into lines **keeping the separators**
   (split on `/(?<=\n)/`, so each element retains its own `\r\n`). This makes CRLF preservation
   automatic rather than something to remember.
2. During parsing, each Task records `lineIndex` and `markerOffset` — the *measured* index of the
   character between `[` and `]` on that line.
3. Toggling replaces exactly that one character (`' '` ↔ `'x'`) in that one line string.
4. `lines.join('')` reconstructs the document — byte-identical everywhere except that character.
5. Encode with `TextEncoder` (UTF-8, adds no BOM) and write.

**Rationale**: This is the only approach that makes FR-012, FR-014 and SC-002 structurally
guaranteed rather than carefully maintained. Tables, gherkin fences, blockquotes, the Decision log,
trailing whitespace, and the 6-space continuation lines survive because **the code never touches
them** — not because the serialiser was written to reproduce them faithfully.

**Alternatives considered**:

- *Parse to AST → mutate → re-serialise via a markdown library* — the standard approach, and wrong
  here. It requires a dependency (violating the zero-build, zero-network constraint), and every
  round-trip risks reformatting tables, emoji, and the em-dashes used throughout the file.
- *Regex replace on the whole document text* — fails on repeated task text (several stories share
  wording like "VSA skeleton"); line coordinates are unambiguous where text is not.

---

## 4. How does the board avoid overwriting external edits?

**Decision**: **Compare-before-write.** On every toggle, re-read the file through the handle and
compare against the snapshot string parsed at load time. Identical → write. Different → abort,
warn, offer reload.

**Rationale**: The user explicitly intends to keep editing `TASKS.md` by hand — that is stated in
the feature request itself. A last-writer-wins tool would eventually eat a hand-written edit, and
the first time it did, the tool would be abandoned. A full-string comparison of a 37 KB file is
sub-millisecond; there is no reason to use anything weaker.

**Alternatives considered**:

- *`File.lastModified` timestamp comparison* — cheaper, but coarse (1 ms granularity) and lies when
  a tool rewrites the file with identical content. Comparing content answers the actual question.
- *A hash* — same guarantee as string comparison, plus code, for a 37 KB file. Pointless here.
- *`FileSystemObserver` / polling for live reload* — genuinely nicer UX, but adds a moving part.
  Deferred; the compare-before-write check already prevents *loss*, which is the requirement.

---

## 5. Framework, styling and fonts

**Decision**: **One self-contained `index.html`** — vanilla ES2022, no framework, no bundler, no
package.json; hand-written CSS using the project's design tokens; **system font stack only**.

**Rationale**:

- The UI is one screen with ~45 cards and ~104 checkboxes. React would add a build step and a
  `node_modules` for a tool whose entire appeal is "double-click and it opens". The project's
  React/Tailwind conventions in `CLAUDE.md` scope to `src/Web/EventifySpa`; this is not that.
- FR-018 forbids network requests, which rules out Google Fonts. **JetBrains Mono and Plus Jakarta
  Sans are therefore not loaded** — the board uses
  `ui-monospace, "JetBrains Mono", Consolas, monospace` and
  `system-ui, "Plus Jakarta Sans", sans-serif`, so the real faces are used *if already installed
  locally* and degrade cleanly otherwise. This is a deliberate, documented deviation from the
  design system, made because an offline local tool must not repeat the Tailwind-CDN debt that
  US-1.4 exists to pay off.
- Design tokens are copied as literal CSS custom properties (`--bg: #08080F`, `--brand: #6366F1`,
  `--brand-2: #8B5CF6`, glass surfaces at `rgba(255,255,255,0.055)`), so the board reads as part of
  the same product.

**Alternatives considered**:

- *A route inside `EventifySpa`* — would inherit the design system for free, but needs a backend
  endpoint to read/write a file on disk, i.e. an API surface built solely to serve a dev tool.
  Rejected as the tail wagging the dog.
- *Publishing as a shareable artifact* — cannot reach the local file; read-only only. Rejected
  against US-2.

---

## 6. How is a zero-build, zero-dependency tool tested?

**Decision**: A built-in **self-test mode** (`index.html?selftest=1`) that runs assertions against
inline fixture strings and renders a pass/fail list — plus the manual `quickstart.md` checks.

**Rationale**: The risk in this feature is concentrated almost entirely in two pure functions:
`parse(text)` and `toggle(lines, lineIndex, markerOffset)`. Both are pure string-in/string-out, so
they are testable without a DOM, a runner, or a network. Fixtures cover the cases that actually
bite: a checkbox inside a ```gherkin fence, a 6-space continuation line, CRLF round-trip, Epic 0's
story-less tasks, a zero-task story, and `[X]`/`[-]` markers.

The decisive assertion is the round-trip one: **parse → toggle → reassemble must differ from the
input by exactly one character.** That single test protects SC-002 directly.

This matches the project's stated testing discipline (code first, tests after — but real tests, not
decorative ones) without dragging Vitest, a `package.json`, and a `node_modules` into a tool whose
premise is having none of those.

**Alternatives considered**:

- *Vitest/Jest* — the right answer for a real app; here it would be the only build tooling in the
  feature, imported to test 200 lines of parsing.
- *Playwright E2E* — cannot drive the native file picker (by security design). Would test
  everything except the interesting part.
- *No tests* — unacceptable for the write path: the failure mode is corrupting the backlog file.

---

## Resolved NEEDS CLARIFICATION

| # | Question | Resolution |
|---|---|---|
| 1 | Write-back or read-only? | Write-back to `docs/TASKS.md` — user-selected |
| 2 | Hosting/stack? | Single static HTML + File System Access API — user-selected |
| 3 | Layout? | Azure-DevOps-style Kanban columns — user-selected |
| 4 | `file://` viability | **Deferred to first-run verification**, with an `http://localhost` fallback that removes the risk (§2) |
| 5 | Line-ending handling | Confirmed CRLF throughout; preserved by splitting with separators retained (§0, §3) |
| 6 | "In Progress" semantics | Derived from partial task completion; the file has no such marker and none is added |
| 7 | Fonts under a no-network rule | System stack with local-font fallback; documented deviation (§5) |
