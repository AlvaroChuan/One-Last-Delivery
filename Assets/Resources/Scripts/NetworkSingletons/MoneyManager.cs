using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoneyManager : NetworkSingleton<MoneyManager>
{
    public struct MoneyChangeInfo
    {
        public float oldMoneyAmount;
        public float newMoneyAmount;
    }
    [SyncVar (hook = nameof(OnMoneyChanged))]
    [SerializeField]
    private float _currentMoney;
    public float CurrentMoney => _currentMoney;
    public Action<MoneyChangeInfo> onMoneyChangedEvent;

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
        onMoneyChangedEvent?.Invoke(new MoneyChangeInfo
        {
            oldMoneyAmount = oldMoneyAmount,
            newMoneyAmount = newMoneyAmount
        });
    }
}
