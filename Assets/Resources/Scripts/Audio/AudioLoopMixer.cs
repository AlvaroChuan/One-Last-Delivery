using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioLoopMixer : MonoBehaviour
{
    [Serializable]
    private struct AudioLoop
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float startFadeInValue;
        [Range(0f, 1f)] public float endFadeInValue;
        [Range(0f, 1f)] public float startFadeOutValue;
        [Range(0f, 1f)] public float endFadeOutValue;
    }

    [SerializeField] private AudioMixerGroup _mixerGroup;
    [Range(0f, 1f)] [SerializeField] private float _volume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _spacialBlend = 1f; // 0 = 2D, 1 = 3D
    [SerializeField] private float _maxDistance = 50f;
    [SerializeField] private AudioLoop[] _audioLoops;
    private float _currentFadeValue;
    private AudioSource[] _audioSources;

    void Awake()
    {
        _audioSources = new AudioSource[_audioLoops.Length];
        for (int i = 0; i < _audioLoops.Length; i++)
        {
            _audioSources[i] = gameObject.AddComponent<AudioSource>();
            _audioSources[i].clip = _audioLoops[i].clip;
            _audioSources[i].loop = true;
            _audioSources[i].playOnAwake = false;
            _audioSources[i].volume = 0f;
            _audioSources[i].outputAudioMixerGroup = _mixerGroup;
            _audioSources[i].spatialBlend = _spacialBlend;
            _audioSources[i].maxDistance = _maxDistance;
            _audioSources[i].rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public void StartPlayback()
    {
        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[i].Play();
        }
    }
    public void StopPlayback()
    {
        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[i].Stop();
        }
    }

    public void SetFadeValue(float fadeValue)
    {
        _currentFadeValue = Mathf.Clamp01(fadeValue);
        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[i].volume = CalculateVolume(_audioLoops[i], _currentFadeValue) * _volume;
        }
    }

    private float CalculateVolume(AudioLoop audioLoop, float fadeValue)
    {
        if (fadeValue < audioLoop.startFadeInValue)
        {
            return 0f;
        }
        if (fadeValue < audioLoop.endFadeInValue)
        {
            return Mathf.InverseLerp(audioLoop.startFadeInValue, audioLoop.endFadeInValue, fadeValue);
        }
        if (fadeValue < audioLoop.startFadeOutValue)
        {
            return 1f;
        }
        if (fadeValue < audioLoop.endFadeOutValue)
        {
            return 1f - Mathf.InverseLerp(audioLoop.startFadeOutValue, audioLoop.endFadeOutValue, fadeValue);
        }
        return 0f;
    }
}
