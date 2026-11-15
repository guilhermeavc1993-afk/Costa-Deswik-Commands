// 07_gap_holes.cs  (DRY_RUN = read-only; else MODIFIES)  - prepend _common.cs
// "Gap production" holes (production template - never call them infill) to fill coverage gaps
// between the buffer row and the production grid.
// Greedy: largest uncovered patch first, >= MIN_HOLE_DIST from any hole, socket overlap <= MAX_OVERLAP,
// not inside a hazard circle. Created one at a time with OPDB CreateHoleAtPoint.
bool DRY_RUN = true;
double BAND_MIN = 8.5, BAND_MAX = 13.5;   // search band, m from crest
double MIN_HOLE_DIST = 2.5, MAX_OVERLAP = 0.5, EDGE_MIN = 1.0, HAZARD_R = 0.5;
string HAZARD_LAYER_CONTAINS = "HAZARDS" + BS + "BLAST HOLE"; // points of old collars to avoid (500 mm)
int FIRST_ID = 0;                          // 0 = continue after the highest numeric hole ID
var C = new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || !f.Layer.Name.StartsWith(DL + BS + "SOCKETS" + BS, StringComparison.OrdinalIgnoreCase)) continue; var ins = f.asInsert; if (ins == null) continue; C.Add(new double[] { ins.InsertionPoint.x, ins.InsertionPoint.y, ins.XScale }); }
var H = new List<double[]>(); int maxId = 0; foreach (var h in Holes()) { H.Add(new double[] { h.CollarX, h.CollarY }); int idn; if (int.TryParse(h.HoleID, out idn)) maxId = Math.Max(maxId, idn); }
var HZ = new List<double[]>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer.Name.IndexOf(HAZARD_LAYER_CONTAINS, StringComparison.OrdinalIgnoreCase) < 0) continue; var bb = f.BoundingBox; HZ.Add(new double[] { (bb.Min.x + bb.Max.x) / 2, (bb.Min.y + bb.Max.y) / 2 }); }
object opt = CFG.GetType().GetMethod("FindOptionByName").Invoke(CFG, new object[] { T_PROD });
double R = Convert.ToDouble(opt.GetType().GetProperty("Spacing").GetValue(opt, null)) / 2.0;
var offs = new List<double[]>(); for (double dx = -R; dx <= R; dx += 0.15) for (double dy = -R; dy <= R; dy += 0.15) if (dx * dx + dy * dy <= R * R) offs.Add(new double[] { dx, dy });
Func<double, double, double> Overlap = (px, py) => { int cov = 0; foreach (var o in offs) { double x = px + o[0], y = py + o[1]; foreach (var c in C) { double ddx = x - c[0], ddy = y - c[1]; if (ddx * ddx + ddy * ddy <= c[2] * c[2]) { cov++; break; } } } return (double)cov / offs.Count; };
double minX = 1e12, maxX = -1e12, minY = 1e12, maxY = -1e12; for (int i = 0; i < NB; i++) { minX = Math.Min(minX, BX[i]); maxX = Math.Max(maxX, BX[i]); minY = Math.Min(minY, BY[i]); maxY = Math.Max(maxY, BY[i]); }
var picks = new List<double[]>();
for (int it = 0; it < 100; it++) {
  double best = 2, bx = 0, by = 0;
  for (double x = minX; x <= maxX; x += 0.5) for (double y = minY; y <= maxY; y += 0.5) {
    double d = DistCrest(x, y); if (d < BAND_MIN || d > BAND_MAX) continue; if (!Inside(x, y) || EdgeDist(x, y) < EDGE_MIN) continue;
    bool bad = false; foreach (var h in H) if ((h[0] - x) * (h[0] - x) + (h[1] - y) * (h[1] - y) < MIN_HOLE_DIST * MIN_HOLE_DIST) { bad = true; break; } if (bad) continue;
    foreach (var z in HZ) if ((z[0] - x) * (z[0] - x) + (z[1] - y) * (z[1] - y) < HAZARD_R * HAZARD_R) { bad = true; break; } if (bad) continue;
    foreach (var c in C) if ((x - c[0]) * (x - c[0]) + (y - c[1]) * (y - c[1]) <= c[2] * c[2]) { bad = true; break; } if (bad) continue;
    double ov = Overlap(x, y); if (ov < best) { best = ov; bx = x; by = y; } }
  if (best > MAX_OVERLAP) break;
  picks.Add(new double[] { bx, by, best }); C.Add(new double[] { bx, by, R }); H.Add(new double[] { bx, by }); }
int id = FIRST_ID > 0 ? FIRST_ID : maxId + 1;
Out.AppendLine("gap production holes: " + picks.Count + (DRY_RUN ? " (dry run)" : "") + ", IDs from " + id);
var mt = _opdbAsm.GetType("Deswik.OPDB.Models.CreateHolesAtDigitizedPointsModel"); var ct = _opdbAsm.GetType("Deswik.OPDB.Commands.CreateHoleAtPoint");
foreach (var p in picks) {
  Out.AppendLine(string.Format("  {0}: {1:0.0} m from crest, socket overlap {2:0}%", id, DistCrest(p[0], p[1]), p[2] * 100));
  if (!DRY_RUN) {
    var m = Activator.CreateInstance(mt);
    mt.GetProperty("ActiveHoleTemplateName").SetValue(m, T_PROD, null); mt.GetProperty("HoleIdType").SetValue(m, Enum.Parse(_opdbAsm.GetType("Deswik.OPDB.Enums.HoleIdType"), "Numeric"), null);
    mt.GetProperty("NumericIdNumber").SetValue(m, id, null); mt.GetProperty("Dip").SetValue(m, 90.0, null); mt.GetProperty("Bearing").SetValue(m, 0.0, null);
    var cmd = Activator.CreateInstance(ct, new object[] { App, new Deswik.Graphics.Geometry.Point(p[0], p[1], GRADE_RL + 10), m, CFG });
    try { ct.GetMethod("Execute").Invoke(cmd, null); } catch (Exception e) { Out.AppendLine("  ERROR " + id + ": " + (e.InnerException != null ? e.InnerException.Message : e.Message)); } }
  id++; }
return null;
