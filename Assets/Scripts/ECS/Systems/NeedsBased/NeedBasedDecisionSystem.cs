using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zoo.Enums;

[BurstCompile]
public partial struct NeedBasedDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        var parallelEcb = ecb.AsParallelWriter();
        var advertisedActionLookup = SystemAPI.GetBufferLookup<AdvertisedActionItem>(true);

        var job = new NeedBasedDecisionJob
        {
            ECB = parallelEcb,
            AdvertisedActionLookup = advertisedActionLookup
        };

        // Schedule the job properly
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
    /*
    partial struct LogJob : IJobEntity
    {
        void Execute(
                in NeedBasedSystemOutput output
            )
        {
            if (output.Advertiser != Entity.Null)
                JobLogger.Log(output.Action);
        }
    }*/

    [BurstCompile]
    partial struct NeedBasedDecisionJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<AdvertisedActionItem> AdvertisedActionLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute(
            [EntityIndexInQuery] int sortKey,
            Entity entity,
            in ActorNeedsComponent actorNeeds,
            [ReadOnly] in DynamicBuffer<VisionItem> visibleEntities)
        {
            float maxSum = float.MinValue;
            Entity bestAdvertiser = Entity.Null;
            ActionID bestAction = default;

            foreach (var visibleEntity in visibleEntities)
            {
                //todo blob
                float2 naturalDecrease = new float2(-0.1f, -0.2f);

                var advertiser = visibleEntity.VisibleEntity;

                if (!AdvertisedActionLookup.TryGetBuffer(advertiser, out var actions))
                    continue;

                foreach (var action in actions)
                {
                    var (sum, valid) = CalculateActionEffect(
                        in actorNeeds,
                        in action,
                        naturalDecrease
                    );

                    if (valid && sum > maxSum)
                    {
                        maxSum = sum;
                        bestAdvertiser = advertiser;
                        bestAction = action.ActionId;
                    }
                }
            }

            if (bestAdvertiser != Entity.Null)
            {
                ECB.SetComponent(sortKey, entity, new NeedBasedSystemOutput
                {
                    Action = bestAction,
                    Advertiser = bestAdvertiser
                });
            }
        }

        (float Sum, bool Valid) CalculateActionEffect(
            in ActorNeedsComponent needs,
            in AdvertisedActionItem action,
            float2 naturalDecrease)
        {

            // Get relevant need values
            float currentNeedValue = action.NeedId == NeedType.Fullness
                ? needs.Fullness
                : needs.Energy;

            float needed = 100 - currentNeedValue;
            float perSecond = action.NeedsMatrix[(int)action.NeedId];

            // Skip invalid/incomplete actions
            if (perSecond <= 0 || needed <= 0)
                return (0, false);

            // Calculate duration needed to fulfill target need
            float duration = needed / perSecond;

            // Calculate final values with natural decrease
            float newFullness = math.clamp(
                needs.Fullness +
                (action.NeedsMatrix[0] * duration) +
                (naturalDecrease[0] * duration),
                0, 100
            );

            float newEnergy = math.clamp(
                needs.Energy +
                (action.NeedsMatrix[1] * duration) +
                (naturalDecrease[1] * duration),
                0, 100
            );

            return (newFullness + newEnergy, true);
        }
    }
}