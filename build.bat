@echo off
REM ============================================================================
REM  MoTV Renamer - build script
REM  Turns the source code (MoTVRenamer.cs) into the program (MoTVRenamer.exe).
REM
REM  What this script does:
REM    1. Checks that the source file and the icon are in this folder.
REM    2. Finds the C# compiler that Windows already includes.
REM    3. Compiles the code into MoTVRenamer.exe, next to this file.
REM
REM  It does NOT download anything, does NOT need an internet connection,
REM  does NOT install anything and does NOT change anything outside this folder.
REM ============================================================================
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo ==============================================
echo   Building MoTVRenamer.exe  (MoTV Renamer)
echo   Nothing to install: it uses the compiler built into Windows
echo ==============================================
echo.

REM Step 1: make sure the needed files are here
if not exist "MoTVRenamer.cs" goto nosrc
if not exist "MoTVRenamer.ico" goto noico

REM Step 2: find the C# compiler that comes with Windows (.NET Framework 4.x)
set "CSC="
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not defined CSC goto nocsc

echo Compiler: %CSC%
echo Building...
echo.

REM Step 3: compile. The options below mean:
REM   /target:winexe    a normal window program (no black console window)
REM   /optimize+        make the program run efficiently
REM   /out:             name of the program to create
REM   /win32icon:       the program's icon
REM   /reference:       the standard Windows libraries the code uses
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 /out:MoTVRenamer.exe /win32icon:MoTVRenamer.ico ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  MoTVRenamer.cs
if errorlevel 1 goto fail

echo.
echo ==============================================
echo   Done: %~dp0MoTVRenamer.exe
echo ==============================================
echo.
pause
exit /b 0

:nosrc
echo [ERROR] MoTVRenamer.cs was not found in this folder.
echo Put build.bat and MoTVRenamer.cs together and run it again.
pause
exit /b 1

:noico
echo [ERROR] MoTVRenamer.ico was not found in this folder.
pause
exit /b 1

:nocsc
echo [ERROR] The .NET Framework 4 compiler was not found on this computer.
echo Windows 10 and 11 include it. On an older version, install
echo ".NET Framework 4.8" from the Microsoft website and try again.
pause
exit /b 1

:fail
echo.
echo [ERROR] The build failed. Copy the messages above and report them.
pause
exit /b 1
