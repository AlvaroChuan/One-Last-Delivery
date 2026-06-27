using UnityEngine;
using Mirror;

public class TruckUpgradeManager : NetPersistentDataManager<TruckUpgradeManager, TruckUpgradeManager.TruckUpgradeStaticData, TruckStatsStruct>
{
    public class TruckUpgradeStaticData : StaticStateBase
    {
        public override void Reset()
        {
            StaticData = new TruckStatsStruct();
        }
    }

    private TruckController _truckController;

    protected override void ServerInitializeStaticData()
    {
        _truckController = FindAnyObjectByType<TruckController>();
        _truckController.SetUpgradeStats(StaticDataState.StaticData);
    }

    protected override void ServerUpdateInstanceData()
    {
        _truckController.SetUpgradeStats(StaticDataState.StaticData);
    }

    public static void AddUpgradeStats(TruckStatsStruct upgradeStats)
    {
        TruckStatsStruct currentStats = StaticDataState.StaticData;
        currentStats.Add(upgradeStats);
        StaticDataState.StaticData = currentStats;
    }
}