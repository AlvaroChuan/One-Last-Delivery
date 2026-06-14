using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Samples;
using Mirror;
using Unity.VisualScripting;

public class PlayerVoiceProxyComponent : MonoBehaviour {
    const string TAG = "[PlayerVoiceProxyComponent]";

    /// <summary>
    /// Whether UniVoice has been setup successfully. This field will return true if the setup was successful.
    /// It runs on both server and client.
    /// </summary>
    public static bool HasSetUp { get; private set; }

    /// <summary>
    /// The server object.
    /// </summary>
    public static IAudioServer<int> AudioServer { get; private set; }

    /// <summary>
    /// The client session.
    /// </summary>
    public static ClientSession<int> ClientSession { get; private set; }

#pragma warning disable CS0414
    [SerializeField] bool useRNNoise4UnityIfAvailable = true;

    [SerializeField] bool useConcentusEncodeAndDecode = true;

    [SerializeField] bool useVad = true;
    [SerializeField] private float distanceProximityChat = 10f;
#pragma warning restore

    void Start() {
        if (HasSetUp) {
            return;
        }
        HasSetUp = Setup();
    }

    bool Setup() {
        bool failed = false;

        var createdAudioServer = SetupAudioServer();
        if (!createdAudioServer) {
            failed = true;
        }

        var setupAudioClient = SetupClientSession();
        if (!setupAudioClient) {
            failed = true;
        }

        if (!failed)
            Debug.unityLogger.Log(LogType.Log, TAG, "UniVoice successfully setup!");
        else
            Debug.unityLogger.Log(LogType.Error, TAG, $"Refer to the notes on top of {typeof(UniVoiceMirrorSetupSample).Name}.cs for setup instructions.");


        return !failed;
    }

    bool SetupAudioServer() {
#if MIRROR
        // ---- CREATE AUDIO SERVER AND SUBSCRIBE TO EVENTS TO PRINT LOGS ----
        // We create a server. If this code runs in server mode, MirrorServer will take care
        // or automatically handling all incoming messages. On a device connecting as a client,
        // this code doesn't do anything.
        AudioServer = new MirrorServer();

        AudioServer.OnServerStart += () => {
            Debug.unityLogger.Log(LogType.Log, TAG, "Server started");
        };

        AudioServer.OnServerStop += () => {
            Debug.unityLogger.Log(LogType.Log, TAG, "Server stopped");
        };
        return true;
#else
        Debug.unityLogger.Log(LogType.Error, TAG, "MirrorServer implementation not found!");
        return false;
#endif
    }

    bool SetupClientSession() {
#if MIRROR
        // ---- CREATE AUDIO CLIENT AND SUBSCRIBE TO EVENTS ----
        IAudioClient<int> client = new MirrorClient();

        client.OnJoined += (id, peerIds) => {
            Debug.unityLogger.Log(LogType.Log, TAG, $"You are Peer ID {id}");
            ClientSession.Client.SubmitVoiceSettings();
        };

        client.OnLeft += () => {
            Debug.unityLogger.Log(LogType.Log, TAG, "You left the chatroom");
        };

        // When a peer joins, we instantiate a new peer view
        client.OnPeerJoined += id =>
        {
            StartCoroutine(ConfigureAudio(id));
        };

        // When a peer leaves, destroy the UI representing them
        client.OnPeerLeft += id => {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left");
        };

        Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorClient object");

        // ---- CREATE AUDIO INPUT ----
        IAudioInput input;
        // Since in this sample we use microphone input via UniMic, we first check if there
        // are any mic devices available.
        Mic.Init(); // Must do this to use the Mic class
        if (Mic.AvailableDevices.Count == 0) {
            Debug.unityLogger.Log(LogType.Log, TAG, "Device has no microphones." +
            "Will only be able to hear other clients, cannot send any audio.");
            input = new EmptyAudioInput();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created EmptyAudioInput");
        }
        else {
            // Get the first recording device that we have available and start it.
            // Then we create a UniMicInput instance that requires the mic object
            // For more info on UniMic refer to https://www.github.com/adrenak/unimic
            var mic = Mic.AvailableDevices[0];
            mic.StartRecording(60);
            Debug.unityLogger.Log(LogType.Log, TAG, "Started recording with Mic device named." +
            mic.Name + $" at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms.");
            input = new UniMicInput(mic);
            Debug.unityLogger.Log(LogType.Log, TAG, "Created UniMicInput");
        }

        // ---- CREATE AUDIO OUTPUT FACTORY ----
        IAudioOutputFactory outputFactory;
        // We want the incoming audio from peers to be played via the StreamedAudioSourceOutput
        // implementation of IAudioSource interface. So we get the factory for it.
        outputFactory = new StreamedAudioSourceOutput.Factory();
        Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

        // ---- CREATE CLIENT SESSION AND ADD FILTERS TO IT ----
        // With the client, input and output factory ready, we create create the client session
        ClientSession = new ClientSession<int>(client, input, outputFactory);
        Debug.unityLogger.Log(LogType.Log, TAG, "Created session");

#if UNIVOICE_FILTER_RNNOISE4UNITY
        if(useRNNoise4UnityIfAvailable) {
            // RNNoiseFilter to remove noise from captured audio
            session.InputFilters.Add(new RNNoiseFilter());
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered RNNoiseFilter as an input filter");
        }
#endif

        if (useVad) {
            // We add the VAD filter after RNNoise.
            // This way lot of the background noise has been removed, VAD is truly trying to detect voice
            ClientSession.InputFilters.Add(new SimpleVadFilter(new SimpleVad()));
        }

        if (useConcentusEncodeAndDecode) {
            // ConcentureEncoder filter to encode captured audio that reduces the audio frame size
            ClientSession.InputFilters.Add(new ConcentusEncodeFilter());
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusEncodeFilter as an input filter");

            // For incoming audio register the ConcentusDecodeFilter to decode the encoded audio received from other clients
            ClientSession.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter());
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusDecodeFilter as an output filter");
        }

