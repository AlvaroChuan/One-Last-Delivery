using Mirror;
using UnityEngine;

public class QuotaSystem : NetworkBehaviour
{
    [SerializeField] private int _initialQuota = 100;
    [SerializeField] private int _quotaIncreasePerDay = 10;
    public static QuotaSystem Instance { get; private set; }
    [SyncVar (hook = nameof(OnQuotaChanged))]
    private int _currentQuota;
    public int CurrentQuota => _currentQuota;

    override public void OnStartServer()
    {
        base.OnStartServer();
        _currentQuota = _initialQuota;
    }

    override public void OnStartClient()
    {
        base.OnStartClient();
        if (Instance != null && Instance != this)
        {
            if(isServer)
            {
                Instance.IncreaseQuota(); // Increase quota on scene reload
                NetworkServer.Destroy(gameObject);
            }
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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