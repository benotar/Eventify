# Contract: the `TASKS.md` grammar the board accepts

**Feature**: `001-tasks-board` | **Date**: 2026-07-27

This is **the** contract of this feature. The board has no API, no endpoints, and no wire format —
its one external interface is the set of markdown shapes it agrees to understand in
`docs/TASKS.md`.

Per the spec's governing principle: **the file is the source of truth.** This document describes
what the parser accepts *today*. Widening it is a **new spec**, not a patch.

---

## Contract stability tiers

| Tier | Meaning | Changing it costs |
|---|---|---|
| **Structural** | The parser depends on this shape. Break it and items vanish from the board | A **new spec** (`/speckit-specify`) |
| **Enriching** | Recognised when present, gracefully absent otherwise | Nothing — board degrades cleanly |
| **Ignored** | Passes through untouched; the board neither reads nor writes it | Nothing |

---

## 1. Document skeleton — *Structural*

```text
# Eventify — Roadmap & Backlog
…
## Roadmap
<markdown table>            ← Enriching (priority source)
…
## Backlog
### Epic 0 — Foundation / BuildingBlocks
- [x] …                     ← Epic-direct tasks
### Epic 1 — Identity Service
**Branch:** `identity` · **Depends on:** E0 · …
#### US-1.1 — Localized registration
*As a Customer, I want …*
```gherkin … ```            ← Ignored (and must not be parsed)
- [ ] task
- [ ] task
      continuation line
…
## Decision log             ← Ignored
```

**Contract**: heading depth carries the hierarchy. `###` = Epic, `####` = Story, `- [ ]` = Task.
Introducing a `#####` level, or demoting Epics to `##`, breaks the parser → new spec.

---

## 2. Epic heading — *Structural*

```text
### Epic <number> — <title>
```

- Pattern: `/^### Epic (\d+)\s*[—-]\s*(.+)$/`
- Both an em dash `—` (used today) and a plain hyphen are accepted.
- `<number>` must be an integer; it is the Epic's identity.

**Anchor derivation** (needed to join with the Roadmap) — must reproduce GitHub's slug rules:

1. Lowercase the full heading text after `### `.
2. Remove every character that is not `a-z`, `0-9`, space, or `-`. **Em dashes are removed, not
   replaced.**
3. Replace spaces with `-`.

Worked example — this is the case that catches naive implementations:

```text
"Epic 1 — Identity Service"
  → "epic 1 — identity service"     (lowercase)
  → "epic 1  identity service"      (em dash removed, its two spaces remain)
  → "epic-1--identity-service"      (spaces → hyphens; note the DOUBLE hyphen)
```

This matches the link `[E1](#epic-1--identity-service)` in the file's own Roadmap table. Collapsing
the double hyphen makes every Roadmap join fail silently and every priority render as `null`.

---

## 3. Roadmap table → priority — *Enriching*

```text
| Phase | Branch/Milestone | Priority | Epics | Goal | Demo |
|---|---|---|---|---|---|
| 1 — Identity | `identity` | 🔴 | [E1](#epic-1--identity-service) | … | … |
| 5 — Ticket & Notification | … | 🟡 | [E5](#epic-5--ticket-service), [E6](#epic-6--notification-service) | … | … |
```

- Rows are read between the `## Roadmap` heading and the next `##` heading.
- Columns are located **by header name**, not by fixed index — reordering columns must not break it.
- The *Epics* cell may contain **several** anchor links; every one maps to this row's phase and
  priority (row 5 above maps two Epics).
- Priority vocabulary: `🔴` blocking, `🟡` required for the end-to-end flow, `🟢` polish.

**Degradation**: no Roadmap section, an unparseable table, or an Epic no row points at → that Epic
renders with no priority chip. Nothing else is affected.

---

## 4. Story heading — *Structural*

```text
#### US-<epic>.<seq> — <title>
```

- Pattern: `/^#### (US-\d+\.\d+)\s*[—-]\s*(.+)$/`
- The ID is used as the card label and for duplicate detection.
- A story belongs to the most recent `### Epic` heading above it.
- **Duplicate IDs** render as separate cards plus a `warning` diagnostic — never merged, never
  dropped.

