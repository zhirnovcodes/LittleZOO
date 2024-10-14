using Unity.Collections;
using Unity.Entities;

public partial class HungerSystem : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob).AsParallelWriter();
        var deltaTime = SystemAPI.Time.DeltaTime;

        Entities.
            WithAll<ActorNeedsComponent>().
            ForEach(
            (
                Entity entity,
                int entityInQueryIndex,
                ref ActorNeedsComponent needs
            ) =>
            {
                needs.Hunger -= needs.Hunger / needs.HungerDecayFactor * deltaTime;

                if (needs.Hunger <= 0)
                {
                    // Death
                    StatesExtentions.SetState<DyingStateTag>(entity, ecb, entityInQueryIndex);

                    ecb.SetComponentEnabled<ActorNeedsComponent>(entityInQueryIndex, entity, false);
                    ecb.SetComponentEnabled<VisionComponent>(entityInQueryIndex, entity, false);
                }
            }).ScheduleParallel();
    }
}
