[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Prompt,

    [switch]$RouteOnly,

    [ValidateSet("read-only", "workspace-write", "danger-full-access")]
    [string]$Sandbox = "workspace-write",

    [switch]$JsonEvents
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$python = Join-Path $projectRoot ".tools\.venv-codex\Scripts\python.exe"

if (-not (Test-Path -LiteralPath $python)) {
    Write-Error "G10 router venv is missing. Reinstall it under .tools\.venv-codex."
    exit 127
}

$codexShim = Get-Command codex.cmd -ErrorAction Stop
$npmPackageRoot = Join-Path (Split-Path $codexShim.Source -Parent) "node_modules\@openai\codex"
$nativeCodex = Get-ChildItem -Path (Join-Path $npmPackageRoot "node_modules\@openai\codex-win32-*\vendor\*\bin\codex.exe") -ErrorAction Stop |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
$env:CODEX_ROUTER_EXECUTABLE = $nativeCodex.FullName

$routerArgs = @(
    "-I",
    "-m", "codex_model_router",
    "--heuristic-only",
    "--no-log",
    "--cd", $projectRoot,
    "--prompt", $Prompt
)

if ($RouteOnly) {
    $routerArgs += "--route-only"
} else {
    $routerArgs += @("--sandbox", $Sandbox)
}

if ($JsonEvents) {
    $routerArgs += "--json-events"
}

& $python @routerArgs
exit $LASTEXITCODE
