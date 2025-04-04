using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Enums;

// Convert to ISystem
public partial struct EatingActionSystem : ISystem
{
    // OnCreate is called when the system is created
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
    }

    // OnDestroy is called when the system is destroyed
    public void OnDestroy(ref SystemState state)
    {
        // Clean up any resources if needed
    }

    // OnUpdate is called every frame the system runs
    public void OnUpdate(ref SystemState state)
    {
        return;
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Create command buffer for structural changes
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Get component lookups
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        var edibleLookup = SystemAPI.GetComponentLookup<EdibleComponent>();
        var referenceLookup = SystemAPI.GetBufferLookup<Child>();
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();

        // TODO from blob
        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        // Schedule the job
        state.Dependency = new EatingActionSyncJob
        {
            TransformLookup = transformLookup,
            EdibleLookup = edibleLookup,
            Ecb = ecb,
            DeltaTime = deltaTime,
            WalkingSpeed = walkingSpeed,
            ReferenceLookup = referenceLookup,
            BiteInterval = config.BlobReference.Value.Actions.Pigs.EatInterval,
            BiteWholeness = config.BlobReference.Value.Actions.Pigs.BiteWholeness
        }.Schedule(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct EatingActionSyncJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> TransformLookup;
        public ComponentLookup<EdibleComponent> EdibleLookup;
        public BufferLookup<Child> ReferenceLookup;

        public EntityCommandBuffer Ecb;
        public float WalkingSpeed;
        public float DeltaTime;

        public float2 BiteWholeness;
        public float2 BiteInterval;

        [BurstCompile]
        private void Execute
        (
            Entity entity,
            ref MovingInputComponent moveInput,
            ref EatingStateTag eatingTag,
            ref ActorNeedsComponent needs,
            ref ActorRandomComponent random,
            in MovingOutputComponent moveOutput,
            in NeedBasedSystemOutput needOutput
        )
        {
            if (needOutput.Action == ActionTypes.Eat == false)
            {
                SetSearchingState(entity);
                Ecb.SetIdleAnimation(ReferenceLookup.GetView(entity));
                return;
            }

            var target = needOutput.Advertiser;

            // TODO lost sight
            if (EdibleLookup.TryGetComponent(target, out var edable) == false)
            {
                SetSearchingState(entity);
                Ecb.SetIdleAnimation(ReferenceLookup.GetView(entity));
                return;
            }

            if (TransformLookup.TryGetComponent(target, out var edableTransform) == false)
            {
                SetSearchingState(entity);
                Ecb.SetIdleAnimation(ReferenceLookup.GetView(entity));
                return;
            }

            moveInput.TargetPosition = edableTransform.Position;
            moveInput.TargetScale = edableTransform.Scale;
            moveInput.Speed = WalkingSpeed;

            var hasArrived = moveOutput.HasArivedToTarget;

            // TODO to DNA
            var eatDeltaTime = random.Random.NextFloat(BiteInterval.x, BiteInterval.y);
            var biteWholeness = random.Random.NextFloat(BiteWholeness.x, BiteWholeness.y);

            if (hasArrived)
            {
                moveInput.Speed = 0;

                eatingTag.BiteTimeElapsed += DeltaTime;

                if (eatingTag.BiteTimeElapsed >= eatDeltaTime)
                {
                    eatingTag.BiteTimeElapsed = 0;
                    Bite(ref needs, target, biteWholeness);

                    if (edable.Wholeness <= 0)
                    {
                        Ecb.DestroyEntity(target);
                        return;
                    }
                }
                return;
            }

            //needs.Energy -= DeltaTime * needs.EnergyDecayFactor;
            //needs.Fullness -= DeltaTime * needs.HungerDecayFactor;

            if (needs.Fullness <= 0)
            {
                UnityEngine.Debug.Log("Die");
                Die(entity);
                return;
            }

            eatingTag.BiteTimeElapsed = 0;
        }

        private void Bite(ref ActorNeedsComponent needs, Entity target, float wholeness)
        {// TODO from advertiser
            var edibleComponent = EdibleLookup.GetRefRW(target);

            var oldWholeness = edibleComponent.ValueRO.Wholeness;
            var biteValue = math.min(wholeness, oldWholeness);

            var nutritiousAll = edibleComponent.ValueRO.Nutrition;

            var nutritiousValue = biteValue * nutritiousAll / 100f;

            needs.Fullness = math.min(100, needs.Fullness + nutritiousValue);
            edibleComponent.ValueRW.BitenPart += biteValue;
        }

        private void SetSearchingState(Entity entity)
        {
            Ecb.SetComponentEnabled<SearchingStateTag>(entity, true);
            Ecb.SetComponentEnabled<EatingStateTag>(entity, false);
        }

        private void Die(Entity entity)
        {
            var viewEntity = ReferenceLookup.GetView(entity);

            Ecb.SetComponentEnabled<DyingStateTag>(entity, true);
            Ecb.SetComponentEnabled<MovingInputComponent>(entity, false);
            Ecb.SetComponentEnabled<EatingStateTag>(entity, false);
            Ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, false);
            Ecb.SetComponentEnabled<StateTimeComponent>(entity, false);
            Ecb.SetComponentEnabled<VisionComponent>(entity, false);

            Ecb.SetDyingAnimation(viewEntity);

        }
    }
}