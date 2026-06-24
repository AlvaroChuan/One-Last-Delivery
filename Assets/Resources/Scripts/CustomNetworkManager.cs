using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Custom Spawner Settings")]
    [SerializeField] private GameObject[] _playerPrefabs;
    [SerializeField] private string _gameScene = "GameScene";

    // Track how many characters we have spawned in the game scene
    private int _numberOfPlayers = 0;
    public List<GameObject> SpawnedPlayers { get; private set; } = new List<GameObject>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        DevLogger.Log("CustomNetworkManager started on server.");
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        // Check if the server is currently in the active gameplay scene
        // (Make sure this matches your actual Game scene name exactly)
        if (SceneManager.GetActiveScene().name == _gameScene)
        {
            DevLogger.Log("Player " + conn.connectionId + " has identity: " + (conn.identity != null) + " when joining active game scene.");
            // Only spawn if this connection doesn't already have an assigned character
            if (conn.identity == null)
            {
                SpawnPlayerForConnection(conn);
            }
        }
    }

    private void SpawnPlayerForConnection(NetworkConnectionToClient conn)
    {
        if (_numberOfPlayers < _playerPrefabs.Length)
        {
            GameObject prefab = _playerPrefabs[_numberOfPlayers];

            // Grab a spawn point if you use NetworkStartPosition components
            Transform startPos = GetStartPosition();
            Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;
            Quaternion spawnRot = startPos != null ? startPos.rotation : Quaternion.identity;

            // Instantiate the prefab
            GameObject playerInstance = Instantiate(prefab, spawnPos, spawnRot);

            // Spawn it on the network and link it to the client
            NetworkServer.AddPlayerForConnection(conn, playerInstance);

            SpawnedPlayers.Add(playerInstance);
            _numberOfPlayers++;
            DevLogger.Log($"Game Scene Loaded: Spawned character index {_numberOfPlayers - 1} for Connection {conn.connectionId}");
        }
        else
        {
            DevLogger.LogWarning("Run out of unique player prefabs for joining players!");
        }
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        _numberOfPlayers = 0;
        SpawnedPlayers.Clear();
    }
}