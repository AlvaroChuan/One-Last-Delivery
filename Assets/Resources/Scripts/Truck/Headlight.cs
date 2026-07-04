using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class Headlight : NetworkBehaviour
{
    [SerializeField] private InputActionReference _toggleHeadlightAction;
    private Light _headlight;
    [SyncVar(hook = nameof(OnHeadlightStateChanged))] private bool _isOn;

    private void Awake()
    {
        _headlight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        _toggleHeadlightAction.action.performed += OnToggleHeadlight;
    }

    private void OnDisable()
    {
        _toggleHeadlightAction.action.performed -= OnToggleHeadlight;
    }

    private void OnToggleHeadlight(InputAction.CallbackContext context)
    {
        if (!isOwned) return;
        _isOn = !_isOn;
    }

    private void OnHeadlightStateChanged(bool oldValue, bool newValue)
    {
        _headlight.enabled = newValue;
    }
}
