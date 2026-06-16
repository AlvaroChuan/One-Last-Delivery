using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System;

internal static class PersistentDataSceneRegistry
{
    public static event Action<Scene, LoadSceneMode> CentralOnSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSceneChangeListener()
    {
        DevLogger.Log("Initializing PersistentDataSceneRegistry and subscribing to sceneLoaded event.");
        SceneManager.sceneLoaded -= OnGlobalSceneChange;
        SceneManager.sceneLoaded += OnGlobalSceneChange;
    }

    private static void OnGlobalSceneChange(Scene scene, LoadSceneMode mode)
    {
        CentralOnSceneLoaded?.Invoke(scene, mode);
    }
}

public abstract class PersistentDataManager<T, TStaticState, TDataType> : MonoBehaviour
    where T : PersistentDataManager<T, TStaticState, TDataType>
    where TStaticState : PersistentDataManager<T, TStaticState, TDataType>.StaticStateBase, new()
{
    public struct DataChangeInfo
    {
        public TDataType oldValue;
        public TDataType newValue;
    }
    // --- Pure C# Static State Container ---
    // This retains data in memory when the GameScene reloads.
    public abstract class StaticStateBase
    {
        private PersistentDataManager<T, TStaticState, TDataType> _managerInstance;
        private TDataType _staticData;
        public TDataType StaticData
        {
            get => _staticData;
            set
            {
                TDataType oldValue = _staticData;
                _staticData = value;
                if (_managerInstance != null)
                {
                    _managerInstance.onDataChangedEvent?.Invoke(new DataChangeInfo { oldValue = oldValue, newValue = value });
                }
            }
        }
        public abstract void Reset();
        public void SetManagerInstance(PersistentDataManager<T, TStaticState, TDataType> manager)
        {
            _managerInstance = manager;
        }
        public PersistentDataManager<T, TStaticState, TDataType> GetManagerInstance()
        {
            return _managerInstance;
        }
    }
    [SerializeField] protected string[] _activeSceneNames;
    private static string[] StaticActiveSceneNames;

    public Action<DataChangeInfo> onDataChangedEvent;

    protected static readonly TStaticState StaticDataState = new TStaticState();

    protected virtual void Awake()
    {
        PersistentDataSceneRegistry.CentralOnSceneLoaded -= OnSceneChange;
        PersistentDataSceneRegistry.CentralOnSceneLoaded += OnSceneChange;
        StaticActiveSceneNames = _activeSceneNames;
        StaticDataState.SetManagerInstance(this);
    }

    void OnDestroy()
    {
        if (StaticDataState.GetManagerInstance() == this)
        {
            StaticDataState.SetManagerInstance(null);
        }
    }

    private static void OnSceneChange(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (StaticActiveSceneNames == null) return;

        bool isActiveScene = Array.Exists(StaticActiveSceneNames, name => name == scene.name);
        if (!isActiveScene)
        {
            StaticDataState.SetManagerInstance(null);
            StaticDataState.Reset();
            PersistentDataSceneRegistry.CentralOnSceneLoaded -= OnSceneChange;
            StaticActiveSceneNames = null;
        }
    }
}