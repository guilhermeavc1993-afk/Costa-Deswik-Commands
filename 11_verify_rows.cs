// 11_verify_rows.cs  (read-only)  - prepend _common.cs
// Compares every hole's ID and row with the snapshot from 09_rows_snapshot.cs.
string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "drill_rows_snapshot.tsv");
var snap = new Dictionary<string, string[]>(); bool first = true;
foreach (var line in System.IO.File.ReadAllLines(path)) { if (first) { first = false; continue; } var p = line.Split('\t'); snap[p[0]] = p; }
int same = 0, idChg = 0, rowChg = 0, newH = 0, noRow = 0; var changes = new List<string>(); var cnt = new SortedDictionary<int, int>();
foreach (var h in Holes()) { var f = (Deswik.Graphics.Primaries.Figure)h;
  if (!cnt.ContainsKey(h.NumberingRow)) cnt[h.NumberingRow] = 0; cnt[h.NumberingRow]++; if (h.NumberingRow == -1) noRow++;
  if (!snap.ContainsKey(f.GUID)) { newH++; continue; } var s = snap[f.GUID];
  bool ic = s[1] != h.HoleID, rc = s[3] != h.NumberingRow.ToString(); if (ic) idChg++; if (rc) { rowChg++; changes.Add(h.HoleID + ": " + s[3] + " -> " + h.NumberingRow); } if (!ic && !rc) same++; }
Out.AppendLine("unchanged " + same + " | ID changed " + idChg + " | row changed " + rowChg + " | new since snapshot " + newH + " | without a row " + noRow);
Out.AppendLine("row changes: " + string.Join("; ", changes.ToArray()));
var sb = new StringBuilder(); foreach (var kv in cnt) sb.Append("row " + kv.Key + ":" + kv.Value + "  "); Out.AppendLine(sb.ToString());
return null;
