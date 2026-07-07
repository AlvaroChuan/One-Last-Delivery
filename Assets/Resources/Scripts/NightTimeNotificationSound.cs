using UnityEngine;

public class NightTimeNotificationSound : MonoBehaviour
{
    [SerializeField] private AudioEvent _nightTimeNotificationAudioEvent;

    void Awake()
    {
        SunManager.OnNightfall += PlayNightTimeNotificationSound;
    }

    void OnDestroy()
    {
        SunManager.OnNightfall -= PlayNightTimeNotificationSound;
    }

    void PlayNightTimeNotificationSound()
    {
        DevLogger.Log("NightTimeNotificationSound: Nightfall detected, playing notification sound.");
        _nightTimeNotificationAudioEvent.Play(gameObject);
    }
}