using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] private Vector3 _axis;
    [SerializeField] private float _maxSteeringAngle = 30f;
    [SerializeField] private float _steeringSpeed = 10f;
    private float _currentSteeringInput;
    private Quaternion _initialRotation;

    private void Start()
    {
        _initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        TruckController.OnSteeringAngleChanged += OnInputChanged;
    }

    private void OnDisable()
    {
        TruckController.OnSteeringAngleChanged -= OnInputChanged;
    }

    private void Update()
    {
        float targetAngle = _maxSteeringAngle * _currentSteeringInput;
        
        Quaternion targetRotation = _initialRotation * Quaternion.AngleAxis(targetAngle, _axis.normalized);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * _steeringSpeed);
    }

    private void OnInputChanged(float steeringInput)
    {
        _currentSteeringInput = steeringInput;
    }
}
