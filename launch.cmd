@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%AutoLogin.App"

if not exist "%PROJECT_DIR%\AutoLogin.App.csproj" (
    echo Projektdatei nicht gefunden: "%PROJECT_DIR%\AutoLogin.App.csproj"
    exit /b 1
)

pushd "%PROJECT_DIR%"
dotnet run --project "AutoLogin.App.csproj"
set "EXIT_CODE=%ERRORLEVEL%"
popd

exit /b %EXIT_CODE%
