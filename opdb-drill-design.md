# Deswik OPDB (Open Pit Drill & Blast): API notes

Everything below was verified from scripts in Deswik CAD 2025.1. Most classes are internal, so create them with reflection.

## Getting the plugin and a pattern
- Find the OPDB instance: walk `App.Plugins` and check each item's `Instance`/`Plugin`/`Value`/`Object` property for type `Deswik.OPDB.BlastHoleDesign`.
- `BlastHoleDesign.DrillPatternConfiguration` lists the patterns (`ToString()` = name); `ActiveBlastHolePattern` gives the active one.
- `DrillPatternConfiguration`:
  - layer names: `PatternLayerName`, `GridHolesLayer`, `PolyLayerName`, `RowLinesLayerName`;
  - `Options`: the hole templates (`Name`, `Diameter`, `Burden`, `Spacing`, `SubDrill`, `Dip`, `MinLength`/`MaxLength`, `Prefix`);
  - `FindOptionByName`, `RefreshBlastHoles(list)`;
  - `PatternConfig.SurfaceOverrides` (e.g. a STAB TOE surface);
  - `PatternConfig` collar, toe and grade surfaces and RLs.
- `Deswik.OPDB.DAL.DrillPatternFactory.Create(App, cfg)` gives an `IDrillPattern`, whose `.RowLines` are row-line objects.

## Blast holes (`Deswik.Graphics.Figures.BlastHole`)
- Create: `new BlastHole(Point designIntersect, azimuth, dip, length, burden, spacing, diameter, id)`. Dip 90 = vertical down.
- Key properties:
  - `HoleOption` (template), `HoleID`, `PatternConfiguration`;
  - `CollarX/Y/Z`, `ToeX/Y/Z`, `HoleLength`;
  - `ToeOverride` (surface override name), `SubDrill`;
  - `NumberingRow` / `GridRow` / `RowIdx`.
- Holes made outside OPDB's tools are **not** in the pattern: they get no markers, annotations or row lines. Use OPDB's own routines instead.

## Hole-creation routines that run headless
| UI tool | Class | Notes |
|---|---|---|
| Create trim holes along polyline | `Deswik.OPDB.Commands.CreateTrimHolesAlongPolylines(App, CreateTrimHolesAlongPolylinesModel, cfg).Execute()` | Model fields: `HoleOptionName`, `ExactSpacing`, `UseDesiredSpacing`, `NumOffsetRows` (**0 = only the line**; 1 adds a row), `Angle`, `DipMode`, `ContourPolylines` |
| Create holes within boundary polyline | `CreateHolesServiceProvider(App, cfgList, patternName).CreateHolesWithinBoundary(CreateHolesWithinBoundaryModel)` | Use `PatternType=Grid` with `PatternDirectionX/Y`, `RowAlignment` and `OriginWorld`, plus `PatternStaggerType` (None/Left/Right) and `PatternOffsetDirection`. `AlignedToPoly` gave only one row |
| Create holes at digitized points | `Deswik.OPDB.Commands.CreateHoleAtPoint(App, Point, CreateHolesAtDigitizedPointsModel, cfg).Execute()` | Model: `ActiveHoleTemplateName`, `HoleIdType` (Numeric/Alphanumeric), `NumericIdNumber`, `Dip`. Also adds a toe marker and an extra collar marker |

`BlastHoleDesign.GetCommands` returns only a few command IDs. The `UICommandIDs` GUIDs aren't accepted by `ExecuteByID`.

## Markers, annotations, row lines
- OPDB writes these to `<Holes>\MARKERS` (INSERT, `Location`=Collar/Toe, `_BlastHoleGuid`), `<Holes>\ANNOTATIONS` (TEXT, `_dwBlastHoleHandle`) and `<Holes>\ROW LINES` (polyline with `RowID`, `RowName`, `_BlastHole00000…` handles and `_BlastHoleGuid` list).
- **Each OPDB run flags the previous markers `Deleted=true` but keeps them in `Entities`.** Never "dedupe" markers without filtering `Deleted`.
- **Adding a hole to a row** (Holes > Re-number > add single hole to row): `HoleModCommands.AddHoleToRowInternal` returned False from script. What works is the row-line object it uses: `row.BlastHoles = list` (insert the hole at the start or end), then `row.UpdateVertices()` and `row.UpdateBlastHoles()`. That sets the hole's `NumberingRow`/`GridRow`/`RowIdx` and redraws the line. Row IDs and hole IDs don't change.

## Socket / intersection markers
- OPDB > Analysis > Socket Intersections opens `Deswik.Graphics.Dialogs.fPointIntersectionMarkers(App, baseLayer, outputLayer)` (a `DeswikForm`; call `.Show()`).
- Controls:
  - `esfOriginalPoints`: base figures (design hole positions);
  - `esfFiguresToTest`: figures to test against, with `rbSelectedEntities` etc.;
  - `lpOutputMarkersLayer`, `spnDistance`, `cboIntBlock` (e.g. DM_Symbol1);
  - `cpIntBlockColor.ColourIndex`: **0-based, ACI − 1**;
  - `cClearLayer`, `btnProcess`. The dialog closes after processing.
- **Design use:** circles of radius half the spacing at each hole's toe show the breakage area at grade, so gaps and overlaps in coverage are visible.

## Coverage → gap production
- When the production rows aren't parallel to the wall rows, the first production hole of each row lands at varying distances behind the buffer. That leaves a strip of floor no circle covers.
- Fill it with **gap production** holes (the production template, not "infill"): largest uncovered patch first, ≥ 2.5 m from other holes, socket overlap ≤ 50%, clear of hazard circles. Then add them to the rows they line up with.
