using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Filters;

namespace Adrenak.UniVoice.Samples
{
    /// <summary>
    /// Manages UniVoice voice chat for a Mirror lobby.
    ///
    /// Responsibilities:
    /// - Creates the UniVoice audio server and client session.
    /// - Captures local microphone audio using UniMic.
    /// - Plays remote player audio using StreamedAudioSourceOutput.
    /// - Applies optional input/output filters such as VAD, Concentus and RNNoise.
    /// - Allows microphone mute/unmute with a configurable key.
    /// - Allows microphone device selection through a TMP dropdown.
    /// - Allows microphone volume control through a UI slider.
    /// - Supports push to talk using a configurable key.
    ///
    /// This script is intended to be started manually from your Steam/Mirror lobby flow
    /// by calling StartVoiceChat(), and stopped with StopVoiceChat().
    /// </summary>
    public class LobbyVoiceChat : MonoBehaviour
    {
        #region Constants

        private const string TAG = "[LobbyVoiceChat]";

        #endregion

        #region Static Properties

        /// <summary>
        /// Whether UniVoice has been successfully initialized.
        /// </summary>
        public static bool HasSetUp { get; private set; }

        /// <summary>
        /// UniVoice audio server implementation used by Mirror.
        /// On clients this object can exist but remain inactive.
        /// </summary>
        public static IAudioServer<int> AudioServer { get; private set; }

        /// <summary>
        /// UniVoice client session used to send local audio and receive remote audio.
        /// </summary>
        public static ClientSession<int> ClientSession { get; private set; }

        #endregion

        #region Inspector Fields

#pragma warning disable CS0414

        [Header("Filters")]
        [Tooltip("Uses RNNoise4Unity if the dependency and scripting define are available.")]
        [SerializeField] private bool _useRNNoise4UnityIfAvailable = true;

        [Tooltip("Encodes outgoing audio and decodes incoming audio using Concentus / Opus.")]
        [SerializeField] private bool _useConcentusEncodeAndDecode = true;

        [Tooltip("Uses voice activity detection to avoid sending silence/background noise.")]
        [SerializeField] private bool _useVad = true;

#pragma warning restore

        [Header("Mute")]
        [Tooltip("Key used to toggle microphone mute/unmute.")]
        [SerializeField] private KeyCode _toggleMuteKey = KeyCode.M;

        [Tooltip("Whether the microphone should start muted when voice chat starts.")]
        [SerializeField] private bool _startMuted = false;

        [Header("Push To Talk")]
        [Tooltip("Whether push to talk starts enabled.")]
        [SerializeField] private bool _pushToTalkEnabled = false;

        [Tooltip("Key that must be held while push to talk is enabled.")]
        [SerializeField] private KeyCode _pushToTalkKey = KeyCode.V;

        [Header("Microphone Selection")]
        [Tooltip("Optional TMP dropdown used to select the microphone device.")]
        [SerializeField] private TMP_Dropdown _microphoneDropdown;

        [Tooltip("Default microphone device index used when voice chat starts.")]
        [SerializeField] private int _defaultMicrophoneIndex = 0;

        [Header("Microphone Volume")]
        [Tooltip("Slider used to control microphone input volume. 1 = 100%.")]
        [SerializeField] private Slider _microphoneVolumeSlider;

        [Tooltip("Optional TMP text used to show microphone volume percentage.")]
        [SerializeField] private TMP_Text _microphoneVolumeText;

        [Tooltip("Microphone input volume. 0 = 0%, 1 = 100%, 2 = 200%.")]
        [Range(0f, 2f)]
        [SerializeField] private float _microphoneVolume = 1f;

        [Header("Push To Talk UI")]
        [SerializeField] private Button _pushToTalkOnButton;
        [SerializeField] private Button _pushToTalkOffButton;

        [SerializeField] private Color _activeButtonColor = Color.green;
        [SerializeField] private Color _inactiveButtonColor = Color.white;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the local microphone is currently muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Whether push to talk is currently enabled.
        /// </summary>
        public bool IsPushToTalkEnabled => _pushToTalkEnabled;

        /// <summary>
        /// Current microphone input volume.
        /// </summary>
        public float MicrophoneVolume => _microphoneVolume;

        #endregion

        #region Private Fields

        private VoiceInputControlFilter _voiceInputControlFilter;

        private int _currentMicrophoneIndex = -1;
        private IAudioInput _currentInput;

        #endregion

        #region Unity Events

