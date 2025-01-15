using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial class EatingActionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        ///var ecb = new EntityCommandBuffer(Allocator.TempJob).AsParallelWriter();


        //new EatingActionJob
        //{

        //};//.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct EatingActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<EdableComponent> EdableLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetInputComponent> MoveToTargetInputLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetOutputComponent> MoveToTargetOutputLookup;
        [ReadOnly] public ComponentLookup<ActorNeedsComponent> NeedsLookup;

        public EntityCommandBuffer.ParallelWriter Ecb;

        public float DeltaTime;

        [BurstCompile]
        private void Execute
            (
                [EntityIndexInQuery] int entityInQueryIndex,
                ref ActionComponent actionComponent,
                in EatingActionComponent eatingAction
            )
        {
            if (TransformLookup.HasComponent(actionComponent.Actor) == false)
            {
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);
                actionComponent.ActionState = Zoo.Enums.ActionStates.Failed;
                return;
            }

            if (TransformLookup.HasComponent(actionComponent.Target) == false)
            {
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);
                actionComponent.ActionState = Zoo.Enums.ActionStates.Failed;
                return;
            }

            var moveToTargetInput = MoveToTargetInputLookup.GetRefRO(actionComponent.Actor);
            var targetTransform = TransformLookup.GetRefRO(actionComponent.Target);

            var newMoveInputData = new MoveToTargetInputComponent
            {
                TargetPosition = targetTransform.ValueRO.Position,
                TargetScale = targetTransform.ValueRO.Scale,
                Speed = moveToTargetInput.ValueRO.Speed
            };

            Ecb.SetComponent(entityInQueryIndex, actionComponent.Actor, newMoveInputData);

            var moveToTargetOutput = MoveToTargetOutputLookup.GetRefRO(actionComponent.Actor);

            if (moveToTargetOutput.ValueRO.HasArivedToTarget == false)
            {
                StatesExtentions.SetState<WalkingStateTag>(actionComponent.Actor, Ecb, entityInQueryIndex);

                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, true);
                return;
            }

            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);

            var edable = EdableLookup.GetRefRO(actionComponent.Target);

            if (edable.IsValid == false)
            {
                actionComponent.ActionState = Zoo.Enums.ActionStates.Failed;
                return;
            }

            if (edable.ValueRO.Wholeness <= 0)
            {
                actionComponent.ActionState = Zoo.Enums.ActionStates.Succeded;
                return;
            }

            var needs = NeedsLookup.GetRefRO(actionComponent.Actor);

            if (needs.ValueRO.Hunger >= 100)
            {
                actionComponent.ActionState = Zoo.Enums.ActionStates.Succeded;
                return;
            }

            StatesExtentions.SetState<EatingStateTag>(actionComponent.Actor, Ecb, entityInQueryIndex);
            // TODO decrease hunger, decrease Wholeness
        }
    }
}
