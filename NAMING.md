# SpaceWar Project - Naming Conventions & Variables Reference

This document summarizes all the key variable names, classes, and their purposes throughout the SpaceWar Unity project.

**Last Updated:** Debug logs removed from all scripts.

---

## Table of Contents
1. [Manager Scripts](#manager-scripts)
2. [Player/Plane Scripts](#playerplane-scripts)
3. [Enemy Scripts](#enemy-scripts)
4. [Weapon Scripts](#weapon-scripts)
5. [GUI/UI Scripts](#guiui-scripts)
6. [Utility Scripts](#utility-scripts)
7. [Pooling Systems](#pooling-systems)
8. [Tags Used](#tags-used)
9. [Layers Used](#layers-used)
10. [Singleton Instances](#singleton-instances)

---

## Manager Scripts

### GameManager (`Assets/Scripts/Manager/GameplayScene/GameManager.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static GameManager` | Singleton instance for global access |
| `deadScreen` | `GameObject` | UI element shown when player dies |
| `deathVideo` | `VideoPlayer` | Video played on death screen |
| `playerPrefab` | `GameObject` | Prefab used to spawn the player |
| `currentPlayer` | `GameObject` | Reference to the currently spawned player |
| `bossPrefab` | `GameObject` | Prefab used to spawn the main boss |
| `bossMinYSpawn` | `float` | Minimum Y position for boss spawn (default: 500) |
| `playerBossYDistance` | `float` | Distance below boss where player spawns (default: 200) |
| `currentBoss` | `GameObject` | Reference to the currently spawned boss |
| `enemyShip1Prefab` | `GameObject` | Prefab for enemy ship 1 (front escort) |
| `enemyShip2Prefab` | `GameObject` | Prefab for enemy ship 2 (left escort) |
| `enemyShip3Prefab` | `GameObject` | Prefab for enemy ship 3 (right escort) |
| `frontDistance` | `float` | Distance of front escort from boss (default: 500) |
| `sideDistance` | `float` | Distance of side escorts from boss (default: 800) |
| `activeEnemyShips` | `List<GameObject>` | List of currently active enemy ships |
| `playerBossMinDistance` | `float` | Minimum horizontal distance from boss when respawning |
| `groundPrefab` | `GameObject` | Ground reference for boundary checking |
| `mainBossHealthBar` | `EnemyHealthBar` | Health bar UI for main boss |
| `enemyShip1HealthBar` | `EnemyHealthBar` | Health bar UI for enemy ship 1 |
| `enemyShip2HealthBar` | `EnemyHealthBar` | Health bar UI for enemy ship 2 |
| `enemyShip3HealthBar` | `EnemyHealthBar` | Health bar UI for enemy ship 3 |
| `uiCamera` | `Camera` | Dedicated camera for rendering UI elements |
| `playerExplosionVFX` | `GameObject` | Explosion VFX prefab for player death |
| `explosionVFXDuration` | `float` | Duration of explosion VFX (default: 2) |
| `reviveCD` | `ReviveCD` | Reference to revive countdown UI |
| `levelUpSystem` | `LevelUpSystem` | Reference to player level system |
| `targetFPS` | `int` | Target FPS lock (default: 60, 0 = disabled) |
| `enemyDestroyedClip` | `AudioClip` | Sound when enemy is destroyed |
| `enemyDestroyedVolume` | `float` | Volume for enemy destroyed sound |

### AudioSetting (`Assets/Scripts/Manager/GameplayScene/AudioSetting.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static AudioSetting` | Singleton instance |
| `machineGunSound` | `AudioClip` | Machine gun firing SFX |
| `machineGunSFXVolume` | `float` | Volume for machine gun (0-1) |
| `missileSound` | `AudioClip` | Missile launch SFX |
| `missileSFXVolume` | `float` | Volume for missile (0-1) |
| `laserSound` | `AudioClip` | Laser firing SFX |
| `laserSFXVolume` | `float` | Volume for laser (0-1) |
| `normalFlightSound` | `AudioClip` | Normal flight ambient sound |
| `normalFlightSoundVolume` | `float` | Volume for flight sound (0-1) |
| `thrusterSound` | `AudioClip` | Thruster boost sound |
| `thrusterSoundVolume` | `float` | Volume for thruster (0-1) |
| `respawnSound` | `AudioClip` | Player respawn sound |
| `respawnSoundVolume` | `float` | Volume for respawn (0-1) |
| `oneShotAudioPool` | `Queue<AudioSource>` | Pool for one-shot audio sources |
| `loopingPlayerAudio` | `Dictionary<GameObject, AudioSource>` | Per-player looping audio sources |

### StartGame (`Assets/Scripts/Manager/StartScene/StartGame.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `targetSceneName` | `string` | Scene to load (default: "Plane Test") |
| `fadeOutAudioOnTransition` | `bool` | Whether to fade audio on scene change |
| `fadeOutDuration` | `float` | Duration of audio fade out |
| `immediateTransition` | `bool` | Skip audio fade for immediate transition |
| `fadeOutAudioOnExit` | `bool` | Whether to fade audio when exiting game |
| `exitFadeOutDuration` | `float` | Duration of exit fade out |

### StartScreenAudio (`Assets/Scripts/Manager/StartScene/StartScreenAudio.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static StartScreenAudio` | Singleton instance (DontDestroyOnLoad) |
| `startScreenMusic` | `AudioClip` | Background music for start screen |
| `startScreenMusicVolume` | `float` | Volume for start music (0-1) |
| `buttonClickSound` | `AudioClip` | Button click SFX |
| `buttonClickVolume` | `float` | Volume for button click (0-1) |

### PauseUI (`Assets/Scripts/Manager/GameplayScene/PauseUI.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `planeCanvas` | `GameObject` | Main gameplay canvas |
| `pauseCanvas` | `GameObject` | Pause menu canvas |
| `controlButtonSetUp` | `GameObject` | Control tutorial panel |
| `continueButton` | `GameObject` | Continue game button |
| `controlButton` | `GameObject` | Show controls button |
| `returnButton` | `GameObject` | Return to main menu button |

---

## Player/Plane Scripts

### PlaneControl (`Assets/Scripts/Plane/PlaneControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `currentSpeed` | `float` | Current forward speed (default: 200) |
| `pitchPower` | `float` | Rotation speed around X axis (default: 50) |
| `yawPower` | `float` | Rotation speed around Y axis (default: 50) |
| `liftPower` | `float` | Upward force when moving forward (default: 5) |
| `gravityMultiplier` | `float` | Strength of simulated gravity (default: 2) |
| `fallMultiplier` | `float` | Extra downward force when stalling (default: 3.5) |
| `flipSpeed` | `float` | Degrees per second for barrel roll (default: 360) |
| `sideShiftAmount` | `float` | Sideways shift during flip (default: 5) |
| `doublePressWindow` | `float` | Time window for double press detection (default: 0.3) |
| `isFlipping` | `bool` | Whether plane is currently flipping |
| `autoBalanceStrength` | `float` | How quickly plane returns to level (default: 2) |
| `autoBalanceThreshold` | `float` | Minimum roll angle before auto-balance (default: 0.1) |
| `acceleration` | `float` | How quickly speed lerps (default: 1) |
| `maxSpeedAir` | `float` | Top cruise speed (default: 150) |
| `boostTargetSpeed` | `float` | Maximum speed during boost (default: 500) |
| `boostAcceleration` | `float` | Speed increase per second during boost (default: 50) |
| `maxThrusterThreshold` | `int` | Maximum thruster energy (default: 10) |
| `currentThrusterThreshold` | `int` | Current thruster energy |
| `mustRechargeThrusterFull` | `bool` | Must fully recharge before using again |
| `isBoosting` | `bool` | Whether currently boosting |
| `planeEffects` | `List<ParticleSystem>` | Particle systems for boost effects |
| `planeCamera` | `Transform` | Reference to plane's camera |
| `flightAudioSource` | `AudioSource` | Cached audio source for flight sounds |
| `thrusterAudioSource` | `AudioSource` | Cached audio source for thruster sounds |

### PlaneStats (`Assets/Scripts/Plane/PlaneStats.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `maxHP` | `int` | Maximum hit points (default: 100) |
| `currentHP` | `int` | Current hit points |
| `regenerationDelay` | `float` | Time without damage before regen starts (default: 3) |
| `regenerationRate` | `float` | % of max HP regenerated per second (default: 0.2) |
| `attackPoint` | `int` | Base damage dealt by player (default: 10) |
| `canTakeDamage` | `bool` | Whether plane can take damage (debug toggle) |

### LevelUpSystem (`Assets/Scripts/Plane/LevelUpSystem.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `currentLevel` | `int` | Current player level (starts at 1) |
| `MAX_LEVEL` | `const int` | Maximum level cap (30) |
| `expToNextLevel` | `float` | Experience needed for next level (default: 1000) |
| `currentExp` | `float` | Current experience points |
| `damageToExpMultiplier` | `float` | Multiplier for damage to exp conversion (default: 1) |
| `onLevelUp` | `UnityEvent<int>` | Event triggered when leveling up |
| `onMaxLevelReached` | `UnityEvent` | Event triggered at max level |
| `nextLvStatsScale` | `float` | Scaling factor for stats on level up (default: 3.14) |
| `SCAN_INTERVAL` | `const float` | How often to scan for enemies (default: 2) |

### AutoTargetLock (`Assets/Scripts/Plane/AutoTargetLock.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `targetingCamera` | `Camera` | Camera used for targeting |
| `targetTags` | `string[]` | Tags for targetable objects |
| `enemyLayer` | `LayerMask` | Layer mask for enemies |
| `lockCircleRadius` | `float` | Radius in viewport coordinates (default: 0.1) |
| `requireLineOfSight` | `bool` | Check line of sight to target |
| `obstacleLayer` | `LayerMask` | Layers that block line of sight |
| `weaponManager` | `PlayerWeaponManager` | Reference to weapon manager |
| `lockedTarget` | `Transform` | Currently locked target |
| `distanceToTarget` | `float` | Distance to locked target |
| `isTargetInLockCircle` | `bool` | Is target still in lock circle |
| `OnTargetLocked` | `Action<Transform>` | Event when target is locked |
| `OnTargetLost` | `Action<Transform>` | Event when target is lost |
| `enemyScanInterval` | `float` | How often to scan for enemies (default: 0.2) |

### CameraVisionControl (`Assets/Scripts/Plane/CameraVisionControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `frontCam` | `Camera` | Front view camera |
| `backCam` | `Camera` | Back view camera |
| `isFrontView` | `bool` | Track which camera is active |

---

## Weapon Scripts (Player)

### PlayerWeaponManager (`Assets/Scripts/Plane/Weapon/PlayerWeaponManager.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `targetLockUI` | `RectTransform` | UI element for targeting |
| `mainCamera` | `Camera` | Reference to main camera |
| `machineGunFireRange` | `float` | Machine gun range (default: 1000) |
| `missileFireRange` | `float` | Missile range (default: 800) |
| `targetableLayers` | `LayerMask` | Layers that can be targeted |
| `machineGunFireRate` | `float` | Time between shots (default: 0.1) |
| `maxBullets` | `int` | Maximum ammo capacity (default: 30) |
| `isInfinite` | `bool` | Infinite ammo mode |
| `currentBullets` | `int` | Current ammo count |
| `isReloading` | `bool` | Whether currently reloading |
| `reloadTime` | `float` | Time to reload (default: 2) |
| `missileLaunchDelay` | `float` | Delay between missiles (default: 3) |
| `maxMissiles` | `int` | Maximum missiles (default: 3) |
| `currentMissiles` | `int` | Current missile count |
| `nextLaunchTime` | `float` | Time until next missile can fire |

### MachineGunControl (`Assets/Scripts/Plane/Weapon/MachineGun/MachineGunControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `weaponManager` | `PlayerWeaponManager` | Reference to weapon manager |
| `machineGunSpawnPoints` | `List<Transform>` | Bullet spawn points |
| `bulletPrefab` | `GameObject` | Bullet prefab for visual feedback |
| `bulletSpeed` | `float` | Speed of visual bullets (default: 2000) |
| `bulletLifetime` | `float` | How long bullets last (default: 5) |
| `damage` | `float` | Damage per hit (default: 10) |
| `poolInitialized` | `bool` | Whether bullet pool is initialized |

### PlayerBullet (`Assets/Scripts/Plane/Weapon/MachineGun/PlayerBullet.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `pooledProjectile` | `PooledProjectile` | Cached reference for pool return |

### LaserActive (`Assets/Scripts/Plane/Weapon/LaserActive.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `laserFireRange` | `float` | Range for damage dealing (default: 100) |
| `laserDamage` | `int` | Damage per tick (default: 111) |
| `fireTickInterval` | `float` | How often to apply damage (default: 0.1) |
| `shootableLayers` | `LayerMask` | Layers the laser can hit |
| `laserCooldown` | `float` | Cooldown between uses |
| `maxThreshold` | `int` | Maximum energy threshold (default: 5) |
| `currentThreshold` | `int` | Current energy threshold |
| `maxLevel` | `int` | Maximum level (from LevelUpSystem.MAX_LEVEL) |
| `mustRechargeFull` | `bool` | Must fully recharge before firing again |
| `laserVFXPrefab` | `GameObject` | Laser VFX prefab |
| `laserVisualScript` | `VisualEffect` | Beam visual effect component |
| `explosionVFXPrefab` | `GameObject` | Explosion VFX at hit point |
| `CurrentBeamLength` | `float` | Current visual beam length |
| `loopingAudioSource` | `AudioSource` | Cached audio source for laser sound |

### MissileLaunch (`Assets/Scripts/Plane/Weapon/Missile/MissileLaunch.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `reloadThreshold` | `float` | Damage needed for 1 missile (default: 1000) |
| `missilePrefab` | `GameObject` | Missile prefab to spawn |
| `missileSpeed` | `float` | Speed of missile (default: 50) |
| `missileLifetime` | `float` | How long missile lives (default: 10) |
| `missileSpawnPoints` | `List<Transform>` | Spawn points for missiles |
| `useAutoTargetLock` | `bool` | Use auto-lock or dumb-fire mode |
| `damageAccumulated` | `float` | Damage accumulated for missile reload |

### MissileController (`Assets/Scripts/Plane/Weapon/Missile/MissileController.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `baseDamage` | `float` | Base missile damage (default: 100) |
| `useAutoTargetLock` | `bool` | Whether missile tracks target |
| `hasExploded` | `bool` | Prevent double explosion |
| `isInitialized` | `bool` | Prevent spawn triggers |

---

## Enemy Scripts

### EnemyStats (`Assets/Scripts/Enemy/EnemyStats.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `maxHP` | `float` | Maximum hit points (default: 1000) |
| `currentHP` | `float` | Current hit points |
| `onDeath` | `UnityEvent` | Event triggered on death |
| `deathVFX` | `GameObject` | VFX prefab on death |
| `weaponDmgControl` | `WeaponDmgControl` | Reference to weapon control |
| `FORCE_RESPAWN_DELAY` | `const float` | Delay before force respawn (10 seconds) |

### MainBossStats (`Assets/Scripts/Enemy/MainBossStats.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `maxHP` | `float` | Maximum hit points (default: 500000) |
| `currentHP` | `float` | Current hit points |
| `onDeath` | `UnityEvent` | Event triggered on death |
| `deathVFX` | `GameObject` | VFX prefab on death |
| `weaponDmgControl` | `WeaponDmgControl` | Reference to weapon control |
| `bossShield` | `GameObject` | Shield that protects boss |
| `sideShipRespawnThresholds` | `float[]` | HP thresholds for respawning side ships |
| `FORCE_RESPAWN_DELAY` | `const float` | Delay before force respawn (10 seconds) |

### WeaponDmgControl (`Assets/Scripts/Enemy/WeaponControl/WeaponDmgControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `bulletDamage` | `float` | Turret bullet damage (default: 20) |
| `turretFireRate` | `float` | Time between turret shots (default: 0.1) |
| `turretFireRange` | `float` | Turret detection range (default: 100) |
| `smallCanonDamage` | `float` | Small cannon DPS (default: 50) |
| `smallCanonFireRate` | `float` | Small cannon fire rate (default: 0.05) |
| `smallCanonFireRange` | `float` | Small cannon range (default: 100) |
| `bigCanonDamage` | `float` | Big cannon DPS (default: 100) |
| `bigCanonFireRate` | `float` | Big cannon fire rate (default: 0.1) |
| `bigCanonFireRange` | `float` | Big cannon range (default: 200) |
| `turretsManager` | `TurretsManager` | Reference to turrets manager |
| `turretReviveTime` | `float` | Time to revive turrets (default: 60) |
| `smallCanonManager` | `SmallCanonManager` | Reference to canon manager |
| `cannonReviveTime` | `float` | Time to revive cannons (default: 60) |
| `bigCannonReviveTime` | `float` | Time to revive big cannons (default: 90) |

### TurretControl (`Assets/Scripts/Enemy/WeaponControl/Turret/TurretsControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `body` | `Transform` | Turret body for rotation |
| `joint` | `Transform` | Turret joint for rotation |
| `gunBarrel` | `Transform` | Turret gun barrel |
| `turretSpawnPoints` | `List<Transform>` | Bullet spawn points |
| `maxRotationSpeed` | `float` | Rotation speed (default: 5) |
| `maxHP` | `int` | Maximum hit points |
| `currentHP` | `int` | Current hit points |
| `trackPlayerInstantly` | `bool` | Instant or smooth tracking |

### TurretsManager (`Assets/Scripts/Enemy/WeaponControl/Turret/TurretsManager.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `maxTurretsPerPlayer` | `int` | Max turrets targeting one player (default: 2) |
| `turrets` | `List<TurretControl>` | All turret instances |
| `bulletSpeed` | `float` | Bullet speed for all turrets (default: 100) |
| `turretHP` | `int` | HP for all turrets (default: 5246) |
| `turretDestroyedVFX` | `GameObject` | VFX when turret destroyed |
| `maxTurretCount` | `int` | Initial turret count |
| `currentTurretCount` | `int` | Current turrets alive |
| `trackPlayerInstantly` | `bool` | Instant or smooth tracking |
| `BACKUP_REFRESH_INTERVAL` | `const float` | Targeting refresh interval (1 second) |

### SmallCanonControl (`Assets/Scripts/Enemy/WeaponControl/SmallCanon/SmallCanonControl.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `body` | `Transform` | Rotates left/right on Z-axis |
| `joint` | `Transform` | Rotates up/down on X-axis |
| `gunBarrel` | `Transform` | Laser origin point |
| `laserVFX` | `VisualEffect` | Laser visual effect |
| `laserVFXPrefab` | `GameObject` | Laser VFX prefab |
| `enemy` | `Transform` | Target to aim at |
| `hittableLayers` | `LayerMask` | Layers the laser can hit |
| `maxRotationSpeed` | `float` | Rotation speed (default: 3) |
| `maxBodyRotationAngle` | `float` | Maximum body yaw (default: 90) |
| `maxJointRotationAngle` | `float` | Maximum joint pitch (default: 45) |
| `maxLaserScale` | `float` | Maximum laser length (default: 1000) |
| `maxHP` | `int` | Maximum hit points (default: 100) |
| `currentHP` | `int` | Current hit points |
| `TARGET_LOCK_DELAY` | `const float` | Delay before target lock (1 second) |
| `ROTATION_LIMIT_DELAY` | `const float` | Delay after hitting rotation limits (2 seconds) |
| `PLAYER_SEARCH_INTERVAL` | `const float` | How often to search for player (1 second) |

### SmallCanonManager (`Assets/Scripts/Enemy/WeaponControl/SmallCanon/SmallCanonManager.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `maxCanonsPerPlayer` | `int` | Max canons per player (default: 1) |
| `canons` | `List<SmallCanonControl>` | All canon instances |
| `canonHP` | `int` | HP for all canons (default: 10000) |
| `canonDestroyedVFX` | `GameObject` | VFX when canon destroyed |
| `reviveTime` | `float` | Time before revive (default: 60) |
| `maxCanonCount` | `int` | Initial canon count |
| `currentCanonCount` | `int` | Current canons alive |
| `trackPlayerInstantly` | `bool` | Instant or smooth tracking |

### BigCanon (`Assets/Scripts/Enemy/WeaponControl/BigCanon.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `body` | `Transform` | Rotates left/right on Z-axis |
| `joint` | `Transform` | Rotates up/down on X-axis |
| `gunBarrel` | `Transform` | Laser origin point |
| `laserVFX` | `VisualEffect` | Laser visual effect |
| `laserVFXPrefab` | `GameObject` | Laser VFX prefab |
| `enemy` | `Transform` | Target to aim at |
| `hittableLayers` | `LayerMask` | Layers the laser can hit |
| `maxRotationSpeed` | `float` | Rotation speed (default: 2) |
| `maxBodyRotationAngle` | `float` | Maximum body rotation (default: 90) |
| `maxJointRotationAngle` | `float` | Maximum joint pitch (default: 45) |
| `maxLaserScale` | `float` | Maximum laser length (default: 1000) |
| `maxHP` | `int` | Maximum hit points (default: 200) |
| `currentHP` | `int` | Current hit points |
| `explosionVFXPrefab` | `GameObject` | Explosion VFX on death |
| `trackPlayerInstantly` | `bool` | Always true for big cannon |

---

## Pooling Systems

### BulletPool (`Assets/Scripts/Enemy/WeaponControl/Turret/BulletPool.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static BulletPool` | Singleton instance |
| `bulletPrefab` | `GameObject` | Bullet prefab to pool |
| `poolSize` | `int` | Size of bullet pool (default: 100) |
| `bulletLifetime` | `float` | Lifetime for turret bullets (default: 5) |

### PlayerProjectilePool (`Assets/Scripts/Manager/GameplayScene/PlayerProjectilePool.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static PlayerProjectilePool` | Singleton instance |
| `initialBulletPoolSize` | `int` | Initial bullet pool size (default: 50) |
| `initialMissilePoolSize` | `int` | Initial missile pool size (default: 20) |
| `bulletPrefab` | `GameObject` | Bullet prefab reference |
| `missilePrefab` | `GameObject` | Missile prefab reference |
| `bulletPool` | `Queue<GameObject>` | Pool of inactive bullets |
| `missilePool` | `Queue<GameObject>` | Pool of inactive missiles |
| `activeBullets` | `List<PooledProjectile>` | Currently active bullets |
| `activeMissiles` | `List<PooledProjectile>` | Currently active missiles |
| `bulletContainer` | `Transform` | Parent for pooled bullets |
| `missileContainer` | `Transform` | Parent for pooled missiles |

### PooledProjectile (`Assets/Scripts/Manager/GameplayScene/PlayerProjectilePool.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `lifetime` | `float` | How long projectile stays active |
| `spawnTime` | `float` | When projectile was activated |
| `isActive` | `bool` | Whether projectile is currently active |

---

## GUI/UI Scripts

### EnemyHealthBar (`Assets/Scripts/GUI/EnemyUI/EnemyHealthBar.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `normalHealthBarSlider` | `Slider` | Main health bar |
| `easeHealthBarSlider` | `Slider` | Animated "ease" bar |
| `nameText` | `TextMeshProUGUI` | Enemy name text |
| `lerpSpeed` | `float` | Animation speed (default: 0.05) |
| `healthTargetType` | `HealthTargetType` | Enemy, MainBoss, or Custom |
| `enemyTarget` | `EnemyStats` | Target if type is Enemy |
| `bossTarget` | `MainBossStats` | Target if type is MainBoss |
| `customTarget` | `MonoBehaviour` | Target if type is Custom (IHasHealth) |

### PlayerHealthBar (`Assets/Scripts/GUI/PlaneUI/Bar/PlayerHealthBar.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `normalHealthBarSlider` | `Slider` | Main health bar |
| `easeHealthBarSlider` | `Slider` | Animated "ease" bar |
| `healthText` | `TextMeshProUGUI` | HP text display |
| `lerpSpeed` | `float` | Animation speed (default: 0.05) |

### ExpBar (`Assets/Scripts/GUI/PlaneUI/Bar/ExpBar.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `expSlider` | `Slider` | Experience bar slider |
| `levelText` | `TextMeshProUGUI` | Level text display |

### LaserAndThrusterBar (`Assets/Scripts/GUI/PlaneUI/Bar/LaserAndThrusterBar.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `laserSlider` | `Slider` | Laser energy slider |
| `thrusterSlider` | `Slider` | Thruster energy slider |
| `lerpSpeed` | `float` | Animation speed (default: 0.05) |

### DmgPopUp (`Assets/Scripts/GUI/PlaneUI/PopUp/DmgPopUp.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `current` | `static DmgPopUp` | Singleton instance |
| `dmgPopUpPrefab` | `GameObject` | Damage popup prefab |

### TargetLockUI (`Assets/Scripts/GUI/PlaneUI/Weapon/TargetLockUI.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `normalUI` | `GameObject` | UI when no target locked |
| `machineGunUI` | `GameObject` | UI for machine gun aiming |
| `missileLockUI` | `GameObject` | UI for missile lock |
| `laserRangeText` | `TMP_Text` | Laser range notification text |
| `blinkInterval` | `float` | How fast text blinks (default: 0.5) |
| `autoTargetLock` | `AutoTargetLock` | Reference to auto-lock system |
| `machineGunControl` | `MachineGunControl` | Reference to machine gun |
| `weaponManager` | `PlayerWeaponManager` | Reference to weapon manager |
| `missileLaunch` | `MissileLaunch` | Reference to missile system |
| `laserActive` | `LaserActive` | Reference to laser weapon |
| `CheatHp` | `TMP_Text` | Cheat mode indicator |
| `MissileModeText` | `TMP_Text` | Missile mode indicator |
| `PLAYER_SEARCH_INTERVAL` | `const float` | How often to search for player (0.5 seconds) |

### ReviveCD (`Assets/Scripts/GUI/PlaneUI/ReviveCD.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `text` | `TextMeshProUGUI` | Countdown text display |

### SpeedDisplay (`Assets/Scripts/GUI/PlaneUI/SpeedDisplay.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `speedText` | `TextMeshProUGUI` | Speed text display |
| `updateInterval` | `float` | How often to update (default: 0.1) |

### ScoreCounting (`Assets/Scripts/GUI/PlaneUI/ScoreCounting.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `Instance` | `static ScoreCounting` | Singleton instance |
| `totalDamageDealt` | `float` | Total damage dealt to enemies |
| `onDamageDealt` | `UnityEvent<float>` | Event when damage is dealt |

---

## Utility Scripts

### ShowExplosion (`Assets/Scripts/ShowExplosion.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `explosionVFX` | `GameObject` | Explosion VFX prefab |
| `vFXDuration` | `float` | Duration before VFX destroyed (default: 2) |
| `hasExploded` | `bool` | Prevent double VFX |

### IgnoreObjectWithTagColliding (`Assets/Scripts/IgnoreObjectWithTagColliding.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `ignoreTags` | `string[]` | Tags to ignore collision with |

### CustomShieldCollisionResponse (`Assets/Scripts/Enemy/CustomShieldCollisionResponse.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `effectDuration` | `float` | Duration of collision effect (default: 1) |
| `collisionRadius` | `float` | Collision effect radius (default: 1) |
| `collisionIntensity` | `float` | Collision effect intensity (default: 2) |
| `MAX_COLLISIONS` | `const int` | Maximum concurrent collisions (10) |

### HpScreen (`Assets/Scripts/GUI/PlaneUI/HpScreen.cs`)
| Variable | Type | Purpose |
|----------|------|---------|
| `volume` | `Volume` | Post-processing volume |
| `vignette` | `Vignette` | Vignette effect for low HP |

---

## Tags Used
| Tag | Used By |
|-----|---------|
| `Player` | Player plane |
| `Enemy` | Enemy ships, main boss |
| `Turret` | All turrets, small cannons, big cannons |
| `Bullet` | Enemy bullets (pooled) |
| `PlayerWeapon` | Player bullets and missiles |
| `Ground` | Ground/terrain |
| `SmallCanon` | Small cannons (legacy) |
| `BigCanon` | Big cannons (legacy) |

---

## Layers Used
| Layer | Purpose |
|-------|---------|
| `Default` | General objects |
| `Player` | Player plane and weapons |
| `Bullet` | Enemy bullets |

---

## Singleton Instances
| Class | Access Pattern |
|-------|----------------|
| `GameManager` | `GameManager.Instance` |
| `AudioSetting` | `AudioSetting.Instance` |
| `StartScreenAudio` | `StartScreenAudio.Instance` |
| `BulletPool` | `BulletPool.Instance` |
| `PlayerProjectilePool` | `PlayerProjectilePool.Instance` |
| `DmgPopUp` | `DmgPopUp.current` |
| `ScoreCounting` | `ScoreCounting.Instance` |

---

## Interfaces

### IHasHealth (`Assets/Scripts/GUI/EnemyUI/IHasHealth.cs`)
| Property | Type | Purpose |
|----------|------|---------|
| `CurrentHP` | `float` | Current health points |
| `MaxHP` | `float` | Maximum health points |
| `name` | `string` | Entity name |

**Implemented by:** `EnemyStats`, `MainBossStats`

---

## Deleted Scripts (Dead Code Removed)
The following scripts were removed as they contained no functional code:
- `LaserCD.cs` - Empty script with only empty Start() and Update()
