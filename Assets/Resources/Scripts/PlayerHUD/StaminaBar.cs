using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    PlayerStaminaComponent _playerStaminaComponent;
    Image _staminaBarImage;

    void Awake()
    {
        _staminaBarImage = GetComponent<Image>();
    }

    void Update()
    {
        while (_playerStaminaComponent == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            _playerStaminaComponent = NetworkClient.connection.identity.GetComponent<PlayerStaminaComponent>();
            if (_playerStaminaComponent != null)
            {
                DevLogger.Log("PlayerStaminaComponent found and subscribed to onStaminaChanged.");
                _playerStaminaComponent.onStaminaChangedEvent += OnStaminaChanged;
            }
        }
    }

    private void OnStaminaChanged(PlayerStaminaComponent.StaminaChangeInfo info)
    {
        float staminaPercentage = info.newStamina / info.maxStamina;
        _staminaBarImage.fillAmount = Mathf.Clamp01(staminaPercentage);
    }
}