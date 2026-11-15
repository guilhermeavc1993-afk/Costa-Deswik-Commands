# Deswik + Claude playbook

Working notes, commands and scripts for using Claude (Claude Code / Claude desktop) with **Deswik CAD**, the **Interactive Scheduler (IS)**, **LHS** (Landform and Haulage) and **OPDB** (Open Pit Drill & Blast) in open-pit mine planning.

Everything here was worked out and checked in a live Deswik 2025.1 session through a Claude Assistant plugin. The plugin exposes `run_csharp`, `get_context`, `list_layers`, `is_*` schedule tools and `run_deswik_command` over a Windows named pipe. Site names, coordinates, file paths and people's names have been removed: use your own layer names and values.

## Contents
| Path | What |
|---|---|
| `commands/command-1-resource-path-priorities/` | **Command 1**: rebuild IS loader or drill resource paths and priorities so they match the current IS sequence, without moving dates, links or assignments |
| `commands/command-drill-design/` | **Command Drill Design**: full OPDB drill design from a boundary, a row-orientation line and a D&B proposal: stab, batter, buffer, production, socket circles, coverage check, gap production holes, row membership |
| `knowledge/deswik-cad-run-csharp.md` | Scripting Deswik CAD through `run_csharp`: core API, pitfalls (C# 5, backslashes, deleted figures, cached dialogs), reflection recipe |
| `knowledge/interactive-scheduler.md` | IS resource-path API, Report Options totaled-field expressions, monthly fleet-hour limits |
| `knowledge/lhs-haulage.md` | LHS truck files, result fields, fitting cycle times, scenario window (destination overrides / mapping), string-network haul distances |
| `knowledge/opdb-drill-design.md` | OPDB API: patterns, templates, headless hole-creation routines, markers / row lines, socket markers, coverage |
| `knowledge/drill-report-review.md` | Filling a probe-drilling review workbook from a drill report export |
| `tools/run_script.ps1` | Sends a script (with `_common.cs` and CONFIG overrides) to the plugin pipe |

## Using the commands with Claude Code
Copy a folder from `commands/` into `~/.claude/skills/`. Claude then runs it when you say e.g. "run Command Drill Design". Each `SKILL.md` lists the inputs, the steps and the checks.

## Working rules
- Read first, show what will change, then change it. Every modifying script asks for approval in Deswik.
- Never save the drawing or schedule unless the user asks.
- Verify after every change: counts, an unchanged-dates check, a snapshot comparison.
- Use the vendor's own routines (OPDB, IS commands) rather than drawing objects directly, so the native tools keep working on the result.
