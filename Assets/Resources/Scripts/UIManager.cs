using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using System;
using Steamworks;
using DG.Tweening;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Serializable]
    private struct PaperSheet
    {
        public GameObject sheetObject;
        public GameObject associatedPanel;
    }

    [Header("Main Menu")]
    [SerializeField] private GameObject _title;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    [Header("Clipboard Panels")]
    [SerializeField] private GameObject _lobbyListPanel;
    [SerializeField] private GameObject _createLobbyPanel;
    [SerializeField] private GameObject _enterPasswordPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _optionsPanel;

    [Header("Panel Elements")]
    [SerializeField] private GameObject _lobbyListItemPrefab;
    [SerializeField] private Transform _lobbyListContent;
    [SerializeField] private GameObject _playersListItemPrefab;
    [SerializeField] private Transform _playersListContent;
    [SerializeField] private Button _confirmPasswordButton;

    [Header("3D Elements")]
    [Tooltip("Paper sheets must be in the same order as the panels in the inspector")]
    [SerializeField] private GameObject _clipboardModel;
    [SerializeField] private PaperSheet[] _paperSheets;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TMP_InputField _lobbyPasswordInput;
    [SerializeField] private TMP_InputField _joinLobbyPasswordInput;

    [Header("References")]
    [SerializeField] private SteamLobbyManager _steamLobbyManager;

    private int _currentPaperSheetIndex = 0;

    public void Start()
    {
        ShowMainMenu();
    }

    public void OnPlayButtonClicked()
    {
        HideMainMenu();
        ShowClipboard();
        OnRefreshLobbiesButtonClicked();
    }

    public void OnOptionsButtonClicked()
    {
        HideMainMenu();
        ShowClipboard();
        ShowPanel(_optionsPanel);
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    public void OnRefreshLobbiesButtonClicked()
    {
        _steamLobbyManager.FetchLobbies();
        ShowPanel(_lobbyListPanel);
    }

    public void OnReturnToMainMenuButtonClicked()
    {
        HideClipboard();
        ShowMainMenu();
    }

    public void OnCreateLobbyButtonClicked()
    {
        _steamLobbyManager.HostLobby(_lobbyNameInput.text, _lobbyPasswordInput.text);
        ShowPanel(_lobbyPanel);
    }

    public void OnClickInviteFriends()
    {
        _steamLobbyManager.InviteFriends();
    }

    public void ShowPanel(GameObject panelToShow)
    {
        int objectivePanelIndex = Array.FindIndex(_paperSheets, ps => ps.associatedPanel == panelToShow);
        StartCoroutine(PassPanelsAndSheets(objectivePanelIndex));
    }

    public void ClearLobbyList()
    {
        for (int i = _lobbyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_lobbyListContent.GetChild(i).gameObject);
        }
    }

    public void AddLobbyToList(CSteamID lobbyID, string lobbyName, string password, int currentPlayers, int maxPlayers, string hostName, string ping)
    {
        GameObject item = Instantiate(_lobbyListItemPrefab, _lobbyListContent);
        LobbyListItem itemScript = item.GetComponent<LobbyListItem>();
        itemScript.Initialize(lobbyID, _steamLobbyManager, this, lobbyName, password, currentPlayers, maxPlayers, hostName, ping);
    }

    public void ClearPlayerList()
    {
        for (int i = _playersListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_playersListContent.GetChild(i).gameObject);
        }
    }

    public void AddPlayerToList(CSteamID playerID) //TODO REVIEW THIS
    {
        GameObject item = Instantiate(_playersListItemPrefab, _playersListContent);
        LobbyPlayerItem itemScript = item.GetComponent<LobbyPlayerItem>();
        itemScript.SetupPlayer(playerID);
    }

    public void OnJoinedLobby()
    {
        ShowPanel(_lobbyPanel);
    }

    public void OnLobbyExit()
    {
        ShowPanel(_lobbyListPanel);
    }

    public void OnClickAudio()
    {
        //ShowPanel(GetPanelByName("AudioSettings"));
    }
    public void OnAudioExit()
    {
        ShowPanel(_lobbyPanel);
    }

    public void OnPassWordRequired(LobbyListItem lobbyListItem)
    {
        ShowPanel(_enterPasswordPanel);
        _confirmPasswordButton.onClick.RemoveAllListeners();
        _confirmPasswordButton.onClick.AddListener(() => lobbyListItem.OnJoinWithPassword(_enterPasswordPanel.GetComponentInChildren<TMP_InputField>().text));
    }

    private void ShowMainMenu()
    {
        _title.transform.DOMoveX(75, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.1f);
        _playButton.transform.DOMoveX(75, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.1f);
        _optionsButton.transform.DOMoveX(75, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.2f);
        _quitButton.transform.DOMoveX(75, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.3f);
    }

    private void HideMainMenu()
    {
        _quitButton.transform.DOMoveX(-500, 0.5f).SetEase(Ease.InOutCubic);
        _optionsButton.transform.DOMoveX(-500, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.1f);
        _playButton.transform.DOMoveX(-500, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.2f);
        _title.transform.DOMoveX(-500, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.3f);
    }

    private void ShowClipboard()
    {
        _clipboardModel.transform.DOMoveY(0, 0.5f).SetEase(Ease.InOutCubic);
    }

    private void HideClipboard()
    {
        _clipboardModel.transform.DOMoveY(-30, 0.5f).SetEase(Ease.InOutCubic);
    }

    private IEnumerator PassPanelsAndSheets(int objectivePanelIndex)
    {
        while (_currentPaperSheetIndex != objectivePanelIndex)
        {
            if(_currentPaperSheetIndex < objectivePanelIndex)
            {
                _paperSheets[_currentPaperSheetIndex].sheetObject.GetComponent<Animator>().SetTrigger("Pass");
                yield return new WaitForSeconds(0.3f);
                _paperSheets[_currentPaperSheetIndex].associatedPanel.SetActive(false);
                _currentPaperSheetIndex = (_currentPaperSheetIndex + 1) % _paperSheets.Length;
                _paperSheets[_currentPaperSheetIndex].associatedPanel.SetActive(true);
                yield return new WaitForSeconds(0.1f);
                
            }
            else
            {
                int tempIndex = _currentPaperSheetIndex;
                _currentPaperSheetIndex = (_currentPaperSheetIndex - 1) % _paperSheets.Length;
                _paperSheets[_currentPaperSheetIndex].sheetObject.GetComponent<Animator>().SetTrigger("Unpass");
                yield return new WaitForSeconds(0.5f);
                _paperSheets[tempIndex].associatedPanel.SetActive(false);
                _paperSheets[_currentPaperSheetIndex].associatedPanel.SetActive(true);
                yield return new WaitForSeconds(0.05f);
            }
            
        }
    }
}