// 08_row_end_holes.cs  (MODIFIES)  - prepend _common.cs
// In-line gap production holes at the boundary end of chosen production rows (staggered rows leave a
// half-spacing gap at alternate row ends). Each new hole sits one spacing along the row from the end hole,
// pulled back to EDGE_IN inside the boundary if the full spacing falls outside.
int[] ROWS = { 5, 7, 9, 11 };       // OPDB row numbers (NumberingRow) - as the user names them
bool AT_START = true;                // true = end with the smallest "along" (start of the row), false = far end
double EDGE_IN = 0.1;
int FIRST_ID = 0;                     // 0 = continue after the highest numeric hole ID
object opt = CFG.GetType().GetMethod("FindOptionByName").Invoke(CFG, new object[] { T_PROD });
double S = Convert.ToDouble(opt.GetType().GetProperty("Spacing").GetValue(opt, null));
var all = Holes(); int maxId = 0; foreach (var h in all) { int idn; if (int.TryParse(h.HoleID, out idn)) maxId = Math.Max(maxId, idn); }
int id = FIRST_ID > 0 ? FIRST_ID : maxId + 1; double sgn = AT_START ? -1 : 1;
var mt = _opdbAsm.GetType("Deswik.OPDB.Models.CreateHolesAtDigitizedPointsModel"); var ct = _opdbAsm.GetType("Deswik.OPDB.Commands.CreateHoleAtPoint");
foreach (int r in ROWS) {
  Deswik.Graphics.Figures.BlastHole end = null; double ea = 0;
  foreach (var h in all) { if (h.HoleOption != T_PROD || h.NumberingRow != r) continue; double a = (h.CollarX - R0X) * AX + (h.CollarY - R0Y) * AY; if (end == null || sgn * a > sgn * ea) { end = h; ea = a; } }
  if (end == null) { Out.AppendLine("row " + r + ": no holes"); continue; }
  double x = end.CollarX + sgn * S * AX, y = end.CollarY + sgn * S * AY;
  for (int k = 0; k < 400 && (!Inside(x, y) || EdgeDist(x, y) < EDGE_IN); k++) { x -= sgn * 0.01 * AX; y -= sgn * 0.01 * AY; }
  double nearest = 1e9; foreach (var h in all) nearest = Math.Min(nearest, Math.Sqrt((h.CollarX - x) * (h.CollarX - x) + (h.CollarY - y) * (h.CollarY - y)));
  var m = Activator.CreateInstance(mt);
  mt.GetProperty("ActiveHoleTemplateName").SetValue(m, T_PROD, null); mt.GetProperty("HoleIdType").SetValue(m, Enum.Parse(_opdbAsm.GetType("Deswik.OPDB.Enums.HoleIdType"), "Numeric"), null);
  mt.GetProperty("NumericIdNumber").SetValue(m, id, null); mt.GetProperty("Dip").SetValue(m, 90.0, null); mt.GetProperty("Bearing").SetValue(m, 0.0, null);
  var cmd = Activator.CreateInstance(ct, new object[] { App, new Deswik.Graphics.Geometry.Point(x, y, GRADE_RL + 10), m, CFG });
  try { ct.GetMethod("Execute").Invoke(cmd, null); } catch (Exception e) { Out.AppendLine("ERROR " + id + ": " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
  Out.AppendLine(string.Format("row {0}: hole {1}, in line with {2}, {3:0.00} m from it, {4:0.00} m inside the boundary, nearest hole {5:0.00} m", r, id, end.HoleID, Math.Sqrt((end.CollarX - x) * (end.CollarX - x) + (end.CollarY - y) * (end.CollarY - y)), EdgeDist(x, y), nearest));
  id++; }
return null;
