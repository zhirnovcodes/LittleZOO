using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Zoo.Enums;

public partial struct SleepingActionSystem : ISystem
{
    private void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Create command buffer for structural changes
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Get component lookups
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        var bufferLookup = SystemAPI.GetBufferLookup<AdvertisedActionItem>();
        var referenceLookup = SystemAPI.GetBufferLookup<Child>();

        // TODO from blob
        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        // Schedule the job
        state.Dependency = new SleepingActionSyncJob
        {
            TransformLookup = transformLookup,
            BufferLookup = bufferLookup,
            Ecb = ecb,
            DeltaTime = deltaTime,
            WalkingSpeed = walkingSpeed,
            ReferenceLookup = referenceLookup
        }.Schedule(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct SleepingActionSyncJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> TransformLookup;
        public BufferLookup<AdvertisedActionItem> BufferLookup;
        public BufferLookup<Child> ReferenceLookup;

        public EntityCommandBuffer Ecb;
        public float WalkingSpeed;
        public float DeltaTime;

        [BurstCompile]
        private void Execute
        (
            Entity entity,
            ref MoveToTargetInputComponent moveInput,
            ref ActorNeedsComponent needs,
            ref SleepingStateTag sleepingTag,
            in MoveToTargetOutputComponent moveOutput,
            in NeedBasedSystemOutput needOutput
        )
        {
            var hasArrived = moveOutput.HasArivedToTarget;
            var target = needOutput.Advertiser;

            if (TransformLookup.TryGetComponent(target, out var sleepableTransform) == false)
            {
                WakeUp(entity, ref sleepingTag);
                return;
            }

            if (sleepingTag.IsSleeping)
            {
                if (needOutput.Action == ActionTypes.Sleep == false)
                {
                    WakeUp(entity, ref sleepingTag);
                    return;
                }


                if (BufferLookup.TryGetBuffer(target, out var buffer) == false)
                {
                    WakeUp(entity, ref sleepingTag);
                    return;
                }

                Sleep(ref needs, buffer);

                return;
            }

            // Is awake and going to target

            if (hasArrived)
            {
                FallAsleep(entity, ref sleepingTag);
                return;
            }

            // Still going
            moveInput.TargetPosition = sleepableTransform.Position;
            moveInput.TargetScale = 0;
            moveInput.Speed = WalkingSpeed;

            needs.Energy -= DeltaTime * needs.EnergyDecayFactor;
            needs.Fullness -= DeltaTime * needs.HungerDecayFactor;

            if (needs.Fullness <= 0)
            {
                UnityEngine.Debug.Log("Die");
                Die(entity);
            }


        }

        private void Sleep(ref ActorNeedsComponent needs, in DynamicBuffer<AdvertisedActionItem> buffer)
        {
            var foundEnergy = 0f;

            foreach (var item in buffer)
            {
                if (item.ActionId == ActionTypes.Sleep)
                {
                    foundEnergy = item.NeedsMatrix[(int)NeedType.Energy];
                    break;
                }
            }

            needs.Energy += DeltaTime * foundEnergy;
        } 

        private void FallAsleep(Entity entity, ref SleepingStateTag tag)
        {
            var viewEntity = ReferenceLookup.GetView(entity);

            // TODO change ingterval of thinking
            Ecb.SetSleepingAnimation(viewEntity);
            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entity, false);
            tag.IsSleeping = true;
        }

        private void WakeUp(Entity entity, ref SleepingStateTag tag)
        {
            var viewEntity = ReferenceLookup.GetView(entity);

            Ecb.SetComponentEnabled<SearchingStateTag>(entity, true);
            Ecb.SetComponentEnabled<SleepingStateTag>(entity, false);
            Ecb.SetIdleAnimation(viewEntity);
            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entity, true);
            tag.IsSleeping = false;
        }

        private void Die(Entity entity)
        {
            var viewEntity = ReferenceLookup.GetView(entity);

            Ecb.SetComponentEnabled<DyingStateTag>(entity, true);
            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entity, false);
            Ecb.SetComponentEnabled<SleepingStateTag>(entity, false);
            Ecb.SetComponentEnabled<ActorNeedsComponent>(entity, false);
            Ecb.SetComponentEnabled<StateTimeComponent>(entity, false);
            Ecb.SetComponentEnabled<VisionComponent>(entity, false);

            Ecb.SetDyingAnimation(viewEntity);

        }
    }
}
