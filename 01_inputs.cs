// 01_inputs.cs  (read-only)  - prepend _common.cs
// Reports the boundary, the row orientation, crest-string candidates at CREST_RL next to the boundary,
// and the OPDB pattern's surfaces, overrides and hole templates (compare with the D&B proposal).
Out.AppendLine("BOUNDARY: " + NB + " vertices, perimeter " + BND.Length.ToString("0.0") + " m, area " + BND.Area.ToString("0") + " m2");
for (int i = 0; i < NB; i++) { int j = (i + 1) % NB; double dx = BX[j] - BX[i], dy = BY[j] - BY[i]; Out.AppendLine(string.Format("  edge {0}: {1:0.0} m, bearing {2:0.0}", i, Math.Sqrt(dx * dx + dy * dy), (Math.Atan2(dx, dy) * 180 / Math.PI + 360) % 360)); }
if (ROWO != null) Out.AppendLine(string.Format("ROW ORIENTATION: {0:0.0} m, bearing {1:0.0}, burden direction bearing {2:0.0}", ROWO.Length, (Math.Atan2(AX, AY) * 180 / Math.PI + 360) % 360, (Math.Atan2(BBX, BBY) * 180 / Math.PI + 360) % 360));
else Out.AppendLine("no ROW ORIENTATION polyline found");
// crest candidates: straight-ish segments at CREST_RL within 15 m of the boundary
int found = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted || f.asPolyline == null || f.Layer.Name.IndexOf(CREST_LAYER_CONTAINS, StringComparison.OrdinalIgnoreCase) < 0) continue;
  var pl = f.asPolyline; var vs = new List<Deswik.Graphics.Geometry.Point>(); foreach (Deswik.Graphics.Geometry.Point p in pl.VertexList) vs.Add(p);
  for (int i = 0; i + 1 < vs.Count; i++) { var a = vs[i]; var b = vs[i + 1];
    if (Math.Abs(a.z - CREST_RL) > 0.2 || Math.Abs(b.z - CREST_RL) > 0.2) continue;
    double len = Math.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y)); if (len < 10) continue;
    double mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2; if (EdgeDist(mx, my) > 15 && !Inside(mx, my)) continue;
    found++; Out.AppendLine(string.Format("CREST candidate: A=({0:0.00},{1:0.00}) B=({2:0.00},{3:0.00}) len {4:0.0} m, mid {5:0.0} m from boundary", a.x, a.y, b.x, b.y, len, EdgeDist(mx, my))); } }
Out.AppendLine("crest candidates: " + found + "  -> copy the wall-side one into CREST_A / CREST_B");
// pattern
var pc = CFG.GetType().GetProperty("PatternConfig").GetValue(CFG, null);
foreach (var pn in new string[] { "CollarSurfaceLayerName", "ToeSurfaceRL", "GradeSurfaceRL" }) Out.AppendLine("pattern " + pn + " = " + pc.GetType().GetProperty(pn).GetValue(pc, null));
foreach (var so in (System.Collections.IEnumerable)pc.GetType().GetProperty("SurfaceOverrides").GetValue(pc, null)) Out.AppendLine("  override " + so.GetType().GetProperty("Name").GetValue(so, null) + " -> " + so.GetType().GetProperty("LayerName").GetValue(so, null));
foreach (var op in (System.Collections.IEnumerable)CFG.GetType().GetProperty("Options").GetValue(CFG, null)) {
  Func<string, object> G = (n) => op.GetType().GetProperty(n).GetValue(op, null);
  Out.AppendLine(string.Format("  template {0}: dia {1} B {2} S {3} subdrill {4} dip {5} min/max {6}/{7} prefix {8}", G("Name"), G("Diameter"), G("Burden"), G("Spacing"), G("SubDrill"), G("Dip"), G("MinLength"), G("MaxLength"), G("Prefix"))); }
Out.AppendLine("holes already on " + HL + ": " + Holes().Count);
return null;
