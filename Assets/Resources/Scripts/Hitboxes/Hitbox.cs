using UnityEngine;
using Mirror;
using System;

public class Hitbox : MonoBehaviour
{
    public Action<GameObject> onHit;

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return; // Ensure this logic only runs on the server

        onHit?.Invoke(other.gameObject);
    }
}