using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(ZooPhysicsSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class FallStateSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        //var ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        //var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

        /*
        Entities.
            WithAll<FallingStateTag>().
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
                ecb.SetComponentEnabled<FallingStateTag>(entityInQueryIndex, entity, false);
                ecb.SetComponentEnabled<MoveToTargetComponent>(entityInQueryIndex, entity, true);
            }
        }).ScheduleParallel();*/
    }
}
