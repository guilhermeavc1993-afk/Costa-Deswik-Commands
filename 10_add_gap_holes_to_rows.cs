// 10_add_gap_holes_to_rows.cs  (DRY_RUN = read-only; else MODIFIES)  - prepend _common.cs
// Same result as OPDB Holes > Re-number > "add single hole to row", for every production hole that has
// no row yet (NumberingRow = -1): match it to the row it lines up with (offset / burden), then put it at
// the start or end of that row. Row numbers and hole IDs are not changed.
// (HoleModCommands.AddHoleToRowInternal returned False when called from a script, so this edits OPDB's
//  RowLine objects directly: set BlastHoles, UpdateVertices, UpdateBlastHoles - what that routine does.)
bool DRY_RUN = true; double MAX_OFFLINE = 0.6;
var pat = _opdbAsm.GetType("Deswik.OPDB.DAL.DrillPatternFactory").GetMethod("Create").Invoke(null, new object[] { App, CFG });
var rows = new Dictionary<int, object>();
foreach (var r in (System.Collections.IEnumerable)pat.GetType().GetProperty("RowLines").GetValue(pat, null)) rows[Convert.ToInt32(r.GetType().GetProperty("RowID").GetValue(r, null))] = r;
Func<object, List<Deswik.Graphics.Figures.BlastHole>> RowHoles = (r) => { var L = new List<Deswik.Graphics.Figures.BlastHole>(); foreach (Deswik.Graphics.Figures.BlastHole x in (System.Collections.IEnumerable)r.GetType().GetProperty("BlastHoles").GetValue(r, null)) L.Add(x); return L; };
Func<Deswik.Graphics.Figures.BlastHole, double> Off = (h) => (h.CollarX - R0X) * BBX + (h.CollarY - R0Y) * BBY;
Func<Deswik.Graphics.Figures.BlastHole, double> Along = (h) => (h.CollarX - R0X) * AX + (h.CollarY - R0Y) * AY;
foreach (var h in Holes()) {
  if (h.HoleOption != T_PROD || h.NumberingRow != -1) continue;
  int bestRow = -1; double bestErr = 1e9;
  foreach (var kv in rows) { var L = RowHoles(kv.Value); if (L.Count == 0 || L[0].HoleOption != T_PROD) continue; double e = Math.Abs(Off(h) - Off(L[0])); if (e < bestErr) { bestErr = e; bestRow = kv.Key; } }
  if (bestRow < 0 || bestErr > MAX_OFFLINE) { Out.AppendLine("hole " + h.HoleID + ": no row within " + MAX_OFFLINE + " m (closest " + bestErr.ToString("0.00") + ")"); continue; }
  var row = rows[bestRow]; var LL = RowHoles(row);
  bool atEnd = Math.Abs(Along(h) - Along(LL[LL.Count - 1])) < Math.Abs(Along(h) - Along(LL[0]));
  Out.AppendLine(string.Format("hole {0} -> row {1} ({2}), off-line {3:0.00} m{4}", h.HoleID, bestRow, atEnd ? "end" : "start", bestErr, DRY_RUN ? " (dry run)" : ""));
  if (DRY_RUN) continue;
  if (atEnd) LL.Add(h); else LL.Insert(0, h);
  try { row.GetType().GetProperty("BlastHoles").SetValue(row, LL, null); row.GetType().GetMethod("UpdateVertices").Invoke(row, null); row.GetType().GetMethod("UpdateBlastHoles").Invoke(row, null); }
  catch (Exception e) { Out.AppendLine("  ERROR: " + (e.InnerException != null ? e.InnerException.Message : e.Message)); } }
return null;
