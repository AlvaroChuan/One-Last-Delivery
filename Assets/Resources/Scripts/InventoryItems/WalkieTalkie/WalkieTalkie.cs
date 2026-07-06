using UnityEngine;
using Mirror;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using System;
using Adrenak.UniVoice.Outputs;
using System.Collections.Generic;

public class WalkieTalkie : InventoryItem
{
    private ConcentusEncodeFilter _encoder;
    private Action<int, int, float[]> _micListener;
    bool _isRecording = false;

    public override void StartUse(GameObject user)
    {
        if (isLocalPlayer)
        {
            StartRecording();
        }
    }

    public override void EndUse(GameObject user)
    {
        if (isLocalPlayer)
        {
            StopRecording();
        }
    }

    void StartRecording() {
        _isRecording = true;
        int micIndex = GetMicrophoneIndex();
        if (Mic.AvailableDevices.Count > micIndex) {
            var device = Mic.AvailableDevices[micIndex];
            if (_encoder == null) _encoder = new ConcentusEncodeFilter();
            if (_micListener == null)
            {
                _micListener = OnFrameCollected;
                device.OnFrameCollected += _micListener;
            }
            if (!device.IsRecording) device.StartRecording(60);
        }
    }

    void StopRecording() {
        _isRecording = false;
        int micIndex = GetMicrophoneIndex();
        if (Mic.AvailableDevices.Count > micIndex && _micListener != null) {
            Mic.AvailableDevices[micIndex].OnFrameCollected -= _micListener;
            _micListener = null;
        }
    }

    void OnFrameCollected(int frequency, int channels, float[] samples) {
        if (!ShouldTransmitAudio()) return;

        float volume = 1f;
        BaseVoiceChat voiceChat = FindAnyObjectByType<BaseVoiceChat>();
        if (voiceChat != null) {
            volume = voiceChat.MicrophoneVolume;
        }

        byte[] bytes = new byte[samples.Length * 4];
        if (Mathf.Approximately(volume, 1f)) {
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        } else {
            float[] modifiedSamples = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++) {
                modifiedSamples[i] = Mathf.Clamp(samples[i] * volume, -1f, 1f);
            }
            Buffer.BlockCopy(modifiedSamples, 0, bytes, 0, bytes.Length);
        }

        var frame = new AudioFrame {
            samples = bytes,
            frequency = frequency,
            channelCount = channels
        };

        var encoded = _encoder.Run(frame);
        if (encoded.samples != null && encoded.samples.Length > 0) {
            CmdSendAudio(encoded.samples, encoded.frequency, encoded.channelCount);
        }
    }

    private int GetMicrophoneIndex()
    {
        BaseVoiceChat voiceChat = FindAnyObjectByType<BaseVoiceChat>();
        if (voiceChat != null)
        {
            int index = voiceChat.CurrentMicrophoneIndex;
            if (index >= 0) return index;
        }
        return 0;
    }

    private bool ShouldTransmitAudio()
    {
        BaseVoiceChat voiceChat = FindAnyObjectByType<BaseVoiceChat>();
        if (voiceChat == null) return true;

        if (voiceChat.IsMuted) return false;

        if (voiceChat.PushToTalkEnabled)
        {
            var filterField = typeof(BaseVoiceChat).GetField("_voiceInputControlFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (filterField != null)
            {
                var filter = filterField.GetValue(voiceChat);
                if (filter != null)
                {
                    var isHeldProp = filter.GetType().GetProperty("IsPushToTalkHeld");
                    if (isHeldProp != null)
                    {
                        bool isHeld = (bool)isHeldProp.GetValue(filter);
                        if (!isHeld) return false;
                    }
                }
            }
        }
        return true;
    }

    [Command(channel = Channels.Unreliable)]
    void CmdSendAudio(byte[] samples, int frequency, int channels) {
        RpcReceiveAudio((int)netId, samples, frequency, channels);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcReceiveAudio(int senderNetId, byte[] samples, int frequency, int channels) {
        if (!isLocalPlayer || _isRecording) return;

        PlayerInventoryComponent playerInventory = GetComponentInParent<PlayerInventoryComponent>();
        InventoryItemData itemData = playerInventory.GetHeldItemData();
        if (itemData.itemID == ItemID.WalkieTalkie && itemData.currentDurability > 0) {
            WalkieTalkieVoiceChannel.Instance.PlayAudio(senderNetId, samples, frequency, channels);
        }
    }

    private void OnDestroy() {
        StopRecording();
    }
}
