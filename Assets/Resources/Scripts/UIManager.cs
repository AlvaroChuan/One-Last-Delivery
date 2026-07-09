using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using System;
using Steamworks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

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
    [SerializeField] private TextMeshProUGUI _lobbyPlayerCountText;
    [SerializeField] private Transform _optionsContent;
    [SerializeField] private TextMeshProUGUI _countdownText;


    [Header("3D Elements")]
    [SerializeField] private GameObject _clipboardModel;
    [SerializeField] private PaperSheet[] _paperSheets;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TMP_InputField _lobbyPasswordInput;
    [SerializeField] private TMP_InputField _joinLobbyPasswordInput;

    [Header("Settings")]
    [SerializeField] private TMP_Dropdown _windowModeDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _inputDeviceDropdown;
    [SerializeField] private TMP_Dropdown _pushToTalkDropdown;
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Slider _inputVolumeSlider;
    [SerializeField] private AudioMixerGroup _masterMixerGroup;

    [Header("References")]
    [SerializeField] private SteamLobbyManager _steamLobbyManager;
    [SerializeField] private KeyBinder _keyBindPrefab;

    [Header("Controls")]
    [SerializeField] private InputActionReference[] _inputReferences;

    private int _currentPaperSheetIndex = 0;
    private int _targetPanelIndex = 0;
    private Coroutine _panelTransitionCoroutine;
    private Resolution[] _resolutions;
    private List<Resolution> _filteredResolutions;
    private int _currentResolutionIndex;
    private RefreshRate _currentRefreshRate;
    private int _currentWindowModeIndex;
    private int _currentQualityIndex;
    private bool _pushToTalkEnabled;
    private BaseVoiceChat _voiceChat;


    void Awake()
    {
        _steamLobbyManager = FindAnyObjectByType<SteamLobbyManager>();
        _voiceChat = FindAnyObjectByType<BaseVoiceChat>();
    }

    public void Start()
    {
        ShowMainMenu();
        AddListeners();
        SetupResolutions();
        SetupWindowModes();
        SetupQualitySettings();
        SetupInputDevices();
        SetupPushToTalkOptions();
        LoadSettings();
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
        if(_steamLobbyManager == null) _steamLobbyManager = FindAnyObjectByType<SteamLobbyManager>();
        _steamLobbyManager.HostLobby(_lobbyNameInput.text, _lobbyPasswordInput.text);
        ShowPanel(_lobbyPanel);
    }

    public void OnClickInviteFriends()
    {
        _steamLobbyManager.InviteFriends();
    }

    public void OnReadyButtonClicked()
    {
        _steamLobbyManager.ToggleReady();
    }

    public void OnLeaveLobbyButtonClicked()
    {
        _steamLobbyManager.ExitLobby();
    }

    public void ShowPanel(GameObject panelToShow)
    {
        int objectivePanelIndex = Array.FindIndex(_paperSheets, ps => ps.associatedPanel == panelToShow);
        if (objectivePanelIndex == -1 || objectivePanelIndex == _targetPanelIndex) return;

        _targetPanelIndex = objectivePanelIndex;

        if (_panelTransitionCoroutine != null)
        {
            StopCoroutine(_panelTransitionCoroutine);
            // Snap to current state to prevent overlapping visual bugs
            for (int i = 0; i < _paperSheets.Length; i++)
            {
                if (_paperSheets[i].associatedPanel != null)
                    _paperSheets[i].associatedPanel.SetActive(i == _currentPaperSheetIndex);
            }
        }

        _panelTransitionCoroutine = StartCoroutine(PassPanelsAndSheets(objectivePanelIndex));
    }

    public void ClearLobbyList()
    {
        for (int i = _lobbyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_lobbyListContent.GetChild(i).gameObject);
        }
    }

    public void AddLobbyToList(CSteamID lobbyID, string lobbyName, string password, int currentPlayers, int maxPlayers, string hostName, int ping)
    {
        if(_steamLobbyManager == null) _steamLobbyManager = FindAnyObjectByType<SteamLobbyManager>();
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

    public void AddPlayerToList(CSteamID playerID, CSteamID lobbyID) //TODO REVIEW THIS
    {
        GameObject item = Instantiate(_playersListItemPrefab, _playersListContent);
        LobbyPlayerItem itemScript = item.GetComponent<LobbyPlayerItem>();
        itemScript.SetupPlayer(playerID, lobbyID);
    }

    public void SyncLobbyData(CSteamID[] activePlayers, CSteamID lobbyID, int maxPlayers)
    {
        for (int i = _playersListContent.childCount - 1; i >= 0; i--)
        {
            Transform child = _playersListContent.GetChild(i);
            LobbyPlayerItem itemScript = child.GetComponent<LobbyPlayerItem>();
            if (itemScript != null)
            {
                if (!activePlayers.Contains(itemScript.SteamID)) Destroy(child.gameObject);
            }
            else Destroy(child.gameObject);
        }

        foreach (CSteamID playerID in activePlayers)
        {
            bool found = false;
            for (int i = 0; i < _playersListContent.childCount; i++)
            {
                LobbyPlayerItem itemScript = _playersListContent.GetChild(i).GetComponent<LobbyPlayerItem>();
                if (itemScript != null && itemScript.SteamID == playerID)
                {
                    itemScript.SetupPlayer(playerID, lobbyID);
                    found = true;
                    break;
                }
            }
            if (!found) AddPlayerToList(playerID, lobbyID);
        }
        _lobbyPlayerCountText.text = $"{activePlayers.Length}/{maxPlayers}";
    }

    public void OnJoinedLobby()
    {
        HideMainMenu();
        ShowClipboard();
        ShowPanel(_lobbyPanel);
    }

    public void OnLobbyExit()
    {
        ShowPanel(_lobbyListPanel);
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
        _title.transform.DOMoveX(75, 1f).SetEase(Ease.InOutBack);
        _playButton.transform.DOMoveX(75, 1f).SetEase(Ease.InOutBack).SetDelay(0.15f);
        _optionsButton.transform.DOMoveX(75, 1f).SetEase(Ease.InOutBack).SetDelay(0.3f);
        _quitButton.transform.DOMoveX(75, 1f).SetEase(Ease.InOutBack).SetDelay(0.45f);
    }

    private void HideMainMenu()
    {
        _title.transform.DOMoveX(-Screen.width, 0.5f).SetEase(Ease.InOutCubic);
        _playButton.transform.DOMoveX(-Screen.width, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.1f);
        _optionsButton.transform.DOMoveX(-Screen.width, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.2f);
        _quitButton.transform.DOMoveX(-Screen.width, 0.5f).SetEase(Ease.InOutCubic).SetDelay(0.3f);
    }

    private void ShowClipboard()
    {
        _clipboardModel.transform.DOMoveY(0.96f, 0.5f).SetEase(Ease.InOutCubic);
    }

    private void HideClipboard()
    {
        _clipboardModel.transform.DOMoveY(-0.96f, 0.5f).SetEase(Ease.InOutCubic);
    }

    private void AddListeners()
    {
        _windowModeDropdown.onValueChanged.AddListener(SetWindowMode);
        _resolutionDropdown.onValueChanged.AddListener(SetResolution);
        _qualityDropdown.onValueChanged.AddListener(SetQuality);
        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        _inputDeviceDropdown.onValueChanged.AddListener(SetInputDevice);
        _inputVolumeSlider.onValueChanged.AddListener(SetInputVolume);
        _pushToTalkDropdown.onValueChanged.AddListener(SetPushToTalk);
    }

    private void SetupResolutions()
    {
        _resolutions = Screen.resolutions;
        _filteredResolutions = new List<Resolution>();
        _resolutionDropdown.ClearOptions();
        _currentRefreshRate = Screen.currentResolution.refreshRateRatio;

        foreach(Resolution resolution in _resolutions)
        {
            if (resolution.refreshRateRatio.Equals(_currentRefreshRate)) _filteredResolutions.Add(resolution);
        }

        List<string> options = new List<string>();
        for (int i = 0; i < _filteredResolutions.Count; i++)
        {
            string option = _filteredResolutions[i].width + " x " + _filteredResolutions[i].height + " " + _filteredResolutions[i].refreshRateRatio + "Hz";
            options.Add(option);
            if (_filteredResolutions[i].width == Screen.currentResolution.width && _filteredResolutions[i].height == Screen.currentResolution.height)
            {
                _currentResolutionIndex = i;
            }
        }

        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.value = _currentResolutionIndex;
        _resolutionDropdown.RefreshShownValue();
    }

    private void SetupWindowModes()
    {
        _windowModeDropdown.ClearOptions();
        List<string> options = new List<string> { "Exclusive Fullscreen", "Fullscreen Window", "Windowed" };
        _windowModeDropdown.AddOptions(options);
        _currentWindowModeIndex = (int)Screen.fullScreenMode;
        _windowModeDropdown.value = _currentWindowModeIndex;
        _windowModeDropdown.RefreshShownValue();
    }

    private void SetupQualitySettings()
    {
        _qualityDropdown.ClearOptions();
        List<string> options = new List<string>(QualitySettings.names);
        _qualityDropdown.AddOptions(options);
        _currentQualityIndex = QualitySettings.GetQualityLevel();
        _qualityDropdown.value = _currentQualityIndex;
        _qualityDropdown.RefreshShownValue();
    }

    private void SetupInputDevices()
    {
        _inputDeviceDropdown.ClearOptions();
        if (Microphone.devices.Length == 0) return;
        _inputDeviceDropdown.AddOptions(Microphone.devices.ToList());
        _inputDeviceDropdown.value = 0;
        _inputDeviceDropdown.RefreshShownValue();
    }

    private void SetupPushToTalkOptions()
    {
        _pushToTalkDropdown.ClearOptions();
        List<string> options = new List<string> { "Voice action", "Push to Talk" };
        _pushToTalkDropdown.AddOptions(options);
        _pushToTalkEnabled = false; // Assuming default is disabled
        _pushToTalkDropdown.value = _pushToTalkEnabled ? 1 : 0;
        _pushToTalkDropdown.RefreshShownValue();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            _currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex");
            _resolutionDropdown.value = _currentResolutionIndex;
            _resolutionDropdown.RefreshShownValue();
        }

        if (PlayerPrefs.HasKey("WindowModeIndex"))
        {
            _currentWindowModeIndex = PlayerPrefs.GetInt("WindowModeIndex");
            _windowModeDropdown.value = _currentWindowModeIndex;
            _windowModeDropdown.RefreshShownValue();
        }

        if (PlayerPrefs.HasKey("QualityIndex"))
        {
            _currentQualityIndex = PlayerPrefs.GetInt("QualityIndex");
            _qualityDropdown.value = _currentQualityIndex;
            _qualityDropdown.RefreshShownValue();
        }

        if (PlayerPrefs.HasKey("MasterVolume") && _masterVolumeSlider != null)
        {
            _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume");
        }

        if (PlayerPrefs.HasKey("MusicVolume") && _musicVolumeSlider != null)
        {
            _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        }

        if (PlayerPrefs.HasKey("SFXVolume") && _sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");
        }

        if (PlayerPrefs.HasKey("InputDeviceIndex") && _inputDeviceDropdown != null && _inputDeviceDropdown.options.Count > PlayerPrefs.GetInt("InputDeviceIndex"))
        {
            _inputDeviceDropdown.value = PlayerPrefs.GetInt("InputDeviceIndex");
            _inputDeviceDropdown.RefreshShownValue();
        }

        if (PlayerPrefs.HasKey("InputVolume") && _inputVolumeSlider != null)
        {
            _inputVolumeSlider.value = PlayerPrefs.GetFloat("InputVolume");
        }

        if (PlayerPrefs.HasKey("PushToTalkEnabled") && _pushToTalkDropdown != null)
        {
            _pushToTalkEnabled = PlayerPrefs.GetInt("PushToTalkEnabled") == 1;
            _pushToTalkDropdown.value = _pushToTalkEnabled ? 1 : 0;
            _pushToTalkDropdown.RefreshShownValue();
        }

        foreach (InputActionReference action in _inputReferences)
        {
            KeyBinder keyBinder = Instantiate(_keyBindPrefab, _optionsContent);
            keyBinder.OnCreate(action.action);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        _currentResolutionIndex = resolutionIndex;
        Resolution resolution = _filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetWindowMode(int windowModeIndex)
    {
        _currentWindowModeIndex = windowModeIndex;
        FullScreenMode mode = windowModeIndex == 2 ? FullScreenMode.Windowed : (windowModeIndex == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen);
        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("WindowModeIndex", windowModeIndex);
    }

    public void SetQuality(int qualityIndex)
    {
        _currentQualityIndex = qualityIndex;
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityIndex", qualityIndex);
    }

    public void SetMasterVolume(float volume)
    {
        _masterMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20); // Convert linear volume to decibels
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        _masterMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20); // Convert linear volume to decibels
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        _masterMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20); // Convert linear volume to decibels
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetInputDevice(int deviceIndex)
    {
        PlayerPrefs.SetInt("InputDeviceIndex", deviceIndex);
        _voiceChat.ChangeMicrophone(deviceIndex);
    }

    public void SetInputVolume(float volume)
    {
        PlayerPrefs.SetFloat("InputVolume", volume);
        _voiceChat.SetMicrophoneVolume(volume);
    }

    public void SetPushToTalk(int pushToTalkIndex)
    {
        _pushToTalkEnabled = pushToTalkIndex == 1;
        PlayerPrefs.SetInt("PushToTalkEnabled", pushToTalkIndex);
        _voiceChat.SetPushToTalk(_pushToTalkEnabled);
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

    public void UpdateCountdown(int secondsLeft = -1)
    {
        if (secondsLeft < 0)
        {
            _countdownText.gameObject.SetActive(false);
        }
        else
        {
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = $"Starting in\n{secondsLeft}";
        }
    }
}