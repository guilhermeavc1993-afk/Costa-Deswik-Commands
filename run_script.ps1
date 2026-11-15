# Sends a Command Drill Design script to Deswik through the Claude Assistant plugin's named pipe.
# _common.cs is prepended automatically. CONFIG values can be overridden without editing files:
#   .\run_script.ps1 ..\commands\command-drill-design\scripts\01_inputs.cs -Set @{ PATTERN='"SHOT_A"'; CREST_RL='345' }
#   .\run_script.ps1 ...\02_design_lines.cs -Modifies -Set @{ CREST_A='{ 1000.0, 2000.0 }' }
param(
  [Parameter(Mandatory)][string]$Script,
  [hashtable]$Set = @{},
  [switch]$Modifies,
  [string]$Pipe = 'KennetDeswikClaude'
)
$common = Join-Path (Split-Path $Script) '_common.cs'
$code = ''
if ((Test-Path $common) -and ((Split-Path $Script -Leaf) -ne '_common.cs')) { $code = [IO.File]::ReadAllText($common) + "`n" }
$code += [IO.File]::ReadAllText($Script)
foreach ($k in $Set.Keys) {
  $rx = '(\b' + [regex]::Escape($k) + '\s*=\s*)(\{[^}]*\}|[^;,]+)'
  $re = New-Object System.Text.RegularExpressions.Regex($rx)
  if (-not $re.IsMatch($code)) { throw "CONFIG key not found: $k" }
  $code = $re.Replace($code, { param($m) $m.Groups[1].Value + $Set[$k] }, 1)
}
$inp = @{ description = (Split-Path $Script -Leaf); code = $code; modifies = [bool]$Modifies; usings = @() } | ConvertTo-Json -Compress -Depth 5
$c = New-Object System.IO.Pipes.NamedPipeClientStream('.', $Pipe, [System.IO.Pipes.PipeDirection]::InOut)
$c.Connect(5000)
$w = New-Object System.IO.StreamWriter($c); $w.AutoFlush = $true
$r = New-Object System.IO.StreamReader($c)
$w.WriteLine('{"op":"call","name":"run_csharp","input":' + $inp + '}')
$resp = $r.ReadLine() | ConvertFrom-Json
$c.Dispose()
if ($resp.isError) { 'ERROR:' }
$resp.text
