using Mirror;
using UnityEngine;

public class MoneyManager : NetPersistentDataManager<MoneyManager, MoneyManager.MoneyStaticState, float>
{
    public class MoneyStaticState : StaticStateBase
    {
        public bool isFirstDay = true;
        public override void Reset()
        {
            isFirstDay = true;
            StaticData = 0f;
        }
    }

    [Header("Settings")]
    [SerializeField] private float _initialMoney = 100f;

    [SyncVar(hook = nameof(OnMoneyChanged))] private float _currentMoney;

    public static float CurrentMoney => StaticDataState.StaticData;

    protected override void ServerInitializeStaticData()
    {
        if (StaticDataState.isFirstDay)
        {
            StaticDataState.StaticData = _initialMoney;
            StaticDataState.isFirstDay = false;
            DevLogger.Log($"Initializing money for the first day. Setting initial money to {_initialMoney}");
        }
    }

    protected override void ServerUpdateInstanceData()
    {
        _currentMoney = StaticDataState.StaticData;
    }

    private void OnMoneyChanged(float oldMoney, float newMoney)
    {
        OnDataChangedEvent?.Invoke(new DataChangeInfo
        {
            oldValue = oldMoney,
            newValue = newMoney
        });
    }

    [Server]
    public static bool ServerSubtractMoney(float amount)
    {
        DevLogger.Log($"Attempting to subtract {amount} from money. Current money: {StaticDataState.StaticData}");
        if (StaticDataState.StaticData >= amount)
        {
            StaticDataState.StaticData -= amount;
            return true;
        }
        return false;
    }

    [Server]
    public static void ServerAddMoney(float amount)
    {
        StaticDataState.StaticData += amount;
    }
}
