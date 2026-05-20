using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class PackageCarryComponent : MonoBehaviour
{
    [SerializeField] private float _carryForce = 40f;
    [SerializeField] private float _distanceForceScaling = .2f;
    [SerializeField] private float _damping = 7.5f;

    Vector3 _offsetFromPlayer;
    Rigidbody _rigidbody;

    Rigidbody _playerRigidbody;
    Camera _playerCamera;

    bool _isCarried = false;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void StartCarrying(GameObject player)
    {
        _rigidbody.useGravity = false;

        _playerCamera = player.GetComponent<PlayerLookComponent>().PlayerCamera;
        _playerRigidbody = player.GetComponent<Rigidbody>();

        _offsetFromPlayer = _playerCamera.transform.InverseTransformPoint(transform.position);
        _offsetFromPlayer.x = 0; // Keep the package centered horizontally relative to the player

        _isCarried = true;
    }

    public void StopCarrying()
    {
        _rigidbody.useGravity = true;

        _playerCamera = null;
        _playerRigidbody = null;

        _isCarried = false;
    }

    void FixedUpdate()
    {
        if (!_isCarried) return;

        HandlePosition();
        HandleRotation();
    }

    void HandlePosition()
    {
        Vector3 targetPosition = _playerCamera.transform.TransformPoint(_offsetFromPlayer);
        Vector3 forceDirection = targetPosition - transform.position;

        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, _playerRigidbody.linearVelocity, _damping * Time.fixedDeltaTime); // Inherit player's velocity while being carried

        // Apply a force to keep the package in position relative to the player
        _rigidbody.AddForce(forceDirection * _carryForce * (1 + forceDirection.magnitude * _distanceForceScaling), ForceMode.Acceleration);

        //Remove all velocity away from the player if package is too far
        Vector3 vectorToPlayer = transform.position - _playerCamera.transform.position;
        if(vectorToPlayer.magnitude > _offsetFromPlayer.magnitude * 1.2f)
        {
            Vector3 velocityAwayFromPlayer = Vector3.Project(_rigidbody.linearVelocity, vectorToPlayer);
            _rigidbody.AddForce(-velocityAwayFromPlayer, ForceMode.VelocityChange);
        }
    }

    void HandleRotation()
    {
        //Apply torque to face the package forward relative to the player
        Quaternion targetRotation = _playerRigidbody.transform.rotation;
        Quaternion currentRotation = transform.rotation;
        Quaternion rotationDifference = targetRotation * Quaternion.Inverse(currentRotation);
        rotationDifference.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
        if (angleInDegrees > 180f) angleInDegrees -= 360f;
        Vector3 torque = rotationAxis * angleInDegrees * Time.fixedDeltaTime;
        _rigidbody.AddTorque(torque, ForceMode.VelocityChange);

        //Apply damping to angular velocity to prevent oscillations
        _rigidbody.angularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, Vector3.zero, _damping * Time.fixedDeltaTime);
    }
}