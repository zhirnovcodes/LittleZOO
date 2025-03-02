using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[UpdateInGroup(typeof(AISystemGroup), OrderFirst = true)]
public partial class DeleteActionsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        return;
        var deletonQueue = new NativeQueue<Entity>(Allocator.Temp);
        var deletedBufferActors = new NativeQueue<Entity>(Allocator.Temp);

        foreach (var deleteBuffer in SystemAPI.Query<DynamicBuffer<DeleteActionItem>>())
        {
            foreach (var item in deleteBuffer)
            {
                deletonQueue.Enqueue(item.Action);
                deletedBufferActors.Enqueue(item.Actor);
            }

            deleteBuffer.Clear();
        }

        while (deletonQueue.IsEmpty() == false)
        {
            var action = deletonQueue.Dequeue();
            var actor = deletedBufferActors.Dequeue();

            var actionBuffer = SystemAPI.GetBuffer<ActionChainItem>(actor);
            actionBuffer.RemoveAt(0);

            EntityManager.DestroyEntity(action);

        }

    }
}

[UpdateInGroup(typeof(AISystemGroup), OrderLast = true)]
public partial class NeedBasedDecisionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        return;
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged);
        var actionLookup = SystemAPI.GetComponentLookup<ActionComponent>(true);

        Entities.
            WithReadOnly(actionLookup).
            ForEach
            ((
                Entity actor,
                ref ActorRandomComponent random,
                in DynamicBuffer<ActionChainItem> decisions
            ) =>
            {
                if (decisions.IsEmpty)
                {
                    if (MakeDecision(out var actionComponent, actor))
                    {
                        var action = CreateAction(actionComponent, ecb, actor, ref random);
                        return;
                    }
                    else
                    {
                        StatesExtentions.SetState<IdleStateTag>(actor, ecb);
                        return;
                    }
                }

                var firstItem = decisions[0];

                var currentAction = actionLookup.GetRefRO(firstItem.Action);

                if (currentAction.IsValid)
                {
                    var state = currentAction.ValueRO.ActionState;

                    if (state == Zoo.Enums.ActionStates.Canceled
                        || state == Zoo.Enums.ActionStates.Failed
                        || state == Zoo.Enums.ActionStates.Succeded)
                    {
                        var deleteAction = new DeleteActionItem
                        {
                            Action = firstItem.Action,
                            Actor = actor,
                            Index = 0
                        };

                        ecb.AppendToBuffer(actor, deleteAction);

                        if (decisions.Length >= 1)
                        {
                            return;
                        }

                        var newAction = decisions[1];
                        ecb.SetComponentEnabled<ActionComponent>(newAction.Action, true);
                    }

                }
            }).Schedule();
    }


    private static Entity CreateAction(ActionComponent actionComponent, EntityCommandBuffer commandBuffer, Entity actor, ref ActorRandomComponent randomComponent)
    {
        var entity = commandBuffer.CreateEntity();

        commandBuffer.AddComponent(entity, new Parent { Value = actor });

        commandBuffer.AddComponent(entity, actionComponent);

        commandBuffer.AddComponent(entity, new SearchActionComponent());

        commandBuffer.AddComponent(entity, new ActionRandomComponent
        {
            Random = new Unity.Mathematics.Random(randomComponent.Random.NextUInt())
        });

        commandBuffer.AppendToBuffer(actor, new ActionChainItem
        {
            Action = entity
        });

        return entity;
    }

    private static bool MakeDecision(out ActionComponent result, Entity actor)
    {
        var actionId = Zoo.Enums.ActionID.Search;

        result = new ActionComponent
        {
            Target = actor,
            Actor = actor,
            ActionId = actionId
        };

        return true;
    }
}
