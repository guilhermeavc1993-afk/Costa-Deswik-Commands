# Deswik LHS (Landform and Haulage Scheduler): notes

## Where things are
- **Truck file:** `C:\ProgramData\Deswik\Trux\<truck>.trux`. It holds the TALPAC method, capacity (bcm), heap/body volume, empty and loaded mass (which give the payload limit), max speed, rimpull and retard curves, and the default swell.
- **Payload settings** are in the LHS scenario, on the destination mapping and truck sections, not in IS.
- **Haul roads come from the scenario's road layers.** Each segment string carries a `Haul ID` attribute, and there are separate layers for roads, dynamic slots and dump slots. A ramp drawn on any other layer is **not** used until it's moved or copied onto a road layer and the mining connectors are recreated.
- **Speed limits layer:** if it's empty, the truck curves alone set the speed. TKPH can be switched off.
- **Haul paths** are written to `...\LHS Reports\DumpingDetailed_*.json` (path id, x, y, z per vertex).

## Results written back to IS (production fields on the digging tasks)
- `LHS Cycle Time [material]` (min)
- `LHS Trucked Distance [material]`: one-way metres; the "- Return" field is two-way
- `LHS Dumped Volume [material]` (lcm)
- `LHS Truck Hours`
- `LHS Truck Hours - SMU` = truck hours × the SMU factor (e.g. 1.25)

Truck hours = cycles × cycle time / 60, with cycles = dumped lcm / lcm per cycle.

## Learning the cycle times from the results
LHS works out a cycle per path. You can reproduce it closely with a linear fit per destination:

`CT (min) = a + b × one-way distance (km) + c × lift (km)`, where lift = destination RL − shot RL.

- a = load, spot and dump time plus the fixed part.
- b ≈ 2.6–3.4 min per km for a 90 t-class truck; c is about 30–45 min per km of lift.
- The RMSE was about 0.5 min over about 1,700 records.
- Implied bcm per cycle = (truck hours × 60 / CT) cycles divided into the bcm. It sits slightly below nominal capacity, and ore and waste differ through density and swell.

**Use:** estimate a route change before rerunning LHS:

estimate = LHS hours + (model on new routes − model on old routes)

That keeps LHS as the base and adds only the change.

## Scenario window (fDumpSchedulerScenario)
- It's opened from "Landform and Haulage Scenarios". The tabs are in `tcScenario` (XtraTabControl).
- **Destination overrides** (DataTable) columns: `Use` (bool), `Style` (e.g. No Haul), `DateFrom`, `DateTo`, `SourceBlockFilter`, `DestinationBlockFilter`, `Grouping`, `Description`.
- **Destination mapping** (DataTable) columns: `ID` (unique together with Phase and Resource; a new row needs ID = max + 1, or you get a ConstraintException), `Use`, `From`, `To`, `VolumeField`, `DensityField`, `Resource`, `Phase`, `TruckType`, `SwellFactorToTruck`, `SwellFactorToDump`, `SpotLoadTime`, `SpotDumpTime`, `CycleTimeAdjustmentFactor`, `RollingResistanceLoaded`/`Unloaded`, `PriorityField`, `PeriodLimitRule` and `Type`.
- **The window caches the drawing's filter names when it opens.** A filter created afterwards shows as invalid (red ✕) until the window is closed and reopened.
- **Changes only stick** after OK in the Scenario window, then OK in "Landform and Haulage Scenarios", then saving the drawing.
- **Writing rules as "No Haul" exclusions:** e.g. stage X waste → No Haul to every dump except its own. Extend `DateTo` past the end of the schedule.
- **Unmapped volume fields** (e.g. a mineralised-waste field) aren't hauled at all. Add a mapping row for each one.
- Rule changes don't need new connectors. Geometry changes (roads, ramps, dumps) need the mining or dump connectors recreated.

## Calibrating LHS truck hours against a contractor's numbers: what moved them
In order of impact in one calibration, the fixes below took the LHS figure from +7% to −4% of the contractor's truck SMU hours for the year. The cycle-time settings were not touched.

