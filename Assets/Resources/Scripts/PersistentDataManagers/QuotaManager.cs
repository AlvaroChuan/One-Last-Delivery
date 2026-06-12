using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuotaManager : PersistentDataManager<QuotaManager, QuotaManager.QuotaStaticState, float>
{
    public class QuotaStaticState : StaticStateBase
    {
        public bool isFirstDay = true;
        public override void Reset()
        {
            isFirstDay = true;
            StaticData = 0f;
        }
    }

    [Header("Settings")]
    [SerializeField] private float _initialQuota = 100f;
    [SerializeField] private float _quotaIncreasePerDay = 10f;

    [SyncVar(hook = nameof(OnQuotaChanged))] private float _currentQuota;

    protected override void ServerInitializeStaticData()
    {
        if (StaticDataState.isFirstDay)
        {
            StaticDataState.StaticData = _initialQuota;
            StaticDataState.isFirstDay = false;
        }
        else
        {
            SubtractQuotaFromMoney();
            StaticDataState.StaticData += _quotaIncreasePerDay;
        }
    }

    protected override void ServerUpdateInstanceData()
    {
        _currentQuota = StaticDataState.StaticData;
    }

    private void OnQuotaChanged(float oldQuota, float newQuota)
    {
        onDataChangedEvent?.Invoke(new DataChangeInfo
        {
            oldValue = oldQuota,
            newValue = newQuota
        });
    }

    void SubtractQuotaFromMoney()
    {
        if (!MoneyManager.ServerSubtractMoney(StaticDataState.StaticData))
        {
            Defeat();
        }
    }

    void Defeat()
    {
        DevLogger.Log("Quota reached zero. Triggering defeat.");
    }

    #if UNITY_EDITOR
    [ContextMenu("Reload Scene (Simulate Day Progression)")]
    private void ReloadScene()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
    }
    #endif
}