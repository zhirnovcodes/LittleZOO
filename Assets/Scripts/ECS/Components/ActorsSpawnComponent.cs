using Unity.Entities;
using Unity.Mathematics;

public struct ActorsSpawnComponent : IComponentData
{
    public Entity PigPrefab;
    public int PigsCount;
    public float SpawnHeightMin;
    public float SpawnHeightMax;
    public float PigSpeed;
}
