# Unused Assets Report

## Summary

Based on analysis of your Unity project, I've identified significant amounts of unused assets that can be safely removed.

### Build Configuration
Your game only uses **2 scenes**:
- `Assets/Scenes/EnterScene/Enter Scene.unity` (Start screen)
- `Assets/Scenes/Plane Test/Plane Test.unity` (Main game)

The `TurretTest.unity` scene is **NOT in the build**.

---

## Complete 3D Models Analysis (64 FBX files)

### ✅ USED 3D Models (5 files)
| Model | Location | Used For |
|-------|----------|----------|
| `StarSparrow1.fbx` | Plane Models/StarSparrow/Meshes/ | Player ship |
| `Ardent.FBX` | SpaceShip/Battleships/Ardent/ | Enemy ship 1 |
| `Fomalhaut.FBX` | SpaceShip/Battleships/Fomalhaut/ | Enemy ship 2 |
| `Tannhauser.FBX` | SpaceShip/Battleships/Tannhauser/ | Enemy ship 3 |
| `IONcannon.fbx` | Turrets/Turrets/ION_Cannon/Models/ | Turret weapon |

### ❌ UNUSED 3D Models (59 files)

#### StarSparrow Variants (24 models) - SAFE TO DELETE
```
Plane Models/StarSparrow/Meshes/
├── StarSparrow2.fbx through StarSparrow20.fbx (19 files)
├── StarSparrowModules.fbx
└── BonusContent/ (13 files)
    ├── AsteroidsSample.FBX
    ├── Clouds.FBX
    ├── FlyingInsects3.FBX + Wings (5 files)
    ├── MinesSample.FBX
    ├── MissilesSample.FBX
    ├── Planet.FBX
    └── StarSparrow_1_LP_Red.fbx, StarSparrow_Modules_LP_Red.fbx
```

#### Turret Models (18 models) - SAFE TO DELETE
```
Turrets/
├── Neck_Mech_Walker/Neck_Mech_Walker_by_3DHaupt.fbx
├── Turrets/centurionweapon/Models/centurion_weapon.fbx
├── Turrets/SimpleTurret/Model/SimpleSciFiTurret.fbx
└── Turrets/WarZone Sci-Fi Turret pack/ (14 files)
    ├── gun.FBX, gun1.FBX, gun2.FBX, gun3.FBX
    ├── lazer0.FBX, lazer1.FBX
    ├── machinegun1.FBX, machinegun2.FBX, machinegun3.FBX
    ├── radar.FBX
    └── rocket.FBX, rocket1.FBX, rocket2.FBX, rocket3.FBX
```

#### SpaceShip Models (5 models) - CHECK BEFORE DELETING
```
SpaceShip/
├── Battleships/Firefly/Firefly.FBX    ← UNUSED
├── Battleships/Harbinger/Harbinger.FBX ← UNUSED
└── Hovl Studio/HSFiles/Models/
    ├── Conus.fbx      ← Demo content
    └── CylinderFromGround.fbx ← Demo content
```

#### Other Models (3 models) - SAFE TO DELETE
```
├── LaserVFX/Models/Mountains01.fbx   ← Demo scene
├── LaserVFX/Models/Plane01.fbx       ← Demo scene
├── LaserVFX/Models/Plane02.fbx       ← Demo scene
└── MODERN WARFARE/Missile/fbx/missile.fbx ← UNUSED
```

---

## Complete 2D Textures Analysis (333 files)

### ✅ USED Textures (approximate)

#### Skybox Textures (6 files used)
Only `SBS Space 2/Medium/Textures/` is used:
- Back_Skybox_Space 2 Medium.png
- Down_Skybox_Space 2 Medium.png
- Front_Skybox_Space 2 Medium.png
- Left_Skybox_Space 2 Medium.png
- Right_Skybox_Space 2 Medium.png
- Up_Skybox_Space 2 Medium.png

#### StarSparrow Textures (13 files used)
```
Plane Models/StarSparrow/Textures/
├── StarSparrow_Black.png (and color variants)
├── StarSparrow_Emission.png
├── StarSparrow_MetallicSmoothness.png
└── StarSparrow_Normal.png
```

