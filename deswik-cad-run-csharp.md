# Deswik CAD scripting with Claude (run_csharp)

Notes verified while working in Deswik CAD 2025.1 through the Claude Assistant plugin, which exposes `run_csharp`, `get_context`, `list_layers`, `is_*` and `run_deswik_command` over a named pipe.

## How the plugin runs code
- You send the **body** of `object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)`.
- It uses the **C# 5 compiler**: no `$""` strings, no `?.`, no `nameof`, no `=>` members, no `out var`. Lambdas are fine.
- A lambda parameter name must not clash with a local declared later in an enclosing scope (CS0136, for example `n`).
- Code that changes the drawing opens an **approval window in Deswik**. A "Request timed out" can still have applied the change, so recount before rerunning.
- `Schedule` can be `null`, depending on which Deswik window has focus. Guard every IS script and retry.
- **If two Deswik windows are open, the pipe can flip between them.** Print `App.Filename` at the start of every script, or close the other window.

## Backslashes
When the code goes through the MCP tool parameter, `"\\"` inside C# strings gets mangled: `"0\\04_..."` arrives as `\0`. Build layer names with
```csharp
string BS = ((char)92).ToString();
string layer = "DRILL_AND_BLAST" + BS + "SHOT" + BS + "HOLES";
```
Code sent from a file through the pipe (see `tools/run_script.ps1`) is not affected.

## Core objects
- `App.Layers` (Deswik.Graphics.Primaries.Layer: `Name`, `Visible`, `Locked`). Create with `App.Layers.Add(name, setActive, 1)`. Compare names case-insensitively.
- `App.ModelSpace.Entities` holds every figure (`Deswik.Graphics.Primaries.Figure`).
  - Typed views: `asPolyline`, `asBlastHole`, `asInsert`, `asText`, `asPolyface`, `asPoint`, with `IsXxx` flags.
  - `TypeName` gives POLYLINE, BLASTHOLE, INSERT and so on.
  - **Always skip `f.Deleted`.** Some tools flag objects deleted but leave them enumerated.
- Add a figure: `fig.Layer = layer; App.ModelSpace.Entities.AddItem(fig, null, null, true, false);`
- Remove a figure: `App.ModelSpace.Entities.Remove(fig)`. It may still be drawn until regen, so also set `Visible=false` and run `CID_View_Regen`.
- Polyline: `new Deswik.Graphics.Figures.Polyline()`, `AddVertex(x,y,z)`, `Closed`, `Length`, `Area`, `VertexList` (points with lower-case `x,y,z`).
- Attributes: `f.AttributeValueSet(name, value)`, `f.AttributeValueGet(name, default)` (2 arguments), `f.AttributeDoubleValueGet(name, default)`, `f.XPropertiesNames`.
- `f.GUID`, `f.Handle` (hex) and `f.HandleID` (decimal). OPDB links annotations by `_dwBlastHoleHandle` (decimal) and markers by `_BlastHoleGuid`.
- Selection: `App.Selections.MainSelection` (`RemoveAll`, `AddItems(list)`), then `App.MainSelectionUpdate(true,true)`. `App.CurrentGripSelection` holds grip-selected polylines.
- Filters: `App.Filters`, of type `Deswik.Graphics.Collections.Filters`. There are two `Filters` properties; pick it by type. Use `CheckExists`, `FindName`, `Add(name)`, then `filter.Add(field, op, value, "And")` and `.Group`.
- Commands: `App.MenuCommand.ExecuteByID("CID_View_Regen", Deswik.Common.Utilities.CommandOrigin.OtherCommand)`.

## Driving a dialog that is already open
- Find it in `System.Windows.Forms.Application.OpenForms` by `Text`, or search its child controls by type name.
- Use `F.Invoke(...)` when not already on the UI thread.
- Walk `Controls` by `Name`. DevExpress controls expose `EditValue`, `Checked` and `Text`.
- Grids: `GridControl.DataSource` is a DataTable or BindingList. Edit the rows, then call `RefreshDataSource()`.
- Click buttons with `PerformClick()`.
- Some Deswik dialogs (`DeswikForm`) are not `Form`s: create them with their constructor and call `.Show()`.
- **Windows that cache data when they open** (scenario windows, Resource Paths) overwrite script changes when the user presses OK or Apply on a stale copy. Ask the user to close them with **Cancel** before you edit, or make the edits inside the open window and have the user press OK.

## Reflection recipe for undocumented APIs
1. List the assemblies whose names contain the product (`AppDomain.CurrentDomain.GetAssemblies()`).
2. List the types, constructors, properties, fields and methods (include non-public).
3. To see what a UI button does, read the IL of its handler. Resolve `call`/`callvirt`/`newobj` tokens (0x28/0x6F/0x73) with `Module.ResolveMethod`, and string tokens (0x72) with `ResolveString`. This shows which service or model class does the real work.
4. Test the call read-only first, for example build an object and read its properties without adding it to the drawing. Then make a one-item change, then the rest.
