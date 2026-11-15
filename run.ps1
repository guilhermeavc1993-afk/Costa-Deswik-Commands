# Helpers for Command 1. Usage:
#   run.ps1 export                 (read-only)
#   run.ps1 apply                  (changes paths; approval window in Deswik)
#   run.ps1 priorities             (changes priorities from priorities.csv; approval window in Deswik)
param([ValidateSet('export','apply','priorities')][string]$Step)
$here = $PSScriptRoot
$d = Join-Path ([IO.Path]::GetTempPath()) 'deswik-cmd1'
New-Item -ItemType Directory -Force $d | Out-Null
function Retry($block) {
  for ($k = 0; $k -lt 6; $k++) {
    $o = & $block
    $txt = $o -join "`n"
    if ($txt -notmatch 'NO_SCHEDULE|No IS schedule is loaded|NullReference') { return $o }
    Start-Sleep 3
  }
  "FAILED: the plugin could not see the IS schedule. Click on the Deswik CAD window and try again."
}
switch ($Step) {
  'export' { Retry { & "$here\cs.ps1" "$here\export.cs" 'Command 1: export tasks and resource paths (read-only)' } }
  'apply'  { Retry { & "$here\cs.ps1" "$here\apply_paths.cs" 'Command 1: rebuild resource paths in IS order (dates, links and assignments unchanged)' -Modifies } }
  'priorities' {
    $ch = @(Import-Csv "$d\priorities.csv" | % { @{ task_id = [string]$_.task_id; field = 'Priority'; value = [string]$_.value } })
    if (-not $ch.Count) { "No priority changes needed."; return }
    $inp = @{ reason = "Command 1: priorities to match the rebuilt resource paths ($($ch.Count) tasks)."; changes = $ch } | ConvertTo-Json -Compress -Depth 5
    $out = (Retry { & "$here\dsw.ps1" is_set_task_fields $inp }) -join "`n"
    ($out -split "`r?`n" | Select-Object -First 3) -join "`n"
  }
}
