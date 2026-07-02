using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _windowModeDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _inputDeviceDropdown;
    [SerializeField] private TMP_Dropdown _pushToTalkDropdown;
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Slider _inputVolumeSlider;

    [SerializeField] private Transform _optionsContent;
    [SerializeField] private KeyBinder _keyBindPrefab;
    [SerializeField] private InputActionReference[] _inputReferences;
    [SerializeField] private BaseVoiceChat _voiceChat;
    private Resolution[] _resolutions;
    private List<Resolution> _filteredResolutions;
    private int _currentResolutionIndex;
    private RefreshRate _currentRefreshRate;
    private int _currentWindowModeIndex;
    private int _currentQualityIndex;
    private bool _pushToTalkEnabled;
    void Start()
    {
        AddListeners();
        SetupResolutions();
        SetupWindowModes();
        SetupQualitySettings();
        SetupInputDevices();
        SetupPushToTalkOptions();
        LoadSettings();
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
        // TODO: Integrate with audio manager to set the actual volume
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        // TODO: Integrate with audio manager to set the actual volume
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        // TODO: Integrate with audio manager to set the actual volume
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
}