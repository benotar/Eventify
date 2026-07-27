# Quickstart — TASKS.md Kanban Board

**Feature**: `001-tasks-board` | **Date**: 2026-07-27

How to run the board and prove it works. This is a **validation guide**, not an implementation
guide — the design lives in [plan.md](./plan.md), [data-model.md](./data-model.md), and
[contracts/](./contracts/).

## Prerequisites

- Chrome or Edge (Chromium 86+). Firefox and Safari render read-only by design.
- A clean working tree for `docs/TASKS.md` — the write tests are verified with `git diff`, so start
  from no pending changes:

  ```powershell
  git status --short docs/TASKS.md   # expect empty output
  ```

- No install step. No `npm install`, no build, no server (unless step 1 says otherwise).

---

## Step 1 — Open it, and settle the `file://` question

This is the one genuinely unverified assumption in the plan
([research.md](./research.md) §2), so resolve it first — it takes under a minute.

**Try `file://` first:**

```powershell
start file:///D:/Programming/Eventify/tools/tasks-board/index.html
```

Click **Open TASKS.md** and pick `docs/TASKS.md`.

- **Picker opens** → `file://` works. Continue to step 2; you never need a server.
- **Picker throws / button reports an unsupported context** → use the fallback:

  ```powershell
  npx --yes serve tools/tasks-board -l 4321
  # then open http://localhost:4321
  ```

  `http://localhost` is unambiguously a secure context. Still no build, still one HTML file.

Record whichever worked in `tools/tasks-board/README.md` so the question is answered once.

---

## Step 2 — Verify the board matches the file (US-1)

The board's header should show counts. Check them against the file itself:

```powershell
cd D:\Programming\Eventify
"epics:      $((Select-String -Path docs\TASKS.md -Pattern '^### Epic').Count)"
"stories:    $((Select-String -Path docs\TASKS.md -Pattern '^#### US-').Count)"
"tasks:      $((Select-String -Path docs\TASKS.md -Pattern '^\s*- \[.\]').Count)"
"done tasks: $((Select-String -Path docs\TASKS.md -Pattern '^\s*- \[[^ ]\]').Count)"
```

**Expected as of 2026-07-27**: `epics: 11`, `stories: 45`, `tasks: 104`, `done tasks: 3`.

Every number must match the board exactly (SC-001). A mismatch means items were dropped — check
the diagnostics panel before anything else.

Then spot-check the three column rules:

