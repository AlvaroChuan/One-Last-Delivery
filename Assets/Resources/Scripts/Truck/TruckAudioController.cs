using System;
using UnityEngine;

public class TruckAudioController : MonoBehaviour
{
    [SerializeField] private AudioEvent _startUpAudioEvent;
    AudioLoopMixer _engineAudioLoopMixer;
    bool _playingEngineLoop = false;
    float _currentSpeed = 0f;

    private void Awake()
    {
        _engineAudioLoopMixer = GetComponent<AudioLoopMixer>();
        TruckController.OnSpeedChanged += HandleSpeedChanged;
    }

    void OnDestroy()
    {
        TruckController.OnSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleSpeedChanged(TruckController.MovementInfo info)
    {
        float newSpeed = Math.Abs(info.speed) / info.maxSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, newSpeed, Time.deltaTime * 0.9f);
        if (!Mathf.Approximately(info.acceleration, 0f) && !_playingEngineLoop)
        {
            DevLogger.Log("Starting engine audio loop");
            _engineAudioLoopMixer.StartPlayback();
            _startUpAudioEvent.Play(gameObject);
            _playingEngineLoop = true;
        }
        else if (_currentSpeed < 0.01f && _playingEngineLoop && Mathf.Approximately(info.acceleration, 0f))
        {
            DevLogger.Log("Stopping engine audio loop");
            _engineAudioLoopMixer.StopPlayback();
            _playingEngineLoop = false;
        }
        _engineAudioLoopMixer.SetFadeValue(_currentSpeed);
    }
}
