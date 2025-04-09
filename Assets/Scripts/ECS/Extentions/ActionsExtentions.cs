using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zoo.Enums;

public static class ActionsExtentions
{
    private static void SetActionDisabled<T>(Entity entity, EntityCommandBuffer commandBuffer) where T : struct, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(entity, false);
    }
    
    private static void SetActionDisabled<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, false);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
    {
        SetActionDisabled<IdleStateTag>(entity, commandBuffer, sortKey);
        SetActionDisabled<SearchingStateTag>(entity, commandBuffer, sortKey);
        SetActionDisabled<SleepingStateTag>(entity, commandBuffer, sortKey);
        SetActionDisabled<EatingStateTag>(entity, commandBuffer, sortKey);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer commandBuffer)
    {
        SetActionDisabled<IdleStateTag>(entity, commandBuffer);
        SetActionDisabled<SearchingStateTag>(entity, commandBuffer);
        SetActionDisabled<MovingToStateTag>(entity, commandBuffer);
        SetActionDisabled<SleepingStateTag>(entity, commandBuffer);
        SetActionDisabled<EatingStateTag>(entity, commandBuffer);
        SetActionDisabled<RunningFromStateTag>(entity, commandBuffer);
        SetActionDisabled<DyingStateTag>(entity, commandBuffer);
    }

    public static void SetState<T>(Entity entity, EntityCommandBuffer commandBuffer) where T: struct, IStateTag, IEnableableComponent
    {
        commandBuffer.SetComponent(entity, new SubActionOutputComponent { Status = ActionStatus.Running });
        SetAllDisabled(entity, commandBuffer);
        commandBuffer.SetComponentEnabled<T>(entity, true);
    }

    public static void SetState<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IStateTag, IEnableableComponent
    {
        SetAllDisabled(entity, commandBuffer, sortKey);
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, true);
    }

    public static void SetAction(EntityCommandBuffer ecb, SubActionTypes type, Entity entity)
    {
        switch (type)
        {
            case SubActionTypes.Idle:
                SetIdle(ecb, entity);
                break;
            case SubActionTypes.MoveTo:
                SetMoving(ecb, entity);
                break;
            case SubActionTypes.Search:
                SetSearching(ecb, entity);
                break;
            case SubActionTypes.Eat:
                SetEating(ecb, entity);
                break;
            case SubActionTypes.Sleep:
                SetSleeping(ecb, entity);
                break;
            case SubActionTypes.RunFrom:
                SetRunningFrom(ecb, entity);
                break;
        }
    }

    public static void SetIdle(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, false);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, true);
        ecb.SetComponentEnabled<HungerComponent>(entity, true);
        ecb.SetComponentEnabled<EnergyComponent>(entity, true);
        ecb.SetComponentEnabled<SafetyComponent>(entity, true);

        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<IdleStateTag>(entity, ecb);

        //AnimationExtensions.SetIdleAnimation(ecb, entity);
    }

    public static void SetMoving(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, true);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, true);
        ecb.SetComponentEnabled<HungerComponent>(entity, true);
        ecb.SetComponentEnabled<EnergyComponent>(entity, true);
        ecb.SetComponentEnabled<SafetyComponent>(entity, true);

        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<MovingToStateTag>(entity, ecb);

        //AnimationExtensions.SetIdleAnimation(ecb, entity);
    }

    public static void SetSearching(EntityCommandBuffer ecb, Entity entity)
    {
        ResetMovement(ecb, entity);
        
        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, true);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, true);
        ecb.SetComponentEnabled<HungerComponent>(entity, true);
        ecb.SetComponentEnabled<EnergyComponent>(entity, true);
        ecb.SetComponentEnabled<SafetyComponent>(entity, true);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<SearchingStateTag>(entity, ecb);

        //AnimationExtensions.SetIdleAnimation(ecb, entity);
    }

    public static void SetEating(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, false);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, true);
        ecb.SetComponentEnabled<HungerComponent>(entity, false);
        ecb.SetComponentEnabled<EnergyComponent>(entity, true);
        ecb.SetComponentEnabled<SafetyComponent>(entity, true);

        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<EatingStateTag>(entity, ecb);

        //AnimationExtensions.SetIdleAnimation(ecb, entity);
    }

    public static void SetSleeping(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponentEnabled<VisionComponent>(entity, false);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, false);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, false);
        ecb.SetComponentEnabled<HungerComponent>(entity, true);
        ecb.SetComponentEnabled<EnergyComponent>(entity, false);
        ecb.SetComponentEnabled<SafetyComponent>(entity, false);

        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<SleepingStateTag>(entity, ecb);

        //AnimationExtensions.SetSleepingAnimation(ecb, entity);
    }

    public static void SetRunningFrom(EntityCommandBuffer ecb, Entity entity)
    {
        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, true);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, true);
        ecb.SetComponentEnabled<HungerComponent>(entity, true);
        ecb.SetComponentEnabled<EnergyComponent>(entity, true);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, true);

        SetState<RunningFromStateTag>(entity, ecb);

        //AnimationExtensions.SetDyingAnimation(ecb, entity);
    }

    public static void SetDying(EntityCommandBuffer ecb, Entity entity)
    {
        ResetMovement(ecb, entity);

        ecb.SetComponentEnabled<VisionComponent>(entity, false);
        ecb.SetComponentEnabled<MovingInputComponent>(entity, false);
        ecb.SetComponentEnabled<NeedBasedDecisionTag>(entity, false);
        ecb.SetComponentEnabled<HungerComponent>(entity, false);
        ecb.SetComponentEnabled<EnergyComponent>(entity, false);
        ecb.SetComponentEnabled<SafetyComponent>(entity, false);

        ecb.SetComponentEnabled<ActionInputComponent>(entity, false);

        SetState<DyingStateTag>(entity, ecb);

        //AnimationExtensions.SetDyingAnimation(ecb, entity);
    }

    private static void ResetMovement(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponent(entity, new MovingInputComponent { Speed = 0, TargetPosition = float3.zero, TargetScale = 0 });
        ecb.SetComponent(entity, new MovingOutputComponent { Speed = 0, HasArivedToTarget = false, NoTargetSet = true });
    }

    public static bool TryGetSubAction(ref BlobArray<int> actions, ActionTypes action, int index, out SubActionTypes subAction, int actionsCount, int subActionsCount)
    {
        var actionIndex = (int)action;
        var subActionIndex = index < 0 || index >= subActionsCount ? -1 : (index + subActionsCount * actionIndex);
        
        if (subActionIndex == -1 || actions[subActionIndex] == -1)
        {
            subAction = SubActionTypes.Idle;
            return false;
        }

        subAction = (SubActionTypes)actions[subActionIndex];
        return true;
    }

    public static bool TryGetSubAction(this ActionChainConfigComponent dto, ActionTypes action, int index, out SubActionTypes subAction)
    {
        return TryGetSubAction(ref dto.BlobReference.Value.ActionsMap, action, index, 
            out subAction, dto.BlobReference.Value.ActionsCount, dto.BlobReference.Value.SubActionsCount);
    }
}

public static class NeedsExtentions
{
    public static float Fullness(this ActorNeedsComponent need)
    {
        return need.Needs[0];
    }
    public static float Energy(this ActorNeedsComponent need)
    {
        return need.Needs[1];
    }
    public static float Safety(this ActorNeedsComponent need)
    {
        return need.Needs[2];
    }

    public static void SetFullness(this ref ActorNeedsComponent need, float value)
    {
        need.Needs = new float3(value, need.Needs.y, need.Needs.z);
    }
    public static void SetEnergy(this ref ActorNeedsComponent need, float value)
    {
        need.Needs = new float3(need.Needs.x, value, need.Needs.z);
    }
    public static void SetSafety(this ref ActorNeedsComponent need, float value)
    {
        need.Needs = new float3(need.Needs.x, need.Needs.y, value);
    }
}