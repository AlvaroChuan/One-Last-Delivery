using Mirror;
using UnityEngine;

public class ShopSoundManager : MonoBehaviour
{
    [SerializeField] private AudioEvent _purchaseSuccessAudioEvent;
    [SerializeField] private AudioEvent _purchaseFailedAudioEvent;

    void Start()
    {
        ItemShopSystem.Instance.onPurchaseEvent += HandlePurchase;
    }

    void OnDestroy()
    {
        if (ItemShopSystem.Instance != null)
        {
            ItemShopSystem.Instance.onPurchaseEvent -= HandlePurchase;
        }
    }

    private void HandlePurchase(ItemShopSystem.ItemPurchaseInfo info)
    {
        if (info.purchaseSuccessful)
        {
            _purchaseSuccessAudioEvent.Play(NetworkClient.connection.identity.gameObject);
        }
        else
        {
            _purchaseFailedAudioEvent.Play(NetworkClient.connection.identity.gameObject);
        }
    }
}