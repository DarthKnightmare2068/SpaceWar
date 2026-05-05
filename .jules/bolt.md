## 2026-05-05 - [Turret System Optimization]
**Learning:** Replacing Vector3.Distance with sqrMagnitude in O(N log N) sorting operations significantly reduces CPU overhead by eliminating redundant square root calculations.
**Action:** Always prefer sqrMagnitude for proximity checks and sorting in Unity performance-critical paths.
