using Mirror;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HideOnDeath))]
public class HealthBar : MonoBehaviour
{
    PlayerHealthComponent _playerHealthComponent;
    Image _healthBarImage;
    void Awake()
    {
        _healthBarImage = GetComponent<Image>();
    }

    void Update()
    {
        while (_playerHealthComponent == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            _playerHealthComponent = NetworkClient.connection.identity.GetComponent<PlayerHealthComponent>();
            if (_playerHealthComponent != null)
            {
                DevLogger.Log("PlayerHealthComponent found and subscribed to onHealthChanged.");
                _playerHealthComponent.onHealthChangedEvent += OnHealthChanged;
            }
        }
    }

    private void OnHealthChanged(PlayerHealthComponent.HealthChangeInfo info)
    {
        float healthPercentage = info.newHealth / info.maxHealth;
        _healthBarImage.fillAmount = Mathf.Clamp01(healthPercentage);
    }

    void OnDestroy()
    {
        if (_playerHealthComponent != null)
        {
            _playerHealthComponent.onHealthChangedEvent -= OnHealthChanged;
        }
    }
}
