using UnityEngine;

// Marker interface for entities that the player's auto-target lock can acquire.
// Lets AutoTargetLock.GetLockableTarget collapse a 4-way type chain into one
// GetComponentInParent<ITargetable>() call.
public interface ITargetable
{
    Transform Transform { get; }
    bool IsAlive { get; }
}
