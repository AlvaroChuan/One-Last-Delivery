using UnityEngine;
using Mirror;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaseBehaviour : MonoBehaviour
{
    [SerializeField] float _recalculationInterval = 0.5f; // Time in seconds between path recalculations
    private float _recalculationTimer = 0f;
    private GameObject _target;
    public GameObject Target => _target;
    private NavMeshAgent _navMeshAgent;
    private bool _isChasing = false;
    public void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if(!NetworkServer.active)
        {
            enabled = false;
        }
    }

    public void SetTarget(GameObject target)
    {
        if (_target == target) return; // No change in target

        _target = target;
        if (_isChasing && _target != null)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
        }
    }

    public void StartChasing()
    {
        if (_isChasing) return; // Already chasing

        _isChasing = true;
        if (_target != null)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
        }
    }

    public void StopChasing()
    {
        _isChasing = false;
        _navMeshAgent.ResetPath();
    }

    void Update()
    {
        if (!_isChasing || _target == null) return;

        _recalculationTimer += Time.deltaTime;
        if (_recalculationTimer >= _recalculationInterval)
        {
            _navMeshAgent.SetDestination(_target.transform.position);
            _recalculationTimer = 0f;
        }
    }
}