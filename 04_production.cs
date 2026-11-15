// 04_production.cs  (MODIFIES)  - prepend _common.cs
// OPDB "holes within boundary polyline" on HOLE DESIGN\PRODS: Grid along ROW ORIENTATION, staggered left.
// (PatternType AlignedToPoly only produced one row - use Grid.)
var prods = PolyOn(DL + BS + "HOLE DESIGN" + BS + "PRODS");
if (prods == null || ROWO == null) { Out.AppendLine("need the HOLE DESIGN PRODS polygon and the ROW ORIENTATION line"); return null; }
object opt = CFG.GetType().GetMethod("FindOptionByName").Invoke(CFG, new object[] { T_PROD });
double B = Convert.ToDouble(opt.GetType().GetProperty("Burden").GetValue(opt, null)), S = Convert.ToDouble(opt.GetType().GetProperty("Spacing").GetValue(opt, null));
var p0 = ROWO.VertexList[0]; double azi = (Math.Atan2(AX, AY) * 180 / Math.PI + 360) % 360;
var mt = _opdbAsm.GetType("Deswik.OPDB.Models.CreateHolesWithinBoundaryModel"); var m = Activator.CreateInstance(mt);
Action<string, object> P = (n, v) => mt.GetProperty(n).SetValue(m, v, null);
Func<string, string, object> E = (t, v) => Enum.Parse(_opdbAsm.GetType("Deswik.OPDB.Enums." + t), v);
P("ActiveHoleTemplateName", T_PROD); P("BoundaryPolyline", prods); P("Burden", B); P("Spacing", S); P("Dip", 90.0);
P("ProximityRadiusMin", 1.5); P("ProximityRadiusMax", 6.0);
P("PatternStaggerType", E("StaggerType", "Left")); P("PatternOffsetDirection", E("OffsetDirection", "Left")); P("PatternOffsetDistance", 0.0);
P("AlignRowsAlongPolyline", false); P("GridRowAlignmentPolyline", null); P("PatternType", E("HolePatternType", "Grid"));
P("PatternDirectionX", AX); P("PatternDirectionY", AY); P("PatternDirectionZ", 0.0); P("PatternDirectionAzi", azi);
P("OriginWorld", new Deswik.Graphics.Geometry.Point(p0.x, p0.y, p0.z)); P("RowAlignment", new Deswik.Graphics.Geometry.Vector(AX, AY, 0));
P("HoleAzimuthOption", E("CreateHoleAzimuthMode", "HoleTemplate")); P("Bearing", 0.0); P("Tolerance", 0.25);
P("AlternateHoleIds", false); P("ContinueNumbering", false); P("CreateBoundaryHoles", false); P("ExtendGridToBoundary", false);
P("MinimumHoleLength", 0.0); P("RemoveHolesUnderMinimumLength", false); P("CreateGrid", false); P("UseVariableSpacing", false);
var cfgList = _opdb.GetType().GetProperty("DrillPatternConfiguration").GetValue(_opdb, null);
var spt = _opdbAsm.GetType("Deswik.OPDB.ServiceProviders.CreateHolesServiceProvider");
var sp = Activator.CreateInstance(spt, new object[] { App, cfgList, PATTERN });
try { spt.GetMethod("CreateHolesWithinBoundary").Invoke(sp, new object[] { m }); } catch (Exception e) { Out.AppendLine("ERROR: " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
var offs = new SortedDictionary<double, int>(); int cnt = 0; double len = 0;
foreach (var h in Holes()) if (h.HoleOption == T_PROD) { cnt++; len += h.HoleLength; double o = Math.Round(((h.CollarX - R0X) * BBX + (h.CollarY - R0Y) * BBY) * 10) / 10; if (!offs.ContainsKey(o)) offs[o] = 0; offs[o]++; }
Out.AppendLine("production: " + cnt + " holes, " + len.ToString("0") + " m, " + offs.Count + " rows");
var sb = new StringBuilder(); foreach (var kv in offs) sb.Append(kv.Key + "m:" + kv.Value + "  "); Out.AppendLine("row offsets: " + sb);
return null;
