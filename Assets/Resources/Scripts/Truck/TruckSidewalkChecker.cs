using UnityEngine;
using Mirror;

public class TruckSidewalkChecker : NetworkBehaviour
{
    [SerializeField] private float _sidewalkFineAmount = 10f; // Fine amount for driving on the sidewalk
    [SerializeField] private float _timeOnSidewalkThreshold = 2f; // Time threshold in seconds before issuing a fine
    [SerializeField] private float _sidewalkHeight = 0.15f; // Height threshold to determine if the truck is on the sidewalk
    [SerializeField] private float _initialGracePeriod = 10f; // Initial grace period in seconds before fines can be issued

    private bool _hasLeftSidewalkForTheFirstTime = false; // Flag to track if the truck has left the sidewalk for the first time
    private float _timeOnSidewalk = 0f;
    private float _gracePeriod = 0f;

    void Update()
    {
        if (!isServer) return;
        if (_gracePeriod < _initialGracePeriod)
        {
            _gracePeriod += Time.deltaTime;
            return; // Skip checking for fines during the initial grace period
        }

        if (IsTruckOnSidewalk())
        {
            if (!_hasLeftSidewalkForTheFirstTime)
            {
                return; // Do not issue a fine if the truck has not left the sidewalk for the first time
            }
            _timeOnSidewalk += Time.deltaTime;

            if (_timeOnSidewalk >= _timeOnSidewalkThreshold)
            {
                // Issue a fine to the player for driving on the sidewalk
                BalanceManager.RegisterTransaction("Driving on the sidewalk", -_sidewalkFineAmount);
                _timeOnSidewalk = 0f; // Reset the timer after issuing the fine
            }
        }
        else
        {
            if(!_hasLeftSidewalkForTheFirstTime)
            {
                DevLogger.Log("Truck has left the sidewalk for the first time.");
                _hasLeftSidewalkForTheFirstTime = true; // Mark that the truck has left the sidewalk for the first time
            }
            _hasLeftSidewalkForTheFirstTime = true; // Mark that the truck has left the sidewalk for the first time
            _timeOnSidewalk = 0f; // Reset the timer if the truck is not on the sidewalk
        }
    }

    private bool IsTruckOnSidewalk()
    {
        // Check if the truck's position is above the sidewalk height
        return transform.position.y > _sidewalkHeight;
    }
}