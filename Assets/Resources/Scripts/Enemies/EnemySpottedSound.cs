using UnityEngine;
using Mirror;

[RequireComponent(typeof(FieldOfViewDetector))]
public class EnemyDetectionSound : MonoBehaviour
{
    [SerializeField] private AudioEvent _creepySound;
    [SerializeField] private float _detectionRange = 20f; // Range within which the sound can be heard
    [SerializeField] private float _detectionInterval = 0.5f; // Interval at which to check for players in range
    [SerializeField] private float _detectionRefreshTime = 30f; // Time after which the detection state resets if no players are detected
    private static float GlobalCooldown = 5f; // Global cooldown for the sound to prevent spamming
    FieldOfViewDetector _fovDetector;
    float _detectionTimer = 0f;
    bool _isPlayerDetected = false;
    bool _hasPlayedSound = false;
    float _detectionRefreshTimer = 0f;
    private static float LastSoundTime = -Mathf.Infinity; // Track the last time the sound was played globally

    void Awake()
    {
        _fovDetector = GetComponent<FieldOfViewDetector>();
        _detectionTimer = Random.Range(0f, _detectionInterval); // Randomize the initial timer to avoid synchronization
    }

    void Update()
    {
        CheckFOV();
        CheckRefresh();
    }
    void CheckFOV()
    {
        if (_detectionTimer < _detectionInterval)
        {
            _detectionTimer += Time.deltaTime;
            return; // Skip checking for players until the interval has passed
        }
        _detectionTimer = 0f; // Reset the timer

        _isPlayerDetected = _fovDetector.IsInFOV(_detectionRange);
        if (_isPlayerDetected && !_hasPlayedSound)
        {
            if (Time.time - LastSoundTime >= GlobalCooldown)
            {
                _creepySound.Play(transform.position); // Play the creepy sound at the object's position
                _hasPlayedSound = true; // Mark that the sound has been played to avoid repetition
                LastSoundTime = Time.time; // Update the last sound time
            }
            else
            {
                _hasPlayedSound = true; // Mark that the sound has been played to avoid repetition, even if it wasn't actually played due to cooldown
            }
        }
    }
    void CheckRefresh()
    {
        if (_isPlayerDetected)
        {
            _detectionRefreshTimer = 0f; // Reset the refresh timer if a player is detected
        }
        else if (_hasPlayedSound)
        {
            _detectionRefreshTimer += Time.deltaTime;
            if (_detectionRefreshTimer >= _detectionRefreshTime)
            {
                _hasPlayedSound = false; // Reset the sound played state after the refresh time
                _detectionRefreshTimer = 0f; // Reset the refresh timer
            }
        }
    }
}