#### Battleship Textures (used ships only)
- Ardent textures (6 files)
- Fomalhaut textures (5 files)  
- Tannhauser textures (5 files)

#### ION Cannon Textures (used)
- IONcannon textures (10 files)

#### GUI/UI Textures (6 files used)
```
GUI/MyUI/
├── icons8-accuracy-80.png
├── icons8-bullet-80.png
├── icons8-pause-button-100.png
├── icons8-plane-64.png
├── icons8-rocket-50.png
└── icons8-target-100 1.png
```

#### LaserVFX Textures (17 files - likely used)
#### UnityTechnologies Explosion Textures (28 files - likely used for VFX)

### ❌ UNUSED Textures (~250+ files)

#### Skybox Unused Variants (84 textures) - BIG SAVING
```
Free Skyboxes - Space/
├── SBS Space 1/ (all 3 sizes = 18 textures)
├── SBS Space 2/Large/ (6 textures)
├── SBS Space 2/Small/ (6 textures)
├── SBS Space 3/ (all 3 sizes = 18 textures)
├── SBS Space 4/ (all 3 sizes = 18 textures)
└── SBS Space 5/ (all 3 sizes = 18 textures)
```

#### StarSparrow Unused (45+ textures)
```
Plane Models/StarSparrow/Textures/
├── BonusContent/ (30+ textures for asteroids, insects, mines, missiles, planet)
└── Masks/ (18 textures)
```

#### Battleship Unused (15 textures)
```
SpaceShip/Battleships/
├── Firefly/Textures/ (5 files)
└── Harbinger/Textures/ (5 files)
```

#### Hovl Studio Demo Textures (35 textures) - DEMO CONTENT
```
SpaceShip/Hovl Studio/
├── 3D Lasers Pack/Demo scene lasers/ (7 lightmaps/probes)
└── HSFiles/Textures/ (35 textures - demo scene only)
```

#### Turret Unused Textures (45+ textures)
```
Turrets/
├── Neck_Mech_Walker/ (10 jpg textures)
├── Turrets/centurionweapon/Textures/ (25+ textures, multiple skins)
├── Turrets/SimpleTurret/ (4 lightmaps + 1 psd)
└── Turrets/WarZone Sci-Fi Turret pack/ (1 texture)
```

#### Other Unused
```
├── GUI/CameraAircraft/Textures/Ground.jpg (demo)
├── GUI/EuroFighterHud/Sprites/ (5 sprites - verify if HUD used)
├── MODERN WARFARE/Missile/Textures/Missile.png
├── RadarSystem/Textures/ (3 files - verify if radar used)
└── Standard Assets/Prototyping/Textures/ (3 files)
```

---

## GameResources Folder Analysis (916 files total)

| Folder | Files | Status | Notes |
|--------|-------|--------|-------|
| Free Skyboxes - Space | 116 | ⚠️ MOSTLY UNUSED | Only 1 skybox material is used |
| Turrets | 301 | ⚠️ MOSTLY UNUSED | Contains 188+ audio files, many unused models |
| Plane Models | 185 | ⚠️ PARTIALLY UNUSED | Only StarSparrow1 model is used |
| SpaceShip | 125 | ✅ USED | Battleship models (Ardent, Fomalhaut, Tannhauser) are used |
| UnityTechnologies | 57 | ⚠️ MOSTLY UNUSED | Only explosion VFX used |
| LaserVFX | 37 | ✅ USED | VFX graph in use |
| GUI | 36 | ✅ USED | HUD and camera scripts in use |
| RadarSystem | 18 | ❓ CHECK | May or may not be used |
| HologramShieldShader | 16 | ✅ USED | Shield effect in use |
| Sounds | 8 | ✅ USED | Audio files referenced |
| MODERN WARFARE | 8 | ❌ UNUSED | Not referenced anywhere |
| Standard Assets | 6 | ❌ UNUSED | Old Unity standard assets |
| StarSparrow | 2 | ❓ CHECK | Colorize shader - check if needed |
| centurionweapon | 1 | ❌ UNUSED | Lighting settings only |

---

## Safe to Delete - Demo/Example Content

These folders contain demo scenes and examples from Asset Store packages:

