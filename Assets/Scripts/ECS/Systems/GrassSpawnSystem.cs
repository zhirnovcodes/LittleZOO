using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;
// System that handles initial grass spawning - updated for main thread execution
[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial struct GrassSpawnSystem : ISystem
{
    private bool m_Initialized;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IcosphereComponent>();
        state.RequireForUpdate<ActorsSpawnComponent>();
        state.RequireForUpdate<SimulationConfigComponent>();
        state.RequireForUpdate<ActorsSpawnRandomComponent>();
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
        var random = SystemAPI.GetSingleton<ActorsSpawnRandomComponent>();
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();
        var grassCount = random.Random.NextInt(config.BlobReference.Value.World.GrassSpawn.Count.x, config.BlobReference.Value.World.GrassSpawn.Count.y);

        // Spawn initial grass entities
        for (int i = 0; i < grassCount; i++)
        {
            // Find an unoccupied triangle
            int triangleIndex;
            do
            {
                triangleIndex = random.Random.NextInt(0, triangleCount - 1);
            }
            while (occupiedTriangles[triangleIndex]);

            // Mark this triangle as occupied
            occupiedTriangles[triangleIndex] = true;

            // Create a new grass entity
            GrassFactory.CreateRandomGrass(ecb, triangleIndex, ref random.Random, in icosphere, spawnData.ValueRO.GrassPrefab, in config);
        }

        // Simulate grass spread across the planet
        SimulateGrassSpread(ecb, occupiedTriangles, ref random.Random, triangleCount, icosphere, spawnData, in config);

        // Cleanup
        occupiedTriangles.Dispose();
    }

    private void SimulateGrassSpread(EntityCommandBuffer ecb, NativeArray<bool> occupiedTriangles,
                                     ref Random random, int triangleCount,
                                     in IcosphereComponent icosphere,
                                     RefRO< ActorsSpawnComponent> spawnData,
                                     in SimulationConfigComponent config)
    {
        var reproductionChance = random.NextFloat( config.BlobReference.Value.Entities.Grass.Stats.ReproductionChance.x, 
            config.BlobReference.Value.Entities.Grass.Stats.ReproductionChance.y); 
        var simulationSteps = config.BlobReference.Value.World.GrassReproductionSteps;

        // Simulate multiple reproduction steps
        for (int step = 0; step < simulationSteps; step++)
        {
            // Keep track of new triangles to occupy in this step
            NativeList<int> newTriangles = new NativeList<int>(triangleCount, Allocator.Temp);

            // For each occupied triangle, try to spread to neighbors
            for (int i = 0; i < triangleCount; i++)
            {
                if (occupiedTriangles[i])
                {
                    // Only reproduce if it has reproduction capability (50% chance)
                    if (random.NextFloat(0, 1) < reproductionChance)
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
                GrassFactory.CreateRandomGrass(ecb, triangleId, ref random, icosphere, spawnData.ValueRO.GrassPrefab, in config);
            }

            newTriangles.Dispose();
        }
    }
}