## 2025-05-14 - [Unity Performance] Replacing Global Tag Searches with Cached References
**Learning:** Using GameObject.FindGameObjectWithTag or FindGameObjectsWithTag in Update() is a common bottleneck in Unity. These operations are O(N) and can cause frame rate stuttering when called frequently across many objects.
**Action:** Replace scene-wide searches with O(1) cached references in a central GameManager. Use local caching (Start/Awake) for components on the same or child objects.
