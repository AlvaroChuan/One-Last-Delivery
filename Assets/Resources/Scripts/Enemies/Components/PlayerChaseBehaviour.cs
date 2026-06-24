using UnityEngine;

[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(PlayerDistanceDetector))]
public class PlayerChaseBehaviour : MonoBehaviour
{
    [SerializeField] private float _chaseSpeed = 5f; // Speed while chasing
    [SerializeField] private float _chaseAcceleration = 10f; // Acceleration while chasing
    [SerializeField] private float _playerRecheckInterval = 1f; // Interval to check for players
    private bool _isChasing = false;
    public bool IsChasing => _isChasing;
    private float _playerRecheckTimer = 0f;
    private NavMeshMovementComponent _navMeshMovementComponent;
    private PlayerDistanceDetector _playerDistanceDetector;
    private GameObject _currentTarget;
    public GameObject CurrentTarget => _currentTarget;

    void Awake()
    {
        _navMeshMovementComponent = GetComponent<NavMeshMovementComponent>();
        _playerDistanceDetector = GetComponent<PlayerDistanceDetector>();

        _playerRecheckTimer = Random.Range(0f, _playerRecheckInterval); // Randomize the initial timer to avoid all enemies checking for players at the same time
    }

    public void UpdateChase(float deltaTime, float detectionRadius)
    {
        _playerRecheckTimer += deltaTime;
        if (_playerRecheckTimer >= _playerRecheckInterval)
        {
            _playerRecheckTimer = 0f;
            CheckForPlayer(detectionRadius);
        }
    }

    public void CheckForPlayer(float detectionRadius)
    {
        GameObject closestPlayer = _playerDistanceDetector.DetectClosestPlayer(detectionRadius);
        if (closestPlayer != null)
        {
            _navMeshMovementComponent.SetSpeed(_chaseSpeed);
            _navMeshMovementComponent.SetAcceleration(_chaseAcceleration);
            _navMeshMovementComponent.SetTarget(closestPlayer);
            _isChasing = true;
            _currentTarget = closestPlayer;
        }
        else if (_isChasing)
        {
            _isChasing = false;
            _currentTarget = null;
            _navMeshMovementComponent.SetTarget(null);
        }
    }
}