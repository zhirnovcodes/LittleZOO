using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ScriptableObject to configure simulation settings in the Unity Editor
[CreateAssetMenu(fileName = "SimulationConfig", menuName = "Simulation/Config")]
public class SimulationConfigAsset : ScriptableObject
{

    // World section
    [Header("World Settings")]
    public Vector2Int PigsCount = new Vector2Int(10, 20);
    public Vector2 PigsSpawnHeight = new Vector2(0.5f, 1.5f);
    public Vector2Int GrassCount = new Vector2Int(50, 100);
    public Vector2Int WolvesCount = new Vector2Int(1, 2);
    public int GrassReproductionSteps = 10;

    // Physics section
    [Header("Physics Settings")]
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
    public Vector3 grassFullnessAdvertisedMin = new Vector3(-0.1f, 1f, 0);
    public Vector3 grassFullnessAdvertisedMax = new Vector3(-0.1f, 1f, 0);
    public Vector3 grassEnergyAdvertisedMin = new Vector3(0.1f, -1f, 0);
    public Vector3 grassEnergyAdvertisedMax = new Vector3(0.1f, -1f, 0);

    // Actors constants section
    [Header("Pig Constants")]
    public int foodPreference = 1;  // 1 = grass
    public Vector2 pigSpeedMin = new Vector2(1f, 3f);
    public Vector2 pigSpeedMax = new Vector2(1f, 3f);
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

    // Needs section
    [Header("Pigs Needs")]
    public Vector2 FullnessNaturalDecay = new Vector2(0.01f, 0.05f);
    public Vector2 EnergyNaturalDecay = new Vector2(0.005f, 0.02f);
    public Vector2 FullnessDecayByDistance = new Vector2(0.02f, 0.1f);
    public Vector2 EnergyDecayByDistance = new Vector2(0.01f, 0.4f);
    public Vector2 safetyInterval = new Vector2(0.2f, 1f);

    // Convert to SimulationSettings struct
    public SimulationSettings ToSimulationSettings(in PrefabsLibraryComponent library)
    {
        SimulationSettings settings = new SimulationSettings
        {
            // Needs
            Needs = new PigsNeedsData
            {
                FullnessNaturalDecay = FullnessNaturalDecay,
                EnergyNaturalDecay = EnergyNaturalDecay,
                FullnessDecayByDistance = FullnessDecayByDistance,
                EnergyDecayByDistance = EnergyDecayByDistance
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
                WolfSpawn = new SpawnData
                {
                    Prefab = library.Wolf,
                    Count = new int2(WolvesCount.x, WolvesCount.y),
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
                    EatInterval = eatInterval,
                    BiteWholeness = biteWholeness,
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
                    EnergyValueMin = grassEnergyAdvertisedMin,
                    EnergyValueMax = grassEnergyAdvertisedMax,
                    FullnessValueMin = grassFullnessAdvertisedMin,
                    FullnessValueMax = grassFullnessAdvertisedMax
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
                        SpeedMin = pigSpeedMin,
                        SpeedMax = pigSpeedMax,
                        Size = new float2(pigSize.x, pigSize.y),
                        VisionInterval = new float2(visionInterval.x, visionInterval.y),
                        VisionRadius = new float2(visionRadius.x, visionRadius.y),
                        SafetyInterval = safetyInterval
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