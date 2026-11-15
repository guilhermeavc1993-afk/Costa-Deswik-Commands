---
name: command-drill-design
description: "Command Drill Design" - builds a full open-pit drill design in Deswik CAD + OPDB (stab, batter, buffer and production holes, socket circles, gap production holes, row membership) from a boundary, a row-orientation line and a signed Drill & Blast Proposal. Use when the user says "run Command Drill Design" or asks for a wall-control + production drill design. Works through the Deswik Claude Assistant plugin (run_csharp).
---

# Command Drill Design

Everything is made with OPDB's own routines, so the holes belong to the OPDB pattern and OPDB draws their markers, annotations and row lines.

## Inputs (the user gives them every time)
1. **Boundary**: a closed polyline on `<pattern>\Design Lines\BOUNDARY`.
2. **Row orientation**: a polyline on `<pattern>\Design Lines\ROW ORIENTATION`. Production rows run along it, and the first row sits on it.
3. **Signed D&B Proposal (PDF)**. Read from it:
   - for stab, batter, buffer and production: the diameter, depth, subdrill, burden Ã— spacing, and the collar distance from the crest peg;
   - the batter angle.
4. The **OPDB pattern** name. Make it the active pattern, with templates `5_Stab`, `1_Batter`, `2_Buffer` and `4_Production`, and a `STAB TOE` surface override.

## How to run the scripts
- Each `NN_*.cs` is a `run_csharp` body. Prepend `_common.cs`, which holds the CONFIG block and the helpers, and reads the boundary, row orientation and pattern layers from the drawing.
- From PowerShell: `run_script.ps1 <script> -Set @{ PATTERN='"SHOT_A"'; CREST_RL='345'; ... } [-Modifies]`.
- Through the MCP tool: paste `_common.cs` + the script, with the CONFIG values filled in.
- Every modifying script opens an approval window in Deswik. **Never save the drawing**; the user saves.

## Steps
1. **`01_inputs.cs`** (read-only). It reports:
   - the boundary edges and the row orientation;
   - the crest candidates at `CREST_RL` (put the wall-side one in `CREST_A`/`CREST_B`);
   - the pattern surfaces, overrides and templates.
   **Compare the templates with the proposal.** The user fixes any differences in OPDB before you continue.
2. **`02_design_lines.cs`**. STAB, BATTER and BUFFER lines at the proposal's crest distances (stab at the stab-toe RL, the others at grade), clipped to the boundary. Plus a PRODS polygon = the boundary clipped to â‰¥ `PROD_OFF` from the crest.
3. **`03_wall_rows.cs`**. OPDB trim-holes-along-polyline, one row per line (`NumOffsetRows=0`). Stab toes go to `STAB TOE`. It reports any stab that misses the surface; the user decides what to do with it.
4. **`04_production.cs`**. OPDB holes-within-boundary on PRODS, as a **Grid** along the row orientation, **staggered left**. Check that the rows are one burden apart and alternate rows are offset half a spacing.
5. **`05_sockets.cs`**. Create the sublayers `Design Lines\SOCKETS\PRODS`, `BUFFER` and `BATTER` (no stabs). For each template:
   - select its holes;
   - test against = **Selected entities**; distance = **half the spacing**; block DM_Symbol1;
   - colour production = ACI 3, batter = 2, buffer = 1 (the dialog's index is 0-based; the script handles that).
6. **`06_coverage.cs`**. Uncovered floor in 1 m bands from the crest. Look for a gap between the buffer row and the production holes, typically where angled production rows start at the PRODS edge.
7. **`07_gap_holes.cs`**. **Gap production** holes: production template, numeric IDs continuing on. **Never call them infill.**
   - Run a dry run first and show the list.
   - Rule: largest uncovered patch first, â‰¥ 2.5 m from any hole, **socket overlap â‰¤ 50%**, clear of hazard (old collar) circles.
   - Then set `DRY_RUN=false`. Make their sockets with `05_sockets.cs`, using a `GAP PRODUCTION` run and `clear=false`.
   - The user may delete some holes. Remove them together with their markers (`_BlastHoleGuid`), annotations (`_dwBlastHoleHandle`) and sockets (same toe XY).
8. **`08_row_end_holes.cs`** (on request). In-line gap production holes at the boundary end of staggered rows, e.g. rows 5, 7, 9 and 11. Each goes one spacing along the row, pulled back to 0.1 m inside the boundary.
9. **Rows.**
   - Run `09_rows_snapshot.cs`.
   - Run `10_add_gap_holes_to_rows.cs`: dry run first, then for real. Each row-less production hole joins the row it lines up with, at that row's start or end.
   - Run `11_verify_rows.cs`. **Row numbers and hole IDs must not change**; only the gap holes gain a row.
10. **`12_compare_metres.cs`**: holes and metres per template, against the original design if there is one.
11. **Report** the counts, metres, coverage and anything left over (stabs off the surface, gaps in the numbering). Remind the user to save.

## Lessons learned
- Two Deswik windows open â†’ the plugin flips between them. Ask the user to close the other one. `_common.cs` prints the document name.
- OPDB flags superseded markers `Deleted` but keeps them. Filter `!f.Deleted` everywhere. `fix_markers.cs` repairs a bad clean-up.
- A removed object can stay drawn until a regen.
- Ask before adding holes the user didn't ask for. Always check new holes against the hazard layers.