1. **Mining routes**: the biggest effect (see "Dynamic Slot Connectors").
2. **Dump solids**: they must be current, on the layer the scenario actually reads (see "Dump blocks").
3. **Destination rules** that send each stage to its intended dump (see "Destination overrides: what really works").
4. **Clean connector layers** (no duplicates, every slot starting on a haul-road vertex).

Check that digger hours and TMM match the original schedule. If they do, any change in truck hours comes from the haulage model alone.

## Dynamic Slot Connectors (mining routes)
- **Route Quality** on each output slot:
  - `Stage|Bench`: a proper route along a ramp on the block's own bench.
  - `Stage|Bench Fallback`: a ramp-based route borrowed from the nearest bench. This is fine.
  - `Closest Block Fallback`: copies a neighbour's route, then **drops vertically** to the block. These "drop-down" slots are the pink lines going down from a top bench, and they distort cycle times.
- **Covering stage** (on the ramp strings): the ramp exists only once the blocks of that stage that contain it are mined. Ramps drawn *through* task solids are then "buried" for most of the schedule, which forces drop-downs. For in-pit ramps that are developed as the benches go down, set covering = **−1**.
- **Supporting stage:** the ramp is removed when that stage mines it. Set it only on ramps a later pushback really mines out. Use −1 for ramps that stay.
- **Surface roads must be the same layer the scenario uses as haul roads.**
  - If not, routes stop at the pit crest on a ramp vertex.
  - LHS then reports *"No haul road connection exists for this connector. Ensure that the connector begins at a haul road polyline vertex."*
  - Ramps are not haul roads in the scenario.
- **End ramp strings exactly on a road vertex** (0.00 m) at the junction. A ramp string that carries on along a road duplicates it. Trim the ramp to the junction.
- **Results are written to the output layer only after OK.** The Yes/No "show unconnected blocks" prompt only highlights them. Keep the test output on its own layer and check which layer the scenario's *mining block connector layer* points to: the production layer can stay stale while you test.
- **Tick "Generate fallback routes"** so blocks with no ramp on their own bench (early or remnant blocks, final cuts against the wall) get a `Stage|Bench Fallback` route instead of none. Blocks with no route are hauled to overflow.
- **Check after generating:**
  - every block ID has at least one slot;
  - count by Route Quality (target: zero `Closest Block Fallback`);
  - every slot's first vertex sits on a haul-road vertex;
  - which junction the slots exit at (north or south exit, for example).

## Dump blocks
- **Check which layer the scenario reads.** It can be a *parent* layer holding an older copy of the solids, while the updated solids sit on a sub-layer. The scenario's process log line "Opening layer …" tells you.
- **The destination type comes from an attribute on the dump solids** (e.g. `Material`).
  - Copying solids from a layer without that attribute leaves it blank, and you get *"Invalid destination type of WASTE for material mapping BENCHBLOCK=>WASTE … Verify dump block types"*.
  - Set it (e.g. `WASTE`) on every dump solid. Leave stockpiles with their ore codes.
- **Updating dump capacity from a survey (flyover) surface:** keep the part of each dump block that is above the surface.
  - `Polyface.CalculateSurfaceCutFillVolumes(surface, tol, out below, out above)` gives the volume below and above the surface.
  - `Polyface.BooleanOut(surface, BooleanConstants.KeepAbove, true)` gives the trimmed solid.
  - Accept a block only if the result is closed and its volume matches `above` within 1%.
  - Copy every attribute, then remove the original.
  - **Big drone surfaces (millions of triangles) freeze Deswik** if you cut against the whole surface. Cut each block against `surface.ExtractSurfaceWithinBounds(new Deswik.Geometry.RTree.Rectangle(minX-10, minY-10, maxX+10, maxY+10, -1000, 2000), true, false)`, a local patch of about 5k triangles that takes about 3 s per block.
  - Skip blocks with nothing under the surface, and leave blocks at the surface edge as they are.
- **"Minimize RL" sends waste to the lowest open dump blocks first.** Stale low blocks that are actually already full (often far away) inflate haul distance. Dropping them, or a dump block disappearing, can move a whole stream from one dump to another.
- **Dump dependencies (`.exf`, finish-to-start):** a block with no slot never fills, so every block stacked on it stays locked. One unconnected bottom block can take a whole in-pit dump out of the run. Check the dependents of any unconnected block.

