// 12_compare_metres.cs  (read-only)  - prepend _common.cs
// Holes and drill metres per template: this pattern vs another holes layer (e.g. the original design).
string OTHER_HOLES_LAYER = "DRILL_AND_BLAST" + BS + "SHOT_NAME" + BS + "HOLES";
int GAP_ID_MIN = int.MaxValue; // first gap production hole ID in this pattern, to report them separately
var res = new Dictionary<string, Dictionary<string, double[]>>(); res["OTHER"] = new Dictionary<string, double[]>(); res["THIS"] = new Dictionary<string, double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted || !f.IsBlastHole) continue; string k = SameLayer(f.Layer.Name, HL) ? "THIS" : (SameLayer(f.Layer.Name, OTHER_HOLES_LAYER) ? "OTHER" : null); if (k == null) continue;
  var h = f.asBlastHole; string t = h.HoleOption; int idn; if (k == "THIS" && t == T_PROD && int.TryParse(h.HoleID, out idn) && idn >= GAP_ID_MIN) t = T_PROD + " (gap)";
  var d = res[k]; if (!d.ContainsKey(t)) d[t] = new double[] { 0, 0 }; d[t][0]++; d[t][1] += h.HoleLength; }
var keys = new SortedSet<string>(); foreach (var d in res.Values) foreach (var k in d.Keys) keys.Add(k);
double[] to = { 0, 0 }, tt = { 0, 0 };
foreach (var k in keys) { var o = res["OTHER"].ContainsKey(k) ? res["OTHER"][k] : new double[] { 0, 0 }; var t = res["THIS"].ContainsKey(k) ? res["THIS"][k] : new double[] { 0, 0 };
  Out.AppendLine(string.Format("{0} | other {1} holes {2:0.0} m | this {3} holes {4:0.0} m | diff {5:+0.0;-0.0;0} m", k, o[0], o[1], t[0], t[1], t[1] - o[1]));
  to[0] += o[0]; to[1] += o[1]; tt[0] += t[0]; tt[1] += t[1]; }
Out.AppendLine(string.Format("TOTAL | other {0} holes {1:0.0} m | this {2} holes {3:0.0} m | diff {4:+0.0;-0.0} m", to[0], to[1], tt[0], tt[1], tt[1] - to[1]));
return null;
