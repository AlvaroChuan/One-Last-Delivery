using UnityEngine;
using Mirror;

public class DisableOnDeath : MonoBehaviour
{
    private PlayerDeathComponent _cachedPlayerDeathComponent;
    void OnEnable()
    {
        if (_cachedPlayerDeathComponent != null)
        {
            if (_cachedPlayerDeathComponent.IsDead)
            {
                Debug.Log("Player is already dead. Disabling the GameObject immediately.");
                gameObject.SetActive(false);
            }
            else
            {
                _cachedPlayerDeathComponent.onPlayerDeathEvent += HandlePlayerDeath;
            }
        }
        else
        {
            Debug.LogWarning("Cached PlayerDeathComponent is null. Attempting to find it.");
            GameObject player = NetworkClient.connection.identity.gameObject;

            if (player == null)
            {
                Debug.LogError("Local player identity is null.");
                return;
            }

            if (player.TryGetComponent(out PlayerDeathComponent deathComponent))
            {
                if (deathComponent.IsDead)
                {
                    Debug.Log("Player is already dead. Disabling the GameObject immediately.");
                    gameObject.SetActive(false);
                }
                else
                {
                    _cachedPlayerDeathComponent = deathComponent;
                    _cachedPlayerDeathComponent.onPlayerDeathEvent += HandlePlayerDeath;
                }
            }
            else
            {
                Debug.LogError("PlayerDeathComponent not found on the local player.");
            }
        }
    }

    void OnDisable()
    {
        if (_cachedPlayerDeathComponent != null)
        {
            _cachedPlayerDeathComponent.onPlayerDeathEvent -= HandlePlayerDeath;
        }
    }

    void HandlePlayerDeath()
    {
        Debug.Log("Player has died. Disabling the GameObject.");
        gameObject.SetActive(false);
    }
}