        /// <summary>
        /// Handles mute input and updates push to talk state while voice chat is running.
        /// </summary>
        private void Update()
        {
            if (!HasSetUp) return;

            if (Input.GetKeyDown(_toggleMuteKey))
            {
                ToggleMicrophoneMute();
            }

            UpdatePushToTalkHeldState();
        }

        #endregion

        #region Public Voice Chat API

        /// <summary>
        /// Starts UniVoice voice chat.
        ///
        /// Call this after Mirror has started as host or after the client has connected.
        /// </summary>
        public void StartVoiceChat()
        {
            if (HasSetUp)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice is already set up. Ignoring...");
                return;
            }

            HasSetUp = Setup();

            SetupAudioSettingsUI();

            if (HasSetUp)
            {
                SetMicrophoneMuted(_startMuted);
                SetMicrophoneVolume(_microphoneVolume);
                SetPushToTalk(_pushToTalkEnabled);
            }
        }

        /// <summary>
        /// Stops UniVoice voice chat and clears local references.
        ///
        /// Call this when leaving the lobby or stopping Mirror.
        /// </summary>
        public void StopVoiceChat()
        {
            if (!HasSetUp) return;

            StopCurrentMicrophone();

            if (_microphoneDropdown != null)
            {
                _microphoneDropdown.onValueChanged.RemoveListener(OnMicrophoneDropdownChanged);
            }

            if (_microphoneVolumeSlider != null)
            {
                _microphoneVolumeSlider.onValueChanged.RemoveListener(SetMicrophoneVolume);
            }

            ClientSession = null;
            AudioServer = null;

            _currentInput = null;
            _voiceInputControlFilter = null;

            IsMuted = false;
            HasSetUp = false;

            Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice stopped.");
        }

        /// <summary>
        /// Toggles the microphone between muted and unmuted.
        /// </summary>
        public void ToggleMicrophoneMute()
        {
            SetMicrophoneMuted(!IsMuted);
        }

        /// <summary>
        /// Sets whether the local microphone should be muted.
        ///
        /// This does not swap the UniVoice input. Instead, it uses a filter that outputs silence
        /// while muted, which is safer than replacing ClientSession.Input at runtime.
        /// </summary>
        /// <param name="muted">True to mute the microphone, false to unmute it.</param>
        public void SetMicrophoneMuted(bool muted)
        {
            IsMuted = muted;

            if (_voiceInputControlFilter != null)
            {
                _voiceInputControlFilter.IsMuted = muted;
            }

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                muted ? "Microphone muted" : "Microphone unmuted"
            );
        }

        /// <summary>
        /// Enables push to talk.
        /// Can be assigned directly to the ON button in the Unity Inspector.
        /// </summary>
        public void EnablePushToTalk()
        {
            SetPushToTalk(true);
        }

        /// <summary>
        /// Disables push to talk.
        /// Can be assigned directly to the OFF button in the Unity Inspector.
        /// </summary>
        public void DisablePushToTalk()
        {
            SetPushToTalk(false);
        }

        /// <summary>
        /// Sets whether push to talk is enabled.
        /// When enabled, local voice is only sent while the push to talk key is held.
        /// </summary>
        /// <param name="enabled">True to enable push to talk, false to disable it.</param>
        public void SetPushToTalk(bool enabled)
        {
            _pushToTalkEnabled = enabled;

            if (_voiceInputControlFilter != null)
            {
                _voiceInputControlFilter.PushToTalkEnabled = _pushToTalkEnabled;
                _voiceInputControlFilter.IsPushToTalkHeld = Input.GetKey(_pushToTalkKey);
            }
            UpdatePushToTalkButtons();

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                enabled ? "Push To Talk enabled" : "Push To Talk disabled"
            );
        }

        /// <summary>
        /// Updates the Push To Talk ON/OFF button colors based on the current state.
        /// </summary>
        private void UpdatePushToTalkButtons()
        {
            SetButtonColor(_pushToTalkOnButton, _pushToTalkEnabled ? _activeButtonColor : _inactiveButtonColor);
            SetButtonColor(_pushToTalkOffButton, !_pushToTalkEnabled ? _activeButtonColor : _inactiveButtonColor);
        }

        /// <summary>
        /// Sets the normal color of a Unity UI button.
        /// </summary>
        /// <param name="button">Button to update.</param>
        /// <param name="color">Color to apply.</param>
        private void SetButtonColor(Button button, Color color)
        {
            if (button == null)
                return;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = color;
            button.colors = colors;
        }

        /// <summary>
        /// Sets the local microphone input volume.
        /// Expected range is 0 to 2, where 1 means 100%.
        /// </summary>
        /// <param name="volume">Microphone volume multiplier.</param>
        public void SetMicrophoneVolume(float volume)
        {
            _microphoneVolume = Mathf.Clamp(volume, 0f, 2f);

            if (_voiceInputControlFilter != null)
            {
                _voiceInputControlFilter.MicrophoneVolume = _microphoneVolume;
            }

            UpdateMicrophoneVolumeText();
        }

        /// <summary>
        /// Changes the active microphone device.
        ///
        /// This stops the currently recording microphone, starts the selected one,
        /// creates a new UniMicInput and assigns it to the current ClientSession.
        /// </summary>
        /// <param name="deviceIndex">Index of the microphone in Mic.AvailableDevices.</param>
        public void ChangeMicrophone(int deviceIndex)
        {
            if (Mic.AvailableDevices.Count == 0)
                return;

            deviceIndex = Mathf.Clamp(deviceIndex, 0, Mic.AvailableDevices.Count - 1);

            if (_currentMicrophoneIndex == deviceIndex)
                return;

            StopCurrentMicrophone();

            IAudioInput newInput = CreateAudioInput(deviceIndex);

            if (ClientSession != null)
            {
                ClientSession.Input = newInput;
            }

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                $"Changed microphone to {Mic.AvailableDevices[deviceIndex].Name}"
            );
        }

        #endregion

        #region Setup

        /// <summary>
        /// Sets up the UniVoice audio server and client session.
        /// </summary>
        /// <returns>True if setup succeeded, false otherwise.</returns>
        private bool Setup()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Trying to setup UniVoice");

            bool failed = false;

            bool createdAudioServer = SetupAudioServer();

            if (!createdAudioServer)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Could not setup UniVoice server.");
                failed = true;
            }

            bool setupAudioClient = SetupClientSession();

            if (!setupAudioClient)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Could not setup UniVoice client.");
                failed = true;
            }

            if (!failed)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice successfully setup!");
            }
            else
            {
                Debug.unityLogger.Log(
                    LogType.Error,
                    TAG,
                    "Could not setup UniVoice. Check that Mirror is imported and UNIVOICE_NETWORK_MIRROR is added as a scripting define symbol."
                );
            }

            return !failed;
        }

        /// <summary>
        /// Creates the UniVoice audio server implementation for Mirror.
        /// </summary>
        /// <returns>True if the Mirror server implementation is available.</returns>
        private bool SetupAudioServer()
        {
#if MIRROR
            AudioServer = new MirrorServer();

            Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorServer object");

            AudioServer.OnServerStart += () =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Voice server started");
            };

            AudioServer.OnServerStop += () =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Voice server stopped");
            };

            return true;
