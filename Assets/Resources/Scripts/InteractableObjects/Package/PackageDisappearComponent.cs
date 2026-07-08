using UnityEngine;
using Mirror;
using System.Collections;

public class PackageDisappearComponent : NetworkBehaviour
{
    [SerializeField] ParticleSystem _disappearEffect;

#if UNITY_EDITOR
    [ContextMenu("Disappear Package")]
    void ContextMenuDisappear()
    {
        StartDisappear();
    }
#endif

    [Server]
    public void StartDisappear()
    {
        Instantiate(_disappearEffect, transform.position, Quaternion.identity);
        NetworkServer.Destroy(gameObject);
        NetworkServer.Spawn(_disappearEffect.gameObject);
    }
}