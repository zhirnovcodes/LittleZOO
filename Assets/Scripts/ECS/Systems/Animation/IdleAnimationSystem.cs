using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Idle Animation System - sets rotation to identity
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct IdleAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IdleAnimationTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var transform in
            SystemAPI.Query<RefRW<LocalTransform>>().
                WithAll<IdleAnimationTag>())
        {
            transform.ValueRW.Rotation = quaternion.identity;
        }
    }
}

// Sleeping Animation System - rotates to 90 degrees around the look axis
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SleepingAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SleepingAnimationTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, sleeping, animation) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<SleepingAnimationTag>, RefRW<AnimationComponent>>())
        {
            animation.ValueRW.TimeElapsed += deltaTime;
            float progress = math.saturate(animation.ValueRO.TimeElapsed / sleeping.ValueRO.Length);
            float angle = progress * math.PI / 2.0f; // 90 degrees in radians
            transform.ValueRW.Rotation = quaternion.AxisAngle(new float3(0, 0, 1), angle);
        }
    }
}
/*
// Waking Up Animation System - rotates from 90 degrees back to identity
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WakingUpAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WakingUpAnimationTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (viewRef, animComp, entity) in
            SystemAPI.Query<RefRO<ViewReference>, RefRW<AnimationComponent>>()
                .WithAll<WakingUpAnimationTag>()
                .WithEntityAccess())
        {
            // Update animation time
            animComp.ValueRW.AnimationTimeElapsed += deltaTime;

            // Calculate progress (0 to 1)
            float progress = math.saturate(animComp.ValueRO.AnimationTimeElapsed / animComp.ValueRO.AnimationLength);

            // Access the View entity
            Entity viewEntity = viewRef.ValueRO.ViewEntity;

            // Apply rotation to the View entity
            if (SystemAPI.HasComponent<LocalTransform>(viewEntity))
            {
                var viewTransform = SystemAPI.GetComponent<LocalTransform>(viewEntity);
                // Rotate from 90 degrees back to 0 degrees around the look axis
                float angle = (1.0f - progress) * math.PI / 2.0f; // 90 degrees to 0 degrees in radians
                viewTransform.Rotation = quaternion.AxisAngle(new float3(0, 0, 1), angle);
                ecb.SetComponent(viewEntity, viewTransform);
            }
        }
    }
}
*/
// Dying Animation System - rotates to 90 degrees around the look axis
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DyingAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DyingAnimationTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, dying, animation) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<DyingAnimationTag>, RefRW<AnimationComponent>>())
        {
            animation.ValueRW.TimeElapsed += deltaTime;
            float progress = math.saturate(animation.ValueRO.TimeElapsed / dying.ValueRO.Length);
            float angle = progress * math.PI / 2.0f; // 90 degrees in radians
            transform.ValueRW.Rotation = quaternion.AxisAngle(new float3(0, 0, 1), angle);
        }
    }
}