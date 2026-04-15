## 2026-04-15 - Optimized Turret Targeting System
**Learning:** High-frequency sorting of lists (like finding the nearest target) and scene-wide searches (FindGameObjectsWithTag) inside Update loops are major performance killers in Unity. Additionally, using Vector3.Distance repeatedly adds unnecessary square root calculations.
**Action:** Throttle complex logic (sorting, assignment) to run at lower frequencies (5Hz) while maintaining smooth tracking by calling lightweight rotation methods every frame using cached results. Always prefer sqrMagnitude for relative distance comparisons.
