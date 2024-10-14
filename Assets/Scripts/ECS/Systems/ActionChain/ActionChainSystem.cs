using Unity.Entities;

/// <summary>
/// Sets Current Action based on Action Chain Buffer
/// </summary>
public partial class ActionChainSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb;

        Entities.ForEach(
            (
                Entity entity,
                ref DynamicBuffer<ActionChainItem> buffer,
                ref CurrentActionComponent currentAction
            ) =>
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            var firstItem = buffer[0];

            // Validate
            if (currentAction.ActionId == firstItem.ActionId == false)
            {
                // Set current action
            }
            
        }).ScheduleParallel();
    }

    public static bool MakeDecision(out ActionChainItem result)
    {
        result = new ActionChainItem
        {
            ActionId = Zoo.Enums.Actions.Search
        };

        return false;
    }
}
