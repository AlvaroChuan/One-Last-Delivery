using System;
using Mirror;
using UnityEngine;

public class TruckAudioController : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnTruckSpeedChanged))] private TruckController.MovementInfo _currentSpeedInfo;
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
        CmdUpdateSpeed(info);
    }

    [Command(requiresAuthority = false)]
    void CmdUpdateSpeed(TruckController.MovementInfo info)
    {
        _currentSpeedInfo = info;
    }

    private void OnTruckSpeedChanged(TruckController.MovementInfo oldInfo, TruckController.MovementInfo newInfo)
    {
        float newSpeed = Math.Abs(newInfo.speed) / newInfo.maxSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, newSpeed, Time.deltaTime * 0.9f);
        if (!Mathf.Approximately(newInfo.acceleration, 0f) && !_playingEngineLoop)
        {
            DevLogger.Log("Starting engine audio loop");
            _engineAudioLoopMixer.StartPlayback();
            _startUpAudioEvent.Play(gameObject);
            _playingEngineLoop = true;
        }
        else if (_currentSpeed < 0.01f && _playingEngineLoop && Mathf.Approximately(newInfo.acceleration, 0f))
        {
            DevLogger.Log("Stopping engine audio loop");
            _engineAudioLoopMixer.StopPlayback();
            _playingEngineLoop = false;
        }
        _engineAudioLoopMixer.SetFadeValue(_currentSpeed);
    }
}