        return true;
#else
        Debug.unityLogger.Log(LogType.Error, TAG, "MirrorClient implementation not found!");
        return false;
#endif
    }

    private IEnumerator ConfigureAudio(int id)
    {
        yield return new WaitForEndOfFrame();

        if (ClientSession == null || !ClientSession.PeerOutputs.ContainsKey(id)) yield break;

        IAudioOutput peerOutput = ClientSession.PeerOutputs[id];
        StreamedAudioSourceOutput streamedAudioSourceOutput = peerOutput as StreamedAudioSourceOutput;
        AudioSource peerAudioSource = streamedAudioSourceOutput.Stream.UnityAudioSource;
        Transform peerAvatar = GetAvatarForPeerID(id);

        if (peerAvatar != null)
        {
            peerAudioSource.transform.SetParent(peerAvatar);
            peerAudioSource.transform.localPosition = Vector3.zero;
        }

        peerAudioSource.rolloffMode = AudioRolloffMode.Linear;
        peerAudioSource.maxDistance = distanceProximityChat;

        bool peerIsAlive = true;
        if (peerAvatar != null)
        {
            var deathComp = peerAvatar.GetComponent<PlayerVoiceDeathComponent>();
            if (deathComp != null)
            {
                peerIsAlive = deathComp.isAlive;
            }
        }

        bool localIsAlive = true;
        var localDeathComp = GetLocalPlayerDeathComponent();
        if (localDeathComp != null)
        {
            localIsAlive = localDeathComp.isAlive;
        }

        if (peerIsAlive)
        {
            // Alive hears Alive spatially
            peerAudioSource.mute = false;
            peerAudioSource.spatialBlend = 1f;
        }
        else if (localIsAlive)
        {
            peerAudioSource.mute = true;
        }
        else
        {
            peerAudioSource.mute = false;
            peerAudioSource.spatialBlend = 0f;
        }
    }

    public void UpdateAudio()
    {
        if (ClientSession == null) return;
        foreach (int id in ClientSession.PeerOutputs.Keys)
        {
            StartCoroutine(ConfigureAudio(id));
        }
    }

    private PlayerVoiceDeathComponent GetLocalPlayerDeathComponent()
    {
        var players = FindObjectsOfType<PlayerVoiceDeathComponent>();
        foreach (var p in players)
        {
            if (p.isLocalPlayer) return p;
        }
        return null;
    }

    private Transform GetAvatarForPeerID(int id)
    {
        // For pure clients, use the synced voiceId
        var players = FindObjectsOfType<PlayerVoiceDeathComponent>();
        foreach (var p in players)
        {
            if (p.voiceId == id) return p.transform;
        }

        // Fallback for Host/Server
        if (NetworkServer.active && NetworkServer.connections.TryGetValue(id, out var connection))
        {
            if (connection.identity != null)
            {
                return connection.identity.transform;
            }
        }
        return null;
    }
}
