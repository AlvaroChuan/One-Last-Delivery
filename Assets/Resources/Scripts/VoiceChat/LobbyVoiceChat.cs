using System.Linq;

using UnityEngine;
using UnityEngine.UI;

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
    /// - Captures microphone audio using UniMic.
    /// - Plays remote player audio using StreamedAudioSourceOutput.
    /// - Applies optional filters such as VAD, Concentus and RNNoise.
    /// - Allows microphone mute/unmute with a configurable key.
    /// - Allows microphone device selection through a Unity UI Dropdown.
    /// 
    /// This script is intended to be started manually from your lobby/network flow
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
        [SerializeField] private KeyCode _toggleMuteKey = KeyCode.V;

        [Tooltip("Whether the microphone should start muted when voice chat starts.")]
        [SerializeField] private bool _startMuted = false;

        [Header("Microphone Selection")]
        [Tooltip("Optional dropdown used to select the microphone device.")]
        [SerializeField] private Dropdown _microphoneDropdown;

        [Tooltip("Default microphone device index used when voice chat starts.")]
        [SerializeField] private int _defaultMicrophoneIndex = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the local microphone is currently muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        #endregion

        #region Private Fields

        private MuteInputFilter _muteInputFilter;

        private int _currentMicrophoneIndex = -1;
        private IAudioInput _currentInput;

        #endregion

        #region Unity Events

        /// <summary>
        /// Checks for the configured mute toggle key while voice chat is running.
        /// </summary>
        private void Update()
        {
            if (!HasSetUp) return;

            if (Input.GetKeyDown(_toggleMuteKey))
            {
                ToggleMicrophoneMute();
            }
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

            if (HasSetUp)
            {
                SetMicrophoneMuted(_startMuted);
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

            ClientSession = null;
            AudioServer = null;

            _currentInput = null;
            _muteInputFilter = null;

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
            if (_muteInputFilter == null)
            {
                Debug.unityLogger.Log(LogType.Warning, TAG, "Cannot mute/unmute because MuteInputFilter is null.");
                return;
            }

            IsMuted = muted;
            _muteInputFilter.IsMuted = muted;

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                muted ? "Microphone muted" : "Microphone unmuted"
            );
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

            _muteInputFilter = new MuteInputFilter();
            ClientSession.InputFilters.Add(_muteInputFilter);
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered MuteInputFilter as an input filter");

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
                .Select(device => new Dropdown.OptionData
                {
                    text = $"{device.Name} [{device.MaxFrequency}, {device.MinFrequency}]"
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

        #region Internal Filters

        /// <summary>
        /// Input filter that outputs silence while muted.
        /// 
        /// This avoids replacing ClientSession.Input at runtime for mute/unmute,
        /// which can break input subscriptions in some UniVoice versions.
        /// </summary>
        private class MuteInputFilter : IAudioFilter
        {
            /// <summary>
            /// Whether this filter should replace outgoing samples with silence.
            /// </summary>
            public bool IsMuted { get; set; }

            /// <summary>
            /// Runs the mute filter over an outgoing audio frame.
            /// </summary>
            /// <param name="frame">Original outgoing audio frame.</param>
            /// <returns>The original frame when unmuted, or a silent frame when muted.</returns>
            public AudioFrame Run(AudioFrame frame)
            {
                if (!IsMuted)
                    return frame;

                if (frame.samples == null || frame.samples.Length == 0)
                    return frame;

                byte[] mutedSamples = new byte[frame.samples.Length];

                return new AudioFrame
                {
                    timestamp = frame.timestamp,
                    frequency = frame.frequency,
                    channelCount = frame.channelCount,
                    samples = mutedSamples
                };
            }
        }

        #endregion
    }
}