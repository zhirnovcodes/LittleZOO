using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ScriptableObject to configure simulation settings in the Unity Editor
[CreateAssetMenu(fileName = "SimulationConfig", menuName = "Simulation/Config")]
public class SimulationConfigAsset : ScriptableObject
{
    // Needs section
    [Header("Pigs Needs")]
    public Vector2 HungerDecayFactor = new Vector2(0.01f, 0.05f);
    public Vector2 EnergyDecayFactor = new Vector2(0.005f, 0.02f);

    // World section
    [Header("World Settings")]
    public Vector2Int PigsCount = new Vector2Int(10, 20);
    public Vector2 PigsSpawnHeight = new Vector2(0.5f, 1.5f);
    public Vector2Int GrassCount = new Vector2Int(50, 100);
    public int GrassReproductionSteps = 10;
    public float PlanetRadius = 50f;
    public float HorizontalDrag = 100f;
    public float GravityForce = 9.81f;

    // Actions section
    [Header("Pig Actions")]
    public Vector2 eatInterval = new Vector2(5f, 10f);
    public Vector2 biteWholeness = new Vector2(0.1f, 0.3f);
    public Vector2 searchMinValuableFullness = new Vector2(0.3f, 0.6f);
    public Vector2 minValuableEnergy = new Vector2(5f, 10f);

    // Advertisers section
    [Header("Grass Advertiser")]
    public Vector2 grassSizeMax = new Vector2(0.5f, 1.5f);
    public Vector2 grassFullnessAdvertised = new Vector2(-0.1f, 1f);
    public Vector2 grassEnergyAdvertised = new Vector2(0.1f, -1f);

    // Actors constants section
    [Header("Pig Constants")]
    public int foodPreference = 1;  // 1 = grass
    public Vector2 pigSpeed = new Vector2(1f, 3f);
    public Vector2 pigSize = new Vector2(0.8f, 1.2f);
    public Vector2 visionInterval = new Vector2(0.2f, 0.5f);
    public Vector2 visionRadius = new Vector2(5f, 10f);

    // Entities constants section
    [Header("Grass Constants")]
    public Vector2 grassSize = new Vector2(0.2f, 1f);
    public Vector2 grassWholenessMin = new Vector2(1f, 100f);
    public Vector2 grassWholenessMax = new Vector2(1f, 100f);
    public Vector2 grassStatNutrition = new Vector2(5f, 15f);
    public Vector2 grassGrowthSpeed = new Vector2(5f, 15f);
    public Vector2 grassAgingSpan = new Vector2(5f, 15f);
    public Vector2 grassAgingFunctionHeight = new Vector2(0.02f, 1f);
    public Vector2 grassReproductionInterval = new Vector2(0.5f, 4f);
    public Vector2 grassReproductionSpan = new Vector2(5f, 15f);
    public Vector2 grassReproductionFunctionHeight = new Vector2(0.02f, 1f);
    public Vector2 grassReproductionChance = new Vector2(0.7f, 1f);

    [Header("Animation data - Pigs")]
    public float PigSleepingTime = 1;
    public float PigDyingTime = 1;
    public float PigComposingTime = 3;

    // Convert to SimulationSettings struct
    public SimulationSettings ToSimulationSettings(in PrefabsLibraryComponent library)
    {
        SimulationSettings settings = new SimulationSettings
        {
            // Needs
            Needs = new PigsNeedsData
            {
                HungerDecayFactor = HungerDecayFactor,
                EnergyDecayFactor = EnergyDecayFactor
            },

            // World
            World = new WorldData
            {
                PigsSpawn = new SpawnData
                {
                    Prefab = library.Pig,
                    Count = new int2(PigsCount.x, PigsCount.y),
                    SpawnHeight = new float2(PigsSpawnHeight.x, PigsSpawnHeight.y)
                },
                GrassSpawn = new SpawnData
                {
                    Prefab = library.Grass,
                    Count = new int2(GrassCount.x, GrassCount.y),
                },
                GrassReproductionSteps = GrassReproductionSteps,
                PlanetRadius = PlanetRadius,
                HorizontalDrag = HorizontalDrag,
                GravityForce = GravityForce
            },

            // Actions
            Actions = new ActionsData
            {
                Pigs = new PigsActionsData
                {
                    EatInterval = new float2(eatInterval.x, eatInterval.y),
                    BiteWholeness = new float2(biteWholeness.x, biteWholeness.y),
                    SearchMinValuableFullness = new float2(searchMinValuableFullness.x, searchMinValuableFullness.y),
                    MinValuableEnnergy = new float2(minValuableEnergy.x, minValuableEnergy.y)
                }
            },

            // Advertisers
            Advertisers = new AdvertisersData
            {
                Grass = new GrassAdvertiserData
                {
                    SizeMax = grassSizeMax,
                    EnergyValue = grassEnergyAdvertised,
                    FullnessValue = grassFullnessAdvertised
                }
            },

            // Actors constants
            Actors = new ActorsData
            {
                Pigs = new PigsData
                {
                    ObjectType = ObjectType.Pig,
                    ActorType = ActorsType.Pig,
                    FoodPreference = foodPreference,
                    Stats = new PigsStatsData
                    {
                        Speed = new float2(pigSpeed.x, pigSpeed.y),
                        Size = new float2(pigSize.x, pigSize.y),
                        VisionInterval = new float2(visionInterval.x, visionInterval.y),
                        VisionRadius = new float2(visionRadius.x, visionRadius.y)
                    }
                }
            },

            // Entities constants
            Entities = new ObjectsData
            {
                Grass = new GrassData
                {
                    FoodType = FoodType.Grass,
                    Stats = new GrassStatsData
                    {
                        Size = grassSize,
                        MinWholeness = grassWholenessMin,
                        MaxWholeness = grassWholenessMax,
                        Nutrition = grassStatNutrition,
                        GrowthSpeed = grassGrowthSpeed,
                        AgingFunctionHeight = grassAgingFunctionHeight,
                        AgingFunctionSpan = grassAgingSpan,
                        ReproductionHeight = grassReproductionFunctionHeight,
                        ReproductionInterval = grassReproductionInterval,
                        ReproductionSpan = grassReproductionSpan,
                        ReproductionChance = grassReproductionChance,
                    }
                }
            },

            AnimationData = new AnimationData
            {
                PigData = new AnimationData.Pig
                {
                    ComposingTime = PigComposingTime,
                    DyingTime = PigDyingTime
                }
            }
        };
        return settings;
    }
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

// Component to store the BLOB reference
public struct SimulationConfigComponent : IComponentData
{
    public BlobAssetReference<SimulationSettings> BlobReference;
}