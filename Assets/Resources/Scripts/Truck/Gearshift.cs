using UnityEngine;

public class Gearshift : MonoBehaviour
{
    [SerializeField] private Vector3 _axis = new Vector3(1, 0, 0);
    [SerializeField] private float _maxShiftAngle = 20f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _speedThreshold = 0.5f;

    private float _targetAngle = 0f;
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
        Quaternion targetRotation = _initialRotation * Quaternion.AngleAxis(_targetAngle, _axis.normalized);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * _rotationSpeed);
    }

    private void OnSpeedChanged(TruckController.MovementInfo movementInfo)
    {
        if (movementInfo.speed > _speedThreshold) _targetAngle = _maxShiftAngle;
        else if (movementInfo.speed < -_speedThreshold) _targetAngle = -_maxShiftAngle;
        else _targetAngle = 0f;
    }
}
