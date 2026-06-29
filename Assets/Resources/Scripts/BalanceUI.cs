using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BalanceUI : NetworkBehaviour
{
    [SerializeField] private string _mainMenuSceneName = "GraphicsMainMenu";
    [SerializeField] private int _countdownTime = 5;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private string _workdaySceneName = "GameScene";
    [SerializeField] private TransactionUIEntry _transactionPrefab;
    [SerializeField] private Transform _transactionContainer;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private TextMeshProUGUI _readyText;
    [SerializeField] private TextMeshProUGUI _totalMoneyText;
    [SerializeField] private TextMeshProUGUI _resultsText;
    [SerializeField] private TextMeshProUGUI _previousMoneyText;
    [SerializeField] private float _animationDuration = 1f;
    [SyncVar(hook = nameof(OnBalanceChanged))] private List<Transaction> _balance;
    [SyncVar(hook = nameof(OnPreviousMoneyChanged))] private float _previousMoney = -1;
    [SyncVar(hook = nameof(OnReadyChanged))] private int _readyCount = 0;
    [SyncVar] private int _playerCount = 0;
    WaitForSeconds _waitForAnimationDelay;
    float _totalBalance;
    bool _balanceUpdated = false;
    bool _previousMoneyUpdated = false;
    bool _ready = false;
    private Coroutine _countdownCoroutine;

    public override void OnStartServer()
    {
        _balance = BalanceManager.GetBalance();
        _previousMoney = MoneyManager.CurrentMoney;
        string balanceString = string.Join(", ", _balance.ConvertAll(t => $"{t.reason}: {t.amount:F2}"));
        DevLogger.Log($"Balance for the day: {balanceString}");
        DevLogger.Log($"Previous money: {_previousMoney:F2}");

        _playerCount = NetworkServer.connections.Count;
    }

    public override void OnStartClient()
    {
        DevLogger.Log($"BalanceUI started on client. Player count: {_playerCount}, Ready count: {_readyCount}");
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

        if (MoneyManager.CurrentMoney >= -_totalBalance)
        {
            MoneyManager.ServerSubtractMoney(-_totalBalance);
            DevLogger.Log($"Money updated successfully. New balance: {MoneyManager.CurrentMoney:F2}");
            RpcShowButtons(true);
            ServerShowButtons(true);
        }
        else
        {
            RpcShowButtons(false);
            ServerShowButtons(false);
        }
    }

    [ClientRpc]
    void RpcShowButtons(bool showReady)
    {
        if (isServer) return; // Server already handles button visibility

        DevLogger.Log($"RpcShowButtons called with showReady: {showReady}");
        _readyButton.gameObject.SetActive(showReady);
        _exitButton.gameObject.SetActive(!showReady);
        _readyText.gameObject.SetActive(showReady);
        _readyText.text = $"Players Ready: {_readyCount}/{_playerCount}";
    }

    [Server]
    void ServerShowButtons(bool showReady)
    {
        DevLogger.Log($"ServerShowButtons called with showReady: {showReady}");
        _readyButton.gameObject.SetActive(showReady);
        _exitButton.gameObject.SetActive(!showReady);
        _readyText.gameObject.SetActive(showReady);
        _readyText.text = $"Players Ready: {_readyCount}/{_playerCount}";
    }

    public void OnContinueButtonClicked()
    {
        if (_ready)
        {
            CmdNotReady();
            _ready = false;
            _readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        }
        else
        {
            CmdReady();
            _ready = true;
            _readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Cancel";
        }
    }

    public void OnExitButtonClicked()
    {
        CmdQuitGame();
    }

    [Command(requiresAuthority = false)]
    void CmdQuitGame()
    {
        NetworkManager.singleton.StopHost();
    }

    [Command(requiresAuthority = false)]
    public void CmdReady()
    {
        _readyCount++;
    }

    [Command(requiresAuthority = false)]
    public void CmdNotReady()
    {
        _readyCount--;
    }

    void OnReadyChanged(int oldCount, int newCount)
    {
        _readyText.text = $"Players Ready: {newCount}/{_playerCount}";

        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
            _countdownText.gameObject.SetActive(false);
        }

        if (newCount >= _playerCount)
        {
            _countdownCoroutine = StartCoroutine(CountdownAndLoadScene());
        }
    }

    IEnumerator CountdownAndLoadScene()
    {
        _countdownText.gameObject.SetActive(true);
        int countdown = _countdownTime;
        while (countdown > 0)
        {
            _countdownText.text = countdown.ToString();
            yield return new WaitForSeconds(1);
            countdown--;
        }

        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene(_workdaySceneName);
        }
    }
}
