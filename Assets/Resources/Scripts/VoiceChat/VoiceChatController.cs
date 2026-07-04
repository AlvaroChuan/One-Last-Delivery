using UnityEngine;
using Adrenak.UniVoice.Outputs;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class VoiceChatController : NetworkBehaviour
{
    [SerializeField] private string _activeSceneName = "GameScene";
    [SerializeField] private float _distanceProximityChat = 10f;
    [SerializeField] private BaseVoiceChat _voiceChat;
    public static VoiceChatController Instance;

    private Dictionary<int, Transform> _peerIdToTransformMap = new Dictionary<int, Transform>();

    private bool _isLocalPlayerSpectator = false;
    private HashSet<int> _spectatorPeerIds = new HashSet<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.activeSceneChanged += OnSceneChanged;
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
        List<int> peerIds = _voiceChat.PeerIds;
        foreach (var peerId in peerIds)
        {
            var output = _voiceChat.Client.PeerOutputs[peerId];
            StreamedAudioSourceOutput streamedOutput = output as StreamedAudioSourceOutput;
            if (streamedOutput != null)
            {
                AudioSource audioSource = streamedOutput.Stream.UnityAudioSource;
                audioSource.spatialBlend = 1f; // Set to 3D
                audioSource.maxDistance = _distanceProximityChat;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
    }

    void SetUpGlobalVoiceChat()
    {
        DevLogger.Log($"voiceChat != null: {_voiceChat != null}");
        DevLogger.Log($"voiceChat.PeerIds != null: {_voiceChat.PeerIds != null}");
        DevLogger.Log($"voiceChat.PeerIds.Count: {_voiceChat.PeerIds.Count}");
        List<int> peerIds = _voiceChat.PeerIds;
        foreach (var peerId in peerIds)
        {
            var output = _voiceChat.Client.PeerOutputs[peerId];
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

            if(_voiceChat.Client.PeerOutputs.TryGetValue(peerId, out var output))
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
        foreach (var peer in _voiceChat.PeerIds)
        {
            if (_voiceChat.Client.PeerOutputs.TryGetValue(peer, out var output))
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
