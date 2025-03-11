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

        // Get the planet info
        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        float3 planetCenter = planetTransform.ValueRO.Position;
        float planetScale = planetTransform.ValueRO.Scale;

        // Get spawn data for speed
        var spawnEntity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(spawnEntity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        // Second job - process selected entities
        state.Dependency = new SearchActionJob
        {
            Ecb = ecb,
            PlanetCenter = planetCenter,
            PlanetScale = planetScale,
            WalkingSpeed = walkingSpeed
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

        public float3 PlanetCenter;
        public float PlanetScale;
        public float WalkingSpeed;

        // Process entities that have either MoveToTargetOutputComponent or HungerComponent
        // We use SystemAPI to get components as needed
        public void Execute
        (
            Entity entity,
            ref MoveToTargetInputComponent moveInput,
            ref ActorRandomComponent randomComponent,
            in MoveToTargetOutputComponent moveOutput,
            in HungerComponent hunger,
            in ActorNeedsComponent needs,
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

            // TODO Blob + DNA + Hunger
            if (needs.Fullness >= 90f)
            {
                return;
            }

            if (needsOutput.Advertiser != Entity.Null)
            {
                switch (needsOutput.Action)
                {
                    case ActionID.Eat:
                        Ecb.SetComponentEnabled<EatingStateTag>(entity, true);
                        break;
                    case ActionID.Sleep:
                        break;
                }
                Ecb.SetComponentEnabled<SearchingStateTag>(entity, false);
            }

            /*
            // Check if has hunger target
            if (hunger.Target != Entity.Null)
            {
                Ecb.SetComponentEnabled<EatingStateTag>(entity, true);
                Ecb.SetComponentEnabled<SearchingStateTag>(entity, false);
            }*/
        }
    }

    // Helper method for generating target positions
    private static float3 GenerateTargetPosition(ref ActorRandomComponent random, float3 planetCenter, float planetScale)
    {
        float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        return planetCenter + math.normalize(randomTarget) * planetScale / 2f;
    }
}
