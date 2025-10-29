# Fix Shader Materials Script - Convert Built-in Pipeline to URP
# This script converts common built-in render pipeline shaders to URP equivalents

Write-Host "🛠️  Starting Shader Material Conversion to URP..." -ForegroundColor Green
Write-Host "📁 Project: PlaneTest" -ForegroundColor Yellow

# Define shader conversions
$shaderConversions = @{
    # Built-in Standard Shader -> URP Lit Shader
    'fileID: 46, guid: 0000000000000000f000000000000000, type: 0' = 'fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3'
    
    # Built-in Mobile/Particles/Additive -> URP Particles/Unlit
    'fileID: 200, guid: 0000000000000000f000000000000000, type: 0' = 'fileID: 4800000, guid: e260cfa7296ee7642b167f1eb5be5023, type: 3'
    
    # Built-in Sprites/Default -> URP 2D/Sprite-Lit-Default
    'fileID: 10753, guid: 0000000000000000f000000000000000, type: 0' = 'fileID: 4800000, guid: e97c80ac0e1024c0b95b08a4d75827d4, type: 3'
    
    # Built-in Unlit/Color -> URP Unlit
    'fileID: 10755, guid: 0000000000000000f000000000000000, type: 0' = 'fileID: 4800000, guid: 650dd9526735d5b46b79224bc6e94025, type: 3'
}

# Get all .mat files in Assets folder
$materialFiles = Get-ChildItem -Path "Assets\" -Filter "*.mat" -Recurse

$convertedCount = 0
$totalCount = 0

foreach ($file in $materialFiles) {
    $totalCount++
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Apply each conversion
    foreach ($oldShader in $shaderConversions.Keys) {
        if ($content -match [regex]::Escape($oldShader)) {
            $newShader = $shaderConversions[$oldShader]
            $content = $content -replace [regex]::Escape($oldShader), $newShader
            Write-Host "  ✅ Converting: $($file.Name)" -ForegroundColor Cyan
        }
    }
    
    # Write back if changes were made
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $convertedCount++
    }
}

Write-Host ""
Write-Host "🎯 Conversion Complete!" -ForegroundColor Green
Write-Host "📊 Converted: $convertedCount materials out of $totalCount total" -ForegroundColor Yellow
Write-Host ""
Write-Host "✨ Next Steps:" -ForegroundColor Magenta
Write-Host "  1. Open Unity and let it reimport the materials" -ForegroundColor White
Write-Host "  2. Check for any remaining pink/magenta materials" -ForegroundColor White
Write-Host "  3. Use Window → Rendering → Render Pipeline Converter for any missed materials" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Your materials should now be URP-compatible!" -ForegroundColor Green 