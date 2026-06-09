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
    [SyncVar (hook = nameof(OnQuotaChanged))]
    public Action<QuotaChangeInfo> onQuotaChangedEvent;
    private int _currentQuota;
    public int CurrentQuota => _currentQuota;

    override public void OnStartServer()
    {
        base.OnStartServer();
        _currentQuota = _initialQuota;
    }

    protected override void OnSceneChange(Scene scene, LoadSceneMode mode)
    {
        base.OnSceneChange(scene, mode);
        if (IsActiveScene())
        {
            IncreaseQuota();
        }
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
}