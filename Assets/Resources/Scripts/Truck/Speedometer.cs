using Mirror;
using UnityEngine;
using TMPro;

public class Speedometer : NetworkBehaviour
{
    private TextMeshPro _speedText;
    [SyncVar(hook = nameof(OnSpeedoMeterUpdate))] private float _currentSpeed;

    private void Awake()
    {
        _speedText = GetComponentInChildren<TextMeshPro>();
    }

    private void OnEnable()
    {
        TruckController.OnSpeedChanged += OnSpeedChanged;
    }

    private void OnDisable()
    {
        TruckController.OnSpeedChanged -= OnSpeedChanged;
    }

    private void OnSpeedChanged(TruckController.MovementInfo movementInfo)
    {
        CmdUpdateSpeed(movementInfo.speed);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateSpeed(float speed)
    {
        _currentSpeed = speed;
    }

    private void OnSpeedoMeterUpdate(float oldSpeed, float newSpeed)
    {
        if (newSpeed >= 1) _speedText.text = $"{Mathf.Abs(newSpeed):0} mph - D";
        else if (newSpeed <= -1) _speedText.text = $"{Mathf.Abs(newSpeed):0} mph - R";
        else _speedText.text = "0 mph - N";
    }
}