| Check | Expected |
|---|---|
| US-1.1 (both tasks unchecked) | **To Do**, `0/2` |
| Epic 0's three direct tasks (all `[x]`) | **Done** — and present at all, not swallowed for lacking a story heading |
| US-4.1 (persona only, no checkbox lines) | **To Do**, `0/0` — *not* Done |
| Any `- [ ]` text inside a ```gherkin block | **Absent** from the board — there are 35 such blocks |

That last row is the fence-skipping check; if phantom tasks appear, the parser is reading inside
code fences.

---

## Step 3 — The decisive test: one toggle = one line (US-2, SC-002)

The single most important verification in this feature.

1. On the board, tick the first task of **US-1.1** (`Add missing keys to Captions.resx…`).
2. Then:

```powershell
git diff --stat docs/TASKS.md
git diff docs/TASKS.md
```

**Expected**: `1 file changed, 1 insertion(+), 1 deletion(-)` and a diff whose only change is
`- [ ]` → `- [x]` on that one line.

**Fail conditions** — each points at a specific defect:

| Symptom | Root cause |
|---|---|
| Whole file reformatted | The document was re-serialised from the parsed model instead of edited in place (FR-012 violated) |
| A different line changed | `markerOffset` / `lineIndex` are wrong — the coordinate assertion in [persistence.md](./contracts/persistence.md) §4 step 5 should have caught this |
| No change at all | The write silently failed; FR-015 requires it to be reported |

Then confirm symmetry — untick the same task and verify the diff is empty:

```powershell
git diff --stat docs/TASKS.md    # expect no output
```

Restore if anything went sideways:

```powershell
git checkout -- docs/TASKS.md
```

---

## Step 3b — Line endings, checked at the byte level (FR-014)

**`git diff` cannot catch this one.** The repo is `core.autocrlf=true` with no `.gitattributes`, so
Git normalises line endings before comparing — a board that wrote LF instead of CRLF would still
produce a clean one-line diff while silently rewriting all 855 line terminators in the working tree
(see [research.md](./research.md) §0).

So check the bytes directly, immediately after a toggle:

```powershell
$bytes = [System.IO.File]::ReadAllBytes('D:\Programming\Eventify\docs\TASKS.md')
$text  = [System.Text.Encoding]::UTF8.GetString($bytes)
"CRLF count : $([regex]::Matches($text, \"`r`n\").Count)"      # expect 855
"bare LF    : $([regex]::Matches($text, \"(?<!`r)`n\").Count)"  # expect 0
"BOM present: $($bytes[0] -eq 0xEF)"                            # expect False
```

`bare LF` above zero means the write path normalised line endings — FR-014 violated, invisible to
`git diff`.

---

## Step 4 — External-edit safety (US-3)

1. With the board open and loaded, edit `docs/TASKS.md` in your IDE (add a blank line) and save.
2. Back on the board, toggle any checkbox.

**Expected**: a warning that the file changed on disk, **no write**, and an offer to reload.
Choosing reload re-parses and discards the toggle.

**Fail condition**: the toggle writes anyway — your IDE edit is gone. This is the failure mode most
worth catching, because it destroys hand-written work silently.

```powershell
git checkout -- docs/TASKS.md
```

---

## Step 5 — Filters and card detail (US-4)

- Filter to **Epic 1** → only Epic 1 cards remain across all three columns; header counts follow
  the filtered set.
- Filter by priority **🔴** → only Epics reached from 🔴 Roadmap rows (Phases 0–3 → E0, E1, E2, E3).
- Expand a card → persona line and full task checklist appear.
- Confirm each card shows its priority chip. **If every chip is missing, the anchor slug derivation
  is wrong** — see the double-hyphen worked example in
  [tasks-md-grammar.md](./contracts/tasks-md-grammar.md) §2.

---

## Step 6 — Remembered file (US-5, optional)

Reload the page. Expect a **Reopen docs/TASKS.md** button; one click re-grants permission and
reloads the board.

If the button is absent, IndexedDB is unavailable in this context — acceptable degradation, not a
bug (see [persistence.md](./contracts/persistence.md) §5).

---

## Step 7 — Automated self-test

```text
file:///D:/Programming/Eventify/tools/tasks-board/index.html?selftest=1
```

Runs the parser and toggle assertions over inline fixtures and prints a pass/fail list. Expected:
all green, covering at minimum —

- `lines.join('') === originalText` round-trip
- CRLF preserved through parse → toggle → reassemble
- a `- [ ]` inside a ```gherkin fence is **not** parsed as a task
- a 6-space continuation line folds into its task and is never modified
- Epic-direct tasks (no story heading) are retained
- a zero-task story derives `todo`, not `done`
- `[X]` and `[-]` parse as done without throwing
- **one toggle changes exactly one character**

---

## Step 8 — Offline and isolation guarantees

```powershell
# FR-018 / SC-006: no network references at all
Select-String -Path tools\tasks-board\index.html -Pattern 'https?://|cdn\.|fonts\.googleapis'
# expect: no matches

# FR-017: not part of any build
Select-String -Path Eventify.slnx -Pattern 'tasks-board'
# expect: no matches
```

Also disconnect the network and reload — the board must work unchanged.

---

## Definition of done

| # | Criterion | Verified by |
|---|---|---|
| SC-001 | All 11 Epics / 45 Stories / 104 Tasks on the board | Step 2 |
| SC-002 | One toggle = one changed line | Step 3 |
| FR-014 | CRLF endings and no BOM preserved | Step 3b (byte level — `git diff` is blind to this) |
| SC-003 | Parse + render under 1 s | Step 1 (observable) |
| SC-004 | "What's in progress" answerable in 5 s | Step 2 |
| SC-005 | New content needs no code change | Add a story to `TASKS.md`, reload, revert |
| SC-006 | Works offline | Step 8 |
