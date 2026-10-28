@echo off
chcp 65001 >nul
echo ===================================================================
echo [RaftTrainer] Titan v14.0 WORLD MAP / REMINDERS / FLOOR COPY / THERMAL...
echo ===================================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set MANAGED=D:\Raft\Raft_Data\Managed
set OUT=RaftTrainer.dll

"%CSC%" /target:library /out:"%OUT%" /optimize+ /reference:"%MANAGED%\UnityEngine.dll","%MANAGED%\UnityEngine.CoreModule.dll","%MANAGED%\UnityEngine.IMGUIModule.dll","%MANAGED%\UnityEngine.InputLegacyModule.dll","%MANAGED%\UnityEngine.InputModule.dll","%MANAGED%\UnityEngine.PhysicsModule.dll","%MANAGED%\UnityEngine.UI.dll","%MANAGED%\UnityEngine.TextRenderingModule.dll","%MANAGED%\UnityEngine.AudioModule.dll","%MANAGED%\UnityEngine.ScreenCaptureModule.dll","%MANAGED%\Assembly-CSharp.dll","%MANAGED%\Assembly-CSharp-firstpass.dll" RaftTrainer.cs RaftTrainer.Esp.cs RaftTrainer.Menu.cs RaftTrainer.V14.cs

if errorlevel 1 (
    echo.
    echo [ERROR] Compilation failed!
    exit /b 1
)

echo.
echo [SUCCESS] Compiled: %OUT%
copy /Y "%OUT%" "%MANAGED%\RaftTrainer.dll" >nul
echo [OK] Updated in game: %MANAGED%\RaftTrainer.dll
copy /Y "%OUT%" "%USERPROFILE%\Desktop\RaftTrainer.dll" >nul
echo [OK] Copied to Desktop: %USERPROFILE%\Desktop\RaftTrainer.dll
echo ===================================================================
echo Done!
