using Unity.Entities;

public static class ActionsExtentions
{
    public static void AddActionToBuffer<A>() where A : IActionComponent
    {

    }

    /*

    private static void SetActionDisabled<T>(Entity entity, EntityCommandBuffer commandBuffer) where T : struct, IActionComponent
    {
        commandBuffer.SetComponentEnabled<T>(entity, false);
    }

    private static void SetActionDisabled<T>(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey) where T : struct, IActionComponent
    {
        commandBuffer.SetComponentEnabled<T>(sortKey, entity, false);
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
    {
        SetActionDisabled<IdleActionComponent>(entity, commandBuffer, sortKey);
        SetActionDisabled<SearchActionComponent>(entity, commandBuffer, sortKey);
        SetActionDisabled<SleepingActionComponent>(entity, commandBuffer, sortKey);
        SetActionDisabled<EatingActionComponent>(entity, commandBuffer, sortKey);
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
    */


}
