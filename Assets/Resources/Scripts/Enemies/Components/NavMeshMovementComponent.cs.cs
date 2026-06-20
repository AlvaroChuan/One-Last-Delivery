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
    private NavMeshAgent _navMeshAgent;
    private bool _isMoving = false;
    public bool IsMoving => _isMoving;
    private bool _canMove = true;
    public bool CanMove
    {
        get => _canMove;
        set
        {
            _canMove = value;
            if (!_canMove)
            {
                StopMoving();
            }
        }
    }
    Vector3 _targetPosition;

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
        if (_isMoving && _target != null)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
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
            if (_isMoving)
            {
                _navMeshAgent.SetDestination(_targetPosition);
            }
        }
    }

    public void StartMoving()
    {
        if (_isMoving) return; // Already moving

        _isMoving = true;
        if (_target != null)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
        }
        else if (_targetPosition != Vector3.zero)
        {
            _navMeshAgent.SetDestination(_targetPosition);
        }
    }

    public void StopMoving()
    {
        _isMoving = false;
        _navMeshAgent.ResetPath();
        _navMeshAgent.velocity = Vector3.zero; // Stop any residual movement
    }

    void Update()
    {
        if (!_isMoving || _target == null || !_canMove) return;

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
        else if (_targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _targetPosition);
        }
    }
#endif
}