using UnityEngine;

[RequireComponent(typeof(DoorInteractionComponent))]
public class DoorSound : MonoBehaviour
{
    [SerializeField] private AudioEvent _successfulDeliveryAudioEvent;
    [SerializeField] private AudioEvent _failedDeliveryAudioEvent;

    void OnEnable()
    {
        GetComponent<DoorInteractionComponent>().onDoorInteractionEvent += HandleDoorInteraction;
    }

    void OnDisable()
    {
        GetComponent<DoorInteractionComponent>().onDoorInteractionEvent -= HandleDoorInteraction;
    }

    private void HandleDoorInteraction(DoorInteractionComponent.DoorInteractionInfo info)
    {
        if (info.isSuccessful)
        {
            _successfulDeliveryAudioEvent.Play(gameObject);
        }
        else
        {
            _failedDeliveryAudioEvent.Play(gameObject);
        }
    }
}