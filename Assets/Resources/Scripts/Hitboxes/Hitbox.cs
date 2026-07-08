using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    public Action<GameObject> onHit;

    private void OnTriggerEnter(Collider other)
    {
        onHit?.Invoke(other.gameObject);
    }

    public void EnableHitbox()
    {
        GetComponent<Collider>().enabled = true;
    }

    public void DisableHitbox()
    {
        GetComponent<Collider>().enabled = false;
    }
}