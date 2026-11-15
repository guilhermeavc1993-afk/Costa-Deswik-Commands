// 03_wall_rows.cs  (MODIFIES)  - prepend _common.cs
// OPDB "trim holes along polyline" on the STAB, BATTER and BUFFER lines, one row each (NumOffsetRows = 0).
// Stab toes are then sent to the STAB TOE surface override.
string HD = DL + BS + "HOLE DESIGN";
var mt = _opdbAsm.GetType("Deswik.OPDB.Models.CreateTrimHolesAlongPolylinesModel");
var ct = _opdbAsm.GetType("Deswik.OPDB.Commands.CreateTrimHolesAlongPolylines");
var IF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object[][] rows = { new object[] { "STAB", T_STAB }, new object[] { "BATTER", T_BATTER }, new object[] { "BUFFER", T_BUFFER } };
foreach (var r in rows) {
  var line = PolyOn(HD + BS + (string)r[0]); if (line == null) { Out.AppendLine("missing line " + r[0]); continue; }
  object opt = CFG.GetType().GetMethod("FindOptionByName").Invoke(CFG, new object[] { (string)r[1] });
  double B = Convert.ToDouble(opt.GetType().GetProperty("Burden").GetValue(opt, null)), S = Convert.ToDouble(opt.GetType().GetProperty("Spacing").GetValue(opt, null));
  var m = Activator.CreateInstance(mt, true);
  Action<string, object> F = (n, v) => mt.GetField(n, IF).SetValue(m, v);
  F("HoleOptionName", (string)r[1]); F("Angle", 90.0); F("Tolerance", 0.25); F("UseDesiredSpacing", true); F("ExactSpacing", S); F("EvenSpacing", S);
  F("NumOffsetRows", 0); F("Burden", B); F("ProximityRadiusMin", 1.25); F("ProximityRadiusMax", 6.0); F("IsStaggered", false); F("AlternateHoleIds", false);
  F("CreateBurdenLines", false); F("OffsetLeft", false); F("MinLength", 0.0); F("DeleteMin", false); F("ContinueNumbering", false);
  F("DipMode", Enum.Parse(_opdbAsm.GetType("Deswik.OPDB.Enums.HoleDipMode"), "Angle"));
  F("ContourPolylines", new List<Deswik.Graphics.Figures.Polyline> { line });
  var cmd = Activator.CreateInstance(ct, IF, null, new object[] { App, m, CFG }, null);
  try { ct.GetMethod("Execute").Invoke(cmd, null); } catch (Exception e) { Out.AppendLine(r[0] + " ERROR: " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
}
var stabs = new List<Deswik.Graphics.Figures.BlastHole>();
foreach (var h in Holes()) if (h.HoleOption == T_STAB) { h.ToeOverride = "STAB TOE"; stabs.Add(h); }
try { CFG.GetType().GetMethod("RefreshBlastHoles").Invoke(CFG, new object[] { stabs }); } catch (Exception e) { Out.AppendLine("refresh ERROR: " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
var g = new Dictionary<string, double[]>();
foreach (var h in Holes()) { if (!g.ContainsKey(h.HoleOption)) g[h.HoleOption] = new double[] { 0, 0, 1e9, -1e9 }; var v = g[h.HoleOption]; v[0]++; v[1] += h.HoleLength; v[2] = Math.Min(v[2], h.ToeZ); v[3] = Math.Max(v[3], h.ToeZ); }
foreach (var kv in g) Out.AppendLine(kv.Key + ": " + kv.Value[0] + " holes, " + kv.Value[1].ToString("0") + " m, toe RL " + kv.Value[2].ToString("0.00") + " to " + kv.Value[3].ToString("0.00"));
foreach (var h in stabs) if (Math.Abs(h.ToeZ - STAB_TOE_RL) > 0.1) Out.AppendLine("  stab " + h.HoleID + " is not on the STAB TOE surface (toe " + h.ToeZ.ToString("0.00") + ")");
return null;
