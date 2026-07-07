using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// This class manages the balance of money in the game. It keeps track of all transactions and provides methods to register and retrieve them.
/// </summary>
public class BalanceManager : MonoBehaviour
{
    public static Action<Transaction> OnTransactionRegistered;
    private static List<Transaction> Balance = new List<Transaction>();
    private void Awake()
    {
        Balance.Clear();
    }

    public static void RegisterTransaction(string reason, float amount)
    {
        DevLogger.Log($"Registering transaction: {reason}: {amount}");
        Balance.Add(new Transaction(reason, amount));
        OnTransactionRegistered?.Invoke(Balance[Balance.Count - 1]);
    }

    public static List<Transaction> GetBalanceList()
    {
        return Balance;
    }

    public static float GetBalance()
    {
        float total = 0f;
        foreach (var transaction in Balance)
        {
            total += transaction.amount;
        }
        return total;
    }
}
[System.Serializable]
public struct Transaction
{
    public string reason;
    public float amount;

    public Transaction(string reason, float amount)
    {
        this.reason = reason;
        this.amount = amount;
    }
}