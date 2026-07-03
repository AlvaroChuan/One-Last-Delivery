using UnityEngine;
using Mirror;
using TMPro;

public class TruckElements : NetworkBehaviour
{
    [Header("Truck Elements")]
    [SerializeField] private Transform _steeringWheel;
    [SerializeField] private Transform _gearShiftLever;
    [SerializeField] private Transform _gasPedal;
    [SerializeField] private Transform _brakePedal;
    [SerializeField] private TextMeshPro _speedometerText;
    
    [Header("Lights")]
    [SerializeField] private Light[] _headlights;
    [SerializeField] private Light[] _brakeLights;
}