### 1. Free Skyboxes - Space Demo Scenes
**Path:** `Assets/GameResources/Free Skyboxes - Space/Demo Scenes/`
- 5 demo scene files (.unity)
- Safe to delete - you're only using one skybox material

### 2. Turrets Demo Scenes & Content
**Path:** `Assets/GameResources/Turrets/`
- `Turrets/centurionweapon/centurion_weapon.unity` - Demo scene
- `Turrets/ION_Cannon/ION_Scene.unity` - Demo scene
- `Turrets/SimpleTurret/Example.unity` - Demo scene + lightmaps
- `SoundFile/Ash Valley Cybernetics Lite/` - **177+ MP3 files** (~large)

### 3. SpaceShip Demo Content
**Path:** `Assets/GameResources/SpaceShip/Hovl Studio/3D Lasers Pack/Demo scene lasers/`
- 2 demo scenes

### 4. RadarSystem Demo
**Path:** `Assets/GameResources/RadarSystem/Demo/`

### 5. HologramShieldShader Demo
**Path:** `Assets/GameResources/HologramShieldShader/HologramShieldDemoScene.unity`

### 6. GUI Demos
- `Assets/GameResources/GUI/EuroFighterHud/EuroHUD-HMD_DemoScene.unity`
- `Assets/GameResources/GUI/CameraAircraft/CameraAircraft_DemoScene.unity`

### 7. UnityTechnologies Effects Examples
**Path:** `Assets/GameResources/UnityTechnologies/EffectExamples/Scenes/Menu.unity`
- Menu scene and most materials

---

## Potentially Large Unused Folders

### Turrets Audio Files (BIGGEST SAVING)
**Path:** `Assets/GameResources/Turrets/SoundFile/Ash Valley Cybernetics Lite/`
- Contains **177+ MP3 files**
- These appear to be stock audio from Asset Store
- Check if any are actually used - likely all unused

### StarSparrow Prefab Variants
**Path:** `Assets/GameResources/Plane Models/StarSparrow/Prefabs/`
- Contains ~68 prefabs (StarSparrow1 through StarSparrow20, modules, examples)
- Only **StarSparrow1.fbx** mesh is actually used
- The other 19 modular variants are unused

### Free Skyboxes Variants
**Path:** `Assets/GameResources/Free Skyboxes - Space/`
- Contains 5 space skybox themes (SBS Space 1-5)
- Each theme has Low/Medium/High quality variants
- Only **1 material** appears to be used

---

## Files Confirmed IN USE

These are actively referenced in your scenes/prefabs:

### Models
- `StarSparrow/Meshes/StarSparrow1.fbx` - Player ship
- `SpaceShip/Battleships/Ardent/Ardent.FBX` - Enemy ship 1
- `SpaceShip/Battleships/Fomalhaut/` - Enemy ship 2  
- `SpaceShip/Battleships/Tannhauser/Tannhauser.FBX` - Enemy ship 3
- `Turrets/ION_Cannon/Models/IONcannon.fbx` - Turret weapon

### VFX/Effects
- `LaserVFX/Prefabs/VFXGraphs/vfxgraph_StylizedBeam01.vfx`
- `UnityTechnologies/.../TinyExplosion.prefab`
- `UnityTechnologies/.../BigExplosion.prefab`
- `HologramShieldShader/` - Shield materials

### Audio (in use)
Various audio files referenced in scripts/prefabs (verify in Unity)

### Skybox
- `Free Skyboxes - Space/SBS Space 2/Medium/Skybox_Space 2 Medium.mat`

---

## Recommended Cleanup Steps

### Step 1: Backup First!
```
Copy your entire project folder before deleting anything
```

### Step 2: Delete Demo Scenes (Low Risk)
All `.unity` files in GameResources are demo scenes - delete them:
- All 5 skybox demo scenes
- All turret demo scenes (3 files)
- Laser demo scenes (2 files)
- GUI demo scenes (2 files)
- Radar demo scene
- UnityTechnologies menu scene
- HologramShield demo scene

### Step 3: Delete Unused Audio (BIG SAVING)
- `Assets/GameResources/Turrets/SoundFile/Ash Valley Cybernetics Lite/` - Entire folder

