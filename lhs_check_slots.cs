// LHS: check mining slots (Dynamic Slot Connectors output) before running a scenario. Read-only.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)  (C# 5)
// Reports: blocks with no slot, count by Route Quality, slots that do not start on a haul-road vertex
// (LHS rejects those: "No haul road connection exists for this connector"), and where routes exit.

// ---- CONFIG ----
string SLOT_LAYER  = "0\\LHS\\DYNAMIC SLOTS";     // the layer the scenario's mining block connector layer points to
string ROAD_LAYER  = "0\\LHS\\ROADS";             // the scenario's haul road layer
string BLOCK_LAYER = "0\\SCHEDULER\\TASK SOLIDS"; // mining blocks (ID attribute)
// ----------------

var road = new Dictionary<string, string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer.Name != ROAD_LAYER || !f.IsPolyline) continue; var p = f.asPolyline;
  for (int i = 0; i < p.VertexCount; i++) { var v = p.GetVertex(i); road[Math.Round(v.x, 2) + "," + Math.Round(v.y, 2) + "," + Math.Round(v.z, 2)] = "" + f.AttributeValueGet("Haul ID", ""); } }

var routed = new HashSet<string>(); var quality = new Dictionary<string, int>(); var exits = new Dictionary<string, int>(); int n = 0, bad = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer.Name != SLOT_LAYER || !f.IsPolyline) continue; n++;
  routed.Add("" + f.AttributeValueGet("TOID", ""));
  string q = "" + f.AttributeValueGet("Route Quality", ""); int c; quality.TryGetValue(q, out c); quality[q] = c + 1;
  var a = f.asPolyline.GetVertex(0); string k = Math.Round(a.x, 2) + "," + Math.Round(a.y, 2) + "," + Math.Round(a.z, 2);
  string r; string e = road.TryGetValue(k, out r) ? "road " + r : "NOT ON ROAD " + k; if (!road.ContainsKey(k)) bad++;
  string ek = "Stage " + f.AttributeValueGet("Stage", "") + " starts at " + e; exits.TryGetValue(ek, out c); exits[ek] = c + 1; }

var missing = new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
  if (f.Layer.Name == BLOCK_LAYER) { string id = "" + f.AttributeValueGet("ID", ""); if (!routed.Contains(id)) missing.Add(id); }

Out.AppendLine("Slots " + n + " | blocks with a slot " + routed.Count + " | blocks without " + missing.Count + " | slots not starting on a road vertex " + bad);
foreach (var kv in quality) Out.AppendLine("  " + kv.Key + ": " + kv.Value + (kv.Key.StartsWith("Closest") ? "   <- drop-down routes, fix the ramps" : ""));
foreach (var kv in exits.OrderBy(x => x.Key)) Out.AppendLine("  " + kv.Key + ": " + kv.Value);
foreach (var m in missing.Take(20)) Out.AppendLine("  no slot: " + m);
return null;
