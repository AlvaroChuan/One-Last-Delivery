using UnityEngine;

using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Filters;

namespace Adrenak.UniVoice.Samples
{
    public class LobbyVoiceChat : MonoBehaviour
    {
        private const string TAG = "[LobbyVoiceChat]";

        public static bool HasSetUp { get; private set; }

        public static IAudioServer<int> AudioServer { get; private set; }

        public static ClientSession<int> ClientSession { get; private set; }

#pragma warning disable CS0414
        [Header("Filters")]
        [SerializeField] private bool _useRNNoise4UnityIfAvailable = true;
        [SerializeField] private bool _useConcentusEncodeAndDecode = true;
        [SerializeField] private bool _useVad = true;
#pragma warning restore

        [Header("Mute")]
        [SerializeField] private KeyCode _toggleMuteKey = KeyCode.V;
        [SerializeField] private bool _startMuted = false;

        public bool IsMuted { get; private set; }

        private IAudioInput _realInput;
        private IAudioInput _emptyInput;

        private void Update()
        {
            if (!HasSetUp) return;

            if (Input.GetKeyDown(_toggleMuteKey))
            {
                ToggleMicrophoneMute();
            }
        }

        public void StartVoiceChat()
        {
            if (HasSetUp)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice is already set up. Ignoring...");
                return;
            }

            HasSetUp = Setup();

            if (HasSetUp && _startMuted)
            {
                SetMicrophoneMuted(true);
            }
        }

        public void StopVoiceChat()
        {
            if (!HasSetUp) return;

            ClientSession = null;
            AudioServer = null;

            _realInput = null;
            _emptyInput = null;

            IsMuted = false;
            HasSetUp = false;

            Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice stopped.");
        }

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

            IAudioInput input = CreateAudioInput();

            IAudioOutputFactory outputFactory = new StreamedAudioSourceOutput.Factory();

            Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

            ClientSession = new ClientSession<int>(client, input, outputFactory);

            Debug.unityLogger.Log(LogType.Log, TAG, "Created voice session");

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

        private IAudioInput CreateAudioInput()
        {
            _emptyInput = new EmptyAudioInput();

            Mic.Init();

            if (Mic.AvailableDevices.Count == 0)
            {
                Debug.unityLogger.Log(
                    LogType.Log,
                    TAG,
                    "Device has no microphones. Will only be able to hear other clients, cannot send any audio."
                );

                Debug.unityLogger.Log(LogType.Log, TAG, "Created EmptyAudioInput");

                _realInput = _emptyInput;

                return _emptyInput;
            }

            var mic = Mic.AvailableDevices[0];

            mic.StartRecording(60);

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                "Started recording with Mic device named " +
                mic.Name +
                $" at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms."
            );

            _realInput = new UniMicInput(mic);

            Debug.unityLogger.Log(LogType.Log, TAG, "Created UniMicInput");

            return _realInput;
        }

        public void ToggleMicrophoneMute()
        {
            SetMicrophoneMuted(!IsMuted);
        }

        public void SetMicrophoneMuted(bool muted)
        {
            if (ClientSession == null)
            {
                Debug.unityLogger.Log(LogType.Warning, TAG, "Cannot mute/unmute because ClientSession is null.");
                return;
            }

            if (_realInput == null || _emptyInput == null)
            {
                Debug.unityLogger.Log(LogType.Warning, TAG, "Cannot mute/unmute because audio inputs are not ready.");
                return;
            }

            IsMuted = muted;

            ClientSession.Input = muted ? _emptyInput : _realInput;

            Debug.unityLogger.Log(
                LogType.Log,
                TAG,
                muted ? "Microphone muted" : "Microphone unmuted"
            );
        }
    }
}