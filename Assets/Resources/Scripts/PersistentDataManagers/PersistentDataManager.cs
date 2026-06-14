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

public abstract class PersistentDataManager<T, TStaticState, TDataType> : NetworkBehaviour
    where T : PersistentDataManager<T, TStaticState, TDataType>
    where TStaticState : PersistentDataManager<T, TStaticState, TDataType>.StaticStateBase, new()
    where TDataType : struct
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
                _staticData = value;
                if (_managerInstance != null && NetworkServer.active)
                {
                    _managerInstance.ServerUpdateInstanceData();
                }
            }
        }
        public void SetManagerInstance(PersistentDataManager<T, TStaticState, TDataType> manager)
        {
            _managerInstance = manager;
        }
        public PersistentDataManager<T, TStaticState, TDataType> GetManagerInstance()
        {
            return _managerInstance;
        }
        public abstract void Reset();
    }
    [SerializeField] protected string[] _activeSceneNames;
    private static string[] StaticActiveSceneNames;

    public Action<DataChangeInfo> onDataChangedEvent;

    protected static readonly TStaticState StaticDataState = new TStaticState();

    protected virtual void Awake()
    {
        StaticDataState.SetManagerInstance(this);
        PersistentDataSceneRegistry.CentralOnSceneLoaded -= OnSceneChange;
        PersistentDataSceneRegistry.CentralOnSceneLoaded += OnSceneChange;
        StaticActiveSceneNames = _activeSceneNames;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerInitializeStaticData();
    }

    protected abstract void ServerInitializeStaticData();

    abstract protected void ServerUpdateInstanceData();

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