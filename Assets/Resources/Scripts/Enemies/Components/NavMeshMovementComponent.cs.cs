using UnityEngine;
using Mirror;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMovementComponent : NetworkBehaviour
{
    [SerializeField] float _recalculationInterval = 0.5f; // Time in seconds between path recalculations
    private float _recalculationTimer = 0f;
    private GameObject _target;
    public GameObject Target => _target;
    private Vector3? _targetPosition = null; // Nullable Vector3 to store the target position when using a position instead of a GameObject
    private NavMeshAgent _navMeshAgent;

    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _recalculationTimer = Random.Range(0f, _recalculationInterval); // Randomize the initial timer to avoid all enemies recalculating paths at the same time
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(!NetworkServer.active)
        {
            enabled = false;
        }
    }

    public void SetTarget(GameObject target)
    {
        if (_target == target) return; // No change in target

        _target = target;
        _targetPosition = null; // Clear the position target since we're using a GameObject now
        if (_target != null)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
        }
        else
        {
            _navMeshAgent.ResetPath();
        }
    }

    public void SetTarget(Vector3 targetPosition, float maxDistance, out bool success)
    {
        success = false;
        _target = null; // Clear the GameObject target since we're using a position
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            success = true;
            _targetPosition = hit.position;
            _navMeshAgent.SetDestination(_targetPosition.Value);
        }
    }

    void Update()
    {
        if (_target == null) return;

        _recalculationTimer += Time.deltaTime;
        if (_recalculationTimer >= _recalculationInterval)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
            _recalculationTimer = 0f;
        }
    }

    public void SetSpeed(float speed)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = speed;
        }
    }
    public void SetAcceleration(float acceleration)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.acceleration = acceleration;
        }
    }
    public void SetAngularSpeed(float angularSpeed)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.angularSpeed = angularSpeed;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _target.transform.position);
        }
        else if (_targetPosition.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _targetPosition.Value);
        }
    }
#endif
}