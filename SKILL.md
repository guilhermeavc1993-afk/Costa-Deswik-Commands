---
name: command-1-resource-path-priorities
description: "Command 1 - Resource Path + Priorities" for a Deswik IS schedule. Use when the user says "run Command 1" or asks to rebuild loader or drilling resource paths and priorities so they match the current IS sequence. Works through the Deswik Claude Assistant plugin (named pipe).
---

# Command 1: Resource Path + Priorities

Makes the IS resource paths and priorities match the sequence IS has right now. It never moves dates, never changes dependencies and never changes resource assignments.

## Why
In CAD, with the interactive filters on the task solids, the planner clicks solids into a resource path, and that order sets each task's priority. In IS, they then filter the activities, sort by priority, and link the tasks. Tasks linked by hand without the path end up at priority 0 or out of order. This command fixes that.

## Scope
- **"Command 1"** / **"Command 1 loaders"**: `build_lists.ps1 -Mode Loaders -Loaders LOADER_1,LOADER_2`. Each path gets the tasks physically on that loader, in IS start order.
- **"Command 1 drilling"**: `-Mode Drilling -DrillPath DRILL_1 [-ExcludePaths OTHER_DRILL]`. The drill path gets every Pre-Split and Production task: shots in IS order, and within each shot Pre-Split then Production. The shot is the task Name. Leave out any path the user wants kept as it is.
- **"Command 1 all"**: loaders, then drilling.

## Steps
1. **Before anything else**, ask the user to:
   - save a backup of the .dsf;
   - **close CAD's Resource Paths window with Cancel**. If it's open, OK or Apply writes its stale copy back, and "Auto-renumber priority" renumbers the priorities;
   - have Deswik CAD open with the schedule loaded and the plugin running.
2. `scripts/run.ps1 export` (read-only). It writes tasks, paths and a date snapshot to `%TEMP%\deswik-cmd1`.
3. `scripts/build_lists.ps1 -Mode ...`. Show the user: tasks per path, added and removed, the priority range, and how many priorities change. For drilling, also list shots with no Pre-Split → Production link. Don't fix links unless asked.
4. `scripts/run.ps1 apply`. The user approves it in Deswik. Expect `MATCH` per path, `Changed dates: 0`, `changed resource assignments: 0`, `other paths unchanged: True` and `RESULT: OK`. If it says CHECK, stop. The change is undoable in IS.
5. `scripts/run.ps1 priorities`. The user approves it in Deswik. Priorities start at the lowest value already on that path and go up by 1.
6. **Verify:**
   - run export again, then build_lists again: it should show **0 priority changes**, 0 added and 0 removed;
   - compare the newest `snapshot_*.tsv` with the first one: dates and assignments must be identical.
7. **Tell the user:** nothing is saved; check it in the Resource Paths window, then close that window with **Cancel**; nothing moves until they re-level.

## Notes
- If the plugin can't see the schedule (`NO_SCHEDULE`), it depends on which window has focus. `run.ps1` retries; if it still fails, ask the user to click on the Deswik window.
- `cs.ps1` sends a C# file to `run_csharp`; `dsw.ps1` calls any other plugin tool. Set the pipe name in both if your plugin uses a different one.
- Path edits use Deswik's internal undoable commands `UpdateManualResourcePaths` and `DeleteManualResourcePaths`, reached through `Schedule.Invoke`.
