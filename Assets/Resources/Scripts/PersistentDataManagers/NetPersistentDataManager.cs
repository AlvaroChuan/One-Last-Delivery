using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System;
public abstract class NetPersistentDataManager<T, TStaticState, TDataType> : NetworkBehaviour
    where T : NetPersistentDataManager<T, TStaticState, TDataType>
    where TStaticState : NetPersistentDataManager<T, TStaticState, TDataType>.StaticStateBase, new()
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
        private NetPersistentDataManager<T, TStaticState, TDataType> _managerInstance;
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
        public void SetManagerInstance(NetPersistentDataManager<T, TStaticState, TDataType> manager)
        {
            _managerInstance = manager;
        }
        public NetPersistentDataManager<T, TStaticState, TDataType> GetManagerInstance()
        {
            return _managerInstance;
        }
        public abstract void Reset();
    }
    [SerializeField] protected string[] _activeSceneNames;
    private static string[] StaticActiveSceneNames;

    public static Action<DataChangeInfo> OnDataChangedEvent;

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