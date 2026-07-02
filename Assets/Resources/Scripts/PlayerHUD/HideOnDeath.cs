using UnityEngine;
using Mirror;

public class HideOnDeath : MonoBehaviour
{
    private PlayerDeathComponent _playerDeathComponent;

    void Update()
    {
        if (_playerDeathComponent == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            _playerDeathComponent = NetworkClient.connection.identity.GetComponent<PlayerDeathComponent>();
            if (_playerDeathComponent != null)
            {
                DevLogger.Log("PlayerDeathComponent found and subscribed to onPlayerDeathEvent.");
                _playerDeathComponent.onPlayerDeathEvent += OnPlayerDeath;
            }
        }
    }

    private void OnPlayerDeath()
    {
        gameObject.SetActive(false); // Hide the GameObject when the player is dead
    }

    void OnDestroy()
    {
        if (_playerDeathComponent != null)
        {
            _playerDeathComponent.onPlayerDeathEvent -= OnPlayerDeath;
        }
    }
}