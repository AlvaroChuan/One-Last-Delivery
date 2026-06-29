using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class manages the balance of money in the game. It keeps track of all transactions and provides methods to register and retrieve them.
/// </summary>
public class BalanceManager : MonoBehaviour
{
    private static List<Transaction> Balance = new List<Transaction>();
    private void Awake()
    {
        Balance.Clear();
    }

    public static void RegisterTransaction(string reason, float amount)
    {
        Balance.Add(new Transaction(reason, amount));
    }

    public static List<Transaction> GetBalance()
    {
        return Balance;
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