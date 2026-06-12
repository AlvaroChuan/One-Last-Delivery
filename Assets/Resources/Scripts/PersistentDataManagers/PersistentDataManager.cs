using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System;

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
                if (_managerInstance != null && _managerInstance.isServer)
                {
                    _managerInstance.ServerUpdateInstanceData();
                }
            }
        }
        public void SetManagerInstance(PersistentDataManager<T, TStaticState, TDataType> manager)
        {
            _managerInstance = manager;
        }
        public abstract void Reset();
    }
    [SerializeField] protected string[] _activeSceneNames;

    public Action<DataChangeInfo> onDataChangedEvent;

    protected static readonly TStaticState StaticDataState = new TStaticState();
    private static T Instance;

    void Awake()
    {
        Instance = this as T;
        StaticDataState.SetManagerInstance(this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerInitializeStaticData();
    }

    protected abstract void ServerInitializeStaticData();

    abstract protected void ServerUpdateInstanceData();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSceneChangeListener()
    {
        SceneManager.sceneLoaded += OnSceneChange;
    }

    private static void OnSceneChange(Scene scene, LoadSceneMode mode)
    {
        T instance = Instance;
        if (instance != null)
        {
            bool isActiveScene = Array.Exists(instance._activeSceneNames, name => name == scene.name);
            if (!isActiveScene)
            {
                StaticDataState.Reset();
            }
        }
    }
}