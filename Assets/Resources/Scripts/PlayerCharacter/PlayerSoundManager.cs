using System;
using Mirror;
using UnityEngine;
using UnityEngine.XR;

public class PlayerSoundManager : MonoBehaviour
{
    private enum SoundType
    {
        InventorySelection,
        Hurt
    }
    [SerializeField] private AudioEvent _inventorySelectionAudioEvent;
    [SerializeField] private AudioEvent _hurtEvent;
    [SerializeField] private AudioEvent _batHitAudioEvent;

    private void Awake()
    {
        GetComponent<PlayerInventoryComponent>().onInventorySlotChangedOwner += HandleInventorySlotChanged;
        GetComponent<PlayerHealthComponent>().onHealthChanged += HandleHealthChanged;
        GetComponentInChildren<Bat>(includeInactive: true).onBatHitEvent += HandleBatHit;
    }

    void OnDestroy()
    {
        GetComponent<PlayerInventoryComponent>().onInventorySlotChangedOwner -= HandleInventorySlotChanged;
        GetComponent<PlayerHealthComponent>().onHealthChanged -= HandleHealthChanged;
        GetComponentInChildren<Bat>(includeInactive: true).onBatHitEvent -= HandleBatHit;
    }

    private void HandleBatHit(Vector3 hitPosition)
    {
        _batHitAudioEvent.Play(hitPosition);
    }

    private void HandleInventorySlotChanged(PlayerInventoryComponent.SlotChangeInfo info)
    {
        _inventorySelectionAudioEvent.Play(gameObject);
    }

    void HandleHealthChanged(PlayerHealthComponent.HealthChangeInfo info)
    {
        if (info.newHealth < info.oldHealth)
        {
            _hurtEvent.Play(gameObject);
        }
    }
}
