using Unity.Entities;
using UnityEngine;

public class ActorsSpawnAuthoring : MonoBehaviour
{
    public GameObject PigPrefab;
    public int PigsCount;
    public Transform Planet;
    public float MinHeightAbovePlanet;
    public float MaxHeightAbovePlanet;
    public float PigSpeed;
    public uint RandomSeed;

    public class ActorsSpawnBaker : Baker<ActorsSpawnAuthoring>
    {
        public override void Bake(ActorsSpawnAuthoring authoring)
        {
            var planetPosition = authoring.Planet.localPosition;
            var entity = GetEntity(TransformUsageFlags.None);
            var pigPrefab = authoring.PigPrefab;


            AddComponent(entity, new ActorsSpawnComponent 
            { 
                PigPrefab = GetEntity(pigPrefab, TransformUsageFlags.Dynamic), 
                PigsCount = authoring.PigsCount,
                SpawnHeightMin = authoring.MinHeightAbovePlanet,
                SpawnHeightMax = authoring.MaxHeightAbovePlanet,
                PigSpeed = authoring.PigSpeed
            });

            AddComponent(entity, new ActorsSpawnRandomComponent
            {
                Random = Unity.Mathematics.Random.CreateFromIndex(authoring.RandomSeed)
            });
        }
    }
}
