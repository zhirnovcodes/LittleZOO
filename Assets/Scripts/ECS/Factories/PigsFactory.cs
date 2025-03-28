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

        // Needs
        var hungerDecayFator = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.HungerDecayFactor);
        var energyDecayFator = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyDecayFactor);
        var fullness = MathExtentions.GetRandom100(ref random.ValueRW.Random);
        var energy = MathExtentions.GetRandom100(ref random.ValueRW.Random);

        commandBuffer.AddComponent(newPig, new ActorNeedsComponent
        {
                Fullness = fullness,
                Energy = energy,

                // Decay functions
                HungerDecayFactor = hungerDecayFator,
                EnergyDecayFactor = energyDecayFator
        });

        // vision
        var radius = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actors.Pigs.Stats.VisionRadius);
        var interval = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actors.Pigs.Stats.VisionInterval);
        commandBuffer.AddComponent(newPig, new VisionComponent
        {
            Radius = radius,
            Interval = interval
        });

        commandBuffer.AddBuffer<VisionItem>(newPig);

        // Moving
        var speed = random.ValueRW.Random.NextFloat(settings.Actors.Pigs.Stats.Speed.x, settings.Actors.Pigs.Stats.Speed.y);

        commandBuffer.AddComponent(newPig, new MoveToTargetInputComponent { Speed = speed });
        commandBuffer.AddComponent(newPig, new MoveToTargetOutputComponent());
        //commandBuffer.AddComponent(newPig, new HungerComponent());

        // States
        commandBuffer.AddComponent(newPig, new StateTimeComponent());
        commandBuffer.AddComponent(newPig, new SearchingStateTag());
        commandBuffer.AddComponent(newPig, new EatingStateTag());
        commandBuffer.AddComponent(newPig, new SleepingStateTag());
        commandBuffer.AddComponent(newPig, new DyingStateTag());

        commandBuffer.SetComponentEnabled<EatingStateTag>(newPig, false);
        commandBuffer.SetComponentEnabled<SleepingStateTag>(newPig, false);
        commandBuffer.SetComponentEnabled<DyingStateTag>(newPig, false);

        commandBuffer.AddComponent(newPig, new NeedBasedSystemOutput());
        commandBuffer.AddBuffer<AdvertisedActionItem>(newPig);

    }
}
