using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private PauseMenuHandler _pauseMenuHandler;

    public void OnResumeButtonClicked()
    {
        _pauseMenuHandler.CmdTogglePause();
    }

    public void OnQuitButtonClicked()
    {
        _pauseMenuHandler.CmdQuitGame();
    }
}