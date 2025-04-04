using Unity.Collections;
using Unity.Entities;
using UnityEngine;
// Main BLOB asset structure
[System.Serializable]
public struct ActionChainSettings
{
    public int ActionsCount;
    public int SubActionsCount;

    public BlobArray<int> ActionsMap;
}

// Component to store the BLOB reference
public struct ActionChainConfigComponent : IComponentData
{
    public BlobAssetReference<ActionChainSettings> BlobReference;
}

// Example system that uses the BLOB
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ActionChainInitSystem : SystemBase
{
    public BlobAssetReference<ActionChainSettings> SimulationBlob { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<PrefabsLibraryComponent>()));
    }

    protected override void OnUpdate()
    {
        // Disable this system as it's only needed once
        Enabled = false;

        // Get the config from resources
        var configAsset = Resources.Load<ActionChainConfigAsset>("Config/ActionChainConfig");
        if (configAsset == null)
        {
            Debug.LogError("SimulationConfig asset not found in Resources folder");
            return;
        }

        // Convert ScriptableObject to SimulationSettings struct
        SimulationBlob = configAsset.ToSettings();


        var configEntity = SystemAPI.GetSingletonEntity<PrefabsLibraryComponent>();
        EntityManager.AddComponentData(configEntity,
            new ActionChainConfigComponent { BlobReference = SimulationBlob });

        // Log success
        Debug.Log("Action chain BLOB created successfully");
    }

    protected override void OnDestroy()
    {
        // Clean up the BLOB asset when the system is destroyed
        if (SimulationBlob.IsCreated)
        {
            SimulationBlob.Dispose();
        }

        base.OnDestroy();
    }
}