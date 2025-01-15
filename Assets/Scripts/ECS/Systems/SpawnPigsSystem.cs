using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpawnPigsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ActorsSpawnComponent>();
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
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var randomData = SystemAPI.GetComponentRW<ActorsSpawnRandomComponent>(entity);

        var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        for (int i = 0; i < spawnData.ValueRO.PigsCount; i++)
        {
            SpawnPig(planetCenter, planetScale, commandBuffer, randomData, spawnData);
        }

        commandBuffer.Playback(state.EntityManager);
    }

    private void SpawnPig(float3 centerPosition, float planetScale, EntityCommandBuffer commandBuffer, RefRW<ActorsSpawnRandomComponent> random, RefRO<ActorsSpawnComponent> spawnData)
    {
        var newPig = commandBuffer.Instantiate(spawnData.ValueRO.PigPrefab);

        var newScale = 1f;
        var newPosition = GetRandomPosition(centerPosition, planetScale, newScale, random, spawnData);
        var newRotation = GetRandomRotation(random, spawnData, newPosition);

        commandBuffer.SetComponent(newPig, new LocalTransform { Position = newPosition, Rotation = newRotation, Scale = 1 });
        commandBuffer.AddComponent(newPig, new GravityComponent ());
        commandBuffer.AddComponent(newPig, new ActorRandomComponent { Random = Unity.Mathematics.Random.CreateFromIndex(random.ValueRW.Random.NextUInt()) });

        // States
        commandBuffer.AddComponent(newPig, new FallingStateTag());
        commandBuffer.AddComponent(newPig, new MoveToTargetInputComponent { Speed = spawnData.ValueRO.PigSpeed });
        commandBuffer.AddComponent(newPig, new MoveToTargetOutputComponent());
        commandBuffer.AddComponent(newPig, new DecisionMakingComponent
        {
            CreatedActions = new NativeList<ActionComponent>(Allocator.Persistent)
        });

        commandBuffer.SetComponentEnabled<FallingStateTag>(newPig, true);
        commandBuffer.SetComponentEnabled<MoveToTargetInputComponent>(newPig, false);

        // Action chain
        commandBuffer.AddBuffer<ActionChainItem>(newPig);
        commandBuffer.AddBuffer<DeleteActionItem>(newPig);
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
