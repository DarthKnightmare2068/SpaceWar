@echo off
echo ===============================================
echo 🔍 VERIFYING HLSL ERROR FIX
echo ===============================================
echo.

echo 📋 Checking project file status...
echo.

if exist "*.sln" (
    echo ✅ Unity solution file (.sln) exists
    for %%f in (*.sln) do echo    📁 Found: %%f
) else (
    echo ❌ No .sln file found - Unity needs to finish importing
    echo    💡 Open Unity and let it complete importing/compiling
)

echo.

if exist "Assembly-CSharp.csproj" (
    echo ✅ Main C# project file exists
) else (
    echo ❌ Assembly-CSharp.csproj missing - Unity still processing
)

echo.

echo 📋 Checking configuration files...
if exist "Directory.Build.props" (
    echo ✅ MSBuild configuration exists (excludes shader files)
) else (
    echo ❌ MSBuild configuration missing
)

if exist "PlaneTest.sln.DotSettings" (
    echo ✅ ReSharper settings exist (excludes Library/Packages)
) else (
    echo ❌ ReSharper settings missing
)

echo.

echo 📋 Checking material conversion...
powershell -Command "if ((Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse | Select-String 'fileID: 46, guid: 0000000000000000f000000000000000').Count -eq 0) { Write-Host '✅ No built-in Standard shaders found' -ForegroundColor Green } else { Write-Host '❌ Some materials still use built-in shaders' -ForegroundColor Red }"

echo.
echo ===============================================
echo 📝 NEXT STEPS:
echo ===============================================

if exist "*.sln" (
    echo ✅ READY FOR VISUAL STUDIO:
    echo.
    echo 1️⃣  Open the generated .sln file in Visual Studio
    echo 2️⃣  You should see NO HLSL errors
    echo 3️⃣  If errors persist, use Tools → Options → Text Editor → File Extension
    echo     Change .shader from "HLSL Editor" to "None"
    echo.
    echo 🎮 Your PlaneTest project is ready for development!
) else (
    echo ⏳ WAITING FOR UNITY:
    echo.
    echo 1️⃣  Let Unity finish importing all assets
    echo 2️⃣  Wait for "Compiling" to complete
    echo 3️⃣  Unity will auto-generate .sln and .csproj files
    echo 4️⃣  Run this script again to verify
    echo.
    echo 💡 Unity is probably still processing in the background
)

echo.
echo ===============================================
echo 🎯 TROUBLESHOOTING:
echo ===============================================
echo.
echo If you still see HLSL errors after Unity finishes:
echo.
echo 🔧 METHOD 1 - Visual Studio Options:
echo    Tools → Options → Text Editor → File Extension
echo    Find ".shader" → Change Editor to "None"
echo    Restart Visual Studio
echo.
echo 🔧 METHOD 2 - Exclude Folders:
echo    Right-click "Library" in Solution Explorer → Exclude
echo    Right-click "Packages" in Solution Explorer → Exclude
echo.
echo 🔧 METHOD 3 - Regenerate (if needed):
echo    Close VS → Delete .sln and .csproj → Reopen Unity
echo.
echo ===============================================
echo ✨ FINAL VERIFICATION CHECKLIST:
echo ===============================================
echo □ Unity imports complete (no "Importing" text)
echo □ Unity compiling complete (no "Compiling" text)
echo □ No pink/magenta materials in Unity scenes
echo □ .sln file exists in project folder
echo □ Visual Studio opens without HLSL errors
echo □ PlaneTest game runs correctly in Unity
echo.
echo 🚀 When all boxes are checked, you're done!
echo.
pause 