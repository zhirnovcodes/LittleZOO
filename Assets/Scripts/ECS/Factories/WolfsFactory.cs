using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Physics;

public static class WolfsFactory
{
    public static void SpawnWolf(
        float3 position,
        quaternion rotation,
        EntityCommandBuffer commandBuffer,
        RefRW<SimulationRandomComponent> random,
        in SimulationSettings settings)
    {
        var newWolf = commandBuffer.Instantiate(settings.World.WolfSpawn.Prefab);

        commandBuffer.SetComponent(newWolf, new LocalTransform { Position = position, Rotation = rotation, Scale = 1 });
        commandBuffer.AddComponent(newWolf, new GravityComponent());
        commandBuffer.AddComponent(newWolf, new ActorRandomComponent { Random = Random.CreateFromIndex(random.ValueRW.Random.NextUInt()) });

        //var dna = CreatePigsDNA(in settings, ref random.ValueRW.Random);
        commandBuffer.AddComponent(newWolf, new ActorTypeComponent { Type = ActorsType.Wolf });

        // Needs
        var fullness = MathExtentions.GetRandom100(ref random.ValueRW.Random);
        var energy = MathExtentions.GetRandom100(ref random.ValueRW.Random);
        var need = new float3(fullness, energy, 100);

        commandBuffer.AddComponent(newWolf, new ActorNeedsComponent
        {
            Needs = need
        });

        // vision
        var radius = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actors.Pigs.Stats.VisionRadius);
        var interval = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actors.Pigs.Stats.VisionInterval);
        /*commandBuffer.AddComponent(newWolf, new VisionComponent
        {
            Radius = radius,
            Interval = interval
        });*/

        //commandBuffer.AddBuffer<VisionItem>(newWolf);

        // Moving
        var speed = random.ValueRW.Random.NextFloat2(settings.Actors.Pigs.Stats.SpeedMin, settings.Actors.Pigs.Stats.SpeedMax);

        commandBuffer.AddComponent(newWolf, new MovingInputComponent());
        commandBuffer.AddComponent(newWolf, new MovingOutputComponent());
        commandBuffer.AddComponent(newWolf, new MovingSpeedComponent { SpeedRange = speed });

        // Stats
        var fullnessDecayByDistance = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.FullnessDecayByDistance);
        var fullnessNaturalDecay = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.FullnessNaturalDecay);
        var energyDecayByDistance = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyDecayByDistance);
        var energyNaturalDecay = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyNaturalDecay);
        var safetyInterval = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actors.Pigs.Stats.SafetyInterval);
        /*
        commandBuffer.AddComponent(newWolf, new HungerComponent
        {
            FullnessDecayByDistance = fullnessDecayByDistance,
            FullnessDecaySpeed = fullnessNaturalDecay
        });

        commandBuffer.AddComponent(newWolf, new EnergyComponent
        {
            EnergyDecayByDistance = energyDecayByDistance,
            EnergyDecaySpeed = energyNaturalDecay
        });

        commandBuffer.AddComponent(newWolf, new SafetyComponent
        {
            CheckInterval = safetyInterval
        });*/

        // States
        var biteInterval = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actions.Pigs.EatInterval);
        var biteWholeness = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actions.Pigs.BiteWholeness);

        //commandBuffer.AddComponent(newWolf, new StateTimeComponent());
        //commandBuffer.AddComponent(newWolf, new IdleStateTag());
        commandBuffer.AddComponent(newWolf, new SearchingStateTag());
        //commandBuffer.AddComponent(newWolf, new MovingToStateTag());
        /*commandBuffer.AddComponent(newWolf, new EatingStateTag
        {
            BiteInterval = biteInterval,
            BiteWholeness = biteWholeness
        });
        commandBuffer.AddComponent(newWolf, new SleepingStateTag());
        commandBuffer.AddComponent(newWolf, new RunningFromStateTag());
        commandBuffer.AddComponent(newWolf, new DyingStateTag());

        commandBuffer.AddComponent(newWolf, new ActionInputComponent());
        commandBuffer.AddComponent(newWolf, new SubActionOutputComponent());

        commandBuffer.AddComponent(newWolf, new NeedBasedDecisionTag());
        commandBuffer.AddBuffer<AdvertisedActionItem>(newWolf);*/

        //Animations
        commandBuffer.AddComponent(newWolf, new SubActionOutputComponent());
        commandBuffer.AddComponent(newWolf, new NeedBasedSystemOutput 
        {
            Action = Zoo.Enums.ActionTypes.Search
        });

        commandBuffer.AddBuffer<AdvertisedActionItem>(newWolf);
        commandBuffer.AppendToBuffer(newWolf, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionTypes.Escape,
            NeedId = Zoo.Enums.NeedType.Safety,
            NeedsMatrix = new float3(0, 0, 100)
        });
    }
}
/*
public struct ActionInputComponent : IComponentData, IEnableableComponent
{
    public float TimeElapsed;
    public Entity Target;
    public ActionTypes Action;
    public int CurrentActionIndex;
}

public struct SubActionOutputComponent : IComponentData
{
    public ActionStatus Status;
}

[InternalBufferCapacity(8)]
public struct SubActionBufferItem : IBufferElementData
{
    public SubActionTypes ActionType;
}
*/