#else
            Debug.unityLogger.Log(LogType.Error, TAG, "MirrorServer implementation not found!");
            return false;
#endif
        }

        /// <summary>
        /// Creates the UniVoice audio client, microphone input, audio output factory and input/output filters.
        /// </summary>
        /// <returns>True if the Mirror client implementation is available.</returns>
        private bool SetupClientSession()
        {
#if MIRROR
            IAudioClient<int> client = new MirrorClient();

            client.OnJoined += (id, peerIds) =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"You are Peer ID {id}");
            };

            client.OnLeft += () =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "You left the voice chatroom");
            };

            client.OnPeerJoined += id =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} joined voice chat");
            };

            client.OnPeerLeft += id =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left voice chat");
            };

            Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorClient object");

            SetupMicrophoneDropdown();

            IAudioInput input = CreateAudioInput(_defaultMicrophoneIndex);

            IAudioOutputFactory outputFactory = new StreamedAudioSourceOutput.Factory();

            Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

            ClientSession = new ClientSession<int>(client, input, outputFactory);

            Debug.unityLogger.Log(LogType.Log, TAG, "Created voice session");

            _voiceInputControlFilter = new VoiceInputControlFilter
            {
                IsMuted = _startMuted,
                PushToTalkEnabled = _pushToTalkEnabled,
                IsPushToTalkHeld = false,
                MicrophoneVolume = _microphoneVolume
            };

            ClientSession.InputFilters.Add(_voiceInputControlFilter);
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered VoiceInputControlFilter as an input filter");

#if UNIVOICE_FILTER_RNNOISE4UNITY
            if (_useRNNoise4UnityIfAvailable)
            {
                ClientSession.InputFilters.Add(new RNNoiseFilter());
                Debug.unityLogger.Log(LogType.Log, TAG, "Registered RNNoiseFilter as an input filter");
            }
