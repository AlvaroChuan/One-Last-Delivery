using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BalanceUI : NetworkBehaviour
{
    [SerializeField] private string _workdaySceneName = "GameScene";
    [SerializeField] private TransactionUIEntry _transactionPrefab;
    [SerializeField] private Transform _transactionContainer;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private TextMeshProUGUI _totalMoneyText;
    [SerializeField] private TextMeshProUGUI _resultsText;
    [SerializeField] private TextMeshProUGUI _previousMoneyText;
    [SerializeField] private float _animationDuration = 1f;
    [SyncVar(hook = nameof(OnBalanceChanged))] private List<Transaction> _balance;
    [SyncVar(hook = nameof(OnPreviousMoneyChanged))] private float _previousMoney = -1;
    WaitForSeconds _waitForAnimationDelay;
    float _totalBalance;
    bool _balanceUpdated = false;
    bool _previousMoneyUpdated = false;

    public override void OnStartServer()
    {
        _balance = BalanceManager.GetBalance();
        _previousMoney = MoneyManager.CurrentMoney;
        string balanceString = string.Join(", ", _balance.ConvertAll(t => $"{t.reason}: {t.amount:F2}"));
        DevLogger.Log($"Balance for the day: {balanceString}");
        DevLogger.Log($"Previous money: {_previousMoney:F2}");
    }

    void OnBalanceChanged(List<Transaction> oldBalance, List<Transaction> newBalance)
    {
        _balanceUpdated = true;
        _waitForAnimationDelay = new WaitForSeconds(_animationDuration / (newBalance.Count + 1));
        if (_previousMoneyUpdated)
        {
            StartCoroutine(UIAnimation());
        }
    }

    void OnPreviousMoneyChanged(float oldMoney, float newMoney)
    {
       DevLogger.Log($"Previous money updated. Old money: {oldMoney:F2}, New money: {newMoney:F2}");
        _previousMoneyUpdated = true;
        _previousMoneyText.text = newMoney.ToString("F2");
        if (_balanceUpdated)
        {
            StartCoroutine(UIAnimation());
        }
    }

    IEnumerator UIAnimation()
    {
        DevLogger.Log("Starting UI Animation Coroutine");
        foreach (Transform child in _transactionContainer)
        {
            Destroy(child.gameObject);
        }

        float totalMoney = _previousMoney;

        _totalMoneyText.text = totalMoney.ToString("F2");
        _totalMoneyText.color = totalMoney >= 0 ? Color.green : Color.red;

        yield return _waitForAnimationDelay;

        foreach (var transaction in _balance)
        {
            TransactionUIEntry entry = Instantiate(_transactionPrefab, _transactionContainer);
            entry.SetTransaction(transaction);
            _totalBalance += transaction.amount;

            totalMoney = _previousMoney + _totalBalance;

            _totalMoneyText.text = totalMoney.ToString("F2");
            _totalMoneyText.color = totalMoney >= 0 ? Color.green : Color.red;

            yield return _waitForAnimationDelay; // Wait for the next frame to allow UI to update
        }

        _resultsText.gameObject.SetActive(true);
        _resultsText.text = totalMoney >= 0 ? "Good job today" : "You're fired";
        _resultsText.color = totalMoney >= 0 ? Color.green : Color.red;

        if (isServer)
        {
            ProcessResults();
        }
    }

    void ProcessResults()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (MoneyManager.ServerSubtractMoney(-_totalBalance))
        {
            _continueButton.gameObject.SetActive(true);
        }
        else
        {
            _exitButton.gameObject.SetActive(true);
        }
    }

    public void OnContinueButtonClicked()
    {
        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene(_workdaySceneName);
        }
    }

    public void OnExitButtonClicked()
    {
        if (isServer)
        {
            NetworkManager.singleton.StopHost();
        }
    }
}
