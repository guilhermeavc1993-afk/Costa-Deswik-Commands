// ===== _common.cs : prepend to every Command Drill Design script =====
// run_csharp body (C# 5). Edit the CONFIG block per shot; everything else is read from the drawing.

// ---------- CONFIG ----------
string PATTERN = "SHOT_NAME";                 // OPDB drill pattern name (make it the active pattern)
string CREST_LAYER_CONTAINS = "PIT DESIGN_STRINGS"; // part of the pit design strings layer name
double CREST_RL = 0;                          // RL of the crest above the wall rows (bench top)
double[] CREST_A = { 0, 0 };                  // crest line end points (X,Y) - from 01_inputs.cs
double[] CREST_B = { 0, 0 };
// from the signed D&B proposal (collar distance from crest peg, m)
double STAB_OFF = 2.8, BATTER_OFF = 5.1, BUFFER_OFF = 7.5, PROD_OFF = 10.8;
double STAB_TOE_RL = 0, GRADE_RL = 0;         // stab toe RL (mid-bench) and grade RL
// hole templates (OPDB names)
string T_STAB = "5_Stab", T_BATTER = "1_Batter", T_BUFFER = "2_Buffer", T_PROD = "4_Production";

// ---------- HELPERS ----------
string BS = ((char)92).ToString(); // never write a backslash literal in run_csharp strings
System.Reflection.Assembly _opdbAsm = null;
foreach (var _a in AppDomain.CurrentDomain.GetAssemblies()) if (_a.GetName().Name == "Deswik.OPDB") _opdbAsm = _a;
object _opdb = null;
foreach (var _o in (System.Collections.IEnumerable)App.Plugins)
  foreach (var _pn in new string[] { "Instance", "Plugin", "Value", "Object" }) {
    var _pp = _o.GetType().GetProperty(_pn); if (_pp == null) continue;
    try { var _v = _pp.GetValue(_o, null); if (_v != null && _v.GetType().FullName == "Deswik.OPDB.BlastHoleDesign") _opdb = _v; } catch { } }
if (_opdb == null) { Out.AppendLine("OPDB is not loaded"); return null; }
object CFG = null;
foreach (var _c in (System.Collections.IEnumerable)_opdb.GetType().GetProperty("DrillPatternConfiguration").GetValue(_opdb, null)) if (_c.ToString() == PATTERN) CFG = _c;
if (CFG == null) { Out.AppendLine("pattern not found: " + PATTERN); return null; }
Func<string, string> CfgStr = (_n) => Convert.ToString(CFG.GetType().GetProperty(_n).GetValue(CFG, null));
string ROOT = CfgStr("PatternLayerName"), HL = CfgStr("GridHolesLayer"), DL = CfgStr("PolyLayerName");
Out.AppendLine("doc " + System.IO.Path.GetFileName(App.Filename) + " | pattern " + PATTERN + " | active " + _opdb.GetType().GetProperty("ActiveBlastHolePattern").GetValue(_opdb, null));

Func<string, string, bool> SameLayer = (_l1, _l2) => string.Equals(_l1, _l2, StringComparison.OrdinalIgnoreCase);
Func<string, Deswik.Graphics.Primaries.Layer> GetLayer = (_name) => {
  foreach (Deswik.Graphics.Primaries.Layer _L in App.Layers) if (SameLayer(_L.Name, _name)) return _L;
  App.Layers.Add(_name, false, 1);
  foreach (Deswik.Graphics.Primaries.Layer _L in App.Layers) if (SameLayer(_L.Name, _name)) return _L;
  return null; };
Func<string, Deswik.Graphics.Figures.Polyline> PolyOn = (_layer) => {
  foreach (Deswik.Graphics.Primaries.Figure _f in App.ModelSpace.Entities) if (!_f.Deleted && _f.asPolyline != null && SameLayer(_f.Layer.Name, _layer)) return _f.asPolyline;
  return null; };
