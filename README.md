# Deswik + Claude commands

Working notes, commands and scripts for using Claude (Claude Code / Claude desktop) with **Deswik CAD**, the **Interactive Scheduler (IS)**, **LHS** (Landform and Haulage) and **OPDB** (Open Pit Drill & Blast) in open-pit mine planning.

Everything here was worked out and checked in a live Deswik 2025.1 session through a Claude Assistant plugin. The plugin exposes `run_csharp`, `get_context`, `list_layers`, `is_*` schedule tools and `run_deswik_command` over a Windows named pipe. Site names, coordinates, file paths and people's names have been removed: use your own layer names and values.

All files sit in one folder. The scripts find their helpers (`_common.cs`, `cs.ps1`, `dsw.ps1`) in the same folder.

## Command 1: Resource Path + Priorities
Rebuilds IS loader or drill resource paths and priorities so they match the current IS sequence, without moving dates, links or assignments.

| File | What |
|---|---|
| `SKILL-command-1.md` | Instructions: inputs, steps, checks |
| `run.ps1` | `export` / `apply` / `priorities` steps |
| `build_lists.ps1` | Builds the new path lists and priority changes (`-Mode Loaders` or `Drilling`) |
| `export.cs` | Read-only export of tasks, paths and a date snapshot |
| `apply_paths.cs` | Sets the paths with Deswik's undoable commands and checks dates and assignments |
| `cs.ps1`, `dsw.ps1` | Send C# or a plugin tool call over the named pipe |

## Command Drill Design
Builds a full OPDB drill design from a boundary, a row-orientation line and a signed D&B proposal: stab, batter, buffer and production holes, socket circles, coverage check, gap production holes, row membership.

| File | What |
|---|---|
| `SKILL-command-drill-design.md` | Instructions: inputs, steps, lessons learned |
| `_common.cs` | CONFIG block and helpers: prepend it to every script below |
| `01_inputs.cs` | Boundary, row orientation, crest candidates, pattern templates (read-only) |
| `02_design_lines.cs` | STAB / BATTER / BUFFER lines and PRODS polygon |
| `03_wall_rows.cs` | OPDB trim-holes-along-polyline for the wall rows, plus stab toe override |
| `04_production.cs` | OPDB holes-within-boundary: grid along the row orientation, staggered left |
| `05_sockets.cs` | Socket circles (half spacing) per template, via the intersection markers tool |
| `06_coverage.cs` | Uncovered floor by distance from the crest (read-only) |
| `07_gap_holes.cs` | Gap production holes: largest gap first, ≤ 50% socket overlap, hazard check |
| `08_row_end_holes.cs` | In-line gap holes at the boundary end of chosen rows |
| `09_rows_snapshot.cs` / `11_verify_rows.cs` | Snapshot and check hole IDs and row numbers |
| `10_add_gap_holes_to_rows.cs` | Add row-less production holes to the row they line up with |
| `12_compare_metres.cs` | Holes and metres per template against another design (read-only) |
| `fix_markers.cs` | Repairs markers after a bad clean-up (OPDB keeps deleted markers) |
| `run_script.ps1` | Sends a script with `_common.cs` and CONFIG overrides to the plugin pipe |

## LHS haulage model
Scripts used when calibrating LHS truck hours (see `lhs-haulage.md` for the full method).

| File | What |
|---|---|
| `lhs_trim_dump_blocks_to_surface.cs` | Trims dump blocks to a flyover surface (keeps the space still above it), in place with all attributes, in batches, with a dry-run mode. Cuts against a local surface patch so large drone surfaces don't freeze Deswik |
| `lhs_check_slots.cs` | Read-only check of mining slots before a run: blocks without a slot, Route Quality counts (drop-down `Closest Block Fallback`), slots not starting on a haul-road vertex, exit points |

## Knowledge notes
| File | What |
|---|---|
| `deswik-cad-run-csharp.md` | Scripting Deswik CAD through `run_csharp`: core API, pitfalls (C# 5, backslashes, deleted figures, cached dialogs), reflection recipe |
| `interactive-scheduler.md` | IS resource-path API, Report Options totaled-field expressions, monthly fleet-hour limits |
| `lhs-haulage.md` | LHS truck files, result fields, fitting cycle times, scenario window, **calibrating truck hours against a contractor**, Dynamic Slot Connectors (covering / supporting stage, route quality, surface roads), dump blocks (flyover trims, destination type, dependencies), dump slots, destination overrides that work, checking a run from the process log and scenario DB, string-network haul distances |
| `opdb-drill-design.md` | OPDB API: patterns, templates, headless hole-creation routines, markers / row lines, socket markers, coverage |
| `drill-report-review.md` | Filling a probe-drilling review workbook from a drill report export |

## Using the commands with Claude Code
Make a folder per command in `~/.claude/skills/` (e.g. `command-drill-design`). Copy that command's files into it and rename its SKILL file to `SKILL.md`. Claude then runs it when you say e.g. "run Command Drill Design".

## Working rules
- Read first, show what will change, then change it. Every modifying script asks for approval in Deswik.
- Never save the drawing or schedule unless the user asks.
- Verify after every change: counts, an unchanged-dates check, a snapshot comparison.
- Use the vendor's own routines (OPDB, IS commands) rather than drawing objects directly, so the native tools keep working on the result.
