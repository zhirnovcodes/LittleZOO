using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
[UpdateInGroup(typeof(ZooPhysicsSystem))]
[RequireMatchingQueriesForUpdate]
public partial class FallStateSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        var ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

        Dependency = Entities.
            WithAll<FallingStateData>().
            ForEach(
            (
                Entity entity,
                int entityInQueryIndex,
                in LocalTransform transform,
                in GravityComponent gravity
            ) =>
        {
            if (gravity.IsTouchingPlanet)
            {
                ecb.SetComponentEnabled<FallingStateData>(entityInQueryIndex, entity, false);
                ecb.SetComponentEnabled<MovingStateData>(entityInQueryIndex, entity, true);
            }
        }).ScheduleParallel(Dependency);
    }
}
