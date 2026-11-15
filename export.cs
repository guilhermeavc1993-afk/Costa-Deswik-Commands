// Read-only. Exports every task and every resource path to %TEMP%\deswik-cmd1\
if (Schedule == null) { Out.AppendLine("NO_SCHEDULE: nothing exported"); return null; }
string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "deswik-cmd1") + "\\";
System.IO.Directory.CreateDirectory(dir);
var byUid = new System.Collections.Generic.Dictionary<int, Deswik.Scheduler.Engine.Task>();
foreach (var t in Schedule.Tasks) if (!t.Deleted) byUid[t.UniqueId] = t;
var preds = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>();
foreach (var d in Schedule.Dependencies)
{
    if (d.Deleted) continue;
    Deswik.Scheduler.Engine.Task a, b;
    if (!byUid.TryGetValue(d.From, out a) || !byUid.TryGetValue(d.To, out b)) continue;
    if (!preds.ContainsKey(d.To)) preds[d.To] = new System.Collections.Generic.List<string>();
    preds[d.To].Add(a.Id);
}
var tb = new System.Text.StringBuilder();
tb.AppendLine("Name\tId\tStart\tFinish\tActType\tRes\tAssigned\tPath\tIdx\tPriority\tPreds");
foreach (var t in Schedule.Tasks)
{
    if (t.Deleted || t.IsParent) continue;
    var at = t.GetNullaleFieldFullValue("Activity Type");
    var ra = t.GetNullaleFieldFullValue("ResourceAssignmentDetails");
    var assigned = new System.Collections.Generic.List<string>();
    if (ra != null) foreach (var part in ra.ToString().Split(';')) { int c = part.IndexOf(':'); if (c > 0) assigned.Add(part.Substring(0, c).Trim()); }
    assigned.Sort();
    string p = preds.ContainsKey(t.UniqueId) ? string.Join(";", preds[t.UniqueId].ToArray()) : "";
    tb.AppendLine((t.Name ?? "") + "\t" + t.Id + "\t" + t.Start.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + t.Finish.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + (at == null ? "" : at.ToString()) + "\t" + (t.PhysicalResource ?? "") + "\t" + string.Join(",", assigned.ToArray()) + "\t" + (t.ResourcePath ?? "") + "\t" + t.ResourcePathIndex + "\t" + t.Priority.ToString("R") + "\t" + p);
}
System.IO.File.WriteAllText(dir + "tasks.tsv", tb.ToString());
var sb = new System.Text.StringBuilder();
sb.AppendLine("Resource\tPos\tID");
foreach (var r in Schedule.GetResources().NonDeletedResources)
{
    int i = 0;
    foreach (var pe in r.ResourcePaths) { sb.AppendLine(r.Name + "\t" + i + "\t" + pe.ID); i++; }
}
System.IO.File.WriteAllText(dir + "paths.tsv", sb.ToString());
var dates = new System.Text.StringBuilder();
foreach (var t in Schedule.Tasks)
{
    if (t.Deleted) continue;
    var ra = t.GetNullaleFieldFullValue("ResourceAssignmentDetails");
    dates.AppendLine(t.Id + "\t" + t.Start.Ticks + "\t" + t.Finish.Ticks + "\t" + (ra == null ? "" : ra.ToString()));
}
System.IO.File.WriteAllText(dir + "snapshot_" + DateTime.Now.ToString("HHmmss") + ".tsv", dates.ToString());
Out.AppendLine("EXPORTED to " + dir);
return null;
