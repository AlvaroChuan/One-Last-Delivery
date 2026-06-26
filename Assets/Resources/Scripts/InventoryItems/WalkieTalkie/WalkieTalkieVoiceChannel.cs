using UnityEngine;
using Mirror;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Outputs;
using System.Collections.Generic;

public class WalkieTalkieVoiceChannel : MonoBehaviour
{
    private static WalkieTalkieVoiceChannel _instance;
    public static WalkieTalkieVoiceChannel Instance {
        get {
            if (_instance == null) {
                var go = new GameObject("WalkieTalkieVoiceChannel");
                _instance = go.AddComponent<WalkieTalkieVoiceChannel>();
            }
            return _instance;
        }
    }

    private Dictionary<int, StreamedAudioSourceOutput> _outputs = new Dictionary<int, StreamedAudioSourceOutput>();
    private Dictionary<int, ConcentusDecodeFilter> _decoders = new Dictionary<int, ConcentusDecodeFilter>();

    public bool IsLocalPlayerHoldingWalkieTalkie()
    {
        PlayerInventoryComponent[] inventories = FindObjectsOfType<PlayerInventoryComponent>();
        foreach (var inv in inventories)
        {
            if (inv.isLocalPlayer)
            {
                InventoryItemData heldData = inv.GetHeldItemData();
                if (heldData.itemID == ItemID.WalkieTalkie)
                    return true;
            }
        }
        return false;
    }

    public void PlayAudio(int senderId, byte[] compressedSamples, int frequency, int channelCount)
    {
        if (!_outputs.ContainsKey(senderId))
        {
            var output = StreamedAudioSourceOutput.New();
            output.gameObject.transform.SetParent(transform);

            var audioSource = output.Stream.UnityAudioSource;
            audioSource.spatialBlend = 0f; // 2D Walkie Talkie sound

            var lowPass = output.gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 2000;
            var highPass = output.gameObject.AddComponent<AudioHighPassFilter>();
            highPass.cutoffFrequency = 500;
            var distortion = output.gameObject.AddComponent<AudioDistortionFilter>();
            distortion.distortionLevel = 0.4f;

            _outputs[senderId] = output;
            _decoders[senderId] = new ConcentusDecodeFilter();
        }

        var frame = new AudioFrame {
            samples = compressedSamples,
            frequency = frequency,
            channelCount = channelCount
        };

        var decoded = _decoders[senderId].Run(frame);
        if (decoded.samples != null && decoded.samples.Length > 0)
        {
            _outputs[senderId].Feed(decoded);
        }
    }
}
