using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class ObjectImpactSound : NetworkBehaviour
{
    [SerializeField] private AudioEvent _impactSoundEvent;
    [SerializeField] private float _impactThreshold = 1f; // Minimum impact force to trigger sound
    [SerializeField] private float _volumeScale = 1f;
    Rigidbody _rigidbody;
    bool _alreadyPlayedSound = false; // Flag to ensure sound is played only once per impact
    Vector3 _velocityBeforeCollision;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        _velocityBeforeCollision = _rigidbody.linearVelocity; // Store the velocity before the collision for impact force calculation
    }

    void OnCollisionEnter(Collision collision)
    {
        DevLogger.Log($"Velocity: {_velocityBeforeCollision.magnitude}, Mass: {_rigidbody.mass}, Impact Threshold: {_impactThreshold}");
        float impactForce = _rigidbody.mass * _velocityBeforeCollision.magnitude; // Calculate impact force based on mass and velocity before collision
        if (impactForce >= _impactThreshold)
        {
            _alreadyPlayedSound = true; // Set the flag to true to prevent multiple sound plays for the same impact
            _impactSoundEvent.Play(transform.position, _volumeScale);
            CmdPlayImpactSound(transform.position, _volumeScale); // Normalize impact force for sound scaling
        }
    }

    [Command]
    private void CmdPlayImpactSound(Vector3 position, float volumeScale)
    {
        RpcPlayImpactSound(position, volumeScale);
    }

    [ClientRpc]
    private void RpcPlayImpactSound(Vector3 position, float volumeScale)
    {
        if (_alreadyPlayedSound)
        {
            _alreadyPlayedSound = false; // Reset the flag for future impacts
            return; // Prevent playing the sound again on the client that initiated the collision
        }
        _impactSoundEvent.Play(position, volumeScale);
    }
}