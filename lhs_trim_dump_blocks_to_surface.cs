// LHS: trim dump blocks to a survey (flyover) surface - keep only the space still above the surface.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)  (C# 5)
// Set DRY_RUN = true first: it reports the cut per block and changes nothing. Then false (modifies the drawing).
// Blocks are replaced in place: same layer, every attribute copied, original removed. Keep your own backup.
// A local patch of the surface is cut per block, so very large drone surfaces do not freeze Deswik.
// Large layers: run in batches (TIME_BUDGET_MS) - processed IDs are logged to LOG_FILE and skipped next run.

// ---- CONFIG ----
string DUMP_LAYER    = "0\\LHS\\DUMP BLOCKS";          // layer the LHS scenario reads dump solids from
string SURFACE_LAYER = "0\\SURVEY\\FLYOVER_LATEST";    // one polyface surface
string FILTER_ATTR   = "DUMP";                          // only blocks with this attribute value ...
string FILTER_VALUE  = "DUMP_A";                        // ... are processed
bool   DRY_RUN       = true;
double PAD           = 10;                              // m around each block for the surface patch
long   TIME_BUDGET_MS = 60000;
string LOG_FILE      = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lhs_trim_done.tsv");
// ----------------

var sw = System.Diagnostics.Stopwatch.StartNew();
Deswik.Graphics.Primaries.Layer D = null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name == DUMP_LAYER) D = l;
if (D == null) { Out.AppendLine("Dump layer not found"); return null; }
var attrs = new List<string>(); foreach (var a in D.Attributes) attrs.Add("" + a.GetType().GetProperty("Name").GetValue(a, null));
Deswik.Graphics.Figures.Polyface surf = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer.Name == SURFACE_LAYER && f.IsPolyface) surf = f.asPolyface;
if (surf == null) { Out.AppendLine("Surface not found"); return null; }
double surfTop = surf.BoundingBox.Max.z;
var done = new HashSet<string>();
if (!DRY_RUN && System.IO.File.Exists(LOG_FILE)) foreach (var ln in System.IO.File.ReadAllLines(LOG_FILE)) done.Add(ln.Split('\t')[0]);

var todo = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
  if (f.Layer.Name == DUMP_LAYER && f.IsPolyface && ("" + f.AttributeValueGet(FILTER_ATTR, "")) == FILTER_VALUE
      && f.asPolyface.BoundingBox.Min.z < surfTop && !done.Contains("" + f.AttributeValueGet("ID", ""))) todo.Add(f);

var log = new StringBuilder(); int trimmed = 0, untouched = 0, problems = 0, n = 0; double removed = 0;
foreach (var f in todo) {
  if (sw.ElapsedMilliseconds > TIME_BUDGET_MS) break;
  string id = "" + f.AttributeValueGet("ID", ""); var pf = f.asPolyface; var b = pf.BoundingBox;
  var patch = surf.ExtractSurfaceWithinBounds(new Deswik.Geometry.RTree.Rectangle(b.Min.x - PAD, b.Min.y - PAD, b.Max.x + PAD, b.Max.y + PAD, -1000, 2000), true, false);
  double v0 = pf.Volume, below = 0, above = 0; string status;
  if (patch == null || patch.FaceCount == 0) { status = "NO_SURFACE"; problems++; }
  else {
    pf.CalculateSurfaceCutFillVolumes(patch, 0.01, out below, out above);
    if (below < 1) { status = "UNTOUCHED"; untouched++; }
    else {
      Deswik.Graphics.Figures.Polyface r = null; try { r = pf.BooleanOut(patch, Deswik.Graphics.Figures.Polyface.BooleanConstants.KeepAbove, true); } catch { }
      bool ok = r != null && r.IsClosed && Math.Abs(r.Volume - above) <= Math.Max(5, 0.01 * v0);
      if (!ok) { status = "BAD_CUT"; problems++; }
      else if (DRY_RUN) { status = "WOULD_TRIM"; trimmed++; removed += v0 - r.Volume; }
      else {
        var vals = new Dictionary<string, object>(); foreach (var a in attrs) { var v = f.AttributeValueGet(a, null); if (v != null && ("" + v) != "") vals[a] = v; }
        var nf = (Deswik.Graphics.Primaries.Figure)(object)r; nf.Layer = D;
        try { f.CopyStyleProperties(nf); } catch { }
        App.ModelSpace.Entities.AddItem(nf, null, null, true, false);
        foreach (var kv in vals) nf.AttributeValueSet(kv.Key, kv.Value);
        bool same = true; foreach (var kv in vals) if (("" + nf.AttributeValueGet(kv.Key, "")) != ("" + kv.Value)) same = false;
        if (same) { App.ModelSpace.Entities.Remove(f); status = "TRIMMED"; trimmed++; removed += v0 - r.Volume; }
        else { App.ModelSpace.Entities.Remove(nf); status = "ATTR_FAIL"; problems++; }
      }
    }
  }
  n++; log.AppendLine(id + "\t" + v0.ToString("0") + "\t" + below.ToString("0") + "\t" + above.ToString("0") + "\t" + status);
}
if (!DRY_RUN) { System.IO.File.AppendAllText(LOG_FILE, log.ToString()); App.Redraw(true); }
Out.AppendLine((DRY_RUN ? "DRY RUN - " : "") + "processed " + n + " | trimmed " + trimmed + " | untouched " + untouched + " | problems " + problems + " | removed " + removed.ToString("N0") + " | left " + (todo.Count - n));
foreach (var ln in log.ToString().Split('\n')) if (ln.Contains("BAD") || ln.Contains("NO_SURFACE") || ln.Contains("FAIL")) Out.AppendLine("  " + ln.Trim());
return null;
