// 06_coverage.cs  (read-only)  - prepend _common.cs
// % of the shot floor not covered by any socket circle, in 1 m bands from the crest.
// 10-12 % between staggered circles is normal; a band well above that is a gap.
var C = new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted || !f.Layer.Name.StartsWith(DL + BS + "SOCKETS" + BS, StringComparison.OrdinalIgnoreCase)) continue;
  var ins = f.asInsert; if (ins == null) continue; C.Add(new double[] { ins.InsertionPoint.x, ins.InsertionPoint.y, ins.XScale }); }
double minX = 1e12, maxX = -1e12, minY = 1e12, maxY = -1e12; for (int i = 0; i < NB; i++) { minX = Math.Min(minX, BX[i]); maxX = Math.Max(maxX, BX[i]); minY = Math.Min(minY, BY[i]); maxY = Math.Max(maxY, BY[i]); }
double step = 0.25; var A = new double[40]; var U = new double[40]; double totU = 0, tot = 0;
for (double x = minX; x <= maxX; x += step) for (double y = minY; y <= maxY; y += step) {
  if (!Inside(x, y)) continue; double d = DistCrest(x, y); int b = (int)Math.Floor(d); if (b < 0) b = 0; if (b > 39) b = 39; A[b] += step * step; tot += step * step;
  bool cov = false; foreach (var c in C) { double dx = x - c[0], dy = y - c[1]; if (dx * dx + dy * dy <= c[2] * c[2]) { cov = true; break; } }
  if (!cov) { U[b] += step * step; totU += step * step; } }
Out.AppendLine("circles " + C.Count + "; uncovered " + totU.ToString("0") + " of " + tot.ToString("0") + " m2 (" + (100 * totU / tot).ToString("0.0") + "%)");
var sb = new StringBuilder(); for (int b = 0; b < 25; b++) if (A[b] > 0) sb.Append(b + "-" + (b + 1) + "m: " + (100 * U[b] / A[b]).ToString("0") + "%  ");
Out.AppendLine("uncovered by distance from crest: " + sb);
return null;
