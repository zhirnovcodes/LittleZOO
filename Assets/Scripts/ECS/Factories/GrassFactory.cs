using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
// Updated static factory class for creating grass entities
public static class GrassFactory
{
    private static GrassDNAComponent CreateRandomGrassDNA(Triangle triangle, Entity prefab, ref Random random, in SimulationConfigComponent config)
    {
        // Calculate random attributes
        var widthVar = config.BlobReference.Value.Entities.Grass.Stats.Size;
        var sizeMin = triangle.RadiusInner * widthVar.x;
        var sizeMax = triangle.RadiusOuter * widthVar.y;

        var minWholenessVar = config.BlobReference.Value.Entities.Grass.Stats.MinWholeness;
        var maxWholenessVar = config.BlobReference.Value.Entities.Grass.Stats.MaxWholeness;
        var maxWholeness = GetRandomVariation(ref random, maxWholenessVar);
        var minWholeness = GetRandomVariation(ref random, minWholenessVar);

        var growthSpeedVar = config.BlobReference.Value.Entities.Grass.Stats.GrowthSpeed;
        var growthSpeed = GetRandomVariation(ref random, growthSpeedVar);

        var agingFunctionSpanVar = config.BlobReference.Value.Entities.Grass.Stats.AgingFunctionSpan;
        var agingFunctionHeightVar = config.BlobReference.Value.Entities.Grass.Stats.AgingFunctionHeight;
        var agingFunctionSpan = GetRandomVariation(ref random, agingFunctionSpanVar);
        var agingFunctionHeight = GetRandomVariation(ref random, agingFunctionHeightVar);

        var nutritionVar = config.BlobReference.Value.Entities.Grass.Stats.Nutrition;
        var nutrition = GetRandomVariation(ref random, nutritionVar);

        var reproductionSpanVar = config.BlobReference.Value.Entities.Grass.Stats.ReproductionSpan;
        var reproudctionHeightVar = config.BlobReference.Value.Entities.Grass.Stats.ReproductionHeight;
        var reproudctionIntervalVar = config.BlobReference.Value.Entities.Grass.Stats.ReproductionInterval;
        var reproductiveChanceVar = config.BlobReference.Value.Entities.Grass.Stats.ReproductionChance;
        var reproductionSpan = GetRandomVariation(ref random, reproductionSpanVar);
        var reproductionHeight = GetRandomVariation(ref random, reproudctionHeightVar);
        var reproductionInterval = GetRandomVariation(ref random, reproudctionIntervalVar);
        var reproductiveChance = GetRandomVariation(ref random, reproductiveChanceVar);

        var advertisedEnergy = GetRandomVariation(ref random, config.BlobReference.Value.Advertisers.Grass.EnergyValueMin, config.BlobReference.Value.Advertisers.Grass.EnergyValueMax);
        var advertisedFullness = GetRandomVariation(ref random, config.BlobReference.Value.Advertisers.Grass.FullnessValueMin, config.BlobReference.Value.Advertisers.Grass.FullnessValueMax);

        GrassDNAComponent dna = new GrassDNAComponent
        {
            Prefab = prefab,

            // Growing parameters
            MinSize = sizeMin,
            MaxSize = sizeMax,
            MinWholeness = minWholeness,
            MaxWholeness = maxWholeness,
            GrowthSpeed = growthSpeed,

            // Aging parameters
            AgingFunctionSpan = agingFunctionSpan,
            AgingFunctionHeight = agingFunctionHeight,

            // Edible parameters
            MaxNutrition = nutrition,

            // Reproduction parameters
            ReproductionFunctionSpan = reproductionSpan,
            ReproductionFunctionHeight = reproductionHeight,
            ReproductionInterval = reproductionInterval,
            ReproductiveChance = reproductiveChance,

            // Advertisers
            AdvertisedEnergy = advertisedEnergy,
            AdvertisedFullness = advertisedFullness
        };

        return dna;
    }

    private static GrassDNAComponent CreateChildGrassDNA(GrassDNAComponent parentDNA,  ref Random random)
    {
        float2 deviation = new float2(0.9f, 1.1f);

        GrassDNAComponent childDNA = new GrassDNAComponent
        {
            Prefab = parentDNA.Prefab,

            // Growing parameters with variations
            MinSize = GetRandomVariationWithDeviation(ref random, parentDNA.MinSize, deviation),
            MaxSize = GetRandomVariationWithDeviation(ref random, parentDNA.MaxSize, deviation),
            MinWholeness = GetRandomVariationWithDeviation(ref random, parentDNA.MinWholeness, deviation),
            MaxWholeness = GetRandomVariationWithDeviation(ref random, parentDNA.MaxWholeness, deviation),
            GrowthSpeed = GetRandomVariationWithDeviation(ref random, parentDNA.GrowthSpeed, deviation),

            // Aging parameters with variations
            AgingFunctionSpan = GetRandomVariationWithDeviation(ref random, parentDNA.AgingFunctionSpan, deviation),
            AgingFunctionHeight = GetRandomVariationWithDeviation(ref random, parentDNA.AgingFunctionHeight, deviation),

            // Edible parameters with variations
            MaxNutrition = GetRandomVariationWithDeviation(ref random, parentDNA.MaxNutrition, deviation),

            // Reproduction parameters with variations
            ReproductionFunctionSpan = GetRandomVariationWithDeviation(ref random, parentDNA.ReproductionFunctionSpan, deviation),
            ReproductionFunctionHeight = GetRandomVariationWithDeviation(ref random, parentDNA.ReproductionFunctionHeight, deviation),
            ReproductionInterval = GetRandomVariationWithDeviation(ref random, parentDNA.ReproductionInterval, deviation),
            ReproductiveChance = GetRandomVariationWithDeviation(ref random, parentDNA.ReproductiveChance, deviation),

            // Advertisers
            AdvertisedEnergy = GetRandomVariationWithDeviation(ref random, parentDNA.AdvertisedEnergy, deviation),
            AdvertisedFullness = GetRandomVariationWithDeviation(ref random, parentDNA.AdvertisedFullness, deviation)
        };

        return childDNA;
    }

