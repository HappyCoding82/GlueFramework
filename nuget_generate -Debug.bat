@echo off
rem Turn off command echo for cleaner output
setlocal enabledelayedexpansion

rem Define project root directory (modify as needed, assuming the script is executed in the solution root)
set "PROJECT_ROOT=."
rem Define NuGet package output directory
set "NUGET_OUTPUT_DIR=../nuget_packages"

rem Automatically create NuGet package output directory if it does not exist (avoid pack failure due to missing directory)
if not exist "%NUGET_OUTPUT_DIR%" md "%NUGET_OUTPUT_DIR%"

rem ==============================================
rem Step 1: Execute dotnet build (Release configuration) for each project first
rem ==============================================
echo Step 1: Building all projects in Release configuration...
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLog.Abstractions/GlueFramework.AuditLog.Abstractions.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLogModule/GlueFramework.AuditLogModule.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.Core/GlueFramework.Core.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCore.Observability/GlueFramework.OrchardCore.Observability.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCoreModule/GlueFramework.OrchardCoreModule.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.OutboxModule/GlueFramework.OutboxModule.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.WebCore/GlueFramework.WebCore.csproj" --configuration Release
dotnet build "%PROJECT_ROOT%/src/framework/GlueFramework.CustomSysSettingsModule/GlueFramework.CustomSysSettingsModule.csproj" --configuration Release
rem Check if build step failed, exit if error occurs
if errorlevel 1 (
    echo Build step failed! Aborting subsequent pack operation.
    pause
    exit /b 1
)

rem ==============================================
rem Step 2: Execute dotnet pack (Release configuration, skip restore and re-build)
rem ADDED: -p:GeneratePackageOnBuild=true to fix NU5017 error
rem ==============================================
echo Step 2: Packing all projects into NuGet packages...
echo Generating packages with debug symbols...

rem Modified pack commands with additional parameters
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLog.Abstractions/GlueFramework.AuditLog.Abstractions.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLogModule/GlueFramework.AuditLogModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.Core/GlueFramework.Core.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCore.Observability/GlueFramework.OrchardCore.Observability.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCoreModule/GlueFramework.OrchardCoreModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OutboxModule/GlueFramework.OutboxModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.WebCore/GlueFramework.WebCore.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.CustomSysSettingsModule/GlueFramework.CustomSysSettingsModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%" -p:GeneratePackageOnBuild=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg

rem Check if pack step failed
if errorlevel 1 (
    echo Pack step failed for some projects!
    pause
    exit /b 1
)

rem Prompt after all NuGet packages are generated successfully
echo All projects have been built and packed successfully. NuGet packages are in: %NUGET_OUTPUT_DIR%
pause
rem ==============================================
rem Step 4: Create debug configuration file
rem ==============================================
echo Step 4: Creating debug configuration helper...
echo.

echo Creating debug configuration file for Visual Studio...

(
  echo ^<?xml version="1.0" encoding="utf-8"?^>
  echo ^<configuration^>
  echo   ^<packageSources^>
  echo     ^<add key="Local Debug Packages" value="%NUGET_OUTPUT_DIR:\=/%" /^>
  echo   ^</packageSources^>
  echo ^</configuration^>
) > "%NUGET_OUTPUT_DIR%\nuget.config"

echo Created: %NUGET_OUTPUT_DIR%\nuget.config
echo.
echo ============================================
echo PACKAGE DEBUGGING READY!
echo ============================================
echo.
echo To enable debugging of these packages:
echo.
echo OPTION 1: For Visual Studio
echo ---------
echo 1. In consuming project, add NuGet source:
echo    %NUGET_OUTPUT_DIR%
echo.
echo 2. Enable debugging:
echo    Tools ^> Options ^> Debugging ^> General
echo    - Check: ^"Enable source server support^"
echo    - Check: ^"Enable Source Link support^"
echo.
echo 3. Configure symbols:
echo    Tools ^> Options ^> Debugging ^> Symbols
echo    - Add: %NUGET_OUTPUT_DIR%
echo    - Add: https://symbols.nuget.org/download/symbols
echo.
echo OPTION 2: For .NET CLI
echo ---------
echo Add to your project's directory:
echo 1. Create or update .csproj file with:
echo    ^<PropertyGroup^>
echo      ^<DebugType^>embedded^</DebugType^>
echo    ^</PropertyGroup^>
echo.
echo 2. Use the packages:
echo    dotnet add package Your.Package.Name --source "%NUGET_OUTPUT_DIR%"
echo.
echo ============================================
echo All projects have been built and packed successfully with debug symbols.
echo NuGet packages are in: %NUGET_OUTPUT_DIR%
echo ============================================
pause