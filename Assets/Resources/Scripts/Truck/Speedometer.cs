using Mirror;
using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{
    private TextMeshPro _speedText;

    private void Awake()
    {
        _speedText = GetComponentInChildren<TextMeshPro>();
        TruckController.OnSpeedChanged += OnSpeedChanged;
    }

    void OnDestroy()
    {
        TruckController.OnSpeedChanged -= OnSpeedChanged;
    }

    private void OnSpeedChanged(TruckController.MovementInfo movementInfo)
    {
        float newSpeed = movementInfo.speed;

        if (newSpeed >= 1) _speedText.text = $"{Mathf.Abs(newSpeed):0} mph - D";
        else if (newSpeed <= -1) _speedText.text = $"{Mathf.Abs(newSpeed):0} mph - R";
        else _speedText.text = "0 mph - N";
    }
}
