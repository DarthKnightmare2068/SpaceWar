@echo off
echo 🔄 Regenerating Unity Project Files...
echo.

echo 📝 Step 1: Cleaning old project files...
if exist *.csproj del *.csproj
if exist *.sln del *.sln

echo 📝 Step 2: Opening Unity to regenerate project files...
echo    Unity will regenerate the project files correctly.
echo    This will exclude shader files from Visual Studio compilation.
echo.

REM Try to find Unity installation and open project
set UNITY_PATH=""
if exist "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe" (
    for /d %%i in ("C:\Program Files\Unity\Hub\Editor\*") do set UNITY_PATH="%%i\Editor\Unity.exe"
)

if %UNITY_PATH%=="" (
    if exist "C:\Program Files\Unity\Editor\Unity.exe" (
        set UNITY_PATH="C:\Program Files\Unity\Editor\Unity.exe"
    )
)

if %UNITY_PATH%=="" (
    echo ⚠️  Unity not found in default paths.
    echo    Please open Unity manually and open this project.
    echo    Unity will automatically regenerate the project files.
) else (
    echo 🚀 Opening Unity...
    %UNITY_PATH% -projectPath "%cd%"
)

echo.
echo ✅ Once Unity opens:
echo    1. Let Unity finish importing/compiling
echo    2. Close Unity
echo    3. Reopen the project in Visual Studio
echo.
echo 🎯 The HLSL errors should be gone!
echo.
pause 