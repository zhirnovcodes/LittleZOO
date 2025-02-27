using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Zoo.Enums;

[BurstCompile]
public partial struct SearchingActionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SearchActionComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var hungerLookup = state.GetComponentLookup<HungerComponent>(true);
        var moverInputLookup = state.GetComponentLookup<MoveToTargetInputComponent>(true);
        var moverOutputLookup = state.GetComponentLookup<MoveToTargetOutputComponent>(true);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // TODO from blob
        var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
        var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);
        var walkingSpeed = spawnData.ValueRO.PigSpeed;

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        float3 planetCenter = planetTransform.ValueRO.Position;
        float planetScale = planetTransform.ValueRO.Scale;

        new SearchingActionSynchJob()
        {
            HungerLookup = hungerLookup,
            MoveToTargetInputLookup = moverInputLookup,
            MoveToTargetOutputLookup = moverOutputLookup,
            Ecb = ecb,
            PlanetCenter = planetCenter,
            PlanetScale = planetScale,
            WalkingSpeed = walkingSpeed
        }.Run();

        ecb.Playback(state.EntityManager);

        ecb.Dispose();
    }

    // TODO enable events
    /*public static void EnableSearchingAction(ref SystemState state)
    {
        var visionLookup = state.GetComponentLookup<VisionComponent>(true);
        var hungerLookup = state.GetComponentLookup<HungerComponent>(true);
        var moverInputLookup = state.GetComponentLookup<MoveToTargetInputComponent>(true);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        if (visionLookup.HasComponent(actor))
        {
            if (visionLookup.IsComponentEnabled(actor) == false)
            {
                Ecb.SetComponentEnabled<VisionComponent>(actor, true);
            }
        }

        // If has hunger
        if (hungerLookup.HasComponent(actor))
        {
            if (hungerLookup.IsComponentEnabled(actor) == false)
            {
                Ecb.SetComponentEnabled<HungerComponent>(actor, true);
            }
            else
            {
                if (hungerLookup[actor].Target == Entity.Null == false)
                {
                    var target = hungerLookup[actor].Target;
                }
            }
        }
    }*/

    [BurstCompile]
    public partial struct SearchingActionSynchJob : IJobEntity
    {
        //[ReadOnly] public BufferLookup<ActionChainItem> ActionsLookup;
        [ReadOnly] public ComponentLookup<HungerComponent> HungerLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetInputComponent> MoveToTargetInputLookup;
        [ReadOnly] public ComponentLookup<MoveToTargetOutputComponent> MoveToTargetOutputLookup;

        public float3 PlanetCenter;
        public float PlanetScale;
        public EntityCommandBuffer Ecb;
        public float WalkingSpeed;

        [BurstCompile]
        public void Execute
        (
            Entity entity,
            ref ActionRandomComponent random,
            ref ActionComponent actionComponent,
            in SearchActionComponent searchAction
        )
        {
            var actor = actionComponent.Actor;

            if (MoveToTargetOutputLookup.TryGetComponent(actor, out var moveOutput))
            {
                if (moveOutput.NoTargetSet || moveOutput.HasArivedToTarget)
                {
                    var moveInputRW = MoveToTargetInputLookup.GetRefRW(actor);
                    moveInputRW.ValueRW.TargetPosition = GenerateTargetPosition(ref random, PlanetCenter, PlanetScale);
                    moveInputRW.ValueRW.Speed = WalkingSpeed;
                }
            }

            if (HungerLookup.TryGetComponent(actor, out var hunger))
            {
                // TODO if not hungry - return
                if (hunger.Target == Entity.Null == false)
                {
                    // Create eat action + stop this
                    actionComponent.ActionState = ActionStates.Succeded;
                    
                    //Ecb.SetComponentEnabled<SearchActionComponent>(entity, false);
                    //Ecb.SetComponentEnabled<ActionComponent>(entity, false);

                    var eatingActionComponent = new EatingActionComponent();
                    var newActionComponent = new ActionComponent
                    {
                        ActionId = ActionID.Eat,
                        ActionState = ActionStates.Created,
                        Actor = actor,
                        Target = hunger.Target
                    };

                    var newEntity = Ecb.CreateEntity();
                    Ecb.AddComponent(newEntity, eatingActionComponent);
                    Ecb.AddComponent(newEntity, newActionComponent);

                    //var newActionItem = new ActionChainItem
                    //{
                    //    Action = newEntity
                    //};
                    //Ecb.AppendToBuffer(actor, newActionItem);

                    Ecb.DestroyEntity(entity);
                }
            }

        }
    }

    private static float3 GenerateTargetPosition(ref ActionRandomComponent random, float3 planetCenter, float planetScale)
    {
        float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        return planetCenter + math.normalize(randomTarget) * planetScale / 2f;
    }

}

