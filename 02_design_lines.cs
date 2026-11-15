// 02_design_lines.cs  (MODIFIES)  - prepend _common.cs
// Draws HOLE DESIGN\STAB, BATTER, BUFFER lines (offset from the crest, clipped to the boundary)
// and the PRODS polygon (boundary clipped to >= PROD_OFF from the crest).
if (CREST_A[0] == 0 && CREST_B[0] == 0) { Out.AppendLine("set CREST_A / CREST_B first (run 01_inputs.cs)"); return null; }
string HD = DL + BS + "HOLE DESIGN";
object[][] rows = { new object[] { "STAB", STAB_OFF, STAB_TOE_RL }, new object[] { "BATTER", BATTER_OFF, GRADE_RL }, new object[] { "BUFFER", BUFFER_OFF, GRADE_RL } };
foreach (var r in rows) {
  double d = (double)r[1], z = (double)r[2]; double tmin = 1e9, tmax = -1e9;
  for (double t = -300; t <= 300; t += 0.01) { double px = CREST_A[0] + d * NX + t * UX, py = CREST_A[1] + d * NY + t * UY; if (Inside(px, py)) { tmin = Math.Min(tmin, t); tmax = Math.Max(tmax, t); } }
  if (tmax < tmin) { Out.AppendLine((string)r[0] + ": line does not cross the boundary"); continue; }
  var pl = new Deswik.Graphics.Figures.Polyline();
  pl.AddVertex(CREST_A[0] + d * NX + tmin * UX, CREST_A[1] + d * NY + tmin * UY, z);
  pl.AddVertex(CREST_A[0] + d * NX + tmax * UX, CREST_A[1] + d * NY + tmax * UY, z);
  Deswik.Graphics.Primaries.Figure fig = pl; fig.Layer = GetLayer(HD + BS + (string)r[0]); App.ModelSpace.Entities.AddItem(fig, null, null, true, false);
  Out.AppendLine((string)r[0] + " line " + (tmax - tmin).ToString("0.0") + " m at " + d + " m from crest, RL " + z);
}
// PRODS polygon (Sutherland-Hodgman against the half plane DistCrest >= PROD_OFF)
var outP = new List<double[]>();
for (int i = 0; i < NB; i++) { double[] a = { BX[i], BY[i] }, b = { BX[(i + 1) % NB], BY[(i + 1) % NB] };
  double da = DistCrest(a[0], a[1]) - PROD_OFF, db = DistCrest(b[0], b[1]) - PROD_OFF;
  if (da >= 0) outP.Add(a);
  if ((da >= 0) != (db >= 0)) { double t = da / (da - db); outP.Add(new double[] { a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]) }); } }
var pp = new Deswik.Graphics.Figures.Polyline(); foreach (var p in outP) pp.AddVertex(p[0], p[1], GRADE_RL); pp.Closed = true;
Deswik.Graphics.Primaries.Figure pf = pp; pf.Layer = GetLayer(HD + BS + "PRODS"); App.ModelSpace.Entities.AddItem(pf, null, null, true, false);
Out.AppendLine("PRODS polygon " + outP.Count + " vertices, " + pp.Area.ToString("0") + " m2, front edge " + PROD_OFF + " m from crest");
return null;
