# Phase 1 — Data Model: TASKS.md Kanban Board

**Feature**: `001-tasks-board` | **Date**: 2026-07-27

There is **no database**. The "data model" here is the in-memory shape produced by
`parseTasksMd(text)` and consumed by the renderer. It exists for exactly one page-lifetime.

The single most important field in this whole document is `Task.lineIndex` / `Task.markerOffset` —
the source coordinates that let a toggle rewrite one character instead of regenerating a file.

---

## Entity: `BoardDocument`

The root. One per loaded file.

| Field | Type | Notes |
|---|---|---|
| `sourceLines` | `string[]` | The file split **with separators retained** — `sourceLines.join('')` must be byte-identical to the input. This is the write substrate |
| `originalText` | `string` | The exact text parsed, kept for the compare-before-write check (FR-013) |
| `epics` | `Epic[]` | Document order |
| `diagnostics` | `Diagnostic[]` | Anything unrecognised — surfaced in the UI, never silently dropped (FR-005) |
| `stats` | `{ total, done }` | Task counts across the whole document |

**Invariants**

- `sourceLines.join('') === originalText` — always. Violating this means a write would corrupt the
  file, so it is the first self-test assertion.
- `epics` may be empty (a file with no Backlog section) — that is a diagnostic, not a crash.

---

## Entity: `Epic`

From `### Epic N — Title`.

| Field | Type | Notes |
|---|---|---|
| `number` | `number` | `0`–`10` today |
| `title` | `string` | e.g. `Identity Service` |
| `anchor` | `string` | Slug matching the Roadmap link target, e.g. `epic-1--identity-service`. This is the join key to the Roadmap table |
| `priority` | `'🔴' \| '🟡' \| '🟢' \| null` | **Derived** from the Roadmap row that links to `anchor`. `null` when no Roadmap row references it |
| `phase` | `string \| null` | e.g. `1 — Identity`, from the same Roadmap row |
| `branch` | `string \| null` | From the `**Branch:** \`identity\`` metadata line |
| `dependsOn` | `string \| null` | From the same metadata line |
| `directTasks` | `Task[]` | Tasks appearing under the Epic **before** any `#### US-` heading (Epic 0 only, today) — FR-004 |
| `stories` | `Story[]` | Document order |
| `lineIndex` | `number` | Heading location, for source navigation |

**Derivation of `priority`** — the only cross-referencing step in the parser:

1. Parse the Roadmap table; each row yields `{ phase, priority, epicAnchors[] }` by extracting
   every `[E<n>](#anchor)` link from the *Epics* cell.
2. Build `anchor → { phase, priority }`.
3. When an Epic heading is parsed, compute its own anchor with the same GitHub slug rules and look
   it up.

Anchor slug rule (must match GitHub's, since the file's own links rely on it): lowercase, strip
characters other than alphanumerics/spaces/hyphens, replace spaces with `-`. Note that `—` (em
dash) is *stripped*, which is why `### Epic 1 — Identity Service` yields
`epic-1--identity-service` with a **double** hyphen. The parser must reproduce this exactly or
every priority silently comes back `null`.

**Invariant**: an Epic with no Roadmap row still renders — priority is decoration, not structure.

---

## Entity: `Story`

From `#### US-N.M — Title`.

| Field | Type | Notes |
|---|---|---|
| `id` | `string` | `US-1.1` |
| `title` | `string` | `Localized registration` |
| `persona` | `string \| null` | The `*As a Customer, I want …*` italic text following the heading — **may span multiple source lines** (every persona in the real file wraps this way; a single-line-only capture returns `null` for 100% of stories, a real bug caught only by testing against production data) |
| `epicNumber` | `number` | Parent |
| `tasks` | `Task[]` | Document order |
| `lineIndex` | `number` | Heading location |
| `status` | *derived* | See below — computed, never stored |

**Derived `status`** (FR-007) — the rule the entire board layout rests on:

```text
doneCount === 0                      → 'todo'
doneCount === tasks.length && n > 0  → 'done'
otherwise                            → 'in-progress'
```

Where `n === tasks.length`. The `n > 0` guard is load-bearing: a story with **zero** tasks lands in
**To Do**, not Done. Without the guard, `0 === 0` would silently mark every unwritten stub as
complete — the most dangerous defect this feature could ship, because it would look like progress.

**Derived `progress`**: `{ done, total }`, rendered as `n/m`.

---

## Entity: `Task`

From a `- [ ]` / `- [x]` list item outside any fenced code block.

| Field | Type | Notes |
|---|---|---|
| `text` | `string` | Display text, with continuation lines folded into one string (whitespace-collapsed) — **display only, never written back** |
| `done` | `boolean` | `true` when the marker character is anything other than a space |
| `lineIndex` | `number` | 0-based index into `BoardDocument.sourceLines` |
| `markerOffset` | `number` | 0-based char index of the marker character *within that line* — i.e. the position between `[` and `]`. Currently always `3`, but **measured, not assumed** (FR-005) |
| `continuationLineCount` | `number` | Indented lines folded into `text`. Recorded so the renderer can show full text; those lines are never modified |
| `ownerKind` | `'story' \| 'epic'` | `'epic'` for Epic 0's direct tasks |

**Invariants**

- `sourceLines[lineIndex][markerOffset]` is the marker character — asserted at parse time. If it
  isn't, the task is emitted as a diagnostic and marked non-toggleable rather than risking a write
  at the wrong offset.
- `text` has **no** write path. Editing task wording is out of scope (FR-019); only
  `sourceLines[lineIndex][markerOffset]` is ever mutated.

---

## Entity: `Diagnostic`

| Field | Type | Notes |
|---|---|---|
| `severity` | `'warning' \| 'error'` | |
| `lineIndex` | `number \| null` | |
| `message` | `string` | e.g. `Duplicate story id US-2.3`, `Checkbox marker not found at expected offset` |

Rendered in a collapsible panel. Its presence is the mechanism behind FR-005: the board is tolerant
of an evolving file, but never *quietly* tolerant.

---

## State transitions

Tasks have exactly two states, and the transition is symmetric:

```text
        click checkbox
 [ ]  ─────────────────►  [x]
      ◄─────────────────
        click checkbox
```

Story status is not a state machine — it is a pure function of its tasks, recomputed after every
toggle. A card therefore *moves between columns as a consequence* of task completion. It cannot be
moved directly, which is exactly why drag-and-drop is out of scope: there is no file mutation that
"drag a card to In Progress" could correspond to.

---

## Worked example (real data from the current file)

`docs/TASKS.md` line 76 (1-based) is:

```text
- [ ] Add missing keys to `Captions.resx` + `Captions.uk-UA.resx` (files already touched — finish the job)
```

Parsed as:

```js
{
  text: "Add missing keys to `Captions.resx` + `Captions.uk-UA.resx` (files already touched — finish the job)",
  done: false,
  lineIndex: 75,          // 0-based
  markerOffset: 3,
  continuationLineCount: 0,
  ownerKind: 'story'      // belongs to US-1.1
}
```

Toggling it changes `sourceLines[75]` from `- [ ] Add missing…` to `- [x] Add missing…` — one
character, in one line, in a 37 211-byte string that is otherwise untouched. US-1.1's status then
recomputes from `todo` to `in-progress` (1 of 2 done) and the card moves column without a reload.
