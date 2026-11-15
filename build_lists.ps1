# Builds the new path lists and priority changes from %TEMP%\deswik-cmd1\tasks.tsv + paths.tsv (written by export.cs).
#   -Mode Loaders  : one path per loader, = tasks whose physical resource is that loader, in IS start order.
#   -Mode Drilling : one drill path (default DRILL_1), Pre-Split then Production per shot (shot = task Name),
#                    shots in IS order; tasks on -ExcludePaths (default none) are left alone.
# Priorities start at the lowest priority currently on that path (fallback 1) and go up by 1.
param(
  [ValidateSet('Loaders','Drilling')][string]$Mode = 'Loaders',
  [string[]]$Loaders = @('LOADER_1','LOADER_2'),
  [string]$DrillPath = 'DRILL_1',
  [string[]]$ExcludePaths = @()
)
$d = Join-Path ([IO.Path]::GetTempPath()) 'deswik-cmd1'
Get-ChildItem $d -Filter 'path__*.txt' -ErrorAction SilentlyContinue | Remove-Item
$T = Import-Csv "$d\tasks.tsv" -Delimiter "`t"
foreach ($x in $T) { $x.Start = [datetime]$x.Start; $x.Finish = [datetime]$x.Finish; $x.Priority = [double]$x.Priority }
$P = Import-Csv "$d\paths.tsv" -Delimiter "`t"
$byId = @{}; foreach ($x in $T) { $byId[[string]$x.Id] = $x }
function StartPri([string]$res) {
  $cur = @($P | ? { $_.Resource -eq $res } | % { $byId[[string]$_.ID].Priority } | ? { $_ -gt 0 })
  if ($cur.Count) { [int]($cur | Measure-Object -Minimum).Minimum } else { 1 }
}
$plan = [ordered]@{}
if ($Mode -eq 'Loaders') {
  foreach ($L in $Loaders) { $plan[$L] = @($T | ? { $_.Res -eq $L } | Sort-Object Start, Finish) }
} else {
  $excl = @($P | ? { $ExcludePaths -contains $_.Resource } | % { [string]$_.ID })
  $scope = @($T | ? { $_.ActType -in 'Pre-Split Drilling','Production Drilling' -and $excl -notcontains $_.Id })
  $shots = $scope | Group-Object Name | Sort-Object { ($_.Group | Measure-Object Start -Minimum).Minimum }
  $plan[$DrillPath] = @(foreach ($s in $shots) { $s.Group | Sort-Object { if ($_.ActType -eq 'Pre-Split Drilling') { 0 } else { 1 } }, Start })
  $odd = @($shots | ? { @($_.Group | ? ActType -eq 'Pre-Split Drilling').Count -ne 1 -or @($_.Group | ? ActType -eq 'Production Drilling').Count -ne 1 })
  if ($odd.Count) { "WARNING: shots without exactly one Pre-Split and one Production: " + (($odd | % Name) -join ', ') }
  $nolink = @($shots | ? { $ps = @($_.Group | ? ActType -eq 'Pre-Split Drilling')[0]; $pr = @($_.Group | ? ActType -eq 'Production Drilling')[0]; $ps -and $pr -and ($pr.Preds -split ';') -notcontains $ps.Id } | % Name)
  "Shots with no Pre-Split -> Production link: $($nolink.Count) " + ($nolink -join ', ')
}
$pri = @()
foreach ($res in $plan.Keys) {
  $list = $plan[$res]; $s0 = StartPri $res
  [IO.File]::WriteAllLines("$d\path__$res.txt", [string[]]($list | % { [string]$_.Id }))
  $old = @($P | ? { $_.Resource -eq $res } | % { [string]$_.ID })
  $added = @($list | ? { $old -notcontains $_.Id }).Count
  $removed = @($old | Select-Object -Unique | ? { ($list.Id) -notcontains $_ }).Count
  $chg = 0
  for ($i = 0; $i -lt $list.Count; $i++) { $np = $i + $s0; if ($list[$i].Priority -ne $np) { $chg++; $pri += [pscustomobject]@{ task_id = [string]$list[$i].Id; value = [string]$np } } }
  "{0}: {1} tasks (was {2}), {3} added, {4} removed, priorities {5}-{6}, {7} priority changes" -f $res, $list.Count, $old.Count, $added, $removed, $s0, ($s0 + $list.Count - 1), $chg
}
$pri | Export-Csv "$d\priorities.csv" -NoTypeInformation
