using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PigsSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
        state.RequireForUpdate<PrefabsLibraryComponent>();
        state.RequireForUpdate<SimulationRandomComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        
        var entity = SystemAPI.GetSingletonEntity<SimulationRandomComponent>();
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();
        var randomData = SystemAPI.GetComponentRW<SimulationRandomComponent>(entity);

        var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        var pigsCountVar = config.BlobReference.Value.World.PigsSpawn.Count;
        var pigsCount = MathExtentions.GetRandomVariation(ref randomData.ValueRW.Random, pigsCountVar);

        var pigsHeightVar = config.BlobReference.Value.World.PigsSpawn.SpawnHeight;

        for (int i = 0; i < pigsCount; i++)
        {
            var newPosition = GetRandomPosition(planetCenter, planetScale, 1, randomData, pigsHeightVar);
            var newRotation = GetRandomRotation();

            PigsFactory.SpawnPig(newPosition, newRotation, commandBuffer, randomData, config.BlobReference.Value);
        }

        commandBuffer.Playback(state.EntityManager);
    }

    private quaternion GetRandomRotation() 
    {
        return quaternion.identity;
    }

    private float3 GetRandomPosition(float3 centerPosition, float planetScale, float pigScale, RefRW<SimulationRandomComponent> random, float2 spawnHeight)
    {
        var randomDirection = new float3(0, 0, 0);
        while (math.distancesq(randomDirection, new float3(0,0,0)) <= 0)
        {
            randomDirection = random.ValueRW.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        }

        var planetOffset = planetScale / 2f + pigScale / 2f;
        var minOffset = planetOffset + spawnHeight.x;
        var maxOffset = planetOffset + spawnHeight.y;

        var randomHeight = random.ValueRW.Random.NextFloat(minOffset, maxOffset);
        var position = math.normalize(randomDirection) * randomHeight + centerPosition;

        return position;
    }
}
