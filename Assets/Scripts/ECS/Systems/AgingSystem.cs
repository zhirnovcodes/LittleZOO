using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
// System that handles plant aging and death - updated for main thread execution
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct AgingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AgingComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (aging, actorRandom, entity) in
                SystemAPI.Query<RefRW<AgingComponent>, RefRO<ActorRandomComponent>>().WithEntityAccess())
        {
            // Increment age
            aging.ValueRW.AgeElapsed += deltaTime;

            // Calculate death chance based on exponential function
            // Using this formula: deathChance = AgingFunctionHeight * exp(AgeElapsed / AgingFunctionSpan)
            float deathChance = aging.ValueRO.AgingFunctionHeight *
                        math.exp(aging.ValueRO.AgeElapsed / aging.ValueRO.AgingFunctionSpan);

            // Cap the death chance at 1.0 to avoid excessive values
            deathChance = math.min(deathChance, 1.0f);

            // First death check
            float randomCheck1 = actorRandom.ValueRO.Random.NextFloat(0, 1);
            if (randomCheck1 < deathChance)
            {
                // Second death check
                float randomCheck2 = actorRandom.ValueRO.Random.NextFloat(0, 1);
                if (randomCheck2 < deathChance)
                {
                    // If both checks pass, destroy the entity
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
