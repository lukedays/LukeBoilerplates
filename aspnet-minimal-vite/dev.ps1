# Sobe backend e frontend lado a lado.
#
# Padrão (-Mode Terminal): abre o Windows Terminal com dois panes interativos.
#   - Pane esquerdo: dotnet watch com hot reload. O codegen roda em dois caminhos:
#       * rebuild/restart -> target MSBuild GenerateTs (AfterTargets=Build);
#       * hot reload (sem rebuild) -> handler MetadataUpdateHandler (HotReloadCodegen).
#   - Pane direito: vite (HMR pega o generated.ts atualizado e recarrega o front).
#
# -Mode Background: roda os dois processos em background (sem Windows Terminal),
#   redirecionando stdout/stderr para .dev-logs\api.log e .dev-logs\web.log.
#   Os PIDs ficam em .dev-logs\pids.txt. Encerre com: .\dev.ps1 -Mode Stop
#
# -Mode Stop: mata os processos iniciados no modo Background (lê os PIDs do arquivo).

param(
    [ValidateSet('Terminal', 'Background', 'Stop')]
    [string]$Mode = 'Terminal'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$logDir = Join-Path $root '.dev-logs'
$pidFile = Join-Path $logDir 'pids.txt'

if ($Mode -eq 'Stop') {
    if (-not (Test-Path $pidFile)) { Write-Host 'Nada para parar (.dev-logs\pids.txt ausente).'; return }
    foreach ($procId in Get-Content $pidFile) {
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        Write-Host "Parado PID $procId"
    }
    Remove-Item $pidFile -Force
    return
}

if ($Mode -eq 'Background') {
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    # `dotnet run` (sem watch) evita o restart-loop que dá double-bind na porta;
    # o codegen ainda roda no build (target GenerateTs). Rebuild manual cobre mudanças no backend.
    $api = Start-Process pwsh -PassThru -WindowStyle Hidden -WorkingDirectory "$root\server" `
        -ArgumentList '-NoProfile', '-Command', "dotnet run *> '$logDir\api.log'"
    $web = Start-Process pwsh -PassThru -WindowStyle Hidden -WorkingDirectory "$root\client" `
        -ArgumentList '-NoProfile', '-Command', "npm run dev *> '$logDir\web.log'"
    Set-Content -Path $pidFile -Value @($api.Id, $web.Id)
    Write-Host "Background iniciado. API PID $($api.Id), Web PID $($web.Id)."
    Write-Host "Logs: $logDir\api.log e $logDir\web.log"
    return
}

# Mode = Terminal
if (-not (Get-Command wt -ErrorAction SilentlyContinue)) {
    Write-Error 'Windows Terminal (wt) não encontrado. Use -Mode Background ou instale pela Microsoft Store.'
    return
}

wt --title 'API'      -d "$root\server" pwsh -NoExit -Command 'dotnet watch run' `
   `; split-pane -V -d "$root\client" pwsh -NoExit -Command 'npm run dev'
