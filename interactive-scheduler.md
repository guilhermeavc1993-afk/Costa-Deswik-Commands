# Deswik Interactive Scheduler (IS): notes

## Resource paths and priorities (see `commands/command-1-resource-path-priorities`)
**Typical workflow:**
1. In CAD, with the interactive filters on the task solids, click the solids in order into a loader's resource path. That order sets each task's priority.
2. In IS, filter the activities, sort by priority, and link the tasks.

Tasks linked by hand without the path end up at priority 0 or out of order. Command 1 fixes that.

**API (internal, reached through reflection):**
- `Deswik.Scheduler.Engine.UpdateManualResourcePaths(Schedule, Dictionary<Resource, List<string>>)`, run through the non-public `Schedule.Invoke(Command)`. It is undoable. It puts the given IDs first and keeps the old extra entries at the end.
- Remove those extras with `Undo_Redo.Resources.DeleteManualResourcePaths`, then update again.
- Afterwards call `task.SetResourcePathDirty()` and `Schedule.TriggerRefreshResourcePaths()`.
- `Resource.ResourcePaths` holds the path order; each entry's `ID` is a task ID.
- Set priorities with the plugin's `is_set_task_fields`. Send the IDs as plain strings.
- **Close CAD's Resource Paths window with Cancel first.** OK or Apply writes its stale copy back, and "Auto-renumber priority" renumbers.

## Report Options: totaled fields (expressions)
- The grid is a `BindingList<Deswik.Scheduler.Engine.TotaledFields>`. Edit `ExpressionCurrentCulture`, `Format`, `Decimals` and `Filter`.
- **Refer to another row:** `[#(row,0):Row Name]`, e.g. `IFERROR(([#(15,0):Trans HG Ounces]*{Ounces Conversion})/[#(13,0):Trans HG Tonnes],0.0)`. `{Name}` is a constant.
- **Check a block of rows** by making sure each total uses the right material, ore type (oxide / transitional / fresh) and grade class (HG+MG, LG). Tonnes, ounces and grade rows are easy to point at the wrong row after copying.
- **Grade rows** = ounces × conversion / tonnes. Copy the number format from an existing grade row.
- **Hours rows** that convert machine hours to SMU multiply by a constant, e.g. `([#(104,0):Loader 1 Hours]+[#(106,0):Loader 2 Hours])*[WOT > SMU Hours]`.
- **Filters on rows** must match the activity the row reports, e.g. "Activity Type = Digging".
- Changes made in the open dialog only stick when the user presses OK.

## Monthly truck-hour limit (gap analysis)
- Fleet limits can sit in a "DoNotExceed" field per period, e.g. a truck-fleet net productive hours monthly limit that scales with month length.
- Compare the LHS truck hours per calendar month with the limit to find the bottleneck months.
- Spreading a task's hours over months by elapsed time is close enough for a first check, to within a few %. Re-levelling changes the split.

## Plugin tools for IS
- `is_list_fields`, `is_query_tasks`, `is_task_details`, `is_set_task_fields`, `is_add_dependencies`, `is_level_schedule` and `run_is_command`. The command IDs come from `list_is_commands`, e.g. `IS_UpdateGraphicsAttributesFromSchedAll`.
- Production fields are read per task (e.g. `Mined Volume_HG`, `LHS Truck Hours`). `ProductionFieldNameSet` threw a NullReference; use `is_list_fields` instead.
