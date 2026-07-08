using Mirror.Examples.Basic;
using UnityEngine;


[RequireComponent(typeof(PlayerGroundCheckComponent))]
public class StepPlayer : MonoBehaviour
{
    [SerializeField] private AudioEvent _stepAudioEvent;
    [SerializeField] private float _stepAmplitude = 1f;
    PlayerGroundCheckComponent _groundCheckComponent;
    float _distanceTraveled = 0f;
    Vector3 _lastPosition;

    void Awake()
    {
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
        _lastPosition = transform.position;
    }
    private void Update()
    {
        if (!_groundCheckComponent.IsGrounded()) return;

        Vector3 currentPosition = transform.position;
        currentPosition.y = 0f; // Ignore vertical movement
        _distanceTraveled += Vector3.Distance(currentPosition, _lastPosition);
        _lastPosition = currentPosition;

        if (_distanceTraveled >= _stepAmplitude)
        {
            _stepAudioEvent.Play(transform.position);
            _distanceTraveled = 0f;
        }
    }
}
