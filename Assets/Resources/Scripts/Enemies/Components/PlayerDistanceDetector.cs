using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PlayerDistanceDetector : MonoBehaviour
{
    public Action onTargetDeathEvent;
    GameObject _closestPlayer;
#if UNITY_EDITOR
    private float _cachedDetectionRadius = -1f;
    private void OnDrawGizmos()
    {
        if (_cachedDetectionRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _cachedDetectionRadius);
        }
    }
#endif

    public GameObject DetectClosestPlayer(float detectionRadius)
    {

#if UNITY_EDITOR
        _cachedDetectionRadius = detectionRadius;
#endif

        List<GameObject> players = PlayerRegistry.SpawnedPlayers.ToList();
        if (players == null || players.Count == 0)
            return null;

        GameObject closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (var player in players)
        {
            if (player == null) continue; // Skip if the player reference is null

            if (player.TryGetComponent<PlayerDeathComponent>(out var deathComponent) && deathComponent.IsDead)
            {
                continue; // Skip dead players
            }

            Vector3 playerPosition = player.transform.position;
            Vector3 detectorPosition = transform.position;
            playerPosition.y = 0f; // Ignore vertical distance
            detectorPosition.y = 0f; // Ignore vertical distance

            float distance = Vector3.Distance(detectorPosition, playerPosition);
            if (distance < closestDistance && distance <= detectionRadius)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        if (closestPlayer != _closestPlayer)
        {
            if (_closestPlayer != null && _closestPlayer.TryGetComponent<PlayerDeathComponent>(out var previousDeathComponent))
            {
                previousDeathComponent.onPlayerDeathEvent -= OnTargetDeath;
            }

            _closestPlayer = closestPlayer;

            if (_closestPlayer != null && _closestPlayer.TryGetComponent<PlayerDeathComponent>(out var newDeathComponent))
            {
                newDeathComponent.onPlayerDeathEvent += OnTargetDeath;
            }
        }

        return closestPlayer;
    }

    void OnTargetDeath()
    {
        _closestPlayer = null;
        onTargetDeathEvent?.Invoke();
    }
}