using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoneyManager : NetworkSingleton<MoneyManager>
{
    [SyncVar (hook = nameof(OnMoneyChanged))]
    private float _currentMoney;
    public float CurrentMoney => _currentMoney;

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

    /// <summary>
    /// Send Network
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="amount"></param>
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
        // Update UI on clients when money changes
    }
}
