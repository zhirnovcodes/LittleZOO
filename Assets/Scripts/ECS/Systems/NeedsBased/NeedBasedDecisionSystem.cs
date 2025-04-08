using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Enums;


[BurstCompile]
[UpdateInGroup(typeof(AISystemGroup))]
[UpdateBefore(typeof(ActionManagerSystem))]
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
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);

        var deltaTime = SystemAPI.Time.DeltaTime;

        var job = new NeedBasedDecisionJob3
        {
            PlanetEntity = planetEntity,
            PlanetSize = planetTransform.ValueRO.Scale,
            AdvertisedActionLookup = advertisedActionLookup,
            TransformLookup = transformLookup,
            DeltaTime = deltaTime
        };

        // Schedule the job properly
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
    [BurstCompile]
    partial struct NeedBasedDecisionJob3 : IJobEntity
    {
        public float PlanetSize;
        public float DeltaTime;
        public Entity PlanetEntity;

        [ReadOnly] public BufferLookup<AdvertisedActionItem> AdvertisedActionLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(
            Entity actor,
            ref NeedBasedSystemOutput output,
            ref NeedBasedDecisionTag tag,
            in ActorNeedsComponent needs,
            in HungerComponent hunger,
            in EnergyComponent energy,
            [ReadOnly] in DynamicBuffer<VisionItem> visibleEntities)
        {
            const float interval = 1f;

            if (tag.TimeElapsed < interval)
            {
                tag.TimeElapsed += DeltaTime;

                return;
            }

            tag.TimeElapsed = 0;

            if (!TransformLookup.TryGetComponent(actor, out var actorTransform))
                return;

            float bestScore = 0;
            Entity bestAdvertiser = Entity.Null;
            ActionTypes bestAction = ActionTypes.Idle;

            float2 bestNeed = float2.zero;
            float bestDuration = 0;

            // --- Visible Entity Checks ---
            foreach (var visible in visibleEntities)
            {
                var advertiser = visible.VisibleEntity;

                if (!TransformLookup.TryGetComponent(advertiser, out var advertiserTransform))
                    continue;

                if (!AdvertisedActionLookup.TryGetBuffer(advertiser, out var ads))
                    continue;

                float distance = GetSphericalArcLength(actorTransform.Position, advertiserTransform.Position, PlanetSize);

                foreach (var ad in ads)
                {
                    float2 resultNeeds = needs.Needs + ad.NeedsMatrix;
                    float performTime = EstimatePerformTime(resultNeeds, needs, hunger, energy, ad.NeedsMatrix);

                    float clampedFullness = math.clamp(resultNeeds.x, 0, 100);
                    float clampedEnergy = math.clamp(resultNeeds.y, 0, 100);

                    float score = clampedFullness + clampedEnergy;

                    // Apply distance attenuation
                    float distanceAttenuation = hunger.FullnessDecayByDistance * distance +
                        energy.EnergyDecayByDistance * distance;
                    score -= distanceAttenuation; // avoid divide by 0

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAdvertiser = advertiser;
                        bestAction = ad.ActionId;
                        bestNeed = resultNeeds;
                        bestDuration = performTime;
                    }
                }
            }

            // --- Planet-Level Advertisements (NO distance attenuation) ---
            if (AdvertisedActionLookup.TryGetBuffer(PlanetEntity, out var planetAds))
            {
                foreach (var ad in planetAds)
                {
                    float2 resultNeeds = needs.Needs + ad.NeedsMatrix;
                    float performTime = EstimatePerformTime(resultNeeds, needs, hunger, energy, ad.NeedsMatrix);

                    float clampedFullness = math.clamp(resultNeeds.x, 0, 100);
                    float clampedEnergy = math.clamp(resultNeeds.y, 0, 100);

                    float score = clampedFullness + clampedEnergy;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAdvertiser = PlanetEntity;
                        bestAction = ad.ActionId;
                        bestNeed = resultNeeds;
                        bestDuration = performTime;
                    }
                }
            }

            // --- IDLE Check ---
            float idleFullness = needs.Fullness() - (hunger.FullnessDecaySpeed * bestDuration);
            float idleEnergy = needs.Energy() - (energy.EnergyDecaySpeed * bestDuration);
            float idleScore = math.clamp(idleFullness, 0, 100) + math.clamp(idleEnergy, 0, 100);

            if (idleScore > bestScore)
            {
                bestScore = idleScore;
                bestAdvertiser = Entity.Null;
                bestAction = ActionTypes.Idle;
            }

            output.Action = bestAction;
            output.Advertiser = bestAdvertiser;
        }

        float GetSphericalArcLength(float3 from, float3 to, float size)
        {
            float dot = math.dot(math.normalize(from), math.normalize(to));
            float angle = math.acos(math.clamp(dot, -1f, 1f));
            return size * angle / 2;
        }

        float EstimatePerformTime(float2 resultingNeeds, in ActorNeedsComponent current, in HungerComponent hunger, in EnergyComponent energy, float2 needGainRate)
        {
            float2 delta = resultingNeeds - current.Needs;

            float timeFullness = delta.x / math.max(0.001f, needGainRate.x - hunger.FullnessDecaySpeed);
            float timeEnergy = delta.y / math.max(0.001f, needGainRate.y - energy.EnergyDecaySpeed);

            return math.max(0, math.max(timeFullness, timeEnergy));
        }
    }
    /*
    [BurstCompile]
    partial struct NeedBasedDecisionJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<AdvertisedActionItem> AdvertisedActionLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float2 HungerDecayFactor;
        public float2 EnergyDecayFactor;

        void Execute(
            [EntityIndexInQuery] int sortKey,
            Entity entity,
            in ActorNeedsComponent actorNeeds,
            in NeedBasedDecisionTag tag,
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
    }*/
    /*
    [BurstCompile]
    partial struct NeedBasedDecisionJob2 : IJobEntity
    {
        public float PlanetSize;
        public float DeltaTime;

        [ReadOnly] public BufferLookup<AdvertisedActionItem> AdvertisedActionLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public EntityCommandBuffer.ParallelWriter Ecb;

        void Execute(
            [EntityIndexInQuery] int sortKey,
            Entity actor,
            ref NeedBasedSystemOutput output,
            ref NeedBasedDecisionTag tag,
            in ActorNeedsComponent actorNeeds,
            in MovingSpeedComponent speedComponent,
            in HungerComponent hungerComponent,
            in EnergyComponent energyComponent,
            //[ReadOnly] in DynamicBuffer<ActionTypes> actorActions,
            [ReadOnly] in DynamicBuffer<VisionItem> visibleEntities)
        {
            const float interval = 1;

            if (tag.TimeElapsed < interval)
            {
                tag.TimeElapsed += DeltaTime;
                return;
            }

            tag.TimeElapsed = 0;

            if (!TransformLookup.TryGetComponent(actor, out var actorTransform))
                return;

            float bestNeedSum = 0;
            Entity bestAdvertiser = Entity.Null;
            ActionTypes bestAction = default;

            // Check all visible entities

            foreach (var visible in visibleEntities)
            {
                var advertiser = visible.VisibleEntity;

                if (!TransformLookup.TryGetComponent(advertiser, out var advertiserTransform))
                    continue;

                float distance = GetSphericalArcLength(actorTransform.Position, advertiserTransform.Position, PlanetSize);

                var (bestActorSum, bestActorAcition) = GetBestAction
                    (
                        advertiser,
                        distance,
                        speedComponent,
                        //actorActions,
                        actorNeeds,
                        hungerComponent,
                        energyComponent
                    );

                if (bestActorSum > bestNeedSum)
                {
                    bestNeedSum = bestActorSum;
                    bestAdvertiser = advertiser;
                    bestAction = bestActorAcition;
                }
            }

            // Check planet

            output.Action = bestAdvertiser == Entity.Null ? ActionTypes.Idle : bestAction;
            output.Advertiser = bestAdvertiser;
        }
        private (float, ActionTypes) GetBestAction(
            Entity advertiser,
            float distance,
            in MovingSpeedComponent speedComponent,
            //in DynamicBuffer<ActionTypes> actorActions,
            in ActorNeedsComponent actorNeeds,
            in HungerComponent hunger,
            in EnergyComponent energy
            )
        {
            float bestSum = 0;
            var resultAction = ActionTypes.Idle;

            if (AdvertisedActionLookup.TryGetBuffer(advertiser, out var advertisedActions) == false)
            {
                return (bestSum, resultAction);
            }

            var speed = math.lerp(speedComponent.SpeedRange.x, speedComponent.SpeedRange.y, 0.5f);
            float travelTime = distance / speed;

            foreach (var advertised in advertisedActions)
            {
                //if (!ContainsAction(actorActions, advertised.ActionId))
                //    continue;

                var (resultingNeed, valid, time) = EvaluateAdvertisedAction(
                    actorNeeds,
                    advertised,
                    travelTime,
                    distance,
                    hunger,
                    energy);

                if (!valid)
                    continue;

                // Now calculate idle result for same duration as this action (travel + perform)
                float idleResult = CalculateIdleOutcome(actorNeeds, hunger, energy, time);

                if (resultingNeed > idleResult)
                {
                    bestSum = resultingNeed;
                    resultAction = advertised.ActionId;
                }
            }

            return (bestSum, resultAction);
        }

        private float CalculateIdleOutcome(
            in ActorNeedsComponent needs,
            in HungerComponent hunger,
            in EnergyComponent energy,
            float duration)
        {
            float newFullness = needs.Fullness - (hunger.FullnessDecaySpeed * duration);
            float newEnergy = needs.Energy - (energy.EnergyDecaySpeed * duration);
            return newFullness + newEnergy;
        }

        private (float resultSum, bool valid, float actionTime) EvaluateAdvertisedAction(
            in ActorNeedsComponent needs,
            in AdvertisedActionItem action,
            float travelTime,
            float travelDistance,
            in HungerComponent hunger,
            in EnergyComponent energy)
        {
            // Spend for travel
            var travelFullnessDecay = hunger.FullnessDecaySpeed * travelTime + hunger.FullnessDecayByDistance * travelDistance;
            var travelEnergyDecay = energy.EnergyDecaySpeed * travelTime + energy.EnergyDecayByDistance * travelDistance;

            var newFullness = needs.Fullness - travelFullnessDecay;
            var newEnergy = needs.Energy - travelEnergyDecay;

            if (newFullness <= 0 || newEnergy <= 0)
            {
                return (0, false, 0);
            }

            // Natural spend while performing
            float2 gainPerSecond = action.NeedsMatrix;
            int needIndex = (int)action.NeedId;
            float currentNeed = needIndex == 0 ? needs.Fullness : needs.Energy;
            float needed = 100f - currentNeed;
            var increase = gainPerSecond[needIndex];
            var decrease = needIndex == 0 ? hunger.FullnessDecaySpeed : energy.EnergyDecaySpeed;

            if (needed <= 0 || increase <= 0 || increase <= decrease)
            {
                return (0, false, 0);
            }

            float performTime = needed / (increase - decrease);

            newFullness = math.clamp( newFullness + ( gainPerSecond[0] - hunger.FullnessDecaySpeed) * performTime, 0, 100);
            newEnergy = math.clamp( newEnergy + (gainPerSecond[1] - energy.EnergyDecaySpeed) * performTime, 0, 100);

            // Pack result needs and time (z) into float3 for comparison
            return (newFullness + newEnergy, true, performTime + travelTime);
        }

        /*
        private bool ContainsAction(in DynamicBuffer<ActionTypes> buffer, ActionTypes action)
        {
            foreach (var a in buffer)
                if (a == action) return true;
            return false;
        }*/
    /*
        private float GetSphericalArcLength(float3 from, float3 to, float radius)
        {
            float dot = math.dot(math.normalize(from), math.normalize(to));
            float angle = math.acos(math.clamp(dot, -1f, 1f));
            return radius * angle;
        }

    }*/
}