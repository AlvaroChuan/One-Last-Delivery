using Mirror;
using TMPro;
using UnityEngine;

public class QuotaIndicator : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _quoteQuantity;

    private QuotaManager _quotaManager;

    private float _dailyQuota, _currentQuota;

    public override void OnStartServer()
    {
        _quotaManager = FindObjectOfType<QuotaManager>();
        _quotaManager.quotaHUD = this;

        BalanceManager.OnTransactionRegistered += SetActualQuota;
    }

    public void SetDailyQuota(float quota)
    {
        _dailyQuota = quota;
        UpdateHUD();
    }

    [Command]
    public void SetActualQuota(Transaction transaction)
    {
        RcpSetCurrentQuota();
    }

    [ClientRpc]
    public void RcpSetCurrentQuota()
    {
        _currentQuota = BalanceManager.GetBalance() + MoneyManager.CurrentMoney;
    }

    void UpdateHUD()
    {
        _quoteQuantity.text = _currentQuota.ToString("N0") + " / " + _dailyQuota.ToString("N0");
    }
}
