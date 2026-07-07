using System;
using System.Collections.Generic;
using Mirror;
using Telepathy;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Custom Spawner Settings")]
    [SerializeField] private GameObject[] _playerPrefabs;
    [SerializeField] private string _gameScene = "GameScene";
    [SerializeField] private string _balanceScene = "BalanceScene";
    [SerializeField] private GameObject _balanceScenePlayerPrefab;
    [SerializeField] private float _allPackagesDeliveredReward = 100f; // Reward for delivering all packages
    private List<NetworkConnectionToClient> _playerIndices = new List<NetworkConnectionToClient>();

    // Track how many characters we have spawned in the game scene
    private int _numberOfPlayers = 0;
    private int _deadPlayers = 0;
    bool _packageDestroyed = false; // Track if a package has been destroyed

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
        else if (SceneManager.GetActiveScene().name == _balanceScene)
        {
            DevLogger.Log("Player " + conn.connectionId + " has identity: " + (conn.identity != null) + " when joining balance scene.");
            // Only spawn if this connection doesn't already have an assigned character
            if (conn.identity == null)
            {
                // Spawn the balance scene player prefab for this connection
                GameObject balancePlayerInstance = Instantiate(_balanceScenePlayerPrefab);
                NetworkServer.AddPlayerForConnection(conn, balancePlayerInstance);
                DevLogger.Log($"Balance Scene Loaded: Spawned balance scene player for Connection {conn.connectionId}");
            }
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        // Changes the scene for everyone else if a client disconnects
        if (SceneManager.GetActiveScene().name != "GraphicsMainMenu")
        {
            ServerChangeScene("GraphicsMainMenu");
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        GetComponent<BaseVoiceChat>().StopVoiceChat();

        // Changes the scene locally for a client if they lose connection or if the host leaves
        if (SceneManager.GetActiveScene().name != "GraphicsMainMenu")
        {
            SceneManager.LoadScene("GraphicsMainMenu");
        }
    }

    private void SpawnPlayerForConnection(NetworkConnectionToClient conn)
    {
        int playerIndex = _numberOfPlayers; // Use the current number of players as the index
        if (_playerIndices.Contains(conn))
        {
            playerIndex = _playerIndices.IndexOf(conn);
        }
        if (playerIndex < _playerPrefabs.Length)
        {
            GameObject prefab = _playerPrefabs[playerIndex];

            // Grab a spawn point if you use NetworkStartPosition components
            Transform startPos = GetStartPosition();
            Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;
            Quaternion spawnRot = startPos != null ? startPos.rotation : Quaternion.identity;

            // Instantiate the prefab
            GameObject playerInstance = Instantiate(prefab, spawnPos, spawnRot);

            // Spawn it on the network and link it to the client
            NetworkServer.AddPlayerForConnection(conn, playerInstance);

            PlayerRegistry.RegisterPlayer(playerInstance);
            _numberOfPlayers++;
            DevLogger.Log($"Game Scene Loaded: Spawned character index {playerIndex} for Connection {conn.connectionId}");
            _playerIndices.Add(conn); // Keep track of the connection for future reference
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
        _deadPlayers = 0;
        _packageDestroyed = false; // Reset package destroyed flag when changing scenes

        if (newSceneName != _gameScene && newSceneName != _balanceScene)
        {
            _playerIndices.Clear(); // Clear the list of player connections if we're not in the game or balance scene
        }
    }

    public void NotifyPlayerDeath()
    {
        _deadPlayers++;
        DevLogger.Log($"Player died. Total dead players: {_deadPlayers}/{_numberOfPlayers}");

        if (_deadPlayers >= _numberOfPlayers)
        {
            DevLogger.Log("All players are dead. Transitioning to balance scene.");
            List<Transaction> transactions = BalanceManager.GetBalanceList();
            float totalBalance = 0f;
            foreach (var transaction in transactions)
            {
                totalBalance += transaction.amount;
            }
            float minimumPenalty = totalBalance + MoneyManager.CurrentMoney + 1;
            int digits = Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minimumPenalty))) + 1;
            digits = Mathf.Max(digits, 4); // Ensure at least 4 digits
            float nines = Mathf.Pow(10, digits) - 1;
            BalanceManager.RegisterTransaction("You all died!", -nines);
            ServerChangeScene(_balanceScene);
        }
    }

    public void NotifyAllPackagesDelivered()
    {
        if (!_packageDestroyed)
        {
            BalanceManager.RegisterTransaction("All packages delivered!", _allPackagesDeliveredReward);
        }
        ServerChangeScene(_balanceScene);
    }

    internal void NotifyPackageDestroyed()
    {
        _packageDestroyed = true;
    }
}