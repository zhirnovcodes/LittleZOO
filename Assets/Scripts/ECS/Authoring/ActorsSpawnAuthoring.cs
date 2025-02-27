using Unity.Entities;
using UnityEngine;

public class ActorsSpawnAuthoring : MonoBehaviour
{
    public Transform Planet;
    public uint RandomSeed;
    
    public GameObject PigPrefab;
    public int PigsCount;
    public float MinHeightAbovePlanet;
    public float MaxHeightAbovePlanet;
    public float PigSpeed;

    public GameObject GrassPrefab;
    public int GrassCount;
    public float GrassRadiusMax;
    public float GrassWholenessMin;
    public float GrassWholenessMax;
    
    public class ActorsSpawnBaker : Baker<ActorsSpawnAuthoring>
    {
        public override void Bake(ActorsSpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);


            AddComponent(entity, new ActorsSpawnComponent 
            { 
                PigPrefab = GetEntity(authoring.PigPrefab, TransformUsageFlags.Dynamic), 
                PigsCount = authoring.PigsCount,
                SpawnHeightMin = authoring.MinHeightAbovePlanet,
                SpawnHeightMax = authoring.MaxHeightAbovePlanet,
                PigSpeed = authoring.PigSpeed,

                GrassPrefab = GetEntity(authoring.GrassPrefab, TransformUsageFlags.Dynamic),
                GrassCount = authoring.GrassCount,
                GrassRadiusMax = authoring.GrassRadiusMax,
                GrassWholenessMin = authoring.GrassWholenessMin,
                GrassWholenessMax = authoring.GrassWholenessMax
            });

            AddComponent(entity, new ActorsSpawnRandomComponent
            {
                Random = Unity.Mathematics.Random.CreateFromIndex(authoring.RandomSeed)
            });
        }
    }
}
