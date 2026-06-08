@echo off
setlocal enabledelayedexpansion

set "REPO=EntexInteractive/Parallel"
set "ASSET_PATTERN=Parallel-.*-cmd-win-x64.zip"
set "INSTALL_ROOT=%ProgramFiles%\Parallel"
set "TMP_DIR=%TEMP%\parallel_%RANDOM%"
set "ZIP_FILE=%TMP_DIR%\parallel.zip"

mkdir "%TMP_DIR%" >nul 2>&1

echo Fetching latest release info...
:: Use PowerShell to safely extract the download URL
for /f "usebackq delims=" %%A in (`powershell -NoLogo -NoProfile -Command "$r = Invoke-WebRequest -UseBasicParsing 'https://api.github.com/repos/%REPO%/releases/latest';" "$json = $r.Content | ConvertFrom-Json;" "$asset = $json.assets | Where-Object { $_.browser_download_url -match '%ASSET_PATTERN%' };" "if ($asset) { $asset.browser_download_url }"`) do (
    set "DOWNLOAD_URL=%%A"
)

if "%DOWNLOAD_URL%"=="" (
    echo ERROR: Could not find a matching release asset.
    exit /b 1
)

echo Downloading latest release...
curl -sSL -o "%ZIP_FILE%" "%DOWNLOAD_URL%"
if not exist "%ZIP_FILE%" (
    echo ERROR: Download failed.
    exit /b 1
)

echo Installing...
if exist "%INSTALL_ROOT%" rmdir /s /q "%INSTALL_ROOT%"
mkdir "%INSTALL_ROOT%"

powershell -NoLogo -NoProfile -Command "Expand-Archive -Path '%ZIP_FILE%' -DestinationPath '%INSTALL_ROOT%' -Force"

:: Find the binary
set "BIN_NAME="
for %%F in ("%INSTALL_ROOT%\parallel*.exe") do (
    set "BIN_NAME=%%~nxF"
    goto foundbin
)

:foundbin
if "%BIN_NAME%"=="" (
    echo ERROR: Could not locate the Parallel executable after extraction.
    exit /b 1
)

:: Add to PATH if needed
echo %PATH% | find /i "%INSTALL_ROOT%" >nul
if errorlevel 1 (
    echo Adding Parallel to PATH...
    setx PATH "%PATH%;%INSTALL_ROOT%" >nul
)

echo Cleaning up...
rmdir /s /q "%TMP_DIR%"

echo Installation complete. You can now run Parallel from the command line.
