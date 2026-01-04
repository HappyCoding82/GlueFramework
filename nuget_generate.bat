@echo off
rem Turn off command echo for cleaner output
setlocal enabledelayedexpansion

rem Define project root directory (modify as needed, assuming the script is executed in the solution root)
set "PROJECT_ROOT=."
rem Define NuGet package output directory
set "NUGET_OUTPUT_DIR=../nuget_packages"

rem Automatically create NuGet output directory if it does not exist (avoid pack failure due to missing directory)
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
rem ==============================================
echo Step 2: Packing all projects into NuGet packages...
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLog.Abstractions/GlueFramework.AuditLog.Abstractions.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.AuditLogModule/GlueFramework.AuditLogModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.Core/GlueFramework.Core.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCore.Observability/GlueFramework.OrchardCore.Observability.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OrchardCoreModule/GlueFramework.OrchardCoreModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.OutboxModule/GlueFramework.OutboxModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.WebCore/GlueFramework.WebCore.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"
dotnet pack "%PROJECT_ROOT%/src/framework/GlueFramework.CustomSysSettingsModule/GlueFramework.CustomSysSettingsModule.csproj" --configuration Release --no-restore --no-build --output "%NUGET_OUTPUT_DIR%"

rem Check if pack step failed
if errorlevel 1 (
    echo Pack step failed for some projects!
    pause
    exit /b 1
)

rem Prompt after all NuGet packages are generated successfully
echo All projects have been built and packed successfully. NuGet packages are in: %NUGET_OUTPUT_DIR%
pause