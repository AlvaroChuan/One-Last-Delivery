using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicPlayer : MonoBehaviour
{
    [Serializable]
    public struct MusicClipData
    {
        public MusicClipID id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }
    [SerializeField] private AudioMixerGroup _mixerGroup;
    [SerializeField] private MusicClipData[] _musicClips;
    private AudioSource[] _audioSources;
    public static MusicPlayer Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize AudioSources for each music clip
        _audioSources = new AudioSource[_musicClips.Length];
        for (int i = 0; i < _musicClips.Length; i++)
        {
            _audioSources[i] = gameObject.AddComponent<AudioSource>();
            _audioSources[i].outputAudioMixerGroup = _mixerGroup;
            _audioSources[i].clip = _musicClips[i].clip;
            _audioSources[i].volume = _musicClips[i].volume;
            _audioSources[i].loop = true;
        }
    }

    public void PlayMusic(MusicClipID id, float fadeDuration = 1f)
    {
        for (int i = 0; i < _musicClips.Length; i++)
        {
            if (_musicClips[i].id == id)
            {
                StartCoroutine(FadeIn(_audioSources[i], fadeDuration, _musicClips[i].volume));
            }
            else
            {
                StartCoroutine(FadeOut(_audioSources[i], fadeDuration));
            }
        }
    }

    private IEnumerator FadeIn(AudioSource audioSource, float duration, float targetVolume)
    {
        audioSource.volume = 0f;
        audioSource.Play();
        float startTime = Time.time;

        while (audioSource.volume < targetVolume)
        {
            audioSource.volume = Mathf.Lerp(0f, targetVolume, (Time.time - startTime) / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOut(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;
        float startTime = Time.time;

        while (audioSource.volume > 0f)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, (Time.time - startTime) / duration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Reset volume for next time
    }
}