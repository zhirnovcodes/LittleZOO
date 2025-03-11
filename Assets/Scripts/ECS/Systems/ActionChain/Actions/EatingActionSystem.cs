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
        // Require these components for the system to run
    }

    // OnDestroy is called when the system is destroyed
    public void OnDestroy(ref SystemState state)
    {
        // Clean up any resources if needed
    }

    // OnUpdate is called every frame the system runs
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Create command buffer for structural changes
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Get component lookups
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        var edibleLookup = SystemAPI.GetComponentLookup<EdibleComponent>();

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
            WalkingSpeed = walkingSpeed
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

        public EntityCommandBuffer Ecb;
        public float WalkingSpeed;
        public float DeltaTime;

        // TODO to blob
        const float eatDeltaTime = 1f;
        const float biteWholeness = 10f;

        [BurstCompile]
        private void Execute
        (
            Entity entity,
            ref MoveToTargetInputComponent moveInput,
            ref EatingStateTag eatingTag,
            ref ActorNeedsComponent needs,
            in MoveToTargetOutputComponent moveOutput,
            in HungerComponent hunger
        )
        {

            // TODO lost sight
            if (EdibleLookup.TryGetComponent(hunger.Target, out var edable) == false)
            {
                SetSearchingState(entity);
                return;
            }

            if (TransformLookup.TryGetComponent(hunger.Target, out var edableTransform) == false)
            {
                SetSearchingState(entity);
                return;
            }

            // TODO to const
            if (needs.Fullness <= 90f)
            {
                SetSearchingState(entity);
                return;
            }

            moveInput.TargetPosition = edableTransform.Position;
            moveInput.TargetScale = edableTransform.Scale;
            moveInput.Speed = WalkingSpeed;

            var hasArrived = moveOutput.HasArivedToTarget;

            if (hasArrived)
            {
                moveInput.Speed = 0;

                eatingTag.BiteTimeElapsed += DeltaTime;

                if (eatingTag.BiteTimeElapsed >= eatDeltaTime)
                {
                    eatingTag.BiteTimeElapsed = 0;
                    Bite(ref needs, hunger.Target);

                    if (edable.Wholeness <= 0)
                    {
                        SetSearchingState(entity);
                        Ecb.DestroyEntity(hunger.Target);
                        return;
                    }
                }
            }
            else
            {
                eatingTag.BiteTimeElapsed = 0;
            }
        }

        private void Bite(ref ActorNeedsComponent needs, Entity target)
        {
            var edibleComponent = EdibleLookup.GetRefRW(target);

            var oldWholeness = edibleComponent.ValueRO.Wholeness;
            var biteValue = math.min(biteWholeness, oldWholeness);

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
    }
}