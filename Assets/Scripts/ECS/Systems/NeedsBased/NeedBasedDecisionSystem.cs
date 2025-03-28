using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zoo.Enums;

[BurstCompile]
public partial struct NeedBasedDecisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();

        var parallelEcb = ecb.AsParallelWriter();
        var advertisedActionLookup = SystemAPI.GetBufferLookup<AdvertisedActionItem>(true);

        var job = new NeedBasedDecisionJob
        {
            Ecb = parallelEcb,
            AdvertisedActionLookup = advertisedActionLookup,
            HungerDecayFactor = config.BlobReference.Value.Needs.HungerDecayFactor,
            EnergyDecayFactor = config.BlobReference.Value.Needs.EnergyDecayFactor
        };

        // Schedule the job properly
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    partial struct NeedBasedDecisionJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<AdvertisedActionItem> AdvertisedActionLookup;
        public float2 HungerDecayFactor;
        public float2 EnergyDecayFactor;
        public EntityCommandBuffer.ParallelWriter Ecb;

        void Execute(
            [EntityIndexInQuery] int sortKey,
            Entity entity,
            in ActorNeedsComponent actorNeeds,
            [ReadOnly] in DynamicBuffer<VisionItem> visibleEntities)
        {
            float maxSum = float.MinValue;
            Entity bestAdvertiser = Entity.Null;
            ActionTypes bestAction = default;

            foreach (var visibleEntity in visibleEntities)
            {
                var advertiser = visibleEntity.VisibleEntity;

                if (!AdvertisedActionLookup.TryGetBuffer(advertiser, out var actions))
                    continue;

                foreach (var action in actions)
                {
                    var naturalDecrease = GetNeedDecayFactor(action.NeedId);

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

            if (bestAdvertiser == Entity.Null)
            {
                Ecb.SetComponent(sortKey, entity, new NeedBasedSystemOutput
                {
                    Action = ActionTypes.Search,
                    Advertiser = Entity.Null
                });
                return;
            }

            Ecb.SetComponent(sortKey, entity, new NeedBasedSystemOutput
            {
                Action = bestAction,
                Advertiser = bestAdvertiser
            });
        }

        private float2 GetNeedDecayFactor(NeedType need)
        {
            switch (need)
            {
                case NeedType.Fullness:
                    return HungerDecayFactor;
                case NeedType.Energy:
                    return EnergyDecayFactor;
            }
            return 0;
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
            float newFullness = 
                needs.Fullness +
                (action.NeedsMatrix[0] * duration) +
                (naturalDecrease[0] * duration);

            float newEnergy = 
                needs.Energy +
                (action.NeedsMatrix[1] * duration) +
                (naturalDecrease[1] * duration);

            var newMoodValue = newFullness + newEnergy;

            return (newMoodValue, newMoodValue > 0);
        }
    }
}