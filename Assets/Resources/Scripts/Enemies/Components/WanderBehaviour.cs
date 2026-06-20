using UnityEngine;

[RequireComponent(typeof(NavMeshMovementComponent))]
public class WanderBehaviour : MonoBehaviour
{
    [SerializeField] private float _wanderInterval = 5f; // Interval to wander when no player is detected
    [SerializeField] private float _wanderRadius = 20f; // Radius within which to wander
    [SerializeField] private float _wanderNavMeshSampleDistance = 2f; // Distance to sample the NavMesh for wandering
    [SerializeField] private int _wanderNavMeshSampleAttempts = 10; // Number of attempts to sample the NavMesh for wandering
    [SerializeField] private float _wanderSpeed = 2f; // Speed while wandering
    [SerializeField] private float _wanderAcceleration = 5f; // Acceleration while wandering

    private float _wanderTimer = 0f;
    private NavMeshMovementComponent _chaseBehaviour;

    void Awake()
    {
        _chaseBehaviour = GetComponent<NavMeshMovementComponent>();
        _wanderTimer = Random.Range(0f, _wanderInterval); // Randomize the initial timer to avoid all enemies wandering at the same time
    }

    public void UpdateWander(float deltaTime)
    {
        _wanderTimer += deltaTime;
        if (_wanderTimer >= _wanderInterval)
        {
            _wanderTimer = 0f;
            StartWander();
        }
    }

    public void StartWander()
    {
        DevLogger.Log("GameObject " + gameObject.name + " is starting to wander.");
        _chaseBehaviour.SetSpeed(_wanderSpeed);
        _chaseBehaviour.SetAcceleration(_wanderAcceleration);
        Vector2 randomDirection;
        Vector3 wanderTarget;
        int sampleAttempts = 0;
        bool isOnNavMesh;
        do
        {
            randomDirection = Random.insideUnitCircle * _wanderRadius;
            wanderTarget = new Vector3(randomDirection.x, transform.position.y, randomDirection.y) + transform.position;
            _chaseBehaviour.SetTarget(wanderTarget, _wanderNavMeshSampleDistance, out isOnNavMesh);
            _chaseBehaviour.StartMoving();
            sampleAttempts++;
        } while (!isOnNavMesh && sampleAttempts < _wanderNavMeshSampleAttempts);

        if (!isOnNavMesh)
        {
            _chaseBehaviour.StopMoving();
        }
    }
}