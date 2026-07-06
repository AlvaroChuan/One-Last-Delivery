using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio Event", fileName = "New Audio Event")]
public class AudioEvent : ScriptableObject
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private AudioMixerGroup _mixerGroup;

    [Range(0f, 1f)] [SerializeField] private float _volume = 1f;
    [Range(0f, 2f)] [SerializeField] private float _minPitch = 0.85f;
    [Range(0f, 2f)] [SerializeField] private float _maxPitch = 1.15f;

    /// <summary>
    /// Plays the audio event from a given GameObject. If the GameObject does not have an AudioSource, one will be added automatically.
    /// </summary>
    public void Play(GameObject sourceObject)
    {
        if (_clips.Length == 0) return;

        if (!sourceObject.TryGetComponent(out AudioSource source))
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        ConfigureAndPlay(source);
    }

    /// <summary>
    /// Plays the audio event at a specific position in the world. A temporary GameObject with an AudioSource will be created to play the sound, and it will be destroyed after the clip finishes playing.
    /// </summary>
    public void Play(Vector3 position)
    {
        if (_clips.Length == 0) return;

        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();

        source.spatialBlend = 1.0f; // Force full 3D sound

        AudioClip selectedClip = ConfigureAndPlay(source);

        float clipLength = selectedClip.length / Mathf.Max(0.001f, source.pitch);
        Destroy(tempGO, clipLength);
    }

    private AudioClip ConfigureAndPlay(AudioSource source)
    {
        source.outputAudioMixerGroup = _mixerGroup;

        AudioClip clip = _clips[Random.Range(0, _clips.Length)];
        source.pitch = Random.Range(_minPitch, _maxPitch);

        source.PlayOneShot(clip, _volume);

        return clip;
    }
}