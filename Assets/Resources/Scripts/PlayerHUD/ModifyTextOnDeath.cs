using UnityEngine;
using Mirror;
using TMPro;

public class ModifyTextOnDeath : MonoBehaviour
{
    [TextArea(3, 10)]
    [SerializeField] private string _newTextOnDeath = "";
    [SerializeField] private TextMeshProUGUI _textComponent;
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
        _textComponent.text = _newTextOnDeath;
    }

    void OnDestroy()
    {
        if (_playerDeathComponent != null)
        {
            _playerDeathComponent.onPlayerDeathEvent -= OnPlayerDeath;
        }
    }
}