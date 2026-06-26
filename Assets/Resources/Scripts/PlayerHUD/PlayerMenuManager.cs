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
        public bool canOpenWhileAlive;
        public bool canOpenWhileDead;
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
        MenuEntry menuEntry = default;
        foreach (var entry in _menuEntries)
        {
            if (context.action == entry.toggleAction.action)
            {
                menuEntry = entry;
            }
            else if (entry.menuPanel.activeSelf)
            {
                ToggleMenuPanel(entry, false); // Close other menus
            }
        }
        ToggleMenuPanel(menuEntry, !menuEntry.menuPanel.activeSelf); // Toggle the selected menu
    }

    void ToggleMenuPanel(MenuEntry menuEntry, bool open)
    {
        GameObject menuPanel = menuEntry.menuPanel;

        if (menuPanel == null)
        {
            Debug.LogError("Menu panel is null.");
            return;
        }

        if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
        {
            Debug.LogError("No local player found.");
            return;
        }

        GameObject player = NetworkClient.connection.identity.gameObject;

        if (player == null)
        {
            Debug.LogError("Local player identity is null.");
            return;
        }

        if (player.TryGetComponent(out PlayerDeathComponent deathComponent))
        {
            if (deathComponent.IsDead && !menuEntry.canOpenWhileDead)
            {
                Debug.Log("Cannot open this menu while dead.");
                return;
            }
            else if (!deathComponent.IsDead && !menuEntry.canOpenWhileAlive)
            {
                Debug.Log("Cannot open this menu while alive.");
                return;
            }
        }

        menuEntry.menuPanel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
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