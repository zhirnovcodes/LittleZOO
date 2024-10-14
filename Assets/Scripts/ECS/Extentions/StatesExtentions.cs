using Unity.Entities;

public  static class StatesExtentions
{
    private static ComponentType GetStateTagComponentType(uint stateId)
    {
        switch (stateId)
        {
            case States.Idle: return typeof(IdleStateTag);
            case States.Dying: return typeof(DyingStateTag);
            case States.Falling: return typeof(FallingStateTag);
            case States.Walking: return typeof(WalkingStateTag);
            case States.Running: return typeof(RunningStateTag);
            case States.Sleeping: return typeof(SleepingStateTag);
            case States.Eating: return typeof(EatingStateTag);
        }

        throw new System.NotImplementedException();
    }

    private static void SetComponentDisabled<T>(Entity entity, EntityCommandBuffer commandBuffer) where T : struct, IStateTag, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(entity, false);
    }

    private static void SetComponentDisabled<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IStateTag, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, false);
    }

    private static void SetComponentDisabled(Entity entity, uint stateId, EntityCommandBuffer commandBuffer)
    {
        commandBuffer.SetComponentEnabled(entity, GetStateTagComponentType(stateId), false);
    }

    private static void SetComponentDisabled(Entity entity, uint stateId, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
    {
        commandBuffer.SetComponentEnabled(sortKey, entity, GetStateTagComponentType(stateId), false);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer commandBuffer)
    {
        SetComponentDisabled(entity, States.Idle, commandBuffer);
        SetComponentDisabled(entity, States.Walking, commandBuffer);
        SetComponentDisabled(entity, States.Running, commandBuffer);
        SetComponentDisabled(entity, States.Sleeping, commandBuffer);
        SetComponentDisabled(entity, States.Dying, commandBuffer);
        SetComponentDisabled(entity, States.Eating, commandBuffer);
        SetComponentDisabled(entity, States.Falling, commandBuffer);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
    {
        SetComponentDisabled<IdleStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<WalkingStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<RunningStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<SleepingStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<DyingStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<EatingStateTag>(entity, commandBuffer, sortKey);
        SetComponentDisabled<FallingStateTag>(entity, commandBuffer, sortKey);
    }

    public static void SetState(Entity entity, uint stateId, EntityCommandBuffer commandBuffer)
    {
        SetAllDisabled(entity, commandBuffer);
        commandBuffer.SetComponentEnabled(entity, GetStateTagComponentType(stateId), true);
    }

    public static void SetState(Entity entity, uint stateId, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
    {
        SetAllDisabled(entity, commandBuffer, sortKey);
        commandBuffer.SetComponentEnabled(sortKey, entity, GetStateTagComponentType(stateId), true);
    }


    public static void SetState<T>(Entity entity, EntityCommandBuffer commandBuffer) where T: struct, IStateTag, IEnableableComponent
    {
        SetAllDisabled(entity, commandBuffer);
        commandBuffer.SetComponentEnabled<T>(entity, true);
    }

    public static void SetState<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IStateTag, IEnableableComponent
    {
        SetAllDisabled(entity, commandBuffer, sortKey);
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, true);
    }


}