### Step 4: Delete Unused Models (Careful)
After confirming in Unity Editor:
- StarSparrow prefab variants (keep StarSparrow1.fbx and its materials)
- Unused skybox variants
- `MODERN WARFARE` folder
- `Standard Assets` folder
- `centurionweapon` folder (root level, not the one in Turrets)

### Step 5: Use Unity's Built-in Tool
After manual cleanup, in Unity:
1. Go to `Edit > Project Settings > Editor`
2. Enable "Remove Unused Assets" for builds
3. Run `Assets > Reimport All` to clean up

---

## Quick Reference: Files to KEEP

```
GameResources/
├── Free Skyboxes - Space/SBS Space 2/Medium/    ← Only this variant
├── GUI/                                          ← Keep all (HUD scripts)
├── HologramShieldShader/                         ← Keep (minus demo scene)
├── LaserVFX/                                     ← Keep
├── Plane Models/StarSparrow/Meshes/StarSparrow1.fbx  ← Only this model
├── RadarSystem/                                  ← Check if used
├── Sounds/                                       ← Keep
├── SpaceShip/Battleships/Ardent/                 ← Keep
├── SpaceShip/Battleships/Fomalhaut/              ← Keep
├── SpaceShip/Battleships/Tannhauser/             ← Keep
├── Turrets/Turrets/ION_Cannon/Models/            ← Keep model only
└── UnityTechnologies/.../Fire & Explosion/       ← Keep explosion prefabs
```

---

## Estimated Space Savings

| Category | Files | Est. Size |
|----------|-------|-----------|
| Turrets audio folder | 177 MP3 | ~50-100MB |
| Unused 3D models (59 files) | 59 FBX | ~30-50MB |
| Unused 2D textures (~250 files) | ~250 PNG/JPG/PSD | ~80-150MB |
| Demo scenes and lightmaps | ~15 files | ~20-30MB |
| Unused skybox textures | 84 PNG | ~40-60MB |

**Total potential savings: ~220-390MB**

---

## Quick Deletion Commands (PowerShell)

**⚠️ BACKUP FIRST! Run these from your project root:**

### Delete all demo .unity scenes in GameResources:
```powershell
Get-ChildItem -Path "Assets\GameResources" -Recurse -Filter "*.unity" | Remove-Item -WhatIf
# Remove -WhatIf to actually delete
```

### Delete unused StarSparrow models (keep only StarSparrow1):
```powershell
Get-ChildItem -Path "Assets\GameResources\Plane Models\StarSparrow\Meshes" -Filter "*.fbx" | 
  Where-Object { $_.Name -notmatch "StarSparrow1\.fbx$" } | Remove-Item -WhatIf
```

### Delete entire unused folders:
```powershell
# These folders are completely unused:
Remove-Item -Recurse -WhatIf "Assets\GameResources\MODERN WARFARE"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Standard Assets"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Turrets\Neck_Mech_Walker"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Turrets\Turrets\WarZone Sci-Fi Turret pack"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Turrets\Turrets\SimpleTurret"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Turrets\Turrets\centurionweapon"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Turrets\SoundFile"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Plane Models\StarSparrow\Meshes\BonusContent"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Plane Models\StarSparrow\Textures\BonusContent"
Remove-Item -Recurse -WhatIf "Assets\GameResources\SpaceShip\Battleships\Firefly"
Remove-Item -Recurse -WhatIf "Assets\GameResources\SpaceShip\Battleships\Harbinger"
Remove-Item -Recurse -WhatIf "Assets\GameResources\SpaceShip\Hovl Studio\3D Lasers Pack\Demo scene lasers"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Free Skyboxes - Space\Demo Scenes"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Free Skyboxes - Space\SBS Space 1"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Free Skyboxes - Space\SBS Space 3"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Free Skyboxes - Space\SBS Space 4"
Remove-Item -Recurse -WhatIf "Assets\GameResources\Free Skyboxes - Space\SBS Space 5"
```

---

## Important Notes

1. Always test your game after deleting assets!
2. Some assets might be loaded via `Resources.Load()` - check your scripts
3. Unity will show errors for missing references - use these to identify if you deleted something needed
4. The Rick Astley video file in Assets root (`Rick Astley - Never Gonna...mp4`) is probably unused too 😄
