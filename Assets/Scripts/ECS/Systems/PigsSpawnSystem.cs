using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PigsSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ActorsSpawnComponent>();
        state.RequireForUpdate<SimulationConfigComponent>();
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
        
        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var randomData = SystemAPI.GetComponentRW<ActorsSpawnRandomComponent>(entity);

        var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        for (int i = 0; i < spawnData.ValueRO.PigsCount; i++)
        {
            var newPosition = GetRandomPosition(planetCenter, planetScale, 1, randomData, spawnData);
            var newRotation = GetRandomRotation(randomData, spawnData, newPosition);

            PigsFactory.SpawnPig(newPosition, newRotation, commandBuffer, randomData, ref config.BlobReference.Value);
        }

        commandBuffer.Playback(state.EntityManager);
    }

    private quaternion GetRandomRotation(RefRW<ActorsSpawnRandomComponent> random, RefRO<ActorsSpawnComponent> spawnData, float3 spawnPosit) 
    {
        return quaternion.identity;
    }

    private float3 GetRandomPosition(float3 centerPosition, float planetScale, float pigScale, RefRW<ActorsSpawnRandomComponent> random, RefRO<ActorsSpawnComponent> spawnData)
    {
        var randomDirection = new float3(0, 0, 0);
        while (math.distancesq(randomDirection, new float3(0,0,0)) <= 0)
        {
            randomDirection = random.ValueRW.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        }

        var planetOffset = planetScale / 2f + pigScale / 2f;
        var minOffset = planetOffset + spawnData.ValueRO.SpawnHeightMin;
        var maxOffset = planetOffset + spawnData.ValueRO.SpawnHeightMax;

        var randomHeight = random.ValueRW.Random.NextFloat(minOffset, maxOffset);
        var position = math.normalize(randomDirection) * randomHeight + centerPosition;

        return position;
    }
}
