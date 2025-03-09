using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// New component that consolidates all grass parameters
public struct GrassDNAComponent : IComponentData
{
    public Entity Prefab;

    // Growing parameters
    public float3 MinSize;
    public float3 MaxSize;
    public float MaxWholeness;
    public float GrowthSpeed;

    // Aging parameters
    public float AgingFunctionSpan;
    public float AgingFunctionHeight;

    // Edible parameters
    public float MaxNutrition;

    // Reproduction parameters
    public float ReproductionFunctionFactor;
    public float ReproductionFunctionHeight;
    public float ReproductionInterval;

    // Random seed for this grass
    public uint RandomSeed;
}

public struct GrowingComponent : IComponentData
{
    public float3 MinSize; // Size (scale) = MaxSize when Wholeness = 0
    public float3 MaxSize; // Size (scale) = MaxSize when Wholeness = 100
    public float MaxWholeness;
    public float GrowthSpeed;
}

public struct AgingComponent : IComponentData
{
    public float AgeElapsed; // Time elapsed in seconds
    public float AgingFunctionSpan; // factor of the aging function. Every frame systems checks if grass should die or not. If it passes check - this check is run second time, and if it passes the other time - grass dies
    public float AgingFunctionHeight; // height of the aging function
    public float Wholeness; // value from 0 to 100
}

// Updated PlantReproductionComponent with elapsed time field
public struct PlantReproductionComponent : IComponentData
{
    public float FunctionFactor; // Each interval reproduction system of the grass checks - if random value overheads function value at this age - grass tries to reproduce
    public float FunctionHeight; // Reproduction function height
    public float Interval;
    public float ReproductionTimeElapsed; // Time elapsed since last reproduction attempt
}

// Updated static factory class for creating grass entities
public static class GrassFactory
{
    // Updated to use GrassDNAComponent and regular ECB (not parallel)
    public static void CreateGrass(EntityCommandBuffer ecb, int triangleId, uint randomSeed, in IcosphereComponent icosphere, Entity prefab)
    {
        var random = Random.CreateFromIndex(randomSeed);

        // Create the grass entity
        Entity grassEntity = ecb.Instantiate(prefab);

        // Get triangle data from the icosphere blob
        var triangle = icosphere.GetTriangle(triangleId);

        // Calculate random attributes
        float randomWidth = random.NextFloat(triangle.RadiusInner * 0.5f, triangle.RadiusOuter * 0.8f);

        // Create the DNA component with all parameters
        GrassDNAComponent dna = new GrassDNAComponent
        {
            Prefab = prefab,

            // Growing parameters
            MinSize = new float3(0.1f, 0.1f, 0.1f),
            MaxSize = new float3(randomWidth, 1f, randomWidth),
            MaxWholeness = random.NextFloat(80, 100),
            GrowthSpeed = random.NextFloat(2, 5),

            // Aging parameters
            AgingFunctionSpan = random.NextFloat(20, 40),
            AgingFunctionHeight = random.NextFloat(0.01f, 0.05f),

            // Edible parameters
            MaxNutrition = random.NextFloat(20, 50),

            // Reproduction parameters
            ReproductionFunctionFactor = random.NextFloat(10, 30),
            ReproductionFunctionHeight = random.NextFloat(0.1f, 0.5f),
            ReproductionInterval = random.NextFloat(1, 5),

            // Random seed
            RandomSeed = (uint)random.NextInt()
        };

        // Add the DNA component
        ecb.AddComponent(grassEntity, dna);

        // Add the transform component
        ecb.AddComponent(grassEntity, new IcosphereTransform
        {
            TriangleId = triangleId
        });

        // Calculate position based on the triangle centroid on surface
        float3 position = triangle.CentroidOnSurface;

        // Get rotation based on the triangle orientation
        quaternion rotation = icosphere.GetRotation(triangleId);

        // Add a local transform component for positioning and scaling
        ecb.AddComponent(grassEntity, new LocalTransform
        {
            Position = position,
            Rotation = rotation,
            Scale = 0.2f // Initial small scale
        });

        // Add aging component
        ecb.AddComponent(grassEntity, new AgingComponent
        {
            AgeElapsed = random.NextFloat(0, 5), // Start with a random age
            AgingFunctionSpan = dna.AgingFunctionSpan,
            AgingFunctionHeight = dna.AgingFunctionHeight,
            Wholeness = random.NextFloat(50, 100) // Start with random wholeness
        });

        // Add growing component
        ecb.AddComponent(grassEntity, new GrowingComponent
        {
            MinSize = dna.MinSize,
            MaxSize = dna.MaxSize,
            MaxWholeness = dna.MaxWholeness,
            GrowthSpeed = dna.GrowthSpeed
        });

        // Add edible component
        ecb.AddComponent(grassEntity, new EdibleComponent
        {
            Nutrition = 0, // Will be calculated based on wholeness
            MaxNutrition = dna.MaxNutrition
        });

        // Add random component
        ecb.AddComponent(grassEntity, new ActorRandomComponent
        {
            Random = Random.CreateFromIndex(dna.RandomSeed)
        });

        // Add reproduction component if can reproduce
        var canReproduce = random.NextFloat() < 0.5f;

        if (canReproduce)
        {
            ecb.AddComponent(grassEntity, new PlantReproductionComponent
            {
                FunctionFactor = dna.ReproductionFunctionFactor,
                FunctionHeight = dna.ReproductionFunctionHeight,
                Interval = dna.ReproductionInterval,
                ReproductionTimeElapsed = 0f // Initialize elapsed time to 0
            });
        }
    }

