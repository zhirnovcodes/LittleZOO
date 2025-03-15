using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
// System that handles plant reproduction - updated for main thread execution
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlantReproductionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlantReproductionComponent>();
        state.RequireForUpdate<IcosphereComponent>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
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

        // Get a reference to the icosphere singleton entity
        var icosphereSingletonEntity = SystemAPI.GetSingletonEntity<IcosphereComponent>();
        var icosphere = SystemAPI.GetComponent<IcosphereComponent>(icosphereSingletonEntity);

        var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = worldSingleton.CollisionWorld;

        var filter = new CollisionFilter()
        {
            BelongsTo = Zoo.Enums.Layers.ActorDynamic,
            CollidesWith = Zoo.Enums.Layers.ActorDynamic,
            GroupIndex = 0
        };

        foreach (var (reproduction, aging, dna, transform, actorRandom) in
                SystemAPI.Query<RefRW<PlantReproductionComponent>, RefRO<AgingComponent>,
                            RefRO<GrassDNAComponent>, RefRO<IcosphereTransform>, RefRO<ActorRandomComponent>>())
        {
            // Update reproduction time elapsed
            reproduction.ValueRW.ReproductionTimeElapsed += deltaTime;

            // Check if it's time to attempt reproduction
            if (reproduction.ValueRO.ReproductionTimeElapsed >= reproduction.ValueRO.Interval)
            {
                // Reset the timer
                reproduction.ValueRW.ReproductionTimeElapsed = 0;

                // Calculate reproduction chance based on normal distribution
                // Using an approximation of normal distribution centered at aging.AgeElapsed/2
                float ageFactor = math.exp(-math.pow(aging.ValueRO.AgeElapsed - reproduction.ValueRO.FunctionFactor, 2) /
                                         (2 * math.pow(reproduction.ValueRO.FunctionFactor, 2)));
                float reproductionChance = reproduction.ValueRO.FunctionHeight * ageFactor;

                // Check if reproduction should occur
                if (actorRandom.ValueRO.Random.NextFloat(0, 1) < reproductionChance)
                {
                    // Try to find an available neighboring triangle
                    bool foundSpot = false;
                    int neighbourId = actorRandom.ValueRO.Random.NextInt(0, 2);

                    // Try each neighboring triangle
                    // Get neighboring triangle index
                    int neighborTriangleId = icosphere.GetNeighbouringTriangleIndex(transform.ValueRO.TriangleId, neighbourId);
                    Triangle neighbourTriangle = icosphere.GetTriangle(neighborTriangleId);
                    // Check if the neighboring triangle is available (simplified check)
                    // In a real implementation, you'd have a proper system to check for occupancy

                    bool triangleIsEmpty = collisionWorld.CheckSphere(neighbourTriangle.CentroidOnSurface, neighbourTriangle.RadiusInner / 2f, filter) == false;

                    // If we found a spot, create a new plant
                    if (triangleIsEmpty)
                    {
                        // Create the new grass entity with attributes based on the parent DNA
                        GrassFactory.CreateGrass(
                            ecb,
                            neighborTriangleId,
                            dna.ValueRO,
                            (uint)actorRandom.ValueRO.Random.NextInt(),
                            icosphere
                        );
                    }
                }
            }
        }
    }
}
