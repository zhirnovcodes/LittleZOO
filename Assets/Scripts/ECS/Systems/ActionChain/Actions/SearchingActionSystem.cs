using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Zoo.Enums;

[BurstCompile]
public partial struct SearchingActionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Get the planet info
        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        float3 planetCenter = planetTransform.ValueRO.Position;
        float planetScale = planetTransform.ValueRO.Scale;

        // Get spawn data for speed
        var spawnEntity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(spawnEntity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        var referenceLookup = SystemAPI.GetBufferLookup<Child>();

        // Second job - process selected entities
        state.Dependency = new SearchActionJob
        {
            Ecb = ecb,
            PlanetCenter = planetCenter,
            PlanetScale = planetScale,
            WalkingSpeed = walkingSpeed,
            ReferenceLookup = referenceLookup,
            DeltaTime = deltaTime
        }.Schedule(state.Dependency);

        state.Dependency.Complete();

        // Clean up
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private partial struct SearchActionJob : IJobEntity
    {
        public EntityCommandBuffer Ecb;

        public BufferLookup<Child> ReferenceLookup;

        public float3 PlanetCenter;
        public float PlanetScale;
        public float WalkingSpeed;
        public float DeltaTime;

        // Process entities that have either MoveToTargetOutputComponent or HungerComponent
        // We use SystemAPI to get components as needed
        public void Execute
        (
            Entity entity,
            ref MoveToTargetInputComponent moveInput,
            ref ActorRandomComponent randomComponent,
            ref ActorNeedsComponent needs,
            in MoveToTargetOutputComponent moveOutput,
            in NeedBasedSystemOutput needsOutput,
            in SearchingStateTag tag
        )
        {
            // Process movement logic
            if (moveOutput.NoTargetSet || moveOutput.HasArivedToTarget)
            {
                moveInput.TargetPosition = GenerateTargetPosition(ref randomComponent, PlanetCenter, PlanetScale);
                moveInput.Speed = WalkingSpeed;

            }

            needs.Energy -= DeltaTime * needs.EnergyDecayFactor;
            needs.Fullness -= DeltaTime * needs.HungerDecayFactor;

            if (needs.Fullness <= 0)
            {
                UnityEngine.Debug.Log("Die");
                Die(entity);
                return;
            }

            switch (needsOutput.Action)
            {
                case ActionTypes.Eat:
                    Ecb.SetComponentEnabled<EatingStateTag>(entity, true);
                    Ecb.SetComponentEnabled<SearchingStateTag>(entity, false);
                    Ecb.SetIdleAnimation(ReferenceLookup.GetView(entity));
                    break;
                case ActionTypes.Sleep:
                    Ecb.SetComponentEnabled<SleepingStateTag>(entity, true);
                    Ecb.SetComponentEnabled<SearchingStateTag>(entity, false);
                    Ecb.SetIdleAnimation(ReferenceLookup.GetView(entity));
                    break;
            }
        }

        private void Die(Entity entity)
        {
            var viewEntity = ReferenceLookup.GetView(entity);

            Ecb.SetComponentEnabled<DyingStateTag>(entity, true);
            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entity, false);
            Ecb.SetComponentEnabled<SearchingStateTag>(entity, false);
            Ecb.SetComponentEnabled<ActorNeedsComponent>(entity, false);
            Ecb.SetComponentEnabled<VisionComponent>(entity, false);
            Ecb.SetComponent(entity, new StateTimeComponent{ StateTimeElapsed = 0 });

            Ecb.SetDyingAnimation(viewEntity);

        }
    }

    // Helper method for generating target positions
    private static float3 GenerateTargetPosition(ref ActorRandomComponent random, float3 planetCenter, float planetScale)
    {
        float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        return planetCenter + math.normalize(randomTarget) * planetScale / 2f;
    }
}