    // Creates a new grass entity based on a parent entity's DNA
    public static void CreateGrass(EntityCommandBuffer ecb, int triangleId, 
                                 GrassDNAComponent parentDNA, uint randomSeed,
                                 in IcosphereComponent icosphere)
    {
        var random = Random.CreateFromIndex(randomSeed);

        // Get triangle data from the icosphere blob
        var triangle = icosphere.GetTriangle(triangleId);

        // Calculate random width based on triangle
        float randomWidth = random.NextFloat(triangle.RadiusInner * 0.5f, triangle.RadiusOuter * 0.8f);

        // Create a new DNA with variations from parent
        GrassDNAComponent childDNA = new GrassDNAComponent
        {
            Prefab = parentDNA.Prefab,

            // Growing parameters with variations
            MinSize = parentDNA.MinSize,
            MaxSize = new float3(randomWidth, 1f, randomWidth),
            MaxWholeness = parentDNA.MaxWholeness * GetRandomVariation(random, 0.9f, 1.1f),
            GrowthSpeed = parentDNA.GrowthSpeed * GetRandomVariation(random, 0.9f, 1.1f),

            // Aging parameters with variations
            AgingFunctionSpan = parentDNA.AgingFunctionSpan * GetRandomVariation(random, 0.9f, 1.1f),
            AgingFunctionHeight = parentDNA.AgingFunctionHeight * GetRandomVariation(random, 0.9f, 1.1f),

            // Edible parameters with variations
            MaxNutrition = parentDNA.MaxNutrition * GetRandomVariation(random, 0.9f, 1.1f),

            // Reproduction parameters with variations
            ReproductionFunctionFactor = parentDNA.ReproductionFunctionFactor * GetRandomVariation(random, 0.9f, 1.1f),
            ReproductionFunctionHeight = parentDNA.ReproductionFunctionHeight * GetRandomVariation(random, 0.9f, 1.1f),
            ReproductionInterval = parentDNA.ReproductionInterval * GetRandomVariation(random, 0.95f, 1.05f),

            // New random seed
            RandomSeed = (uint)random.NextInt()
        };

        // Create the grass entity
        Entity grassEntity = ecb.Instantiate(parentDNA.Prefab);

        // Add the DNA component
        ecb.AddComponent(grassEntity, childDNA);

        // Add the transform component
        ecb.AddComponent(grassEntity, new IcosphereTransform
        {
            TriangleId = triangleId
        });

        // Calculate position based on the triangle centroid on surface
        float3 position = triangle.CentroidOnSurface;

        // Get rotation based on the triangle orientation
        quaternion rotation = icosphere.GetRotation(triangleId);

        // Add a local transform component for positioning and scaling
        ecb.AddComponent(grassEntity, new LocalTransform
        {
            Position = position,
            Rotation = rotation,
            Scale = 0.2f // Initial small scale
        });

        // Add aging component
        ecb.AddComponent(grassEntity, new AgingComponent
        {
            AgeElapsed = 0f, // Start fresh
            AgingFunctionSpan = childDNA.AgingFunctionSpan,
            AgingFunctionHeight = childDNA.AgingFunctionHeight,
            Wholeness = 10f // Start with low wholeness
        });

        // Add growing component
        ecb.AddComponent(grassEntity, new GrowingComponent
        {
            MinSize = childDNA.MinSize,
            MaxSize = childDNA.MaxSize,
            MaxWholeness = childDNA.MaxWholeness,
            GrowthSpeed = childDNA.GrowthSpeed
        });

        // Add edible component
        ecb.AddComponent(grassEntity, new EdibleComponent
        {
            Nutrition = 0, // Will be calculated based on wholeness
            MaxNutrition = childDNA.MaxNutrition
        });

        // Add random component
        ecb.AddComponent(grassEntity, new ActorRandomComponent
        {
            Random = Random.CreateFromIndex(childDNA.RandomSeed)
        });

        // Add reproduction component if can reproduce
        if (random.NextFloat(0,1) <= 0.5f)
        {
            ecb.AddComponent(grassEntity, new PlantReproductionComponent
            {
                FunctionFactor = childDNA.ReproductionFunctionFactor,
                FunctionHeight = childDNA.ReproductionFunctionHeight,
                Interval = childDNA.ReproductionInterval,
                ReproductionTimeElapsed = 0f // Initialize elapsed time to 0
            });
        }
    }

    private static float GetRandomVariation(Random random, float min, float max)
    {
        return random.NextFloat(min, max);
    }
}