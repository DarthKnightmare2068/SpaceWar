## 2026-04-12 - Optimized Turret Target Assignment
**Learning:** Sorting lists and searching for game objects by tag in Update() creates significant per-frame overhead. Separating target assignment (expensive) from target tracking (required per frame) and using timers for the former is a highly effective optimization pattern in Unity.
**Action:** Always check high-frequency methods (Update, FixedUpdate) for sorting logic, GetComponent calls, or tag-based searches. Move them to timer-based intervals if 100% real-time accuracy isn't critical for gameplay.
