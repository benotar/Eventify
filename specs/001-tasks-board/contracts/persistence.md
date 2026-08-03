# Contract: file access, write protocol, and handle persistence

**Feature**: `001-tasks-board` | **Date**: 2026-07-27

Covers the board's two side-effecting surfaces: the file it writes, and the one handle it caches.

---

## 1. Capability detection (startup)

Runs before anything else and drives which mode the UI announces:

| Check | Consequence when false |
|---|---|
| `'showOpenFilePicker' in window` | **Read-only mode.** Banner: "This browser can't write files — open in Chrome or Edge." File may still be loaded via `<input type="file">` for viewing (FR-016) |
| IndexedDB opens successfully | US-5 disabled — the file must be picked each session. Everything else works |

The board must **state its mode plainly**. Silently accepting a toggle that cannot be persisted is
the one failure this contract exists to prevent.

---

## 2. Opening the file

```js
const [handle] = await window.showOpenFilePicker({
  types: [{ description: 'Markdown', accept: { 'text/markdown': ['.md'] } }],
  multiple: false,
});
```

- **Must** be called inside a click handler — the API requires transient user activation.
- Read as bytes, then decode explicitly:

  ```js
  const buf  = await (await handle.getFile()).arrayBuffer();
  const text = new TextDecoder('utf-8', { ignoreBOM: true }).decode(buf);
  ```

  `ignoreBOM: true` is deliberate: the default decoder *strips* a leading BOM, which would silently
  drop it on write-back and produce a whole-file diff. `docs/TASKS.md` has no BOM today, so this is
  insurance rather than a fix — cheap insurance, and the failure it prevents is invisible until
  `git diff` explodes.

- Split retaining separators, so CRLF handling needs no special code anywhere else:

  ```js
  const lines = text.split(/(?<=\n)/);   // lines.join('') === text
  ```

---

## 3. Permission model

| Moment | Call | Notes |
|---|---|---|
| After picking | `handle.queryPermission({ mode: 'readwrite' })` | Usually already `granted` for a freshly picked file |
| Before first write, if not granted | `handle.requestPermission({ mode: 'readwrite' })` | Requires a user gesture — trigger it from the checkbox click itself |
| After page reload with a restored handle | `queryPermission` → `'prompt'` | **Permission never survives a reload.** US-5 is therefore "one click to re-grant", never zero |
| Denied | — | Switch to read-only mode and say so |

---

## 4. Write protocol — the critical path

Every toggle follows this sequence, in this order. Steps 2–3 are the whole point:

```text
1. Compute        newLines = toggleTaskInLines(sourceLines, lineIndex, markerOffset)
2. Re-read        currentText = decode(await handle.getFile())
3. Compare        currentText !== originalText  →  ABORT, warn, offer reload   (FR-013)
4. Assemble       newText = newLines.join('')
5. Assert         newText differs from originalText by exactly one character   (dev guard)
6. Write          const w = await handle.createWritable();
                  await w.write(new TextEncoder().encode(newText));
                  await w.close();
7. Commit         originalText = newText; sourceLines = newLines
8. Re-render      recompute story status; move the card if needed             (FR-015)
9. On failure     revert UI, show the error                                    (FR-015)
```

**Why re-read on every toggle rather than once per session**: the user is expected to keep editing
`TASKS.md` in their IDE — that is stated in the feature request. A 37 KB read is sub-millisecond;
there is no budget argument for skipping it, and skipping it is how a hand-written edit gets eaten.

**Why `createWritable()` still writes the whole document**: the API truncates on open, so a partial
write is not available. The *surgical* part happens in memory (step 1) — the file is rewritten in
full, byte-identical except one character. This is not a contradiction of FR-012; FR-012 constrains
how `newText` is produced, not how many bytes reach the disk.

**Ordering note**: the UI updates only after `close()` resolves (step 8). Optimistic UI is
explicitly rejected here — showing a task as done when the write failed is precisely the
"board and file disagree" state this feature exists to eliminate.

---

## 5. Handle persistence (US-5, P3)

```text
Database:     eventify-tasks-board   (version 1)
Object store: handles                (keyPath: 'id')
Record:       { id: 'tasks-md', handle: FileSystemFileHandle, savedAt: <ISO string> }
```

- `FileSystemFileHandle` is structured-cloneable, so it stores directly — no serialisation.
- On load: read the record → if present, show *"Reopen docs/TASKS.md"* → on click,
  `requestPermission` → read → parse.
- The handle exposes only `handle.name` (`TASKS.md`), not a full path, so the UI cannot display
  where the file lives. Show the name and let the user re-pick if it is the wrong one.
- If IndexedDB is unavailable (a live possibility under `file://` — see research §2), catch and
  continue without it. **US-5 degrading must never block US-1–US-4.**

---

## 6. What is never persisted

- No task/story/epic state outside `docs/TASKS.md` — that would create the second source of truth
  the governing principle forbids.
- No `localStorage` mirror of checkbox state.
- No cache of parsed output — parsing 855 lines is ~milliseconds; caching would only create
  staleness bugs.

UI-only preferences (selected filter, expanded cards) live in memory and are intentionally lost on
reload. They are not worth a storage dependency.

---

## 7. Error surface

| Condition | UI behaviour |
|---|---|
| Picker cancelled | Nothing. Not an error |
| Permission denied | Read-only banner; toggles disabled with a tooltip explaining why |
| External modification detected (step 3) | Warning bar: "TASKS.md changed on disk. Reload to continue." Toggle discarded, not queued |
| Write threw | Revert the checkbox, show the error text verbatim. Never swallow it |
| Parse produced diagnostics | Collapsible panel with a count badge; the board still renders |
| `newText` differs by more than one character (step 5) | **Abort the write** and report a bug. A failed invariant means the parser is wrong about coordinates — writing anyway risks the backlog file |
