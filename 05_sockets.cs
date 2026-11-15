// 05_sockets.cs  (MODIFIES)  - prepend _common.cs
// Socket circles = each hole's breakage area at grade (radius = half the spacing), made with
// Deswik's "Generate Intersection Markers" dialog (OPDB > Analysis > Socket Intersections):
//   base = pattern Holes layer, "figures to test against" = SELECTED holes of one template,
//   output = DESIGN LINES\SOCKETS\<sublayer>, block DM_Symbol1, distance = spacing / 2.
// RUNS: template, sublayer, ACI colour, hole-ID filter (min,max; 0,0 = all), clear previous markers
object[][] RUNS = {
  new object[] { T_PROD,   "PRODS",  3, 0, 0, true },
  new object[] { T_BUFFER, "BUFFER", 1, 0, 0, true },
  new object[] { T_BATTER, "BATTER", 2, 0, 0, true }
  // gap production example: new object[] { T_PROD, "GAP PRODUCTION", 3, 1001, 1999, false }
};
bool EXCLUDE_GAP_FROM_TEMPLATE_RUN = true; int GAP_ID_MIN = int.MaxValue; // set to the first gap production hole ID once gap holes exist
Type t = null; foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { var x = a.GetType("Deswik.Graphics.Dialogs.fPointIntersectionMarkers"); if (x != null) t = x; }
foreach (var run in RUNS) {
  string tpl = (string)run[0]; string outL = DL + BS + "SOCKETS" + BS + (string)run[1]; int aci = (int)run[2]; int idMin = (int)run[3], idMax = (int)run[4]; bool clear = (bool)run[5];
  GetLayer(outL);
  object opt = CFG.GetType().GetMethod("FindOptionByName").Invoke(CFG, new object[] { tpl });
  double half = Convert.ToDouble(opt.GetType().GetProperty("Spacing").GetValue(opt, null)) / 2.0;
  var figs = new List<Deswik.Graphics.Primaries.Figure>();
  foreach (var h in Holes()) { if (h.HoleOption != tpl) continue; int idn; bool num = int.TryParse(h.HoleID, out idn);
    if (idMin > 0) { if (!num || idn < idMin || idn > idMax) continue; }
    else if (EXCLUDE_GAP_FROM_TEMPLATE_RUN && num && idn >= GAP_ID_MIN) continue;
    figs.Add((Deswik.Graphics.Primaries.Figure)h); }
  var dlg = (System.Windows.Forms.Control)Activator.CreateInstance(t, new object[] { App, HL, outL });
  t.GetMethod("Show", new Type[0]).Invoke(dlg, null);
  var sel = App.Selections.MainSelection; sel.RemoveAll(); sel.AddItems(figs); App.MainSelectionUpdate(true, true);
  var esf = ByName(dlg, "esfFiguresToTest"); var rb = ByName(esf, "rbSelectedEntities"); rb.GetType().GetProperty("Checked").SetValue(rb, true, null); esf.GetType().GetMethod("UpdateDisplay").Invoke(esf, null);
  var lp = ByName(dlg, "lpOutputMarkersLayer"); lp.GetType().GetProperty("Text").SetValue(lp, outL, null);
  var sp = ByName(dlg, "spnDistance"); sp.GetType().GetProperty("EditValue").SetValue(sp, (decimal)half, null);
  var cp = ByName(dlg, "cpIntBlockColor"); cp.GetType().GetProperty("ColourIndex").SetValue(cp, aci - 1, null); // 0-based!
  var cl = ByName(dlg, "cClearLayer"); cl.GetType().GetProperty("Checked").SetValue(cl, clear, null);
  var btn = ByName(dlg, "btnProcess"); btn.GetType().GetMethod("PerformClick").Invoke(btn, null); // dialog closes itself
  int cnt = 0; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && SameLayer(f.Layer.Name, outL)) cnt++;
  Out.AppendLine(tpl + " -> " + (string)run[1] + ": selected " + figs.Count + ", markers on layer " + cnt + ", radius " + half);
}
App.Selections.MainSelection.RemoveAll(); App.MainSelectionUpdate(true, true);
return null;
