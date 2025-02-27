using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Enums;

// Convert to ISystem
public partial struct EatingActionSystem : ISystem
{
    // Systems need EntityQuery fields for efficient filtering
    private EntityQuery _actionQuery;

    // OnCreate is called when the system is created
    public void OnCreate(ref SystemState state)
    {
        // Build query for entities with both ActionComponent and EatingActionComponent
        _actionQuery = SystemAPI.QueryBuilder()
            .WithAll<ActionComponent, EatingActionComponent>()
            .Build();

        // Require these components for the system to run
        state.RequireForUpdate<ActionComponent>();
        state.RequireForUpdate<EatingActionComponent>();
    }

    // OnDestroy is called when the system is destroyed
    public void OnDestroy(ref SystemState state)
    {
        // Clean up any resources if needed
    }

    // OnUpdate is called every frame the system runs
    public void OnUpdate(ref SystemState state)
    {
        // Early out if no relevant entities
        if (_actionQuery.IsEmpty)
            return;

        var deltaTime = SystemAPI.Time.DeltaTime;

        // Create command buffer for structural changes
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Get component lookups
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var edibleLookup = SystemAPI.GetComponentLookup<EdibleComponent>(true);
        var moveToTargetInputLookup = SystemAPI.GetComponentLookup<MoveToTargetInputComponent>(true);
        var moveToTargetOutputLookup = SystemAPI.GetComponentLookup<MoveToTargetOutputComponent>(true);
        var needsLookup = SystemAPI.GetComponentLookup<ActorNeedsComponent>(true);

        // TODO from blob
        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        // Schedule the job
        var job = new EatingActionSyncJob
        {
            TransformLookup = transformLookup,
            EdibleLookup = edibleLookup,
            MoveInputLookup = moveToTargetInputLookup,
            MoveOutputLookup = moveToTargetOutputLookup,
            NeedsLookup = needsLookup,
            Ecb = ecb,
            DeltaTime = deltaTime,
            WalkingSpeed = walkingSpeed
        };

        job.Run();


    }

    [BurstCompile]
    public partial struct EatingActionSyncJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<EdibleComponent> EdibleLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetInputComponent> MoveInputLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetOutputComponent> MoveOutputLookup;
        [ReadOnly] public ComponentLookup<ActorNeedsComponent> NeedsLookup;

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
            ref ActionComponent actionComponent,
            ref EatingActionComponent eatingAction
        )
        {

            // TODO lost sight
            if (EdibleLookup.TryGetComponent(actionComponent.Target, out var edable) == false)
            {
                CreateSearchEntity(ref actionComponent, entity);
                return;
            }

            if (TransformLookup.TryGetComponent(actionComponent.Target, out var edableTransform) == false)
            {
                CreateSearchEntity(ref actionComponent, entity);
                return;
            }

            if (MoveInputLookup.TryGetComponent(actionComponent.Actor, out var move) == false)
            {
                CreateSearchEntity(ref actionComponent, entity);
                return;
            }

            if (NeedsLookup.TryGetComponent(actionComponent.Actor, out var needs) == false)
            {
                CreateSearchEntity(ref actionComponent, entity);
                return;
            }

            // TODO to const
            if (needs.Fullness >= 100f)
            {
                CreateSearchEntity(ref actionComponent, entity);
                return;
            }

            move.TargetPosition = edableTransform.Position;
            move.TargetScale = edableTransform.Scale;
            move.Speed = WalkingSpeed;

            var hasArrived = MoveOutputLookup[actionComponent.Actor].HasArivedToTarget;

            if (hasArrived)
            {
                move.Speed = 0;

                eatingAction.BiteTimeElapsed += DeltaTime;

                if (eatingAction.BiteTimeElapsed >= eatDeltaTime)
                {
                    eatingAction.BiteTimeElapsed = 0;
                    Bite(actionComponent.Actor, actionComponent.Target);

                    if (edable.Wholeness <= 0)
                    {
                        CreateSearchEntity(ref actionComponent, entity);
                        Ecb.DestroyEntity(actionComponent.Target);
                        return;
                    }
                }
            }
            else
            {
                eatingAction.BiteTimeElapsed = 0;
            }

            // If eating target does not exist || wholeness <= 0 - set searching action. Dispose action
            // Set move target to eating target
            // If arrived to grass
            //      Stop motion
            //      Bite with delta time:
            //          Add fullness
            //          Reduse wholeness
            //          If fullness = 100 - return
            //          if woleness <= 0 - dispose food
            // else
            //      Set speed = speed
        }

        private void Bite(Entity actor, Entity target)
        {
            var edibleComponent = EdibleLookup.GetRefRW(target);
            var needComponent = NeedsLookup.GetRefRW(actor);

            var oldWholeness = edibleComponent.ValueRO.Wholeness;
            var biteValue = math.min(biteWholeness, oldWholeness);
            var newWholeness = oldWholeness - biteValue;

            var nutritiousAll = edibleComponent.ValueRO.Nutrition;

            var nutritiousValue = biteValue * nutritiousAll / 100f;

            needComponent.ValueRW.Fullness = math.min(100, needComponent.ValueRW.Fullness + nutritiousValue);
            edibleComponent.ValueRW.Wholeness = newWholeness;
        }

        private void CreateSearchEntity(ref ActionComponent actionComponent, Entity entity)
        {
            // Create search action + stop this
            //actionComponent.ActionState = ActionStates.Succeded;

            //Ecb.SetComponentEnabled<SearchActionComponent>(entity, false);
            //Ecb.SetComponentEnabled<ActionComponent>(entity, false);

            var searchingActionComponent = new SearchActionComponent();
            var newActionComponent = new ActionComponent
            {
                ActionId = ActionID.Search,
                ActionState = ActionStates.Created,
                Actor = actionComponent.Actor,
                Target = Entity.Null
            };

            var newEntity = Ecb.CreateEntity();
            Ecb.AddComponent(newEntity, searchingActionComponent);
            Ecb.AddComponent(newEntity, newActionComponent);

            //var newActionItem = new ActionChainItem
            //{
            //    Action = newEntity
            //};
            //Ecb.AppendToBuffer(actor, newActionItem);

            Ecb.DestroyEntity(entity);
        }
    }

    // Keep the job definition the same
    [BurstCompile]
    public partial struct EatingActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<EdibleComponent> EdableLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetInputComponent> MoveToTargetInputLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetOutputComponent> MoveToTargetOutputLookup;
        [ReadOnly] public ComponentLookup<ActorNeedsComponent> NeedsLookup;

        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;
        public float WalkingSpeed;

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
                actionComponent.ActionState = ActionStates.Failed;
                return;
            }
            if (TransformLookup.HasComponent(actionComponent.Target) == false)
            {
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);
                actionComponent.ActionState = ActionStates.Failed;
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
                actionComponent.ActionState = ActionStates.Failed;
                return;
            }
            if (edable.ValueRO.Wholeness <= 0)
            {
                actionComponent.ActionState = ActionStates.Succeded;
                return;
            }
            var needs = NeedsLookup.GetRefRO(actionComponent.Actor);
            if (needs.ValueRO.Fullness >= 100)
            {
                actionComponent.ActionState = ActionStates.Succeded;
                return;
            }
            StatesExtentions.SetState<EatingStateTag>(actionComponent.Actor, Ecb, entityInQueryIndex);
            // TODO decrease hunger, decrease Wholeness
        }
    }
}