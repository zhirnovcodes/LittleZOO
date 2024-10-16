using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Sets Current Action based on Action Chain Buffer
/// </summary>
[UpdateInGroup(typeof(AISystemGroup))]
public partial class ActionChainSystem : SystemBase
{
    protected override void OnUpdate()
    {
        /*
 var ecb = new EntityCommandBuffer(Allocator.TempJob).AsParallelWriter();
 var actionLookup = GetComponentLookup<ActionComponent>();
 var decisionSystem = World.GetOrCreateSystemManaged<NeedBasedDecisionSystem>();
 var dependency = h
 //var needBasedDependency = SystemAPI.GetSingleton<NeedBasedDecisionSystem>();

 Entities.
     WithReadOnly(actionLookup).
     ForEach(
     (
         Entity entity,
         int entityInQueryIndex,
         ref DynamicBuffer<ActionChainItem> buffer,
         ref ActorRandomComponent random
     ) =>
 {
     if (buffer.IsEmpty)
     {
         StatesExtentions.SetState<IdleStateTag>(entity, ecb, entityInQueryIndex);
         return;
     }

     var firstItem = buffer[0];

     var currentAction = actionLookup.GetRefRO(firstItem.Action);

     if (currentAction.IsValid)
     {
         var state = currentAction.ValueRO.ActionState;

         if (state == Zoo.Enums.ActionStates.Canceled 
             || state == Zoo.Enums.ActionStates.Failed 
             || state == Zoo.Enums.ActionStates.Succeded)
         {
             buffer.RemoveAt(0);
             ecb.DestroyEntity(entityInQueryIndex, firstItem.Action);

             if (buffer.IsEmpty)
             {
                 return;
             }

             var newAction = buffer[0];
             ecb.SetComponentEnabled<ActionComponent>(entityInQueryIndex, newAction.Action, true);
         }
     }

 }).ScheduleParallel();*/
    }

}
