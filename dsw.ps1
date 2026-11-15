param([string]$Name, [string]$InputJson = '{}')
$c = New-Object System.IO.Pipes.NamedPipeClientStream('.', 'KennetDeswikClaude', [System.IO.Pipes.PipeDirection]::InOut)
$c.Connect(5000)
$w = New-Object System.IO.StreamWriter($c); $w.AutoFlush = $true
$r = New-Object System.IO.StreamReader($c)
$w.WriteLine('{"op":"call","name":"' + $Name + '","input":' + $InputJson + '}')
$resp = $r.ReadLine() | ConvertFrom-Json
$c.Dispose()
if ($resp.isError) { "ERROR: " }
$resp.text
