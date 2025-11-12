@echo off

set SCRIPT_DIR=%~dp0
set BUILDS_DIR="%SCRIPT_DIR%Builds"

echo Building to folder: %BUILDS_DIR%
rd /s /q "%BUILDS_DIR%"

echo Building Parallel.Cli...
CALL dotnet publish "%SCRIPT_DIR%Parallel.Cli\Parallel.Cli.csproj" -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishDir="%BUILDS_DIR%\Parallel.Cli\win-x64"
CALL dotnet publish "%SCRIPT_DIR%Parallel.Cli\Parallel.Cli.csproj" -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishDir="%BUILDS_DIR%\Parallel.Cli\osx-x64"
CALL dotnet publish "%SCRIPT_DIR%Parallel.Cli\Parallel.Cli.csproj" -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishDir="%BUILDS_DIR%\Parallel.Cli\linux-x64"
CALL dotnet publish "%SCRIPT_DIR%Parallel.Cli\Parallel.Cli.csproj" -r linux-arm -c Release /p:PublishSingleFile=true /p:PublishDir="%BUILDS_DIR%\Parallel.Cli\linux-arm"