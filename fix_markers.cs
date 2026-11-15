// fix_markers.cs  (MODIFIES)  - prepend _common.cs
// Each OPDB run flags the previous markers Deleted=true but they stay in ModelSpace.Entities.
// If a clean-up removed the live markers by mistake, this brings back one collar marker per hole
// (the one sitting exactly on the collar) and hides deleted markers that are still drawn.
string ML = HL + BS + "MARKERS";
var live = new HashSet<string>(); var byGuid = new Dictionary<string, Deswik.Graphics.Figures.BlastHole>();
foreach (var h in Holes()) byGuid[((Deswik.Graphics.Primaries.Figure)h).GUID] = h;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && SameLayer(f.Layer.Name, ML) && Convert.ToString(f.AttributeValueGet("Location", "")) == "Collar") live.Add(Convert.ToString(f.AttributeValueGet("_BlastHoleGuid", "")));
int restored = 0, hidden = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (!f.Deleted || !SameLayer(f.Layer.Name, ML)) continue;
  string g = Convert.ToString(f.AttributeValueGet("_BlastHoleGuid", ""));
  if (Convert.ToString(f.AttributeValueGet("Location", "")) == "Collar" && byGuid.ContainsKey(g) && !live.Contains(g)) {
    var h = byGuid[g]; var bb = f.BoundingBox; double cx = (bb.Min.x + bb.Max.x) / 2, cy = (bb.Min.y + bb.Max.y) / 2;
    if (Math.Abs(cx - h.CollarX) < 0.05 && Math.Abs(cy - h.CollarY) < 0.05) { f.Deleted = false; f.Visible = true; live.Add(g); restored++; continue; } }
  if (f.Visible) { f.Visible = false; hidden++; } }
try { App.MenuCommand.ExecuteByID("CID_View_Regen", Deswik.Common.Utilities.CommandOrigin.OtherCommand); } catch { }
Out.AppendLine("restored " + restored + " collar markers, hid " + hidden + " deleted markers");
return null;
