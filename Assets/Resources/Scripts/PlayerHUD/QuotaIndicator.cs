using Mirror;
using TMPro;
using UnityEngine;

public class QuotaIndicator : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _quoteQuantity;

    [SyncVar (hook = nameof(OnQuotaSync))] private float _dailyQuota;
    [SyncVar (hook = nameof(OnMoneySync))] private float _currentMoney;

    public override void OnStartServer()
    {
        base.OnStartServer();
        BalanceManager.OnTransactionRegistered += OnTransaction;
        QuotaManager.OnDataChangedEvent += OnQuotaChanged;
        MoneyManager.OnDataChangedEvent += OnMoneyChanged;

        _dailyQuota = QuotaManager.CurrentQuota;
        _currentMoney = MoneyManager.CurrentMoney + BalanceManager.GetBalance();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _quoteQuantity.text = $"{_currentMoney:0.00}/{_dailyQuota:0.00}";
    }

    private void OnDestroy()
    {
        BalanceManager.OnTransactionRegistered -= OnTransaction;
        QuotaManager.OnDataChangedEvent -= OnQuotaChanged;
        MoneyManager.OnDataChangedEvent -= OnMoneyChanged;
    }

    void OnMoneyChanged(MoneyManager.DataChangeInfo moneyChangeInfo)
    {
        _currentMoney = moneyChangeInfo.newValue + BalanceManager.GetBalance();
    }

    void OnQuotaChanged(QuotaManager.DataChangeInfo quota)
    {
        _dailyQuota = quota.newValue;
    }

    public void OnTransaction(Transaction transaction)
    {
        _currentMoney = MoneyManager.CurrentMoney + BalanceManager.GetBalance();
    }

    void OnQuotaSync(float oldQuota, float newQuota)
    {
        _quoteQuantity.text = $"{_currentMoney:0.00}/{newQuota:0.00}";
    }

    void OnMoneySync(float oldMoney, float newMoney)
    {
        _quoteQuantity.text = $"{newMoney:0.00}/{_dailyQuota:0.00}";
    }
}
