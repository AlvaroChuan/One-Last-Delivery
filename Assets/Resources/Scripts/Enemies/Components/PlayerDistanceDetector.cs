using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerDistanceDetector : MonoBehaviour
{
    [SerializeField] private float _detectionRadius = 10f; // Radius within which to detect players

    public GameObject DetectClosestPlayer()
    {
        List<GameObject> players = (NetworkManager.singleton as CustomNetworkManager)?.SpawnedPlayers;
        if (players == null || players.Count == 0)
            return null;

        GameObject closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (var player in players)
        {
            if (player == null) continue; // Skip if the player reference is null

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance && distance <= _detectionRadius)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }
}