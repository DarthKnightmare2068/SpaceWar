## 2025-05-14 - Optimized Expensive Component Lookups
**Learning:** Found significant performance bottlenecks in `EnemyStats.cs` and `WeaponDmgControl.cs` where `GetComponentsInChildren` and `GameObject.FindObjectsOfType` were called in `Update` or frequent lifecycle methods. These calls scan large parts of the scene or object hierarchy and can lead to major frame rate drops in Unity.
**Action:** Always cache component references in `Awake()` or `Start()` for any objects accessed frequently in `Update()`, `FixedUpdate()`, or damage-related methods.
