using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
// Updated static factory class for creating grass entities
public static class GrassFactory
{
    private static GrassDNAComponent CreateRandomGrassDNA(Triangle triangle, Entity prefab, Random random)
    {
        // Calculate random attributes
        float randomWidth = random.NextFloat(triangle.RadiusInner * 0.5f, triangle.RadiusOuter * 0.8f);

        GrassDNAComponent dna = new GrassDNAComponent
        {
            Prefab = prefab,

            // Growing parameters
            MinSize = new float3(0.2f, 0.2f, 0.2f),
            MaxSize = new float3(randomWidth, 1f, randomWidth),
            MaxWholeness = random.NextFloat(80, 100),
            GrowthSpeed = random.NextFloat(2, 15),

            // Aging parameters
            AgingFunctionSpan = random.NextFloat(20, 50),
            AgingFunctionHeight = random.NextFloat(0.01f, 0.05f),

            // Edible parameters
            MaxNutrition = random.NextFloat(20, 50),

            // Reproduction parameters
            ReproductionFunctionFactor = random.NextFloat(10, 100),
            ReproductionFunctionHeight = random.NextFloat(0.1f, 0.8f),
            ReproductionInterval = random.NextFloat(0.2f, 3),

            // Random seed
            RandomSeed = (uint)random.NextInt()
        };

        return dna;
    }

    // Updated to use GrassDNAComponent and regular ECB (not parallel)
    public static void CreateGrass(EntityCommandBuffer ecb, int triangleId, uint randomSeed, in IcosphereComponent icosphere, Entity prefab)
    {
        var random = Random.CreateFromIndex(randomSeed);

        // Create the grass entity
        Entity grassEntity = ecb.Instantiate(prefab);

        // Get triangle data from the icosphere blob
        var triangle = icosphere.GetTriangle(triangleId);

        // Create the DNA component with all parameters
        GrassDNAComponent dna = CreateRandomGrassDNA(triangle, prefab, random);

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
        quaternion rotation = icosphere.GetRotation(triangleId, random.NextInt(0, 2));

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
            MaxNutrition = dna.MaxNutrition,
            SizeMax = dna.MaxSize.x
        });

        // Add random component
        ecb.AddComponent(grassEntity, new ActorRandomComponent
        {
            Random = Random.CreateFromIndex(dna.RandomSeed)
        });

        ecb.AddBuffer<AdvertisedActionItem>(grassEntity);
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Eat,
            NeedId = Zoo.Enums.NeedType.Fullness,
            NeedsMatrix = new float2(-100f, 0.1f)
        });
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Sleep,
            NeedId = Zoo.Enums.NeedType.Energy,
            NeedsMatrix = new float2(1f, 2f)
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
        quaternion rotation = icosphere.GetRotation(triangleId, random.NextInt(0,2));

        // Add a local transform component for positioning and scaling
        ecb.AddComponent(grassEntity, new LocalTransform
        {
            Position = position,
            Rotation = rotation,
            Scale = 0.1f // Initial small scale
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

        ecb.AddBuffer<AdvertisedActionItem>(grassEntity);
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Eat,
            NeedId = Zoo.Enums.NeedType.Fullness,
            NeedsMatrix = new float2 (-100, -0.1f )
        });
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionID.Sleep,
            NeedId = Zoo.Enums.NeedType.Energy,
            NeedsMatrix = new float2( 1f, 2f)
        });

        // Add reproduction component if can reproduce
        if (random.NextFloat(0,1) <= 0.9f)
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