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
        GameObject effect = Instantiate(_disappearEffect, transform.position, Quaternion.identity).gameObject;
        NetworkServer.Destroy(gameObject);
        NetworkServer.Spawn(effect);
    }
}