using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PauseMenuHandler : NetworkBehaviour
{
    [SerializeField] private GameObject _pauseMenuPanel;
    [SerializeField] private InputActionReference _togglePauseAction;
    [SyncVar(hook = nameof(OnPauseStateChanged))] private bool _isPaused;

    private void OnEnable()
    {
        _togglePauseAction.action.performed += OnTogglePause;
    }

    private void OnDisable()
    {
        _togglePauseAction.action.performed -= OnTogglePause;
    }

    private void OnTogglePause(InputAction.CallbackContext context)
    {
        CmdTogglePause();
    }
    [Command(requiresAuthority = false)]
    public void CmdTogglePause()
    {
        _isPaused = !_isPaused;
    }

    void OnPauseStateChanged(bool oldValue, bool newValue)
    {
        _pauseMenuPanel.SetActive(newValue);
        Time.timeScale = newValue ? 0f : 1f; // Pause or resume the game
        if (newValue)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdQuitGame()
    {
        NetworkManager.singleton.StopHost();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f; // Ensure the game is unpaused when the object is destroyed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}