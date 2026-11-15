// 09_rows_snapshot.cs  (read-only, writes a TSV to %TEMP%)  - prepend _common.cs
// Snapshot of every hole's ID and row before touching rows, for 11_verify_rows.cs.
var sb = new StringBuilder(); sb.AppendLine("GUID\tHoleID\tTemplate\tNumberingRow\tGridRow\tRowIdx");
var rows = new SortedDictionary<int, int>(); int n = 0;
foreach (var h in Holes()) { var f = (Deswik.Graphics.Primaries.Figure)h; n++;
  sb.AppendLine(f.GUID + "\t" + h.HoleID + "\t" + h.HoleOption + "\t" + h.NumberingRow + "\t" + h.GridRow + "\t" + h.RowIdx);
  if (!rows.ContainsKey(h.NumberingRow)) rows[h.NumberingRow] = 0; rows[h.NumberingRow]++; }
string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "drill_rows_snapshot.tsv");
System.IO.File.WriteAllText(path, sb.ToString());
Out.AppendLine("saved " + n + " holes to " + path);
var s2 = new StringBuilder(); foreach (var kv in rows) s2.Append("row " + kv.Key + ":" + kv.Value + "  "); Out.AppendLine(s2.ToString());
return null;
