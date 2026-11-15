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

## Haul distances without LHS (string network routing)
- Build a graph from the haul strings (3D length per segment) and connect each shot centre to the nearest ramp at its flitch floor (2D leg).
- Connect each destination with a |dz| limit so routes don't jump between benches.
- Run Dijkstra per shot and material, and weight the averages by mined volume.
- Forcing a route (e.g. "exit only by ramp X") = restrict the allowed segment set for those shots.
- Save the result strings with attributes (task, shot, fleet, stage, bench, material, destination, distance, volume, route) on their own layer, so they can be checked visually.
- Excel through COM from PowerShell: write `Value2` cell by cell with `[double]` casts (assigning a 2D array failed). Remember that PowerShell variable names are case-insensitive, so `$T` and `$t` are the same variable.
