# Eventify Backlog Board

A local, zero-dependency Kanban board over `docs/TASKS.md`. See
`specs/001-tasks-board/spec.md` for the full feature specification.

`docs/TASKS.md` is the single source of truth. This board conforms to the file's format;
the file never conforms to the board. See `specs/001-tasks-board/contracts/tasks-md-grammar.md`
before changing the file's structure.

## Opening it

Try `file://` first — no server needed:

```powershell
start file:///D:/Programming/Eventify/tools/tasks-board/index.html
```

If the file picker fails to open (some Chrome builds restrict `file://` origins from using
`showOpenFilePicker` / IndexedDB), fall back to a local static server:

```powershell
npx --yes serve tools/tasks-board -l 4321
# then open http://localhost:4321
```

**Verified working via: `http://localhost` (automated, this session).** The board's parser,
renderer, filters, self-test suite, and write logic were all exercised against the real
`docs/TASKS.md` served over `http://localhost:4322` — see `specs/001-tasks-board/tasks.md` T034/
T047-T049 for what was checked.

**`file://` itself: not automatically verified — please do a 10-second manual check.** The browser
automation used to build this tool refuses to navigate `file://` URLs at all (a restriction of that
tooling, not necessarily of Chrome or the File System Access API), so this specific path could not
be exercised end-to-end here. To confirm:

```powershell
start file:///D:/Programming/Eventify/tools/tasks-board/index.html
```

Click **Open TASKS.md** and pick `docs/TASKS.md`. If the picker opens and the board renders, `file://`
works and you never need a server. If the button does nothing or errors, use the `npx serve`
fallback above — the code itself is already proven correct against real data either way.

## Build isolation

This tool is intentionally outside every build:

- `Eventify.slnx` has no `<Project>` or `<File>` entry for anything under `tools/`. (The
  `specs/001-tasks-board/*.md` entries visible in `Eventify.slnx` are IDE-only Solution Items —
  the same mechanism that already lists `docs/TASKS.md` — and are never fed to `dotnet build`.)
- `Directory.Build.props` sets only `TargetFramework`/`Nullable`/etc. on `.csproj` files it is
  imported into; it does not glob arbitrary directories, so `tools/` is unaffected.
- `src/Web/EventifySpa`'s Vite root is that directory itself; `tools/` sits outside it and is
  never bundled.

## Self-test

```text
file:///D:/Programming/Eventify/tools/tasks-board/index.html?selftest=1
```

Runs the parser and toggle assertions against inline fixtures with no test runner and no
dependency.
