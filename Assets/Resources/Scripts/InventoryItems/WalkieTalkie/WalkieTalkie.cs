using UnityEngine;
using Mirror;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using System;
using System.Reflection;

public class WalkieTalkie : InventoryItem
{
    private ConcentusEncodeFilter _encoder;
    private Action<int, int, float[]> _micListener;

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

    void StartRecording()
    {
        DevLogger.Log("Starting recording for WalkieTalkie");
        int micIndex = GetMicrophoneIndex();
        if (Mic.AvailableDevices.Count > micIndex)
        {
            DevLogger.Log($"Using microphone: {Mic.AvailableDevices[micIndex].Name}");
            Mic.Device device = Mic.AvailableDevices[micIndex];
            if (_encoder == null) _encoder = new ConcentusEncodeFilter();
            if (_micListener == null)
            {
                _micListener = OnFrameCollected;
                device.OnFrameCollected += _micListener;
            }
            if (!device.IsRecording) device.StartRecording(60);
        }
    }

    void StopRecording()
    {
        int micIndex = GetMicrophoneIndex();
        if (Mic.AvailableDevices.Count > micIndex && _micListener != null)
        {
            Mic.AvailableDevices[micIndex].OnFrameCollected -= _micListener;
            _micListener = null;
        }
    }

    void OnFrameCollected(int frequency, int channels, float[] samples)
    {
        DevLogger.Log($"Frame collected: Frequency={frequency}, Channels={channels}, Samples={samples.Length}");
        if (!ShouldTransmitAudio()) return;
        DevLogger.Log("Transmitting audio frame for WalkieTalkie");
        float volume = 1f;
        BaseVoiceChat voiceChat = FindAnyObjectByType<BaseVoiceChat>();
        if (voiceChat != null)
        {
            volume = voiceChat.MicrophoneVolume;
        }

        byte[] bytes = new byte[samples.Length * 4];
        if (Mathf.Approximately(volume, 1f))
        {
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        }
        else
        {
            float[] modifiedSamples = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                modifiedSamples[i] = Mathf.Clamp(samples[i] * volume, -1f, 1f);
            }
            Buffer.BlockCopy(modifiedSamples, 0, bytes, 0, bytes.Length);
        }

        AudioFrame frame = new AudioFrame
        {
            samples = bytes,
            frequency = frequency,
            channelCount = channels
        };

        AudioFrame encoded = _encoder.Run(frame);
        if (encoded.samples != null && encoded.samples.Length > 0)
        {
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
            FieldInfo filterField = typeof(BaseVoiceChat).GetField("_voiceInputControlFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (filterField != null)
            {
                var filter = filterField.GetValue(voiceChat);
                if (filter != null)
                {
                    PropertyInfo isHeldProp = filter.GetType().GetProperty("IsPushToTalkHeld");
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
    void CmdSendAudio(byte[] samples, int frequency, int channels)
    {
        RpcReceiveAudio((int)netId, samples, frequency, channels);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcReceiveAudio(int senderNetId, byte[] samples, int frequency, int channels)
    {
        if (isLocalPlayer) return;

        WalkieTalkieVoiceChannel.Instance.PlayAudio(senderNetId, samples, frequency, channels);
    }

    private void OnDestroy()
    {
        StopRecording();
    }
}
