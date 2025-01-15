using Unity.Entities;

public  static class StatesExtentions
{
    private static void SetComponentDisabled<T>(Entity entity, EntityCommandBuffer commandBuffer) where T : struct, IStateTag, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(entity, false);
    }

    private static void SetComponentDisabled<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IStateTag, IEnableableComponent
    {
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, false);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer commandBuffer)
    {
        SetComponentDisabled<IdleStateTag>(entity, commandBuffer);
        SetComponentDisabled<WalkingStateTag>(entity, commandBuffer);
        SetComponentDisabled<RunningStateTag>(entity, commandBuffer);
        SetComponentDisabled<SleepingStateTag>(entity, commandBuffer);
        SetComponentDisabled<DyingStateTag>(entity, commandBuffer);
        SetComponentDisabled<EatingStateTag>(entity, commandBuffer);
        SetComponentDisabled<FallingStateTag>(entity, commandBuffer);
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