#endif

            if (_useVad)
            {
                ClientSession.InputFilters.Add(new SimpleVadFilter(new SimpleVad()));
                Debug.unityLogger.Log(LogType.Log, TAG, "Registered SimpleVadFilter as an input filter");
            }

            if (_useConcentusEncodeAndDecode)
            {
                ClientSession.InputFilters.Add(new ConcentusEncodeFilter());
                Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusEncodeFilter as an input filter");

                ClientSession.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter());
                Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusDecodeFilter as an output filter");
            }

            return true;
#else
            Debug.unityLogger.Log(LogType.Error, TAG, "MirrorClient implementation not found!");
            return false;
#endif
        }

        #endregion

        #region Audio Settings UI

        /// <summary>
        /// Initializes audio settings UI callbacks such as microphone volume slider.
        /// </summary>
        private void SetupAudioSettingsUI()
        {
            if (_microphoneVolumeSlider != null)
            {
                _microphoneVolumeSlider.minValue = 0f;
                _microphoneVolumeSlider.maxValue = 2f;
                _microphoneVolumeSlider.value = _microphoneVolume;

                _microphoneVolumeSlider.onValueChanged.RemoveListener(SetMicrophoneVolume);
                _microphoneVolumeSlider.onValueChanged.AddListener(SetMicrophoneVolume);
            }

            UpdateMicrophoneVolumeText();
        }

        /// <summary>
        /// Updates the microphone volume percentage text if one is assigned.
        /// </summary>
        private void UpdateMicrophoneVolumeText()
        {
            if (_microphoneVolumeText == null)
                return;

            int percentage = Mathf.RoundToInt(_microphoneVolume * 100f);
            _microphoneVolumeText.text = percentage + "%";
        }

        #endregion

        #region Microphone Selection

        /// <summary>
        /// Initializes the microphone dropdown with all available UniMic devices.
        /// </summary>
        private void SetupMicrophoneDropdown()
        {
            Mic.Init();

            if (_microphoneDropdown == null)
                return;

            _microphoneDropdown.ClearOptions();

            if (Mic.AvailableDevices.Count == 0)
            {
                _microphoneDropdown.interactable = false;
                return;
            }

            _microphoneDropdown.options = Mic.AvailableDevices
                .Select(device => new TMP_Dropdown.OptionData
                {
                    text = $"{device.Name}"
                })
                .ToList();

            _defaultMicrophoneIndex = Mathf.Clamp(
                _defaultMicrophoneIndex,
                0,
                Mic.AvailableDevices.Count - 1
            );

            _microphoneDropdown.value = _defaultMicrophoneIndex;
            _microphoneDropdown.RefreshShownValue();
            _microphoneDropdown.interactable = true;

            _microphoneDropdown.onValueChanged.RemoveListener(OnMicrophoneDropdownChanged);
            _microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDropdownChanged);
        }

        /// <summary>
        /// Creates an audio input for the selected microphone device.
        /// If no microphone is available, returns an EmptyAudioInput.
        /// </summary>
        /// <param name="deviceIndex">Index of the microphone in Mic.AvailableDevices.</param>
        /// <returns>An IAudioInput for UniVoice.</returns>
        private IAudioInput CreateAudioInput(int deviceIndex)
        {
            Mic.Init();

            if (Mic.AvailableDevices.Count == 0)
            {
                Debug.unityLogger.Log(
                    LogType.Log,
                    TAG,
                    "Device has no microphones. Will only be able to hear other clients, cannot send any audio."
                );

                Debug.unityLogger.Log(LogType.Log, TAG, "Created EmptyAudioInput");

                _currentInput = new EmptyAudioInput();
                _currentMicrophoneIndex = -1;

                return _currentInput;
            }

            deviceIndex = Mathf.Clamp(deviceIndex, 0, Mic.AvailableDevices.Count - 1);

            var mic = Mic.AvailableDevices[deviceIndex];

            mic.StartRecording(60);

            _currentMicrophoneIndex = deviceIndex;

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                "Started recording with Mic device named " +
                mic.Name +
                $" at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms."
            );

            _currentInput = new UniMicInput(mic);

            Debug.unityLogger.Log(LogType.Log, TAG, "Created UniMicInput");

            return _currentInput;
        }

        /// <summary>
        /// Stops the currently selected microphone device if one is recording.
        /// </summary>
        private void StopCurrentMicrophone()
        {
            if (_currentMicrophoneIndex < 0)
                return;

            if (Mic.AvailableDevices.Count == 0)
                return;

            if (_currentMicrophoneIndex >= Mic.AvailableDevices.Count)
                return;

            Mic.AvailableDevices[_currentMicrophoneIndex].StopRecording();
            _currentMicrophoneIndex = -1;
        }

        /// <summary>
        /// Dropdown callback used when the player selects a different microphone.
        /// </summary>
        /// <param name="deviceIndex">Selected dropdown option index.</param>
        private void OnMicrophoneDropdownChanged(int deviceIndex)
        {
            ChangeMicrophone(deviceIndex);
        }

        #endregion

        #region Push To Talk

        /// <summary>
        /// Updates the internal push to talk held state using Unity input.
        /// The filter reads this cached state instead of reading Input directly.
        /// </summary>
        private void UpdatePushToTalkHeldState()
        {
            if (_voiceInputControlFilter == null)
                return;

            _voiceInputControlFilter.IsPushToTalkHeld = Input.GetKey(_pushToTalkKey);
        }

        #endregion

        #region Internal Filters

        /// <summary>
        /// Input filter that controls outgoing microphone audio.
        ///
        /// It can:
        /// - Output silence while muted.
        /// - Output silence when push to talk is enabled but the key is not held.
        /// - Apply microphone volume gain to outgoing samples.
        ///
        /// This avoids replacing ClientSession.Input at runtime for mute/unmute,
        /// which can break input subscriptions in some UniVoice versions.
        /// </summary>
        private class VoiceInputControlFilter : IAudioFilter
        {
            /// <summary>
            /// Whether outgoing audio should be silenced.
            /// </summary>
            public bool IsMuted { get; set; }

            /// <summary>
            /// Whether push to talk is enabled.
            /// </summary>
            public bool PushToTalkEnabled { get; set; }

            /// <summary>
            /// Whether the push to talk key is currently being held.
            /// </summary>
            public bool IsPushToTalkHeld { get; set; }

            /// <summary>
            /// Microphone volume multiplier.
            /// 0 = silence, 1 = original volume, 2 = double volume.
            /// </summary>
            public float MicrophoneVolume { get; set; } = 1f;

            /// <summary>
            /// Runs the input control filter over an outgoing audio frame.
            /// </summary>
            /// <param name="frame">Original outgoing audio frame.</param>
            /// <returns>Modified outgoing audio frame.</returns>
            public AudioFrame Run(AudioFrame frame)
            {
                if (ShouldSilence())
                    return CreateSilentFrame(frame);

                if (Mathf.Approximately(MicrophoneVolume, 1f))
                    return frame;

                return ApplyVolume(frame);
            }

            /// <summary>
            /// Checks whether the current frame should be replaced with silence.
            /// </summary>
            /// <returns>True if audio should be silenced.</returns>
            private bool ShouldSilence()
            {
                if (IsMuted)
                    return true;

                if (PushToTalkEnabled && !IsPushToTalkHeld)
                    return true;

                return false;
            }

            /// <summary>
            /// Creates a silent audio frame while preserving original metadata.
            /// </summary>
            /// <param name="frame">Original audio frame.</param>
            /// <returns>Silent audio frame.</returns>
            private AudioFrame CreateSilentFrame(AudioFrame frame)
            {
                if (frame.samples == null || frame.samples.Length == 0)
                    return frame;

                return new AudioFrame
                {
                    timestamp = frame.timestamp,
                    frequency = frame.frequency,
                    channelCount = frame.channelCount,
                    samples = new byte[frame.samples.Length]
                };
            }

            /// <summary>
            /// Applies volume gain to the audio frame samples.
            ///
            /// UniMic provides samples as bytes that represent float samples.
            /// If the byte array cannot be interpreted as float samples, the original frame is returned.
            /// </summary>
            /// <param name="frame">Original audio frame.</param>
            /// <returns>Audio frame with adjusted sample volume.</returns>
            private AudioFrame ApplyVolume(AudioFrame frame)
            {
                if (frame.samples == null || frame.samples.Length == 0)
                    return frame;

                if (frame.samples.Length % sizeof(float) != 0)
                    return frame;

                byte[] newSamples = new byte[frame.samples.Length];
                float[] floatSamples = new float[frame.samples.Length / sizeof(float)];

                Buffer.BlockCopy(frame.samples, 0, floatSamples, 0, frame.samples.Length);

                float volume = Mathf.Clamp(MicrophoneVolume, 0f, 2f);

                for (int i = 0; i < floatSamples.Length; i++)
                {
                    floatSamples[i] = Mathf.Clamp(floatSamples[i] * volume, -1f, 1f);
                }

                Buffer.BlockCopy(floatSamples, 0, newSamples, 0, newSamples.Length);

                return new AudioFrame
                {
                    timestamp = frame.timestamp,
                    frequency = frame.frequency,
                    channelCount = frame.channelCount,
                    samples = newSamples
                };
            }
        }

        #endregion
    }
}