Func<List<Deswik.Graphics.Figures.BlastHole>> Holes = () => {
  var _r = new List<Deswik.Graphics.Figures.BlastHole>();
  foreach (Deswik.Graphics.Primaries.Figure _f in App.ModelSpace.Entities) if (!_f.Deleted && _f.IsBlastHole && SameLayer(_f.Layer.Name, HL)) _r.Add(_f.asBlastHole);
  return _r; };

// boundary polygon
var BND = PolyOn(DL + BS + "BOUNDARY");
if (BND == null) { Out.AppendLine("no polyline on " + DL + BS + "BOUNDARY"); return null; }
var BXl = new List<double>(); var BYl = new List<double>();
foreach (Deswik.Graphics.Geometry.Point _p in BND.VertexList) { BXl.Add(_p.x); BYl.Add(_p.y); }
double[] BX = BXl.ToArray(), BY = BYl.ToArray(); int NB = BX.Length;
double MX = 0, MY = 0; for (int _i = 0; _i < NB; _i++) { MX += BX[_i] / NB; MY += BY[_i] / NB; }
Func<double, double, bool> Inside = (_x, _y) => { bool _c = false; for (int _i = 0, _j = NB - 1; _i < NB; _j = _i++) { if (((BY[_i] > _y) != (BY[_j] > _y)) && (_x < (BX[_j] - BX[_i]) * (_y - BY[_i]) / (BY[_j] - BY[_i]) + BX[_i])) _c = !_c; } return _c; };
Func<double, double, double> EdgeDist = (_x, _y) => { double _b = 1e9; for (int _i = 0, _j = NB - 1; _i < NB; _j = _i++) { double _ex = BX[_i] - BX[_j], _ey = BY[_i] - BY[_j]; double _t = ((_x - BX[_j]) * _ex + (_y - BY[_j]) * _ey) / (_ex * _ex + _ey * _ey); _t = Math.Max(0, Math.Min(1, _t)); double _dx = BX[_j] + _t * _ex - _x, _dy = BY[_j] + _t * _ey - _y; _b = Math.Min(_b, Math.Sqrt(_dx * _dx + _dy * _dy)); } return _b; };

// crest frame: U along the crest, N into the shot
double UX = CREST_B[0] - CREST_A[0], UY = CREST_B[1] - CREST_A[1]; double _ul = Math.Sqrt(UX * UX + UY * UY); if (_ul > 0) { UX /= _ul; UY /= _ul; }
double NX = -UY, NY = UX; if ((MX - CREST_A[0]) * NX + (MY - CREST_A[1]) * NY < 0) { NX = -NX; NY = -NY; }
Func<double, double, double> DistCrest = (_x, _y) => (_x - CREST_A[0]) * NX + (_y - CREST_A[1]) * NY;

// row frame: A along ROW ORIENTATION, B (burden) into the shot
var ROWO = PolyOn(DL + BS + "ROW ORIENTATION");
double R0X = 0, R0Y = 0, AX = 1, AY = 0, BBX = 0, BBY = 1;
if (ROWO != null) {
  var _p0 = ROWO.VertexList[0]; var _p1 = ROWO.VertexList[ROWO.VertexList.Count - 1];
  R0X = _p0.x; R0Y = _p0.y; AX = _p1.x - _p0.x; AY = _p1.y - _p0.y; double _al = Math.Sqrt(AX * AX + AY * AY); AX /= _al; AY /= _al;
  BBX = -AY; BBY = AX; if ((MX - R0X) * BBX + (MY - R0Y) * BBY < 0) { BBX = -BBX; BBY = -BBY; } }

// find a control by name inside a dialog
Func<System.Windows.Forms.Control, string, System.Windows.Forms.Control> ByName = null;
ByName = (_ctl, _nm) => { if (_ctl.Name == _nm) return _ctl; foreach (System.Windows.Forms.Control _ch in _ctl.Controls) { var _r = ByName(_ch, _nm); if (_r != null) return _r; } return null; };
// ===== end _common.cs =====