    // Updated to use GrassDNAComponent and regular ECB (not parallel)
    public static void CreateRandomGrass(EntityCommandBuffer ecb, int triangleId, ref Random random, in IcosphereComponent icosphere, Entity prefab, in SimulationConfigComponent config)
    {
        var triangle = icosphere.GetTriangle(triangleId);
        
        var dna = CreateRandomGrassDNA(triangle, prefab, ref random, in config);

        CreateGrass(ecb, in dna, in icosphere, triangleId, ref random);
    }

    public static void CreateGrass(EntityCommandBuffer ecb, in GrassDNAComponent dna, in IcosphereComponent icosphere, int triangleId, ref Random random)
    {
        // Create the grass entity
        Entity grassEntity = ecb.Instantiate(dna.Prefab);

        // Add the DNA component
        ecb.AddComponent(grassEntity, dna);

        // Add the transform component
        ecb.AddComponent(grassEntity, new IcosphereTransform
        {
            TriangleId = triangleId
        });

        var triangle = icosphere.GetTriangle(triangleId);

        // Calculate position based on the triangle centroid on surface
        float3 position = triangle.CentroidOnSurface;

        var randomSeed = random.NextUInt();
        var randomNew = Random.CreateFromIndex(randomSeed);

        // Get rotation based on the triangle orientation
        quaternion rotation = icosphere.GetRotation(triangle.TriangleIndex, randomNew.NextInt(0, 2));

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
            AgingFunctionSpan = dna.AgingFunctionSpan,
            AgingFunctionHeight = dna.AgingFunctionHeight,
            Wholeness = dna.MinWholeness // Start with low wholeness
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
            Nutrition = dna.AdvertisedFullness.x, // Will be calculated based on wholeness
            MaxNutrition = dna.MaxNutrition
        });

        // Add sleepable component
        ecb.AddComponent(grassEntity, new SleepableComponent
        {
            EnergyIncreaseSpeed = dna.AdvertisedEnergy.y
        });

        // Add random component
        ecb.AddComponent(grassEntity, new ActorRandomComponent
        {
            Random = randomNew
        });

        ecb.AddBuffer<AdvertisedActionItem>(grassEntity);
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionTypes.Eat,
            NeedId = Zoo.Enums.NeedType.Fullness,
            NeedsMatrix = dna.AdvertisedFullness
        });
        ecb.AppendToBuffer(grassEntity, new AdvertisedActionItem
        {
            ActionId = Zoo.Enums.ActionTypes.Sleep,
            NeedId = Zoo.Enums.NeedType.Energy,
            NeedsMatrix = dna.AdvertisedEnergy
        });

        // Add reproduction component if can reproduce
        if (IsBeating(ref randomNew, dna.ReproductiveChance))
        {
            ecb.AddComponent(grassEntity, new PlantReproductionComponent
            {
                FunctionFactor = dna.ReproductionFunctionSpan,
                FunctionHeight = dna.ReproductionFunctionHeight,
                Interval = dna.ReproductionInterval,
                ReproductionTimeElapsed = 0f // Initialize elapsed time to 0
            });
        }
    }

    // Creates a new grass entity based on a parent entity's DNA
    public static void CreateChildGrass(EntityCommandBuffer ecb, int triangleId, 
                                 in GrassDNAComponent parentDNA, ref Random random,
                                 in IcosphereComponent icosphere)
    {
        var childDna = CreateChildGrassDNA(parentDNA, ref random);
        CreateGrass(ecb, childDna, in icosphere, triangleId, ref random);
    }


    private static bool IsBeating(ref Random random, float chance)
    {
        return random.NextFloat(0, 1) <= chance;
    }

    private static float GetRandomVariationWithDeviation(ref Random random, float oldValue, float2 deviation)
    {
        return random.NextFloat(deviation.x, deviation.y) * oldValue;
    }

    private static float2 GetRandomVariationWithDeviation(ref Random random, float2 oldValue, float2 deviation)
    {
        return random.NextFloat(deviation.x, deviation.y) * oldValue;
    }

    private static float2 GetRandomVariation(ref Random random, float2 min, float2 max)
    {
        return random.NextFloat2(min, max);
    }

    private static float GetRandomVariation(ref Random random, float2 minMax)
    {
        return random.NextFloat(minMax.x, minMax.y);
    }

    private static float2 GetRandomVariation(ref Random random, float4 minMax)
    {
        return new float2(random.NextFloat(minMax.x, minMax.z), random.NextFloat(minMax.y, minMax.w));
    }
}