using System;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class TruckController : NetworkBehaviour
{
    private enum TrainShaftType
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    public struct MovementInfo
    {
        public float acceleration;
        public float speed;
        public float maxSpeed;
    }

    [Header("Input")]
    [SerializeField] private InputActionReference _movementInputActionReference;

    [Header("Suspension")]
    [Tooltip("Rest position of the suspension springs (height of the vehicle)")]
    [SerializeField] private float _suspensionRestLength; //rest position of the suspension springs (height of the vehicle)
    [Tooltip("How stiff the springs are")]
    [SerializeField] private float _springStrength; //how stiff the springs are
    [Tooltip("How much the springs resist oscillation")]
    [SerializeField] private float _springDamping; //how much the springs resist oscillation

    [Header("Steering")]
    [Tooltip("Maximum angle the front wheels can turn")]
    [SerializeField] private float _maxSteeringAngle; //maximum angle the front wheels can turn
    [Tooltip("How fast the wheels turn to the target angle")]
    [SerializeField] private float _steeringSpeed; //how fast the wheels turn to the target angle
    [Tooltip("How much the wheels resist lateral sliding")]
    [SerializeField] private float _wheelGripFactor; //how much the wheels resist lateral sliding WIP must implement a curve for grip vs speed
    [Tooltip("Mass of each wheel, affects how much force is needed to change its velocity")]
    [SerializeField] private float _wheelMass; //mass of each wheel, affects how much force is needed to change its velocity
    [Tooltip("Maximum tilt angle of the vehicle before it starts to flip over")]
    [SerializeField] private float _maxTiltAngle; //maximum tilt angle of the vehicle before it starts to flip over

    [Header("Powertrain")]
    [Tooltip("Type of drivetrain")]
    [SerializeField] private TrainShaftType _trainShaftType; //type of drivetrain
    [Tooltip("Maximum speed of the car")]
    [SerializeField] private float _carMaxSpeed; //maximum speed of the car
    [Tooltip("Maximum torque output of the engine")]
    [SerializeField] private float _maxEngineTorque; //maximum torque output of the engine
    [Tooltip("Power output of the engine based on current speed")]
    [SerializeField] private AnimationCurve _engineTorqueCurve; //% power output of the engine based on current speed (x = normalized speed, y = % torque)

    [Header("Braking")]
    [Tooltip("Torque applied when not accelerating")]
    [SerializeField] private float _engineBrakeTorque; //torque applied when not accelerating
    [Tooltip("Torque applied by the brakes")]
    [SerializeField] private float _brakeTorque; //torque applied by the brakes
    [Tooltip("Distribution of braking force between front and rear wheels (0 = all front, 1 = all rear)")]
    [SerializeField, Range(0f, 1f)] private float _brakeBias; //distribution of braking force between front and rear wheels (0 = all front, 1 = all rear)

    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    private float _mpsToMph = 2.23694f; // Conversion factor from meters per second to miles per hour
    private Transform[] _wheels;
    private Rigidbody _vehicleRigidbody;
    Vector2 _movementInput;
    RaycastHit[] _rayCastHitBuffer = new RaycastHit[3];

    [SyncVar(hook = nameof(OnUpgradeStatsChanged))] private TruckStatsStruct _currentUpgradeStats;

    public static Action<float> OnSteeringAngleChanged;
    public static Action<MovementInfo> OnSpeedChanged;
    Vector3 _lastPosition;
    float _lastSpeed;

    void Awake()
    {
        _vehicleRigidbody = GetComponent<Rigidbody>();
        _wheels = new Transform[] { _frontLeftWheel, _frontRightWheel, _rearLeftWheel, _rearRightWheel };
    }

    public override void OnStartAuthority()
    {
        _movementInputActionReference.action.Enable();

        _movementInputActionReference.action.performed += OnMovementInput;
        _movementInputActionReference.action.canceled += OnMovementInput;
    }

    public override void OnStopAuthority()
    {
        _movementInputActionReference.action.Disable();

        _movementInputActionReference.action.performed -= OnMovementInput;
        _movementInputActionReference.action.canceled -= OnMovementInput;

        _movementInput = Vector2.zero; // Reset movement input when authority is lost
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        if (!isOwned) return;

        _movementInput = context.ReadValue<Vector2>();
        OnSteeringAngleChanged?.Invoke(_movementInput.x);
    }

    private void FixedUpdate()
    {
        bool hasAuthority = isOwned || (isServer && netIdentity.connectionToClient == null);
        if (!isOwned)
        {
            _movementInput = Vector2.zero; // Reset movement input for non-owned instances to prevent unintended movement
        }
        if (!hasAuthority) return;

        float currentSpeed = Vector3.Dot(_vehicleRigidbody.transform.forward, _vehicleRigidbody.linearVelocity);
        OnSpeedChanged?.Invoke(new MovementInfo { acceleration = _movementInput.y, speed = currentSpeed * _mpsToMph, maxSpeed = _carMaxSpeed * _mpsToMph });

        foreach (var wheel in _wheels)
        {
            int contacts = Physics.RaycastNonAlloc(wheel.position, -wheel.up, _rayCastHitBuffer, _suspensionRestLength);
            if (contacts == 0) continue;

            RaycastHit? wheelHit = null;

            for (int i = 0; i < contacts; i++)
            {
                if (_rayCastHitBuffer[i].collider.gameObject != gameObject) //ignore self collisions
                {
                    wheelHit = _rayCastHitBuffer[i];
                    break;
                }
            }

            if (wheelHit == null) continue;

            //suspension logic
            Vector3 springDirection = wheel.up; //world-space direction of the spring force
            Vector3 wheelWorldVelocity = _vehicleRigidbody.GetPointVelocity(wheel.position); //world-space velocity of the wheel
            float offset = _suspensionRestLength - wheelHit.Value.distance; //how far the spring is compressed
            float springVelocity = Vector3.Dot(springDirection, wheelWorldVelocity); //velocity of the spring along its direction
            float springForce = (offset * _springStrength) - (springVelocity * _springDamping); //spring force formula
            _vehicleRigidbody.AddForceAtPosition(springDirection * Mathf.Abs(springForce), wheel.position); //apply the spring force at the wheel position

            //Debugging rays for suspension
            //Ray debugRay = new Ray(wheel.position, springDirection * Mathf.Abs(springForce));
            //Debug.DrawRay(debugRay.origin, debugRay.direction, Color.green);

            //steering/friction logic
            Vector3 steeringDirection = wheel.right; //world-space direction of the steering/friction force
            float steeringVelocity = Vector3.Dot(steeringDirection, wheelWorldVelocity); //velocity of the wheel along the steering direction
            float desiredVelocityChange = -steeringVelocity * _wheelGripFactor; //desired change in velocity to reduce lateral sliding
            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime; //necessary acceleration to achieve the desired velocity change
            _vehicleRigidbody.AddForceAtPosition(steeringDirection * _wheelMass * desiredAcceleration, wheel.position); //apply the steering/friction force at the wheel position

            //Debugging rays for steering/friction
            //Ray debugRay2 = new Ray(wheel.position, steeringDirection * wheelMass * desiredAcceleration);
            //Debug.DrawRay(debugRay2.origin, debugRay2.direction, Color.red);

            //Propulsion logic
            Vector3 accelerationDirection = wheel.forward; //world-space direction of the propulsion force
            float carSpeed = Vector3.Dot(_vehicleRigidbody.transform.forward, _vehicleRigidbody.linearVelocity); //current speed of the car in the forward direction
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / _carMaxSpeed); //normalize speed to a 0-1 range based on its max speed
            if (isOwned)
            {
                float availableTorque = _engineTorqueCurve.Evaluate(normalizedSpeed) * _maxEngineTorque * Mathf.Abs(_movementInput.y); //get the available torque from the engine based on the current speed
                if (_movementInput.y > 0 && carSpeed >= 0f) //car acceleration forward (input && car moving forward)
                {
                    switch (_trainShaftType)
                    {
                        case TrainShaftType.FrontWheelDrive:
                            if (wheel == _frontLeftWheel || wheel == _frontRightWheel)
                            {
                                _vehicleRigidbody.AddForceAtPosition(accelerationDirection * availableTorque, wheel.position);
                            }
                            break;
                        case TrainShaftType.RearWheelDrive:
                            if (wheel == _rearLeftWheel || wheel == _rearRightWheel)
                            {
                                _vehicleRigidbody.AddForceAtPosition(accelerationDirection * availableTorque, wheel.position);
                            }
                            break;
                        case TrainShaftType.AllWheelDrive:
                            _vehicleRigidbody.AddForceAtPosition(accelerationDirection * availableTorque / 2, wheel.position);
                            break;
                    }
                }
                else if (_movementInput.y < 0 && carSpeed <= 0f) //car acceleration backward (input && car moving backward)
                {
                    switch (_trainShaftType)
                    {
                        case TrainShaftType.FrontWheelDrive:
                            if (wheel == _frontLeftWheel || wheel == _frontRightWheel)
                            {
                                _vehicleRigidbody.AddForceAtPosition(-accelerationDirection * availableTorque, wheel.position);
                            }
                            break;
                        case TrainShaftType.RearWheelDrive:
                            if (wheel == _rearLeftWheel || wheel == _rearRightWheel)
                            {
                                _vehicleRigidbody.AddForceAtPosition(-accelerationDirection * availableTorque, wheel.position);
                            }
                            break;
                        case TrainShaftType.AllWheelDrive:
                            _vehicleRigidbody.AddForceAtPosition(-accelerationDirection * availableTorque / 2, wheel.position);
                            break;
                    }
                }
                //Debugging rays for powertrain
                //Ray debugRay3 = new Ray(wheel.position, accelerationDirection * Mathf.Sign(driverPlayerManager.MovementInput.y) * availableTorque);
                //Debug.DrawRay(debugRay3.origin, debugRay3.direction * availableTorque * 0.1f, Color.blue);
            }

            //Braking logic
            float actualBrakeTorque = 0f;
            if (!isOwned) actualBrakeTorque = _engineBrakeTorque;
            else if (_movementInput.y == 0 && normalizedSpeed > 0f) actualBrakeTorque = _engineBrakeTorque; //no input, apply engine braking when moving
            else if ((_movementInput.y < 0 && carSpeed >= 0) || (_movementInput.y > 0 && carSpeed <= 0)) actualBrakeTorque = _brakeTorque; //input opposite to movement direction, apply brakes
            Vector3 breakingDirection = carSpeed >= 0f ? -accelerationDirection : accelerationDirection;
            if (actualBrakeTorque != 0f)
            {
                if (wheel == _frontLeftWheel || wheel == _frontRightWheel) _vehicleRigidbody.AddForceAtPosition(breakingDirection * actualBrakeTorque * (1f - _brakeBias), wheel.position);
                else _vehicleRigidbody.AddForceAtPosition(breakingDirection * actualBrakeTorque * _brakeBias, wheel.position);
            }

            //Debugging rays for braking
            //Ray debugRay4 = new Ray(wheel.position, breakingDirection * actualBrakeTorque);
            //Debug.DrawRay(debugRay4.origin, debugRay4.direction * actualBrakeTorque * 0.1f, Color.yellow);
        }

        //Clamp the vehicle's rotation to prevent flipping over
        Vector3 currentEuler = _vehicleRigidbody.rotation.eulerAngles;

        // 2. Normalize the angles to -180 to 180, then clamp them to your max allowed tilt
        float clampedX = Mathf.Clamp(Mathf.DeltaAngle(0, currentEuler.x), -_maxTiltAngle, _maxTiltAngle);
        float clampedZ = Mathf.Clamp(Mathf.DeltaAngle(0, currentEuler.z), -_maxTiltAngle, _maxTiltAngle);

        // 3. Apply the clamped rotation, preserving the Y (steering) axis
        _vehicleRigidbody.rotation = Quaternion.Euler(clampedX, currentEuler.y, clampedZ);
    }

    void OnUpgradeStatsChanged(TruckStatsStruct oldStats, TruckStatsStruct newStats)
    {
        Debug.Log($"Upgrade stats changed from {oldStats} to {newStats}");
        _suspensionRestLength -= oldStats.suspensionRestLength;
        _suspensionRestLength += newStats.suspensionRestLength;

        _springStrength -= oldStats.springStrength;
        _springStrength += newStats.springStrength;

        _springDamping -= oldStats.springDamping;
        _springDamping += newStats.springDamping;

        _maxSteeringAngle -= oldStats.maxSteeringAngle;
        _maxSteeringAngle += newStats.maxSteeringAngle;

        _maxSteeringAngle = Mathf.Clamp(_maxSteeringAngle, 0f, 70f);

        _steeringSpeed -= oldStats.steeringSpeed;
        _steeringSpeed += newStats.steeringSpeed;

        _wheelGripFactor -= oldStats.wheelGripFactor;
        _wheelGripFactor += newStats.wheelGripFactor;

        _wheelMass -= oldStats.wheelMass;
        _wheelMass += newStats.wheelMass;

        _carMaxSpeed -= oldStats.carMaxSpeed;
        _carMaxSpeed += newStats.carMaxSpeed;

        _maxEngineTorque -= oldStats.maxEngineTorque;
        _maxEngineTorque += newStats.maxEngineTorque;

        _engineBrakeTorque -= oldStats.engineBrakeTorque;
        _engineBrakeTorque += newStats.engineBrakeTorque;

        _brakeTorque -= oldStats.brakeTorque;
        _brakeTorque += newStats.brakeTorque;
    }

    [Server]
    public void SetUpgradeStats(TruckStatsStruct upgradeStats)
    {
        _currentUpgradeStats = upgradeStats;
    }
}