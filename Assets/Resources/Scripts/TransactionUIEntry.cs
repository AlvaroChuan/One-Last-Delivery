using TMPro;
using UnityEngine;

public class TransactionUIEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _reasonText;
    [SerializeField] private TextMeshProUGUI _amountText;

    public void SetTransaction(Transaction transaction)
    {
        _reasonText.text = transaction.reason;
        _amountText.text = transaction.amount >= 0 ? $"+{transaction.amount:F2}" : $"{transaction.amount:F2}";
    }
}