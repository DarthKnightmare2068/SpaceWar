using System.Collections.Generic;
using UnityEngine;

// Central registry for game entities. Populated by GameManager on spawn/despawn.
// All other systems read from here instead of calling FindGameObjectsWithTag / FindObjectOfType.
public static class GameEntityRegistry
{
    public static Transform Player { get; private set; }
    public static GameObject PlayerObject { get; private set; }

    public static void RegisterPlayer(GameObject player)
    {
        PlayerObject = player;
        Player = player != null ? player.transform : null;
    }

    public static void UnregisterPlayer()
    {
        PlayerObject = null;
        Player = null;
    }

    public static List<GameObject> GetEnemyShips() =>
        GameManager.Instance != null ? GameManager.Instance.GetActiveEnemyShips() : null;
}
