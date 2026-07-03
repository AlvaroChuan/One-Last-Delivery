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
using UnityEngine.InputSystem;

public class BaseVoiceChat : MonoBehaviour
{
    #region Constants

    protected string TAG = "[LobbyVoiceChat]";

    #endregion

    #region Static Properties

    /// <summary>
    /// Whether UniVoice has been successfully initialized.
    /// </summary>
    public static bool HasSetUp { get; protected set; }

    /// <summary>
    /// UniVoice audio server implementation used by Mirror.
    /// On clients this object can exist but remain inactive.
    /// </summary>
    public static IAudioServer<int> AudioServer { get; protected set; }

    /// <summary>
    /// UniVoice client session used to send local audio and receive remote audio.
    /// </summary>
    public static ClientSession<int> ClientSession { get; protected set; }

    #endregion

    #region Inspector Fields

    [Header("Filters")]
    /*[Tooltip("Uses RNNoise4Unity if the dependency and scripting define are available.")]
    [SerializeField] private bool _useRNNoise4UnityIfAvailable = true;*/

    [Tooltip("Encodes outgoing audio and decodes incoming audio using Concentus / Opus.")]
    [SerializeField] private bool _useConcentusEncodeAndDecode = true;

    [Tooltip("Uses voice activity detection to avoid sending silence/background noise.")]
    [SerializeField] private bool _useVad = true;

    [Header("Mute")] [Tooltip("Key used to toggle microphone mute/unmute.")]
    [SerializeField] private InputActionReference _muteInput;

    [Tooltip("Whether the microphone should start muted when voice chat starts.")]
    [SerializeField] private bool _startMuted;

    [Header("Push To Talk")]
    [Tooltip("Whether push to talk starts enabled.")]
    [SerializeField] private bool _pushToTalkEnabled;

    [Tooltip("Key that must be held while push to talk is enabled.")]
    [SerializeField] private InputActionReference _pushToTalkInput;

    [Header("Microphone Volume")]
    [Tooltip("Microphone input volume. 0 = 0%, 1 = 100%, 2 = 200%.")]
    [Range(0f, 2f)]
    [SerializeField] private float _microphoneVolume = 1f;

    #endregion

    #region Public Properties

    /// <summary>
    /// Whether the local microphone is currently muted.
    /// </summary>
    public bool IsMuted { get; private set; }
    public bool PushToTalkEnabled => _pushToTalkEnabled;
    public int CurrentMicrophoneIndex => _currentMicrophoneIndex;
    public float MicrophoneVolume => _microphoneVolume;

    #endregion

    #region Private Fields

    private VoiceInputControlFilter _voiceInputControlFilter;
    private int _currentMicrophoneIndex = -1;
    private IAudioInput _currentInput;

    protected IAudioClient<int> _client;

    #endregion

    #region Unity Events

    private void OnEnable()
    {
        _muteInput.action.Enable();
        _pushToTalkInput.action.Enable();

        _muteInput.action.performed += ToggleMicrophoneMute;

        _pushToTalkInput.action.started += ActivatePushToTalk;
        _pushToTalkInput.action.canceled += DeactivatePushToTalk;
    }

    private void OnDisable()
    {
        _muteInput.action.Disable();
        _pushToTalkInput.action.Disable();

        _muteInput.action.performed -= ToggleMicrophoneMute;

        _pushToTalkInput.action.started -= ActivatePushToTalk;
        _pushToTalkInput.action.canceled -= DeactivatePushToTalk;

        StopVoiceChat();
    }

    #endregion

    #region Getters

    /*public bool IsMuted()
    {

    }*/
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
    public void ToggleMicrophoneMute(InputAction.CallbackContext obj)
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
    /// Sets whether push to talk is enabled.
    /// When enabled, local voice is only sent while the push to talk key is held.
    /// </summary>
    /// <param name="isEnabled">True to enable push to talk, false to disable it.</param>
    public void SetPushToTalk(bool isEnabled)
    {
        _pushToTalkEnabled = isEnabled;

        if (_voiceInputControlFilter != null)
        {
            _voiceInputControlFilter.PushToTalkEnabled = _pushToTalkEnabled;
        }

        Debug.unityLogger.Log(
            LogType.Log,
            TAG,
            enabled ? "Push To Talk enabled" : "Push To Talk disabled"
        );
    }

    /// <summary>
    /// If Push To Talk is enabled, unmute the user.
    /// </summary>
    public void ActivatePushToTalk(InputAction.CallbackContext obj)
    {
        if (_voiceInputControlFilter != null)
        {
            _voiceInputControlFilter.IsPushToTalkHeld = true;
        }
    }

    /// <summary>
    /// If Push To Talk is enabled, mute the user.
    /// </summary>
    public void DeactivatePushToTalk(InputAction.CallbackContext obj)
    {
        if (_voiceInputControlFilter != null)
        {
            _voiceInputControlFilter.IsPushToTalkHeld = true;
        }
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
    protected bool Setup()
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
     protected virtual bool SetupAudioServer()
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
    protected virtual bool SetupClientSession()
    {
#if MIRROR
        _client = new MirrorClient();

        _client.OnJoined += (id, peerIds) =>
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"You are Peer ID {id}");
        };

        _client.OnLeft += () =>
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "You left the voice chatroom");
        };

        /*_client.OnPeerJoined += id =>
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} joined voice chat");
        };*/

        _client.OnPeerLeft += id =>
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left voice chat");
        };

        Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorClient object");

        IAudioInput input = CreateAudioInput(0);

        IAudioOutputFactory outputFactory = new StreamedAudioSourceOutput.Factory();

        Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

        ClientSession = new ClientSession<int>(_client, input, outputFactory);

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
        return false;
#endif
    }

    #endregion

    #region Microphone Selection

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


        private AudioFrame _silentFrame;
        private AudioFrame _audioFrame;

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

            _silentFrame.timestamp = frame.timestamp;
            _silentFrame.frequency = frame.frequency;
            _silentFrame.channelCount = frame.channelCount;
            _silentFrame.samples = new byte[frame.samples.Length];

            return _silentFrame;
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

            _audioFrame.timestamp = frame.timestamp;
            _audioFrame.frequency = frame.frequency;
            _audioFrame.channelCount = frame.channelCount;
            _audioFrame.samples = newSamples;

            return _audioFrame;
        }
    }

    #endregion
}