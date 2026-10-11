@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

echo ==============================================
echo   Generando MoTVRenamer.exe  (MoTV Renamer)  (C# / .NET Framework)
echo   No necesita Python ni instalar nada
echo ==============================================
echo.

if not exist "MoTVRenamer.cs" goto nosrc
if not exist "MoTVRenamer.ico" goto noico

REM Compilador de C# que ya trae Windows (.NET Framework 4.x)
set "CSC="
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not defined CSC goto nocsc

echo Compilador: %CSC%
echo Compilando...
echo.

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 /out:MoTVRenamer.exe /win32icon:MoTVRenamer.ico ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  MoTVRenamer.cs
if errorlevel 1 goto fail

echo.
echo ==============================================
echo   Listo: %~dp0MoTVRenamer.exe
echo ==============================================
echo.
pause
exit /b 0

:nosrc
echo [ERROR] No encuentro MoTVRenamer.cs en esta carpeta.
echo Pon build.bat y MoTVRenamer.cs juntos y vuelve a ejecutarlo.
pause
exit /b 1

:noico
echo [ERROR] No encuentro MoTVRenamer.ico en esta carpeta.
pause
exit /b 1

:nocsc
echo [ERROR] No se encontro el compilador de .NET Framework 4 en este equipo.
echo Windows 10 y 11 lo incluyen. Si usas una version antigua, instala
echo ".NET Framework 4.8" desde la web de Microsoft y vuelve a probar.
pause
exit /b 1

:fail
echo.
echo [ERROR] La compilacion ha fallado. Copia los mensajes de arriba y envialos.
pause
exit /b 1
