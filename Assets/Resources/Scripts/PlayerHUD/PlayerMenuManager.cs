using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMenuManager : MonoBehaviour
{
    [System.Serializable]
    private struct MenuEntry
    {
        public GameObject menuPanel;
        public InputActionReference toggleAction;
    }

    [SerializeField] private MenuEntry[] _menuEntries;
    CinemachineInputAxisController _playerCamera;

    void Awake()
    {
        _playerCamera = FindAnyObjectByType<CinemachineInputAxisController>();
    }

    private void OnEnable()
    {
        foreach (var entry in _menuEntries)
        {
            entry.toggleAction.action.performed += OnMenuInput;
        }
    }

    private void OnDisable()
    {
        foreach (var entry in _menuEntries)
        {
            entry.toggleAction.action.performed -= OnMenuInput;
        }
    }

    private void OnMenuInput(InputAction.CallbackContext context)
    {
        GameObject menuPanel = null;
        foreach (var entry in _menuEntries)
        {
            if (context.action == entry.toggleAction.action)
            {
                menuPanel = entry.menuPanel;
            }
            else if (entry.menuPanel.activeSelf)
            {
                ToggleMenuPanel(entry.menuPanel, false); // Close other menus
            }
        }
        ToggleMenuPanel(menuPanel, !menuPanel.activeSelf); // Toggle the selected menu
    }

    void ToggleMenuPanel(GameObject menuPanel, bool open)
    {
        if (menuPanel == null)
        {
            Debug.LogError("Menu panel is null.");
            return;
        }
        menuPanel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
        GameObject player = NetworkClient.connection.identity.gameObject;
        if (player != null)
        {
            PlayerItemUseComponent itemUseComponent = player.GetComponent<PlayerItemUseComponent>();
            if (itemUseComponent != null)
            {
                itemUseComponent.enabled = !open;
            }
        }
        if (_playerCamera != null)
        {
            _playerCamera.enabled = !open;
        }
    }
}