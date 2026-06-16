using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public float CurrentMoney => StaticDataState.StaticData;

    protected override void ServerInitializeStaticData()
    {
        if (StaticDataState.isFirstDay)
        {
            StaticDataState.StaticData = _initialMoney;
            StaticDataState.isFirstDay = false;
        }
    }

    protected override void ServerUpdateInstanceData()
    {
        _currentMoney = StaticDataState.StaticData;
    }

    private void OnMoneyChanged(float oldMoney, float newMoney)
    {
        onDataChangedEvent?.Invoke(new DataChangeInfo
        {
            oldValue = oldMoney,
            newValue = newMoney
        });
    }

    [Server]
    public static bool ServerSubtractMoney(float amount)
    {
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