## Dump slot connectors
- **Rerunning the tool adds to the existing slots instead of replacing them.** You end up with duplicates, plus slots pointing to IDs that no longer exist. Clear the layer before regenerating.
- **A rule filtered on a road attribute** (e.g. `DUMP = <name>`) needs a road with that value. Otherwise: "Application of filter … resulted in no haul roads being selected", and the whole dump is unconnected.
- **Deep dump blocks far below the road** may not connect because of the rule's grade or drop limit.
- **Changing ramps doesn't require new dump slots** (they attach to roads). Changing roads or dump solids does.

## Destination overrides: what really works
- Style `No Haul` = 0 and `Haul Only` = 1 (`OverrideSettingTypeConstants`).
- **Grouping:** leave it blank. With `ID` grouping, rules apply only between a mining block and a dump block with the same ID, which never happens. The rules are saved and the filters match, but nothing changes.
- **Haul Only on a stage filter also restricts ore.** Ore from that stage can then only go to the dump and is sent to OVERFLOW ("reported to 'OVERFLOW' … This indicates no path"). To steer waste only, use **No Haul** rows that block the dumps a stage must not use.
- **Pattern used (stage → dump):**
  - stage 1 No Haul to dump B and to the in-pit dump;
  - stage 2 No Haul to dump A and to the in-pit dump.
- **End the block when the target dump is full.** Work out when cumulative stage waste × swell reaches the dump capacity, and end the "No Haul to the other dump" row there. Otherwise the late waste has nowhere to go.
- **The filter picker may only offer some values** (e.g. `Stage: 2` but not `Stage: 1`). Build a normal attribute filter (`Stage` = `1`) instead.

## Checking a run without opening dialogs
- **Process log:** `<scenario folder>\Logs\LHS_ProcessLog_<scenario>_<date>.txt`. Useful lines:
  - "Processing source <ID>, <volume field>, … Potential destinations N, Reachable destinations M - Best Destination <ID>": shows which dumps a rule left open;
  - "reported to 'OVERFLOW'";
  - "No block exists with this ID";
  - "have no dump slots";
  - "Mine block not added due to no associated solid" (schedule tasks whose ID has no solid);
  - the "manual start / finish dates" warning.
- **The scenario as run is saved to `LHS_Scenario_<scenario>.db` (SQLite).** Read it read-only (e.g. with `System.Data.SQLite.dll` from the Deswik install folder):
  - `Configs` (Name/Value): every setting, including `DumpSchedule_OverrideSetting[…][n]_*`, haul road layers and connector layers;
  - `Filters` and `FilteredEntities`: how many blocks each filter matched.
- **Results:**
  - `DumpingDetailed_*.csv`: one row per source, material and destination. It has FROM ID, TO ID, START DATE, BANK VOLUME, NUMBER CYCLES, DISTANCE LOADED, CYCLETIME, TRUCK HOURS and HAUL ROAD (the road strings used, which shows the exit).
  - The IS fields `LHS Truck Hours` and `LHS Truck Hours - SMU`. Split them by month with `ScheduleQuery.GetProportionOfTaskWithinDates`.
- **A month low on truck hours** may be a digger idle in the schedule (waiting on drill and blast), not a haulage effect. Compare each digger's task hours per month against the hours in the month. Digger hours = the task `Work` property (not a field).

## Haul distances without LHS (string network routing)
- Build a graph from the haul strings (3D length per segment) and connect each shot centre to the nearest ramp at its flitch floor (2D leg).
- Connect each destination with a |dz| limit so routes don't jump between benches.
- Run Dijkstra per shot and material, and weight the averages by mined volume.
- Forcing a route (e.g. "exit only by ramp X") = restrict the allowed segment set for those shots.
- Save the result strings with attributes (task, shot, fleet, stage, bench, material, destination, distance, volume, route) on their own layer, so they can be checked visually.
- Excel through COM from PowerShell: write `Value2` cell by cell with `[double]` casts (assigning a 2D array failed). Remember that PowerShell variable names are case-insensitive, so `$T` and `$t` are the same variable.
