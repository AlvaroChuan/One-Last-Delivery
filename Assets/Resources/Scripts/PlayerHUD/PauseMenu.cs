using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _optionsMenu;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private PauseMenuHandler _pauseMenuHandler;

    void Awake()
    {
        _pauseMenuHandler = FindAnyObjectByType<PauseMenuHandler>();
    }

    public void OnResumeButtonClicked()
    {
        _pauseMenuHandler.CmdTogglePause();
    }

    public void OnOptionsButtonClicked()
    {
        _pauseMenu.SetActive(false);
        _optionsMenu.SetActive(true);
    }

    public void OnBackButtonClicked()
    {
        _pauseMenu.SetActive(true);
        _optionsMenu.SetActive(false);
    }

    public void OnQuitButtonClicked()
    {
        _pauseMenuHandler.CmdQuitGame();
    }

    void OnDisable()
    {
        _pauseMenu.SetActive(true);
        _optionsMenu.SetActive(false);
    }
}