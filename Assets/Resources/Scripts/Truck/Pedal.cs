using UnityEngine;

public class Pedal : MonoBehaviour
{
    private enum PedalType
    {
        Gas,
        Brake
    }

    [SerializeField] private Vector3 _axis = new Vector3(1, 0, 0);
    [SerializeField] private float _maxPedalAngle = 30f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private PedalType _pedalType = PedalType.Gas;
    private float _currentPedalRotation;
    private Quaternion _initialRotation;

    private void Start()
    {
        _initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        TruckController.OnSpeedChanged += OnSpeedChanged;
    }

    private void OnDisable()
    {
        TruckController.OnSpeedChanged -= OnSpeedChanged;
    }

    private void Update()
    {
        Quaternion targetRotation = _initialRotation * Quaternion.AngleAxis(_currentPedalRotation, _axis.normalized);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * _rotationSpeed);
    }

    private void OnSpeedChanged(TruckController.MovementInfo movementInfo)
    {
        switch (_pedalType)
        {
            case PedalType.Gas:
                bool isAccelerating = (movementInfo.acceleration > 0 &&  movementInfo.speed >= 0f) || (movementInfo.acceleration < 0 && movementInfo.speed <= 0f);
                _currentPedalRotation = isAccelerating ? -_maxPedalAngle * Mathf.Abs(movementInfo.acceleration) : 0f;
                break;
            case PedalType.Brake:
            bool isBraking = (movementInfo.acceleration < 0 &&  movementInfo.speed > 0f) || (movementInfo.acceleration > 0 && movementInfo.speed < 0f);
                _currentPedalRotation = isBraking ? -_maxPedalAngle * Mathf.Abs(movementInfo.acceleration) : 0f;
                break;
        }
    }
}
