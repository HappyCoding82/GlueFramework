@echo off
setlocal EnableExtensions

REM Usage:
REM   clear_nuget_cache_package.bat <packageId> [version]
REM   clear_nuget_cache_package.bat --glue-all
REM Examples:
REM   clear_nuget_cache_package.bat GlueFramework.CustomSysSettingsModule
REM   clear_nuget_cache_package.bat GlueFramework.CustomSysSettingsModule 0.0.1
REM   clear_nuget_cache_package.bat --glue-all

if "%~1"=="" (
  echo Usage: %~nx0 ^<packageId^> [version]
  echo        %~nx0 --glue-all
  exit /b 1
)

set "PKG=%~1"

REM NuGet global-packages default path
set "NUGET_GLOBAL=%USERPROFILE%\.nuget\packages"

REM If NUGET_PACKAGES is set, it overrides the default
if not "%NUGET_PACKAGES%"=="" (
  set "NUGET_GLOBAL=%NUGET_PACKAGES%"
)

echo NuGet global-packages: "%NUGET_GLOBAL%"

if /I "%PKG%"=="--glue-all" goto :glueAll

if "%~2"=="" (
  set "TARGET=%NUGET_GLOBAL%\%PKG%"
  echo Deleting package cache folder:
  echo   %TARGET%
  if exist "%TARGET%" (
    rmdir /s /q "%TARGET%"
    echo Deleted.
  ) else (
    echo Not found (nothing to delete).
  )
) else (
  set "VER=%~2"
  set "TARGET=%NUGET_GLOBAL%\%PKG%\%VER%"
  echo Deleting package version cache folder:
  echo   %TARGET%
  if exist "%TARGET%" (
    rmdir /s /q "%TARGET%"
    echo Deleted.
  ) else (
    echo Not found (nothing to delete).
  )
)

:afterDelete

echo.
echo Optional: clearing NuGet http-cache and temp (safe, but will require re-download)
choice /c YN /m "Clear NuGet http-cache and temp as well?"
if errorlevel 2 goto :skipLocals

dotnet nuget locals http-cache --clear
dotnet nuget locals temp --clear

:skipLocals
echo.
echo Done.
echo Next steps in the consuming project:
echo   1^) Close the running site (stop IIS Express/Kestrel)
echo   2^) Delete bin\ and obj\
echo   3^) Restore/build again
echo.
endlocal

goto :eof

:glueAll
echo(
echo Deleting all package cache folders matching:
echo   %NUGET_GLOBAL%\GlueFramework*
set "FOUND_ANY="
for /d %%D in ("%NUGET_GLOBAL%\GlueFramework*") do (
  set "FOUND_ANY=1"
  echo   Deleting: %%~fD
  rmdir /s /q "%%~fD"
)
if not defined FOUND_ANY (
  echo Not found (nothing to delete).
)
goto :afterDelete
