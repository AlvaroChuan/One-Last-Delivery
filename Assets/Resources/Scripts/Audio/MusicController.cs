using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicController : MonoBehaviour
{
    [SerializeField] private string _mainMenuSceneName = "GraphicsMainMenu";
    [SerializeField] private string _gameSceneName = "GameScene";
    [SerializeField] private string _balanceSceneName = "BalanceScene";

    private void Start()
    {
        // Determine which music to play based on the current scene
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == _mainMenuSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.MainMenu);
        }
        else if (currentSceneName == _gameSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.Daytime);
        }
        else if (currentSceneName == _balanceSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.Balance);
        }

        SunManager.OnNightfall += OnNightfall;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Determine which music to play based on the newly loaded scene
        if (scene.name == _mainMenuSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.MainMenu);
        }
        else if (scene.name == _gameSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.Daytime);
        }
        else if (scene.name == _balanceSceneName)
        {
            MusicPlayer.Instance.PlayMusic(MusicClipID.Balance);
        }
    }

    private void OnNightfall()
    {
        MusicPlayer.Instance.PlayMusic(MusicClipID.Nighttime);
    }

    private void OnDestroy()
    {
        SunManager.OnNightfall -= OnNightfall;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}