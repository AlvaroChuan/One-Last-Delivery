using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class PlayerRegistry : NetworkBehaviour
{
    private static PlayerRegistry Instance;
    private readonly SyncList<GameObject> _spawnedPlayers = new SyncList<GameObject>();
    public static List<GameObject> SpawnedPlayers => Instance?._spawnedPlayers.ToList();
    private void Awake()
    {
        Instance = this;
    }

    public static void RegisterPlayer(GameObject player)
    {
        if (Instance == null) return;
        if (!Instance.isServer) return;

        if (!Instance._spawnedPlayers.Contains(player))
        {
            Instance._spawnedPlayers.Add(player);
        }
    }
}