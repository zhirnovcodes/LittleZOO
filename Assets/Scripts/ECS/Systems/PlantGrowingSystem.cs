using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlantGrowingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GrowingComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Execute jobs directly on the main thread
        foreach (var (aging, growing) in SystemAPI.Query<RefRW<AgingComponent>, RefRO<GrowingComponent>>())
        {
            // Increase wholeness based on growth speed
            aging.ValueRW.Wholeness = math.min(aging.ValueRO.Wholeness + growing.ValueRO.GrowthSpeed * deltaTime, growing.ValueRO.MaxWholeness);
        }

        foreach (var (transform, aging, growing) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<AgingComponent>, RefRO<GrowingComponent>>())
        {
            // Calculate growth factor (0 to 1)
            float growthFactor = aging.ValueRO.Wholeness / 100f;

            // Interpolate between min and max size based on wholeness
            float3 newScale = math.lerp(growing.ValueRO.MinSize, growing.ValueRO.MaxSize, growthFactor);

            // Apply the new scale
            transform.ValueRW.Scale = math.length(newScale);
        }

        foreach (var (edible, aging) in SystemAPI.Query<RefRW<EdibleComponent>, RefRO<AgingComponent>>())
        {
            // Calculate nutrition based on wholeness
            float nutritionFactor = aging.ValueRO.Wholeness / 100f;
            edible.ValueRW.Nutrition = edible.ValueRO.MaxNutrition * nutritionFactor;
        }
    }
}

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

        foreach (var (reproduction, aging, dna, transform, actorRandom, entity) in
                SystemAPI.Query<RefRW<PlantReproductionComponent>, RefRO<AgingComponent>,
                            RefRO<GrassDNAComponent>, RefRO<IcosphereTransform>, RefRO<ActorRandomComponent>>().
                            WithEntityAccess())
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

                JobLogger.Log(0 + " " + reproductionChance);
                // Check if reproduction should occur
                if (actorRandom.ValueRO.Random.NextFloat(0, 1) < reproductionChance)
                {
                    JobLogger.Log(1);
                    // Try to find an available neighboring triangle
                    bool foundSpot = false;
                    int triangleToSpawn = -1;

                    // Try each neighboring triangle
                    for (int i = 0; i < 3; i++)
                    {
                        // Get neighboring triangle index
                        int neighborIndex = icosphere.GetNeighbouringTriangleIndex(transform.ValueRO.TriangleId, i);

                        // Check if the neighboring triangle is available (simplified check)
                        // In a real implementation, you'd have a proper system to check for occupancy
                        bool triangleIsEmpty = actorRandom.ValueRO.Random.NextFloat() < 0.7f; // 70% chance it's empty

                        if (triangleIsEmpty && neighborIndex >= 0)
                        {
                            JobLogger.Log(2);
                            triangleToSpawn = neighborIndex;
                            foundSpot = true;
                            break;
                        }
                    }

                    // If we found a spot, create a new plant
                    if (foundSpot)
                    {
                        // Create the new grass entity with attributes based on the parent DNA
                        GrassFactory.CreateGrass(
                            ecb,
                            triangleToSpawn,
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

// System that handles initial grass spawning - updated for main thread execution
[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial struct GrassSpawnSystem : ISystem
{
    private const int INITIAL_GRASS_COUNT = 50;
    private const int SIMULATION_STEPS = 3;
    private const float REPRODUCTION_CHANCE = 0.5f;

    private bool m_Initialized;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IcosphereComponent>();
        state.RequireForUpdate<ActorsSpawnComponent>();
        m_Initialized = false;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only run once
        if (m_Initialized)
        {
            state.Enabled = false;
            return;
        }

        m_Initialized = true;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Get a reference to the icosphere singleton entity
        var icosphereSingletonEntity = SystemAPI.GetSingletonEntity<IcosphereComponent>();
        var icosphere = SystemAPI.GetComponent<IcosphereComponent>(icosphereSingletonEntity);

        // Get the number of triangles in the icosphere
        int triangleCount = icosphere.Length();

        // Create a NativeArray to track which triangles are occupied
        NativeArray<bool> occupiedTriangles = new NativeArray<bool>(triangleCount, Allocator.Temp);

        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);

        // Create a random number generator with a fixed seed
        var random = Random.CreateFromIndex(1234);

        // Spawn initial grass entities
        for (int i = 0; i < INITIAL_GRASS_COUNT; i++)
        {
            // Find an unoccupied triangle
            int triangleIndex;
            do
            {
                triangleIndex = random.NextInt(0, triangleCount);
            }
            while (occupiedTriangles[triangleIndex]);

            // Mark this triangle as occupied
            occupiedTriangles[triangleIndex] = true;

            // Create a new grass entity
            GrassFactory.CreateGrass(ecb, triangleIndex, (uint)random.NextInt(), icosphere, spawnData.ValueRO.GrassPrefab);
        }

        // Simulate grass spread across the planet
        SimulateGrassSpread(ecb, occupiedTriangles, random, triangleCount, icosphere, spawnData);

        // Cleanup
        occupiedTriangles.Dispose();
    }

    private void SimulateGrassSpread(EntityCommandBuffer ecb, NativeArray<bool> occupiedTriangles,
                                     Random random, int triangleCount,
                                     in IcosphereComponent icosphere,
                                     RefRO< ActorsSpawnComponent> spawnData)
    {
        // Simulate multiple reproduction steps
        int totalEntitiesCreated = INITIAL_GRASS_COUNT;

        for (int step = 0; step < SIMULATION_STEPS; step++)
        {
            // Keep track of new triangles to occupy in this step
            NativeList<int> newTriangles = new NativeList<int>(triangleCount, Allocator.Temp);

            // For each occupied triangle, try to spread to neighbors
            for (int i = 0; i < triangleCount; i++)
            {
                if (occupiedTriangles[i])
                {
                    // Only reproduce if it has reproduction capability (50% chance)
                    if (random.NextFloat() < REPRODUCTION_CHANCE)
                    {
                        // Try to spread to neighboring triangles
                        for (int j = 0; j < 3; j++)
                        {
                            // Get neighbor index
                            int neighborIndex = icosphere.GetNeighbouringTriangleIndex(i, j);

                            // Check if this triangle is available and valid
                            if (neighborIndex >= 0 && !occupiedTriangles[neighborIndex])
                            {
                                // 30% chance to successfully spread to this triangle
                                if (random.NextFloat() < 0.3f)
                                {
                                    newTriangles.Add(neighborIndex);
                                    break; // Only spread to one neighbor per step
                                }
                            }
                        }
                    }
                }
            }

            // Create entities for all the new triangles
            for (int i = 0; i < newTriangles.Length; i++)
            {
                int triangleId = newTriangles[i];

                // Mark this triangle as occupied
                occupiedTriangles[triangleId] = true;

                // Create a new grass entity
                GrassFactory.CreateGrass(ecb, triangleId, (uint)random.NextInt(), icosphere, spawnData.ValueRO.GrassPrefab);
            }

            totalEntitiesCreated += newTriangles.Length;
            newTriangles.Dispose();
        }
    }
}