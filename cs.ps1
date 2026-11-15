param([string]$CodeFile, [string]$Description = 'read-only query', [switch]$Modifies, [string[]]$Usings = @())
$code = [string][System.IO.File]::ReadAllText($CodeFile)
$inp = @{ description = $Description; code = $code; modifies = [bool]$Modifies; usings = $Usings } | ConvertTo-Json -Compress -Depth 5
$c = New-Object System.IO.Pipes.NamedPipeClientStream('.', 'KennetDeswikClaude', [System.IO.Pipes.PipeDirection]::InOut)
$c.Connect(5000)
$w = New-Object System.IO.StreamWriter($c); $w.AutoFlush = $true
$r = New-Object System.IO.StreamReader($c)
$w.WriteLine('{"op":"call","name":"run_csharp","input":' + $inp + '}')
$resp = $r.ReadLine() | ConvertFrom-Json
$c.Dispose()
if ($resp.isError) { "ERROR:" }
$resp.text
