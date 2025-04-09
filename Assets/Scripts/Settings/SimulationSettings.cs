using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Main BLOB asset structure
[System.Serializable]
public struct SimulationSettings
{
    // Needs section
    public PigsNeedsData Needs;

    // World section
    public WorldData World;

    // Actions section
    public ActionsData Actions;

    // Advertisers section
    public AdvertisersData Advertisers;

    // Actors constants section
    public ActorsData Actors;

    // Entities constants section
    public ObjectsData Entities;

    // Animation Data
    public AnimationData AnimationData;
}

// Needs - Pigs section
[System.Serializable]
public struct PigsNeedsData
{
    public float2 FullnessNaturalDecay;
    public float2 EnergyNaturalDecay;
    public float2 FullnessDecayByDistance;
    public float2 EnergyDecayByDistance;
}

// World section
[System.Serializable]
public struct WorldData
{
    public SpawnData PigsSpawn;
    public SpawnData GrassSpawn;
    public SpawnData WolfSpawn;
    public int GrassReproductionSteps;
    public float PlanetRadius;
    public float HorizontalDrag;
    public float GravityForce;
}

// Actions section
[System.Serializable]
public struct ActionsData
{
    public PigsActionsData Pigs;
}

// Advertisers section
[System.Serializable]
public struct AdvertisersData
{
    public GrassAdvertiserData Grass;
}

// Actors constants section
[System.Serializable]
public struct ActorsData
{
    public PigsData Pigs;
}

// Entities constants section
[System.Serializable]
public struct ObjectsData
{
    public GrassData Grass;
}

// Spawn data structure
[System.Serializable]
public struct SpawnData
{
    public Entity Prefab;
    public int2 Count; // x = min, y = max
    public float2 SpawnHeight; // x = min, y = max
}

// Pigs Actions Data
[System.Serializable]
public struct PigsActionsData
{
    public float2 EatInterval;
    public float2 BiteWholeness;
    public float2 SearchMinValuableFullness;
    public float2 MinValuableEnnergy;
}

// Grass Advertiser Data
[System.Serializable]
public struct GrassAdvertiserData
{
    public float2 Nutrition;
    public float2 SizeMax;
    public float3 FullnessValueMin;
    public float3 FullnessValueMax;
    public float3 EnergyValueMin;
    public float3 EnergyValueMax;
}

// Pigs Constants Data
[System.Serializable]
public struct PigsData
{
    public ObjectType ObjectType;
    public ActorsType ActorType;
    public int FoodPreference;
    public PigsStatsData Stats;
}

// Pigs Stats Data
[System.Serializable]
public struct PigsStatsData
{
    public float2 SpeedMin;
    public float2 SpeedMax;
    public float2 Size;
    public float2 VisionInterval;
    public float2 VisionRadius;
    public float2 SafetyInterval;
}

// Grass Constants Data
[System.Serializable]
public struct GrassData
{
    public FoodType FoodType;
    public GrassStatsData Stats;
}

// Grass Stats Data
[System.Serializable]
public struct GrassStatsData
{
    public float2 Size;
    public float2 MinWholeness;
    public float2 MaxWholeness;
    public float2 Nutrition;
    public float2 GrowthSpeed;
    public float2 AgingFunctionSpan;
    public float2 AgingFunctionHeight;
    public float2 ReproductionSpan;
    public float2 ReproductionHeight;
    public float2 ReproductionInterval;
    public float2 ReproductionChance;
}

// AnimationData
[System.Serializable]
public struct AnimationData
{
    public Pig PigData;

    public struct Pig
    {
        public float ComposingTime;
        public float DyingTime;
    }
}

// 

// Enum definitions
public enum ObjectType { None, Pig, Grass }
public enum ActorsType { None, Pig, Wolf }
public enum FoodType { None, Grass }


// Component to store the BLOB reference
public struct SimulationConfigComponent : IComponentData
{
    public BlobAssetReference<SimulationSettings> BlobReference;
}

// Example system that uses the BLOB
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class SimulationInitSystem : SystemBase
{
    public BlobAssetReference<SimulationSettings> SimulationBlob { get; private set; }

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
        var configAsset = Resources.Load<SimulationConfigAsset>("Config/SimulationConfig");
        if (configAsset == null)
        {
            Debug.LogError("SimulationConfig asset not found in Resources folder");
            return;
        }

        var library = SystemAPI.GetSingleton<PrefabsLibraryComponent>();

        // Convert ScriptableObject to SimulationSettings struct
        var settings = configAsset.ToSimulationSettings(library);

        // Create the BLOB asset
        SimulationBlob = CreateSimulationBlob(settings);

        // Store BLOB reference in the config component for other systems to access
        var configEntity = SystemAPI.GetSingletonEntity<PrefabsLibraryComponent>();
        EntityManager.AddComponentData(configEntity,
            new SimulationConfigComponent { BlobReference = SimulationBlob });

        // Log success
        Debug.Log("Simulation configuration BLOB created successfully");
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

    private static BlobAssetReference<SimulationSettings> CreateSimulationBlob(SimulationSettings settings)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<SimulationSettings>();

        // Copy all data from settings to blob
        root = settings;

        // Create the blob asset
        var blobAsset = builder.CreateBlobAssetReference<SimulationSettings>(Allocator.Persistent);
        builder.Dispose();

        return blobAsset;
    }
}