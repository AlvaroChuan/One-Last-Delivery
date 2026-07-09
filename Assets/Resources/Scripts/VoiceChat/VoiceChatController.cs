using UnityEngine;
using Adrenak.UniVoice.Outputs;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class VoiceChatController : NetworkBehaviour
{
    [SerializeField] private string _activeSceneName = "GameScene";
    [SerializeField] private float _proximityChatRange = 20f;
    private static BaseVoiceChat VoiceChat;
    public static VoiceChatController Instance;

    private Dictionary<int, Transform> _peerIdToTransformMap = new Dictionary<int, Transform>();

    private bool _isLocalPlayerSpectator = false;
    private HashSet<int> _spectatorPeerIds = new HashSet<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            VoiceChat = FindAnyObjectByType<BaseVoiceChat>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.activeSceneChanged -= OnSceneChanged;
        SceneManager.activeSceneChanged += OnSceneChanged;

        VoiceChat.onStopVoiceChat -= OnStopVoiceChat;
        VoiceChat.onStopVoiceChat += OnStopVoiceChat;
    }

    void OnStopVoiceChat()
    {
        _peerIdToTransformMap.Clear();
        ResetSpectatorState();
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        ResetSpectatorState();
        if (newScene.name == _activeSceneName)
        {
            SetUpProximityVoiceChat();
        }
        else
        {
            SetUpGlobalVoiceChat();
        }
    }

    void ResetSpectatorState()
    {
        _isLocalPlayerSpectator = false;
        _spectatorPeerIds.Clear();
    }

    void SetUpProximityVoiceChat()
    {
        List<int> peerIds = VoiceChat.PeerIds;
        foreach (var peerId in peerIds)
        {
            var output = VoiceChat.Client.PeerOutputs[peerId];
            StreamedAudioSourceOutput streamedOutput = output as StreamedAudioSourceOutput;
            if (streamedOutput != null)
            {
                AudioSource audioSource = streamedOutput.Stream.UnityAudioSource;
                audioSource.spatialBlend = 1f; // Set to 3D
                audioSource.maxDistance = _proximityChatRange;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }
    }

    void SetUpGlobalVoiceChat()
    {
        DevLogger.Log($"voiceChat != null: {VoiceChat != null}");
        DevLogger.Log($"voiceChat.PeerIds != null: {VoiceChat.PeerIds != null}");
        DevLogger.Log($"voiceChat.PeerIds.Count: {VoiceChat.PeerIds.Count}");
        List<int> peerIds = VoiceChat.PeerIds;
        foreach (var peerId in peerIds)
        {
            var output = VoiceChat.Client.PeerOutputs[peerId];
            StreamedAudioSourceOutput streamedOutput = output as StreamedAudioSourceOutput;
            if (streamedOutput != null)
            {
                streamedOutput.Stream.UnityAudioSource.spatialBlend = 0f; // Set to 2D
            }
        }

        _peerIdToTransformMap.Clear();
    }

    public void RegisterPlayerVoice(int peerId, Transform playerTransform)
    {
        _peerIdToTransformMap[peerId] = playerTransform;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != _activeSceneName) return;

        foreach (var kvp in _peerIdToTransformMap)
        {
            int peerId = kvp.Key;
            Transform playerTransform = kvp.Value;

            if (playerTransform == null) continue;

            if(VoiceChat.Client.PeerOutputs.TryGetValue(peerId, out var output))
            {
                StreamedAudioSourceOutput streamedOutput = output as StreamedAudioSourceOutput;
                if (streamedOutput != null)
                {
                    AudioSource audioSource = streamedOutput.Stream.UnityAudioSource;
                    audioSource.transform.position = playerTransform.position;
                }
            }
        }
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (VoiceChat != null)
        {
            VoiceChat.onStopVoiceChat -= OnStopVoiceChat;
        }
    }

    public void SetSpectatorState(int peerId, bool isSpectator)
    {
        if (peerId == BaseVoiceChat.LocalPeerId)
        {
            _isLocalPlayerSpectator = isSpectator;
        }
        else
        {
            if (isSpectator)
            {
                _spectatorPeerIds.Add(peerId);
            }
            else
            {
                _spectatorPeerIds.Remove(peerId);
            }
        }
        ApplySpectatorRules();
    }

    void ApplySpectatorRules()
    {
        foreach (var peer in VoiceChat.PeerIds)
        {
            if (VoiceChat.Client.PeerOutputs.TryGetValue(peer, out var output))
            {
                StreamedAudioSourceOutput streamedOutput = output as StreamedAudioSourceOutput;
                if (streamedOutput != null)
                {
                    AudioSource audioSource = streamedOutput.Stream.UnityAudioSource;

                    bool isPeerSpectator = _spectatorPeerIds.Contains(peer);

                    if(isPeerSpectator)
                    {
                        audioSource.spatialBlend = 0f; // Set to 2D for spectators
                        if (_isLocalPlayerSpectator)
                        {
                            audioSource.mute = false; // Spectators can hear each other
                        }
                        else
                        {
                            audioSource.mute = true; // Non-spectators cannot hear spectators
                        }
                    }
                    else
                    {
                        if (SceneManager.GetActiveScene().name == _activeSceneName)
                        {
                            audioSource.spatialBlend = 1f; // Set to 3D for non-spectators in the game scene
                        }
                        else
                        {
                            audioSource.spatialBlend = 0f; // Set to 2D for non-spectators in other scenes
                        }
                        audioSource.mute = false; // Non-spectators can hear each other
                    }
                }
            }
        }
    }
}
