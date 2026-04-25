using System;
using System.Collections.Generic;
using UnityEngine;

// Central registry for game entities. Populated by GameManager on spawn/despawn.
// All other systems read from here instead of calling FindGameObjectsWithTag / FindObjectOfType.
public static class GameEntityRegistry
{
    public static Transform Player { get; private set; }
    public static GameObject PlayerObject { get; private set; }
    public static event Action<GameObject> PlayerChanged;

    public static void RegisterPlayer(GameObject player)
    {
        SetPlayer(player, notifyListeners: true);
    }

    public static void UnregisterPlayer()
    {
        SetPlayer(null, notifyListeners: true);
    }

    public static bool TryGetPlayerObject(out GameObject player)
    {
        if (PlayerObject != null)
        {
            player = PlayerObject;
            return true;
        }

        player = ResolvePlayerObject();
        if (player == null)
            return false;

        SetPlayer(player, notifyListeners: false);
        return true;
    }

    public static bool TryGetPlayerTransform(out Transform playerTransform)
    {
        playerTransform = null;
        if (!TryGetPlayerObject(out GameObject player))
            return false;

        playerTransform = player.transform;
        return playerTransform != null;
    }

    public static bool TryGetPlayerComponent<T>(out T component) where T : Component
    {
        component = null;
        if (!TryGetPlayerObject(out GameObject player))
            return false;

        component = player.GetComponent<T>();
        return component != null;
    }

    public static List<GameObject> GetEnemyShips() =>
        GameManager.Instance != null ? GameManager.Instance.GetActiveEnemyShips() : null;

    private static void SetPlayer(GameObject player, bool notifyListeners)
    {
        Transform playerTransform = player != null ? player.transform : null;
        bool hasChanged = PlayerObject != player || Player != playerTransform;

        PlayerObject = player;
        Player = playerTransform;

        if (notifyListeners && hasChanged)
            PlayerChanged?.Invoke(player);
    }

    private static GameObject ResolvePlayerObject()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            return GameManager.Instance.currentPlayer;

        return GameObject.FindGameObjectWithTag("Player");
    }
}
