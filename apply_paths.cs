// MODIFIES the schedule. For every file %TEMP%\deswik-cmd1\path__<Resource>.txt (one task ID per line, in order),
// sets that resource's path to exactly that list, using Deswik's own undoable commands. Dates, links and assignments are not touched.
if (Schedule == null) { Out.AppendLine("NO_SCHEDULE: nothing changed"); return null; }
string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "deswik-cmd1") + "\\";
var BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var asm = typeof(Deswik.Scheduler.Engine.Schedule).Assembly;
var baseT = asm.GetType("Deswik.Scheduler.Engine.Command");
var inv = typeof(Deswik.Scheduler.Engine.Schedule).GetMethod("Invoke", BF, null, new Type[] { baseT }, null);
var updT = asm.GetType("Deswik.Scheduler.Engine.UpdateManualResourcePaths");
var delT = asm.GetType("Deswik.Scheduler.Engine.Undo_Redo.Resources.DeleteManualResourcePaths");
var res = Schedule.GetResources();
var dict = new System.Collections.Generic.Dictionary<Deswik.Scheduler.Engine.Resource, System.Collections.Generic.List<string>>();
foreach (var f in System.IO.Directory.GetFiles(dir, "path__*.txt"))
{
    string rn = System.IO.Path.GetFileNameWithoutExtension(f).Substring(6);
    var r = res.FindName(rn);
    if (r == null) { Out.AppendLine("ABORT: resource not found " + rn); return null; }
    var l = new System.Collections.Generic.List<string>(System.IO.File.ReadAllLines(f));
    l.RemoveAll(s => string.IsNullOrWhiteSpace(s));
    foreach (var id in l) if (Schedule.GetTask(id.Trim()) == null) { Out.AppendLine("ABORT: task not found " + id); return null; }
    dict[r] = l;
}
if (dict.Count == 0) { Out.AppendLine("ABORT: no path__*.txt files"); return null; }
var before = new System.Collections.Generic.Dictionary<int, string>();
foreach (var t in Schedule.Tasks)
{
    if (t.Deleted) continue;
    var ra = t.GetNullaleFieldFullValue("ResourceAssignmentDetails");
    before[t.UniqueId] = t.Start.Ticks + "|" + t.Finish.Ticks + "|" + (ra == null ? "" : ra.ToString());
}
var untouched = new System.Collections.Generic.Dictionary<string, string>();
foreach (var r in res.NonDeletedResources) if (!dict.ContainsKey(r)) { var ids = new System.Collections.Generic.List<string>(); foreach (var pe in r.ResourcePaths) ids.Add(pe.ID); untouched[r.Name] = string.Join(",", ids.ToArray()); }

inv.Invoke(Schedule, new object[] { updT.GetConstructor(BF, null, new Type[] { typeof(Deswik.Scheduler.Engine.Schedule), dict.GetType() }, null).Invoke(new object[] { Schedule, dict }) });
var del = new System.Collections.Generic.Dictionary<Deswik.Scheduler.Engine.Resource, System.Collections.Generic.List<string>>();
foreach (var kv in dict)
{
    var extra = new System.Collections.Generic.List<string>(); int pos = 0;
    foreach (var pe in kv.Key.ResourcePaths) { if (pos >= kv.Value.Count && !extra.Contains(pe.ID)) extra.Add(pe.ID); pos++; }
    if (extra.Count > 0) { del[kv.Key] = extra; Out.AppendLine(kv.Key.Name + ": removing " + extra.Count + " leftover entries"); }
}
if (del.Count > 0)
{
    inv.Invoke(Schedule, new object[] { delT.GetConstructor(BF, null, new Type[] { typeof(Deswik.Scheduler.Engine.Schedule), del.GetType() }, null).Invoke(new object[] { Schedule, del }) });
    inv.Invoke(Schedule, new object[] { updT.GetConstructor(BF, null, new Type[] { typeof(Deswik.Scheduler.Engine.Schedule), dict.GetType() }, null).Invoke(new object[] { Schedule, dict }) });
}
foreach (var t in Schedule.Tasks) if (!t.Deleted && !t.IsParent) t.SetResourcePathDirty();
Schedule.TriggerRefreshResourcePaths();

bool allOk = true;
foreach (var kv in dict)
{
    var got = new System.Collections.Generic.List<string>(); foreach (var pe in kv.Key.ResourcePaths) got.Add(pe.ID);
    bool m = got.Count == kv.Value.Count;
    for (int i = 0; m && i < got.Count; i++) if (got[i] != kv.Value[i].Trim()) m = false;
    if (!m) allOk = false;
    Out.AppendLine(kv.Key.Name + ": " + got.Count + " entries (wanted " + kv.Value.Count + ") " + (m ? "MATCH" : "MISMATCH"));
}
foreach (var r in res.NonDeletedResources)
{
    if (dict.ContainsKey(r)) continue;
    var ids = new System.Collections.Generic.List<string>(); foreach (var pe in r.ResourcePaths) ids.Add(pe.ID);
    if (untouched[r.Name] != string.Join(",", ids.ToArray())) { allOk = false; Out.AppendLine("WARNING: path of " + r.Name + " changed"); }
}
int dateChg = 0, asgChg = 0;
foreach (var t in Schedule.Tasks)
{
    string b; if (t.Deleted || !before.TryGetValue(t.UniqueId, out b)) continue;
    var ra = t.GetNullaleFieldFullValue("ResourceAssignmentDetails");
    string[] parts = b.Split(new char[] { '|' }, 3);
    if (parts[0] != t.Start.Ticks.ToString() || parts[1] != t.Finish.Ticks.ToString()) dateChg++;
    if (parts[2] != (ra == null ? "" : ra.ToString())) asgChg++;
}
Out.AppendLine("Changed dates: " + dateChg + "; changed resource assignments: " + asgChg + "; other paths unchanged: " + allOk);
Out.AppendLine(allOk && dateChg == 0 && asgChg == 0 ? "RESULT: OK" : "RESULT: CHECK");
return null;
