using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(BiologicalSystemGroup))]
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
        //var deltaTime = SystemAPI.Time.DeltaTime;
        //var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        var transfromLookup = SystemAPI.GetComponentLookup<LocalTransform>();

        // Execute jobs directly on the main thread
        foreach (var (aging, edible, growing, children, entity) in 
            SystemAPI.Query<
                    RefRO<AgingComponent>, 
                    RefRW<EdibleComponent>, 
                    RefRO<GrowingComponent>,
                    DynamicBuffer<Child>
                >().
                WithEntityAccess())
        {
            var lifespan = aging.ValueRO.AgeElapsed;
            var function = math.clamp( lifespan * growing.ValueRO.GrowthSpeed, 0, 1);
            var size = math.lerp(growing.ValueRO.Size.x, growing.ValueRO.Size.y, function);
            size -= 1 - edible.ValueRO.Wholeness / 100f;
            size = math.max(growing.ValueRO.Size.x, size);

            var nutrition = math.lerp(edible.ValueRO.NutritionRange.x, edible.ValueRO.NutritionRange.y, function);

            edible.ValueRW.Nutrition = nutrition;

            foreach (var child in children)
            {
                var scaling = child.Value;
                transfromLookup[scaling] = transfromLookup[scaling].WithScale(size);
                break;
            }
        }

        //ecb.Playback(state.EntityManager);
        /*
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
            transform.ValueRW.Scale = newScale.x;
        }

        foreach (var (edible, aging) in SystemAPI.Query<RefRW<EdibleComponent>, RefRO<AgingComponent>>())
        {
            // Calculate nutrition based on wholeness
            float nutritionFactor = aging.ValueRO.Wholeness / 100f;
            edible.ValueRW.NutritionRange = edible.ValueRO.MaxNutrition * nutritionFactor;
        }*/
    }
}
