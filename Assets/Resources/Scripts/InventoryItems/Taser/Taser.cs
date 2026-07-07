using UnityEngine;
using Mirror;

public class Taser : InventoryItem
{
    [SerializeField] private AudioEvent _taserFireEvent;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    public override void StartUse(GameObject user)
    {
        _taserFireEvent.Play(transform.position); // Play taser fire sound
        if (!isLocalPlayer) return; // Only allow local player to use the taser

        CmdFireTaser();
    }
    [Command]
    private void CmdFireTaser()
    {
        GameObject taserProjectile = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);
        NetworkServer.Spawn(taserProjectile);
    }
}