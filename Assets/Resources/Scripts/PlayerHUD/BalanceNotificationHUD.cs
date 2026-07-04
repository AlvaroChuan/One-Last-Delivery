using UnityEngine;
using Mirror;

public class BalanceNotificationHUD : NetworkBehaviour
{
    [SerializeField] private TransactionNotification _notificationPrefab;
    [SerializeField] private Transform _notificationParent;

    private void OnEnable()
    {
        BalanceManager.OnTransactionRegistered += OnTransactionRegistered;
    }

    private void OnDisable()
    {
        BalanceManager.OnTransactionRegistered -= OnTransactionRegistered;
    }

    private void OnTransactionRegistered(Transaction transaction)
    {
        RpcSpawnNotification(transaction);
    }

    [ClientRpc]
    private void RpcSpawnNotification(Transaction transaction)
    {
        var notification = Instantiate(_notificationPrefab, _notificationParent);
        notification.Initialize(transaction);
    }
}