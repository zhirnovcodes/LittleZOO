using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
    public TuningParameter HungerDecayFactor;
    public TuningParameter EnergyDecayFactor;
}

// World section
[System.Serializable]
public struct WorldData
{
    public SpawnData PigsSpawn;
    public SpawnData GrassSpawn;
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

// Tuning parameter (float with min/max range)
[System.Serializable]
public struct TuningParameter
{
    public float2 Value; // x = min, y = max
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
    public float2 FullnessWholeness;
    public float2 Nutrition;
    public float2 SizeMax;
    public float2 EnergyComfortFactor;
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
    public float2 Speed;
    public float2 Size;
    public float2 VisionInterval;
    public float2 VisionRadius;
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
    public float2 Wholeness;
    public float2 Nutrition;
}

// AnimationData
[System.Serializable]
public struct AnimationData
{
    public Pig PigData;

    public struct Pig
    {
        public float DyingTime;
    }
}

// 

// Enum definitions
public enum ObjectType { None, Pig, Grass }
public enum ActorsType { None, Pig }
public enum FoodType { None, Grass }
