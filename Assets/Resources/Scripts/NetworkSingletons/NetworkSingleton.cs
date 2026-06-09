using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// A generic singleton class for networked objects that should persist across scenes. It automatically destroys itself if another instance already exists, and it destroys itself when the scene changes to a scene that is not in the specified list of active scenes.
/// </summary>
/// <typeparam name="T"></typeparam>
public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
{
    [SerializeField] protected string[] _activeSceneNames = new string[] { "GameScene" };
    [SerializeField] protected bool _oldInstanceTakesPrecedence = true;

    bool _isSubscribedToSceneChange = false;

    public static T Instance { get; private set; }

    public override void OnStartClient()
    {
        if (Instance != null && Instance != this)
        {
            if (_oldInstanceTakesPrecedence)
            {
                if (NetworkServer.active)
                {
                    NetworkServer.Destroy(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                if (NetworkServer.active)
                {
                    NetworkServer.Destroy(Instance.gameObject);
                }
                else
                {
                    Destroy(Instance.gameObject);
                }
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneChange;
                _isSubscribedToSceneChange = true;
            }
            return;
        }
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneChange;
        _isSubscribedToSceneChange = true;
    }

    protected virtual void OnSceneChange(Scene scene, LoadSceneMode mode)
    {
        if (!IsActiveScene())
        {
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (_isSubscribedToSceneChange)
        {
            SceneManager.sceneLoaded -= OnSceneChange;
        }
    }

    protected bool IsActiveScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        foreach (string gameSceneName in _activeSceneNames)
        {
            if (currentScene.name == gameSceneName)
            {
                return true;
            }
        }
        return false;
    }
}