using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public partial struct SpawnGrassSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ActorsSpawnComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        return;

        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var randomData = SystemAPI.GetComponentRW<ActorsSpawnRandomComponent>(entity);

        var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        for (int i = 0; i < spawnData.ValueRO.GrassCount; i++)
        {
            SpawnGrass(planetCenter, planetScale, commandBuffer, randomData, spawnData);
        }

        commandBuffer.Playback(state.EntityManager);
        commandBuffer.Dispose();
    }

    private void SpawnGrass(float3 centerPosition, float planetScale, EntityCommandBuffer commandBuffer, RefRW<ActorsSpawnRandomComponent> random, RefRO<ActorsSpawnComponent> spawnData)
    {
        var grass = commandBuffer.Instantiate(spawnData.ValueRO.GrassPrefab);

        var newScale = 1f;
        GetRandomPosition(centerPosition, planetScale, newScale, random, spawnData, 
            out var newPosition, out var newRotation);

        var wholeness = random.ValueRW.Random.NextFloat(spawnData.ValueRO.GrassWholenessMin, spawnData.ValueRO.GrassWholenessMax);
        var wholenessFactor = math.unlerp(0, spawnData.ValueRO.GrassWholenessMax, wholeness);
        var radius = math.lerp(0, spawnData.ValueRO.GrassRadiusMax, wholenessFactor);
        var nutrition = random.ValueRW.Random.NextFloat(spawnData.ValueRO.GrassNutritionMin, spawnData.ValueRO.GrassNutritionMax);

        commandBuffer.SetComponent(grass, new LocalTransform { Position = newPosition, Rotation = newRotation, Scale = radius });
        commandBuffer.AddComponent(grass, new ActorRandomComponent { Random = Unity.Mathematics.Random.CreateFromIndex(random.ValueRW.Random.NextUInt()) });
        commandBuffer.AddComponent(grass, new EdibleComponent
        {
            Wholeness = 100,
            Nutrition = nutrition,
            RadiusMax = radius
        });

        commandBuffer.AddComponent(grass, new GrassComponent
        {
        });


        commandBuffer.AddBuffer<AdvertisedActionItem>(grass);
        commandBuffer.AppendToBuffer(grass, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Eat,
            NeedId = Zoo.Enums.NeedType.Fullness,
            NeedsMatrix = new float2(1, -0.5f)
        });
        commandBuffer.AppendToBuffer(grass, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Sleep,
            NeedId = Zoo.Enums.NeedType.Energy,
            NeedsMatrix = new float2(-0.2f, 0.5f)
        });
    }

    private void GetRandomPosition(float3 centerPosition, float planetScale, float pigScale, RefRW<ActorsSpawnRandomComponent> random, RefRO<ActorsSpawnComponent> spawnData, 
        out float3 outputPosition, out quaternion outputRotation)
    {
        var randomDirection = new float3(0, 0, 0);
        while (math.distancesq(randomDirection, new float3(0, 0, 0)) <= 0)
        {
            randomDirection = random.ValueRW.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        }

        var planetOffset = planetScale / 2f;

        var randomHeight = planetOffset;
        randomDirection = math.normalize(randomDirection);
        var position = randomDirection * randomHeight + centerPosition;
        outputPosition = position;

        var up = randomDirection;
        var forward = new float3(0, 0, 1);
        var left = math.normalize( math.cross(up, forward) );
        forward = math.normalize(math.cross(left, up));
        outputRotation = quaternion.LookRotation(forward, up);
    }
}
