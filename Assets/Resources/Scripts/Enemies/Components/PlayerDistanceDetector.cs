using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerDistanceDetector : MonoBehaviour
{
    public GameObject DetectClosestPlayer(float detectionRadius)
    {
        List<GameObject> players = (NetworkManager.singleton as CustomNetworkManager)?.SpawnedPlayers;
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

        return closestPlayer;
    }
}