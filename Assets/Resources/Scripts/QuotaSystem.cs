using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuotaSystem : NetworkBehaviour
{
    [SerializeField] private int _initialQuota = 100;
    [SerializeField] private int _quotaIncreasePerDay = 10;
    public static QuotaSystem Instance { get; private set; }
    [SyncVar (hook = nameof(OnQuotaChanged))]
    private int _currentQuota;
    public int CurrentQuota => _currentQuota;

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

    override public void OnStartServer()
    {
        base.OnStartServer();
        _currentQuota = _initialQuota;
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
        if (isGameScene)
        {
            IncreaseQuota();
        }
        else
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
    public void CmdSubtractQuota(int amount)
    {
        if(!MoneyManager.Instance.ServerSubtractQuota(amount)) // Subtract quota from the MoneyManager)
        {
            ServerDefeat(); // Handle defeat condition if quota cannot be subtracted
        }
    }

    [Server]
    public void ServerDefeat()
    {
        // Handle defeat condition (e.g., end game, show defeat screen, etc.)

    }
    private void OnQuotaChanged(int oldQuotaAmount, int newQuotaAmount)
    {
        // Update UI on clients when quota changes
    }

    private void IncreaseQuota()
    {
        _currentQuota += _quotaIncreasePerDay;
    }
}