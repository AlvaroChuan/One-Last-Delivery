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

    [Header("Input")]
    [SerializeField] private InputActionReference _movementInputActionReference;

    [Header("Suspension")]
    [SerializeField] private float _suspensionRestLength; //rest position of the suspension springs (height of the vehicle)
    [SerializeField] private float _springStrength; //how stiff the springs are
    [SerializeField] private float _springDamping; //how much the springs resist oscillation

    [Header("Steering")]
    [SerializeField] private float _maxSteeringAngle; //maximum angle the front wheels can turn
    [SerializeField] private float _steeringSpeed; //how fast the wheels turn to the target angle
    [SerializeField] private float _wheelGripFactor; //how much the wheels resist lateral sliding WIP must implement a curve for grip vs speed
    [SerializeField] private float _wheelMass; //mass of each wheel, affects how much force is needed to change its velocity

    [Header("Powertrain")]
    [SerializeField] private TrainShaftType _trainShaftType; //type of drivetrain
    [SerializeField] private float _carMaxSpeed; //maximum speed of the car
    [SerializeField] private float _maxEngineTorque; //maximum torque output of the engine
    [SerializeField] private AnimationCurve _engineTorqueCurve; //% power output of the engine based on current speed (x = normalized speed, y = % torque)

    [Header("Braking")]
    [SerializeField] private float _engineBrakeTorque; //torque applied when not accelerating
    [SerializeField] private float _brakeTorque; //torque applied by the brakes
    [SerializeField, Range(0f, 1f)] private float _brakeBias; //distribution of braking force between front and rear wheels (0 = all front, 1 = all rear)

    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    private Transform[] _wheels;
    private Rigidbody _vehicleRigidbody;
    Vector2 _movementInput;
    RaycastHit[] _rayCastHitBuffer = new RaycastHit[3];

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
    }

    private void Update()
    {
        if (!isOwned) return;

        if (_movementInput.x != 0)
        {
            float steeringAngle = _maxSteeringAngle * _movementInput.x;
            _frontLeftWheel.localRotation = Quaternion.Slerp(_frontLeftWheel.localRotation, Quaternion.Euler(0f, steeringAngle, 0f), Time.deltaTime * _steeringSpeed);
            _frontRightWheel.localRotation = Quaternion.Slerp(_frontRightWheel.localRotation, Quaternion.Euler(0f, steeringAngle, 0f), Time.deltaTime * _steeringSpeed);
        }
        else //return wheels to straight position when no input
        {
            _frontLeftWheel.localRotation = Quaternion.Slerp(_frontLeftWheel.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * _steeringSpeed);
            _frontRightWheel.localRotation = Quaternion.Slerp(_frontRightWheel.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * _steeringSpeed);
        }
    }

    private void FixedUpdate()
    {
        bool hasAuthority = isOwned || (isServer && netIdentity.connectionToClient == null);
        if (!hasAuthority) return;

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
    }
}