/*
[BurstCompile]
public partial struct SearchingActionSystem : ISystem
{
    private NativeList<ActionDiff> ActionChanges;
    private NativeList<ActionDiff> WalkingChanges;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SearchActionComponent>();
    }

    private static void SetWalkngTarget()
    {

    }

    private static void CreateAction(NativeList<ActionDiff>.ParallelWriter actionChanges, Entity actor, Entity target, ActionID actionId)
    {
        actionChanges.AddNoResize(new ActionDiff
        {
            ActionId = actionId,
            Target = target,
            Actor = actor,
            ActionOrder = int.MaxValue,
            ActionState = ActionStates.Running
        });
    }

    private static void SetActionState(NativeList<ActionDiff>.ParallelWriter actionChanges, ActionComponent action, ActionStates state)
    {
        actionChanges.AddNoResize(new ActionDiff
        {
            ActionId = action.ActionId,
            Target = action.Target,
            Actor = action.Actor,
            ActionState = state,
            ActionOrder = -1
        });
    }

    private static void SetActionOrder(NativeList<ActionDiff>.ParallelWriter actionChanges, ActionComponent action, int order)
    {
        actionChanges.AddNoResize(new ActionDiff
        {
            ActionId = action.ActionId,
            Target = action.Target,
            Actor = action.Actor,
            ActionState = action.ActionState,
            ActionOrder = order
        });
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UpdateActions(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var transformLookup = state.GetComponentLookup<LocalTransform>(true);
        var moveToTargetInput = state.GetComponentLookup<MoveToTargetInputComponent>(true);
        var moveToTargetOutput = state.GetComponentLookup<MoveToTargetOutputComponent>(true);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        float3 planetCenter = planetTransform.ValueRO.Position;
        float planetScale = planetTransform.ValueRO.Scale;

        var searchingActionJob = new SearchingActionJob
        {
            TransformLookup = transformLookup,
            MoveToTargetInputLookup = moveToTargetInput,
            MoveToTargetOutputLookup = moveToTargetOutput,
            PlanetCenter = planetCenter,
            PlanetScale = planetScale,
            Ecb = ecb
        };

        state.Dependency = searchingActionJob.ScheduleParallel(state.Dependency);
    }

    private void UpdateActions(ref SystemState state)
    {
        EntityQuery targetQuery = SystemAPI.QueryBuilder().
            WithAny<DynamicBuffer<ActionChainItem>, ActionComponent>().
            Build();
        int entityCount = targetQuery.CalculateEntityCount();

        if (ActionChanges.IsCreated)
        {
            if (ActionChanges.Capacity != entityCount)
            {
                ActionChanges.Capacity = entityCount;
            }
        }
        else
        {
            ActionChanges = new NativeList<ActionDiff>(entityCount, Allocator.Persistent);
        }

        state.Dependency.Complete();

        foreach (var actionDiff in ActionChanges)
        {
            switch (actionDiff.ActionState)
            {
                case ActionStates.Failed:
                case ActionStates.Succeded:
                case ActionStates.Canceled:
                    DestroyAction(state, actionDiff.Actor, actionDiff.ActionId);
                    break;
                case ActionStates.Running:
                    var existing = GetAction(state, actionDiff.Actor, actionDiff.ActionId);

                    if (existing == Entity.Null)
                    {
                        CreateAction(state, actionDiff.Actor, actionDiff.ActionId, actionDiff.ActionState, actionDiff.Target, actionDiff.ActionOrder);
                    }
                    else
                    {
                        ChangeActionOrder(state, actionDiff.Actor, existing, actionDiff.ActionOrder);
                        ChangeActionTarget(state, existing, actionDiff.Target);
                        ChangeActionState(state, existing, actionDiff.ActionState);
                    }
                    break;
            }
        }

        ActionChanges.Clear();
    }

    private void DestroyAction(SystemState state, Entity actor, ActionID actionID)
    {
        var bufferLookup = state.GetBufferLookup<ActionChainItem>();
        var actionLookup = state.GetComponentLookup<ActionComponent>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        if (bufferLookup.TryGetBuffer(actor, out var buffer))
        {
            var index = 0;

            foreach (var item in buffer)
            {
                var actionEntity = item.Action;
                if (actionLookup.TryGetComponent(actionEntity, out var actionComponent))
                {
                    if (actionComponent.ActionId == actionID)
                    {
                        break;
                    }
                }
                index++;
            }

            if (index >= buffer.Length)
            {
                // No action were found
                return;
            }

            var entity = buffer[index].Action;

            buffer.RemoveAt(index);

            ecb.DestroyEntity(entity);

            ecb.Playback(state.EntityManager);
        }
    }

    private Entity GetAction(SystemState state, Entity actor, ActionID actionID)
    {
        var bufferLookup = state.GetBufferLookup<ActionChainItem>();
        var actionLookup = state.GetComponentLookup<ActionComponent>();

        if (bufferLookup.TryGetBuffer(actor, out var buffer))
        {
            foreach (var item in buffer)
            {
                var actionEntity = item.Action;
                if (actionLookup.TryGetComponent(actionEntity, out var actionComponent))
                {
                    if (actionComponent.ActionId == actionID)
                    {
                        return actionEntity;
                    }
                }
            }
        }

        return Entity.Null;
    }

    private void CreateAction(SystemState state, Entity actor, ActionID actionID, ActionStates actionState, Entity target, int order)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var bufferLookup = state.GetBufferLookup<ActionChainItem>();

        if (bufferLookup.TryGetBuffer(actor, out var buffer) == false)
        {
            return;
        }

        order = order < 0  ? 0 : math.min(order, buffer.Length);

        var newAction = CreateActionEntity(ecb, actionID, actionState, target);

        buffer.Insert(order, new ActionChainItem { Action = newAction });

        ecb.Playback(state.EntityManager);
    }

    private Entity CreateActionEntity(EntityCommandBuffer ecb, ActionID actionID, ActionStates actionState, Entity target)
    {
        var entity = ecb.CreateEntity();

        ecb.AddComponent(entity, new ActionComponent
        {
            ActionId = actionID,
            ActionState = actionState,
            Target = target
        });

        return entity;
    }


    private void ChangeActionOrder(SystemState state, Entity actor, Entity action, int newOrder)
    {
        var bufferLookup = state.GetBufferLookup<ActionChainItem>();

        var buffer = bufferLookup[actor];

        var index = 0;

        foreach (var item in buffer)
        {
            if (item.Action == action)
            {
                break;
            }
            index++;
        }

        if (index >= buffer.Length)
        {
            return;
        }

        newOrder = newOrder < 0 ? index : math.min(newOrder, buffer.Length-1);

        buffer.RemoveAt(index);

        buffer.Insert(newOrder, new ActionChainItem { Action = action });
    }

    private void ChangeActionTarget(SystemState state, Entity action, Entity target)
    {
        var actionLookup = state.GetComponentLookup<ActionComponent>();

       var actionComponent = actionLookup.GetRefRW(action);

        actionComponent.ValueRW.Target = target;
    }

    private void ChangeActionState(SystemState state, Entity action, ActionStates actionState)
    {
        var actionLookup = state.GetComponentLookup<ActionComponent>();

        var actionComponent = actionLookup.GetRefRW(action);

        actionComponent.ValueRW.ActionState = actionState;
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

        [BurstCompile]
        public void Execute(
            [EntityIndexInQuery] int entityInQueryIndex,
            ref ActionComponent actionComponent,
            ref ActionRandomComponent random,
            in SearchActionComponent searchingAction)
        {
            if (!TransformLookup.TryGetComponent(actionComponent.Actor, out var actorTransform))
            {
                // Disable component
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);
                actionComponent.ActionState = ActionStates.Failed;
                return;
            }

            if (MoveToTargetInputLookup.IsComponentEnabled(actionComponent.Actor))
            {
                // Disable component
                Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, false);

                if (MoveToTargetOutputLookup.TryGetComponent(actionComponent.Actor, out var data) && data.HasArivedToTarget)
                {
                    actionComponent.ActionState = ActionStates.Succeded;
                    return;
                }

                actionComponent.ActionState = ActionStates.Failed;
                return;
            }

            if (!MoveToTargetInputLookup.TryGetComponent(actionComponent.Actor, out var moveToTargetInput))
            {
                actionComponent.ActionState = ActionStates.Failed;
                return;
            }

            float actorSpeed = moveToTargetInput.Speed;
            float3 targetPosition = GenerateTargetDistance(ref random, PlanetCenter, PlanetScale);

            var inputMoveData = new MoveToTargetInputComponent
            {
                TargetPosition = targetPosition,
                TargetScale = 0,
                Speed = actorSpeed
            };

            Ecb.SetComponentEnabled<MoveToTargetInputComponent>(entityInQueryIndex, actionComponent.Actor, true);
            Ecb.SetComponent(entityInQueryIndex, actionComponent.Actor, inputMoveData);
        }
    }

    private static float3 GenerateTargetDistance(ref ActionRandomComponent random, float3 planetCenter, float planetScale)
    {
        float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        return planetCenter + math.normalize(randomTarget) * planetScale / 2f;
    }
}
*/