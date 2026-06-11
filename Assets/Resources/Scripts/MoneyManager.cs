using Mirror;
using UnityEngine;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance { get; private set; }
    [SyncVar (hook = nameof(OnMoneyChanged))]
    private int _currentMoney;
    public int CurrentMoney => _currentMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Command]
    public void CmdAddMoney(int amount)
    {
        _currentMoney += amount;
    }

    /// <summary>
    /// Send Network
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="amount"></param>
    [Server]
    public bool ServerSubtractMoney(int amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            return true;
        }
        return false;
    }

    [Server]
    public bool ServerSubtractQuota(int amount)
    {
        if (_currentMoney >= amount)
        {
            _currentMoney -= amount;
            return true;
        }
        return false;
    }

    private void OnMoneyChanged(int oldMoneyAmount, int newMoneyAmount)
    {
        // Update UI on clients when money changes
    }
}
