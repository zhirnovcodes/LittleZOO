using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlantGrowingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GrowingComponent>();
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

        // Execute jobs directly on the main thread
        foreach (var (aging, growing) in SystemAPI.Query<RefRW<AgingComponent>, RefRO<GrowingComponent>>())
        {
            // Increase wholeness based on growth speed
            aging.ValueRW.Wholeness = math.min(aging.ValueRO.Wholeness + growing.ValueRO.GrowthSpeed * deltaTime, growing.ValueRO.MaxWholeness);
        }

        // Execute jobs directly on the main thread
        foreach (var (aging, edible) in SystemAPI.Query<RefRW<AgingComponent>, RefRW<EdibleComponent>>())
        {
            // Increase wholeness
            edible.ValueRW.Wholeness = aging.ValueRO.Wholeness - edible.ValueRO.BitenPart;
            edible.ValueRW.BitenPart = 0;
            aging.ValueRW.Wholeness = edible.ValueRO.Wholeness;
        }

        foreach (var (transform, aging, growing) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<AgingComponent>, RefRO<GrowingComponent>>())
        {
            // Calculate growth factor (0 to 1)
            float growthFactor = aging.ValueRO.Wholeness / 100f;

            // Interpolate between min and max size based on wholeness
            float3 newScale = math.lerp(growing.ValueRO.MinSize, growing.ValueRO.MaxSize, growthFactor);

            // Apply the new scale
            transform.ValueRW.Scale = newScale.x;
        }

        foreach (var (edible, aging) in SystemAPI.Query<RefRW<EdibleComponent>, RefRO<AgingComponent>>())
        {
            // Calculate nutrition based on wholeness
            float nutritionFactor = aging.ValueRO.Wholeness / 100f;
            edible.ValueRW.Nutrition = edible.ValueRO.MaxNutrition * nutritionFactor;
        }
    }
}
