using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial class SearchingActionSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

        var transformLookup = GetComponentLookup<LocalTransform>();
        var moveToTargetInput = GetComponentLookup<MoveToTargetInputComponent>();
        var moveToTargetOutput = GetComponentLookup<MoveToTargetOutputComponent>();

        var planet = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTranform = SystemAPI.GetComponent<LocalTransform>(planet);

        var deltaTime = SystemAPI.Time.DeltaTime;

        new SearchingActionJob
        {
            TransformLookup = transformLookup,
            MoveToTargetInputLookup = moveToTargetInput,
            MoveToTargetOutputLookup = moveToTargetOutput,
            PlanetCenter = planetTranform.Position,
            PlanetScale = planetTranform.Scale,
            Ecb = ecb,
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct SearchingActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetInputComponent> MoveToTargetInputLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetOutputComponent> MoveToTargetOutputLookup;

        public float3 PlanetCenter;
        public float PlanetScale;

        public EntityCommandBuffer.ParallelWriter Ecb;

        public float DeltaTime;

        [BurstCompile]
        private void Execute
            (
                [EntityIndexInQuery] int entityInQueryIndex,
                ref ActionComponent actionComponent,
                ref ActionRandomComponent random,
                in SearchActionComponent searchingAction
            )
        {
            var actorTransform = TransformLookup.GetRefRO(actionComponent.Actor);

            if (actorTransform.IsValid == false)
            {
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);

                actionComponent.ActionState = Zoo.Enums.ActionStates.Failed;
                return;
            }


            if (MoveToTargetInputLookup.IsComponentEnabled(actionComponent.Actor))
            {
                if (MoveToTargetOutputLookup.TryGetComponent(actionComponent.Actor, out var data))
                {
                    if (data.HasArivedToTarget)
                    {

                        Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);

                        actionComponent.ActionState = Zoo.Enums.ActionStates.Succeded;
                        return;
                    }
                    return;
                }

                actionComponent.ActionState = Zoo.Enums.ActionStates.Failed;
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);

                return;
            }

            var actorPosition = actorTransform.ValueRO.Position;

            var actorSpeed = MoveToTargetInputLookup.GetRefRO(actionComponent.Actor).ValueRO.Speed;

            var targetPosition = GenerateTargetDistance(ref random, PlanetCenter, PlanetScale);
            const float targetScale = 0;

            var inputMoveData = new MoveToTargetInputComponent
            {
                TargetPosition = targetPosition,
                TargetScale = targetScale,
                Speed = actorSpeed
            };

            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, true);
            Ecb.SetComponent(entityInQueryIndex, actionComponent.Actor, inputMoveData);
        }
    }

    private static float3 GenerateTargetDistance(ref ActionRandomComponent random, float3 planetCenter, float planetScale)
    {
        var randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        var target = planetCenter + math.normalize(randomTarget) * planetScale / 2f;
        return target;
    }
}
