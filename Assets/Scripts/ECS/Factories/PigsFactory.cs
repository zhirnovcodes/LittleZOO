using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Physics;

public static class PigsFactory
{
    public static void SpawnPig(
        float3 position, 
        quaternion rotation, 
        EntityCommandBuffer commandBuffer,
        RefRW<ActorsSpawnRandomComponent> random,
        ref SimulationSettings settings)
    {
        var newPig = commandBuffer.Instantiate(settings.World.PigsSpawn.Prefab);

        commandBuffer.SetComponent(newPig, new LocalTransform { Position = position, Rotation = rotation, Scale = 1 });
        commandBuffer.AddComponent(newPig, new GravityComponent());
        commandBuffer.AddComponent(newPig, new ActorRandomComponent { Random = Random.CreateFromIndex(random.ValueRW.Random.NextUInt()) });

        var speed = random.ValueRW.Random.NextFloat(settings.Actors.Pigs.Stats.Speed.x, settings.Actors.Pigs.Stats.Speed.y);

        commandBuffer.AddComponent(newPig, new MoveToTargetInputComponent { Speed = speed });
        commandBuffer.AddComponent(newPig, new MoveToTargetOutputComponent());
        commandBuffer.AddComponent(newPig, new HungerComponent());

        // States
        commandBuffer.AddComponent(newPig, new StateTimeComponent());
        commandBuffer.AddComponent(newPig, new SearchingStateTag());
        commandBuffer.AddComponent(newPig, new EatingStateTag());
        commandBuffer.AddComponent(newPig, new SleepingStateTag());

        commandBuffer.SetComponentEnabled<EatingStateTag>(newPig, false);
        commandBuffer.SetComponentEnabled<SleepingStateTag>(newPig, false);

        commandBuffer.AddComponent(newPig, new NeedBasedSystemOutput());
        commandBuffer.AddBuffer<AdvertisedActionItem>(newPig);
    }
}
