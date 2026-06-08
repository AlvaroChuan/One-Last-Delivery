using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance { get; private set; }
    [SyncVar (hook = nameof(OnMoneyChanged))]
    private float _currentMoney;
    public float CurrentMoney => _currentMoney;
    string[] _gameSceneNames = new string[] { "GameScene" };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        SceneManager.sceneLoaded += OnSceneChange;
    }

    void OnSceneChange(Scene scene, LoadSceneMode mode)
    {
        bool isGameScene = false;
        foreach (string gameSceneName in _gameSceneNames)
        {
            if (scene.name == gameSceneName)
            {
                isGameScene = true;
                break;
            }
        }
        if (!isGameScene)
        {
            NetworkManager.Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        SceneManager.sceneLoaded -= OnSceneChange;
    }

    [Command]
    public void CmdAddMoney(float amount)
    {
        _currentMoney += amount;
    }
    [Server]
    public void ServerAddMoney(float amount)
    {
        _currentMoney += amount;
    }

    /// <summary>
    /// Send Network
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="amount"></param>
    [Server]
    public bool ServerSubtractMoney(float amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerSubtractQuota(float amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            return true;
        }
        return false;
    }

    private void OnMoneyChanged(float oldMoneyAmount, float newMoneyAmount)
    {
        // Update UI on clients when money changes
    }
}