---

## 5. Persona line — *Enriching*

The first line after a Story heading matching `/^\*.+\*$/` (a fully italicised line, possibly
wrapped across source lines) is captured as the story's persona and shown on card expansion.
Absent → the card simply shows no persona.

---

## 6. Task checkbox — *Structural*

```text
- [ ] <text>
- [x] <text>
```

- Pattern: `/^(\s*)([-*])\s\[([^\]])\]\s(.*)$/`
- `done` is **`marker !== ' '`** — so `[x]`, `[X]`, `[-]`, `[✓]` all count as done. Only a literal
  space means not done. This is deliberately permissive: an unrecognised marker must never crash
  the board (spec edge case), and treating "something is in the box" as done is the safer reading.
- `markerOffset` is the **measured** index of the marker character within its line. All 104
  checkboxes in the file sit at offset `3` today; the parser must not hard-code that.
- Leading whitespace is permitted (nested list items) even though none exist today.

**Ownership**: a task belongs to the most recent `#### US-` heading; if no story heading has been
seen since the current `### Epic` heading, it attaches to the **Epic directly**
(`ownerKind: 'epic'`). This is Epic 0's shape today and must not be normalised away.

### Continuation lines — *Structural*

```text
- [ ] Decide: **Option A** (implement now, needs an email channel — blocked on Notification) vs **Option B**
      (defer to Epic 6, record decision in the Decision log below)
```

A line that is indented (currently 6 spaces), non-empty, and not itself a checkbox or heading is
folded into the preceding task's display text. Ten such lines exist today.

**Write guarantee**: continuation lines are **read-only**. Only the checkbox line is ever modified,
so folding is a display concern with no write consequences.

---

## 7. Fenced code blocks — *Ignored, and mandatory to skip*

````text
```gherkin
Given …
```
````

- Any line starting with ``` at column 0 toggles fenced state.
- **Every line inside a fence is skipped entirely** — headings, checkboxes, everything.
- 35 such blocks exist today. They contain Given/When/Then text; if a future acceptance scenario
  ever contains a `- [ ]`, skipping is what stops it becoming a phantom task.

---

## 8. Explicitly ignored regions — *Ignored*

- The intro blockquote (`> As of 2026-07-27 …`)
- The `## Priority legend` section
- `**📚 Topics to know:**` paragraphs
- Horizontal rules `---`
- The `## Decision log` table
- Any `**Status:** ✅ Done.` line

These pass through the write path untouched, since the write path only ever replaces one character
on a known checkbox line.

---

## 9. Byte-level contract — *Structural*

| Property | Contract | Measured today |
|---|---|---|
| Encoding | UTF-8, decoded with `ignoreBOM: true` and re-encoded with `TextEncoder` | UTF-8, **no BOM** |
| Line endings | Preserved verbatim per line; never normalised. **Not verifiable via `git diff`** — the repo is `core.autocrlf=true`, so Git normalises before comparing. Assert at the byte level | **CRLF on all 855 lines** |
| Final newline | Preserved | Present |
| Trailing whitespace | Preserved | — |

**The governing assertion**, and the one that makes SC-002 true:

```text
For any single toggle:
  countDifferingCharacters(before, after) === 1
```

If this ever fails, the write path is broken regardless of how correct the board looks.

---

## 10. Change protocol

When `docs/TASKS.md` changes:

| Change | Action |
|---|---|
| New/renamed/deleted/reordered Epic, Story, or Task | **Nothing.** Reload the board (SC-005) |
| Task text edited, including continuation lines | **Nothing.** Reload |
| Roadmap priorities changed | **Nothing.** Reload |
| New heading level, new ID scheme, new checkbox states, per-task metadata (assignee/estimate/due date), restructured Roadmap table | **Write a new spec** — `/speckit-specify`, referencing this contract as the prior baseline |

That last row is the user's explicit instruction, encoded as a rule: the board is never patched ad
hoc to chase the file, and the file is never bent to suit the board.
