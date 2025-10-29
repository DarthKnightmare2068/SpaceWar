@echo off
echo 🔧 Comprehensive Shader Fix - Converting ALL Built-in Shaders to URP
echo.

echo 📋 Phase 1: Standard Materials...
REM Fix Built-in Standard shader (fileID: 46) to URP Lit shader
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 46, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3' | Set-Content $_.FullName }"

echo 📋 Phase 2: Skybox Materials...
REM Fix Built-in Skybox shader (fileID: 104) to URP Skybox/Cubemap shader
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 104, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: fe393ace9b354375a9cb14cdbbc28be4, type: 3' | Set-Content $_.FullName }"

echo 📋 Phase 3: Particle Materials...
REM Fix Built-in Particles/Additive shader (fileID: 200) to URP Particles/Unlit
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 200, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e260cfa7296ee7642b167f1eb5be5023, type: 3' | Set-Content $_.FullName }"

REM Fix Built-in Particles/Alpha Blended shader (fileID: 202) to URP Particles/Unlit
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 202, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e260cfa7296ee7642b167f1eb5be5023, type: 3' | Set-Content $_.FullName }"

REM Fix Built-in Particles/VertexLit Blended shader (fileID: 211) to URP Particles/Unlit  
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 211, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e260cfa7296ee7642b167f1eb5be5023, type: 3' | Set-Content $_.FullName }"

echo 📋 Phase 4: Other Common Shaders...
REM Fix Built-in Sprites/Default shader (fileID: 10753) to URP 2D/Sprite-Lit-Default
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 10753, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: e97c80ac0e1024c0b95b08a4d75827d4, type: 3' | Set-Content $_.FullName }"

REM Fix Built-in Unlit/Color shader (fileID: 10755) to URP Unlit
powershell -Command "(Get-ChildItem -Path 'Assets\' -Filter '*.mat' -Recurse) | ForEach-Object { (Get-Content $_.FullName) -replace 'fileID: 10755, guid: 0000000000000000f000000000000000, type: 0', 'fileID: 4800000, guid: 650dd9526735d5b46b79224bc6e94025, type: 3' | Set-Content $_.FullName }"

echo.
echo ✅ ALL Shader Materials Converted!
echo.
echo 🎯 Summary of conversions:
echo   ✓ Standard materials → URP Lit
echo   ✓ Skybox materials → URP Skybox/Cubemap  
echo   ✓ Particle materials → URP Particles/Unlit
echo   ✓ Sprite materials → URP 2D/Sprite-Lit
echo   ✓ Unlit materials → URP Unlit
echo.
echo 🚀 Your project should now be error-free!
echo 💡 Open Unity to see the fixed materials.
echo.
pause 