@echo off
rem Capy Music Studio: starts the little local server (hidden) and opens the studio in its own window.
cd /d "%~dp0"
where node >nul 2>nul || (echo Node.js is needed: https://nodejs.org & pause & exit /b 1)
powershell -NoProfile -WindowStyle Hidden -Command "Start-Process node -ArgumentList 'server.js','--auto-exit' -WorkingDirectory '%~dp0' -WindowStyle Hidden"
powershell -NoProfile -Command "for ($i=0; $i -lt 40; $i++) { try { Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5178/api/songs -TimeoutSec 1 | Out-Null; break } catch { Start-Sleep -Milliseconds 150 } }"
set "EDGE=%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"
if not exist "%EDGE%" set "EDGE=%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
if exist "%EDGE%" (
  start "" "%EDGE%" --app=http://127.0.0.1:5178 --window-size=1440,900
) else (
  start "" http://127.0.0.1:5178
)
