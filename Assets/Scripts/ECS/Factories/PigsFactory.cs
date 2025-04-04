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
        RefRW<SimulationRandomComponent> random,
        in SimulationSettings settings)
    {
        var newPig = commandBuffer.Instantiate(settings.World.PigsSpawn.Prefab);

        commandBuffer.SetComponent(newPig, new LocalTransform { Position = position, Rotation = rotation, Scale = 1 });
        commandBuffer.AddComponent(newPig, new GravityComponent());
        commandBuffer.AddComponent(newPig, new ActorRandomComponent { Random = Random.CreateFromIndex(random.ValueRW.Random.NextUInt()) });

        //var dna = CreatePigsDNA(in settings, ref random.ValueRW.Random);

        // Needs
        var hungerDecayFator = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.FullnessDecayByDistance);
        var energyDecayFator = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyDecayByDistance);
        var fullness = MathExtentions.GetRandom100(ref random.ValueRW.Random);
        var energy = MathExtentions.GetRandom100(ref random.ValueRW.Random);

        commandBuffer.AddComponent(newPig, new ActorNeedsComponent
        {
            Fullness = fullness,
            Energy = energy,
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
        var speed = random.ValueRW.Random.NextFloat2(settings.Actors.Pigs.Stats.SpeedMin, settings.Actors.Pigs.Stats.SpeedMax);

        commandBuffer.AddComponent(newPig, new MovingInputComponent());
        commandBuffer.AddComponent(newPig, new MovingOutputComponent());
        commandBuffer.AddComponent(newPig, new MovingSpeedComponent { SpeedRange = speed });

        // Stats
        var fullnessDecayByDistance = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.FullnessDecayByDistance);
        var fullnessNaturalDecay = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.FullnessNaturalDecay);
        var energyDecayByDistance = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyDecayByDistance);
        var energyNaturalDecay = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Needs.EnergyNaturalDecay);

        commandBuffer.AddComponent(newPig, new HungerComponent 
        { 
            FullnessDecayByDistance = fullnessDecayByDistance,
            FullnessDecaySpeed = fullnessNaturalDecay
        });

        commandBuffer.AddComponent(newPig, new EnergyComponent
        {
            EnergyDecayByDistance = energyDecayByDistance,
            EnergyDecaySpeed = energyNaturalDecay
        });

        // States
        var biteInterval = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actions.Pigs.EatInterval);
        var biteWholeness = MathExtentions.GetRandomVariation(ref random.ValueRW.Random, settings.Actions.Pigs.BiteWholeness);

        commandBuffer.AddComponent(newPig, new StateTimeComponent());
        commandBuffer.AddComponent(newPig, new IdleStateTag());
        commandBuffer.AddComponent(newPig, new SearchingStateTag());
        commandBuffer.AddComponent(newPig, new MovingToStateTag());
        commandBuffer.AddComponent(newPig, new EatingStateTag 
        {
            BiteInterval = biteInterval,
            BiteWholeness = biteWholeness
        });
        commandBuffer.AddComponent(newPig, new SleepingStateTag());
        commandBuffer.AddComponent(newPig, new DyingStateTag());

        commandBuffer.AddComponent(newPig, new ActionInputComponent ());
        commandBuffer.AddComponent(newPig, new SubActionOutputComponent());

        commandBuffer.AddComponent(newPig, new NeedBasedSystemOutput());
        commandBuffer.AddComponent(newPig, new NeedBasedDecisionTag());
        commandBuffer.AddBuffer<AdvertisedActionItem>(newPig);

        //Animations

        ActionsExtentions.SetAction(commandBuffer, Zoo.Enums.SubActionTypes.Idle, newPig);

    }

    private static PigsDNAComponent CreatePigsDNA(in SimulationSettings settings, ref Random random)
    {
        var dna = new PigsDNAComponent
        {
            // Needs
            FullnessNaturalDecay = MathExtentions.GetRandomVariation(ref random, settings.Needs.FullnessNaturalDecay),
            EnergyNaturalDecay = MathExtentions.GetRandomVariation(ref random, settings.Needs.EnergyNaturalDecay),
            FullnessDecayByDistance = MathExtentions.GetRandomVariation(ref random, settings.Needs.FullnessDecayByDistance),
            EnergyDecayByDistance = MathExtentions.GetRandomVariation(ref random, settings.Needs.EnergyDecayByDistance),

            // Actions
            EatInterval = MathExtentions.GetRandomVariation(ref random, settings.Actions.Pigs.EatInterval),
            BiteWholeness = MathExtentions.GetRandomVariation(ref random, settings.Actions.Pigs.BiteWholeness),

            // Stats
            Speed = MathExtentions.GetRandomVariation(ref random, settings.Actors.Pigs.Stats.SpeedMin, settings.Actors.Pigs.Stats.SpeedMax), 
            Size = MathExtentions.GetRandomVariation(ref random, settings.Actors.Pigs.Stats.Size),
            VisionInterval = MathExtentions.GetRandomVariation(ref random, settings.Actors.Pigs.Stats.VisionInterval),
            VisionRadius = MathExtentions.GetRandomVariation(ref random, settings.Actors.Pigs.Stats.VisionRadius)
        };

        return dna;
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