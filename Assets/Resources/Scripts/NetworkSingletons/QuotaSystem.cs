using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuotaSystem : NetworkSingleton<QuotaSystem>
{
    public struct QuotaChangeInfo
    {
        public int oldQuotaAmount;
        public int newQuotaAmount;
    }
    [SerializeField] private int _initialQuota = 100;
    [SerializeField] private int _quotaIncreasePerDay = 10;
    [SerializeField] private int _initialPackagesToSpawn = 5;
    [SerializeField] private int _packageIncreasePerDay = 1;
    public Action<QuotaChangeInfo> onQuotaChangedEvent;
    [SyncVar (hook = nameof(OnQuotaChanged))]
    private int _currentQuota;
    public int CurrentQuota => _currentQuota;

    override public void OnStartServer()
    {
        base.OnStartServer();
        _currentQuota = _initialQuota;
        PackageSpawner.PackagesToSpawn = _initialPackagesToSpawn;
    }

    protected override void OnLoadActiveScene()
    {
        base.OnLoadActiveScene();
        IncreaseQuota();
        IncreasePackagesToSpawn();
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
        onQuotaChangedEvent?.Invoke(new QuotaChangeInfo
        {
            oldQuotaAmount = oldQuotaAmount,
            newQuotaAmount = newQuotaAmount
        });
    }

    private void IncreaseQuota()
    {
        _currentQuota += _quotaIncreasePerDay;
    }
    private void IncreasePackagesToSpawn()
    {
        PackageSpawner.PackagesToSpawn += _packageIncreasePerDay;
    }
}