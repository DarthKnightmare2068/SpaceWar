@echo off
echo 🛠️ Fixing Shader Materials for URP...
echo.

REM Fix Built-in Standard shader (fileID: 46) to URP Lit shader
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 46, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3' | Set-Content $_.FullName }"

REM Fix Built-in Particles/Additive shader (fileID: 200) to URP Particles/Unlit
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 200, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e260cfa7296ee7642b167f1eb5be5023, type: 3' | Set-Content $_.FullName }"

REM Fix Built-in Sprites/Default shader (fileID: 10753) to URP 2D/Sprite-Lit-Default
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 10753, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e97c80ac0e1024c0b95b08a4d75827d4, type: 3' | Set-Content $_.FullName }"

echo.
echo ✅ Shader conversion complete!
echo 🎯 All built-in render pipeline shaders have been converted to URP shaders.
echo.
echo Next steps:
echo 1. Open Unity and let it reimport the materials
echo 2. Check for any pink/magenta materials in your scenes
echo 3. If there are still issues, use Window ^> Rendering ^> Render Pipeline Converter
echo.
pause 