using UnityEngine;
using Mirror;

public class TruckSidewalkChecker : NetworkBehaviour
{
    [SerializeField] private float _sidewalkFineAmount = 10f; // Fine amount for driving on the sidewalk
    [SerializeField] private float _timeOnSidewalkThreshold = 2f; // Time threshold in seconds before issuing a fine
    [SerializeField] private float _sidewalkHeight = 0.15f; // Height threshold to determine if the truck is on the sidewalk
    [SerializeField] private float _gracePeriod = 10f; // Initial grace period from when the driver sits in the seat in seconds before fines can be issued
    [SerializeField] private TruckSeat _driverSeat;
    [SerializeField] private LayerMask _sidewalkLayer;
    [SerializeField] private float _sidewalkCheckRadius = 0.075f; // Radius for checking if the truck is on the sidewalk
    private float _timeOnSidewalk = 0f;
    private float _gracePeriodTimer = 0f;
    private bool _isDriverInSeat = false;
    private Collider[] _hitbuffer = new Collider[1];

    void Awake()
    {
        _driverSeat.onOccupantChanged += OnOccupantChanged;
    }
    void OnDestroy()
    {
        _driverSeat.onOccupantChanged -= OnOccupantChanged;
    }

    void OnOccupantChanged(GameObject oldOccupant, GameObject newOccupant)
    {
        if (newOccupant != null)
        {
            _isDriverInSeat = true;
            _gracePeriodTimer = 0f; // Reset the grace period timer when a new occupant sits in the driver seat
        }
        else
        {
            _isDriverInSeat = false;
            _timeOnSidewalk = 0f; // Reset the sidewalk timer when the driver leaves the seat
            _gracePeriodTimer = 0f; // Reset the grace period timer when the driver leaves the seat
        }
    }

    void Update()
    {
        if (!isServer) return;
        if (!_isDriverInSeat) return; // Only check for fines if the driver is in the seat
        if (_gracePeriodTimer < _gracePeriod)
        {
            _gracePeriodTimer += Time.deltaTime;
            return; // Skip checking for fines during the initial grace period
        }

        if (IsTruckOnSidewalk())
        {
            _timeOnSidewalk += Time.deltaTime;

            if (_timeOnSidewalk >= _timeOnSidewalkThreshold)
            {
                // Issue a fine to the player for driving outside the road
                BalanceManager.RegisterTransaction("Driving outside the road", -_sidewalkFineAmount);
                _timeOnSidewalk = 0f; // Reset the timer after issuing the fine
            }
        }
        else
        {
            _timeOnSidewalk = 0f; // Reset the timer if the truck is not on the sidewalk
        }
    }

    private bool IsTruckOnSidewalk()
    {
        if (transform.position.y < _sidewalkHeight) return false; // If the truck's position is below the sidewalk height, it's not on the sidewalk

        // Check if the truck is colliding with the sidewalk layer using a sphere overlap
        int colliderCount = Physics.OverlapSphereNonAlloc(transform.position, _sidewalkCheckRadius, _hitbuffer, _sidewalkLayer);
        DevLogger.Log($"{_hitbuffer[0]}");
        return colliderCount > 0; // If there are any colliders in the sidewalk layer, the truck is considered to be on the sidewalk
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _sidewalkCheckRadius);
    }
}