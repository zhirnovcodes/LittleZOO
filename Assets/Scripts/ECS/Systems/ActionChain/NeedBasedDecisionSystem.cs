using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[UpdateInGroup(typeof(AISystemGroup), OrderFirst = true)]
public partial class NeedBasedDecisionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

        var actionLookup = GetComponentLookup<ActionComponent>();

        /*new ActionChainJob
        {
            Ecb = ecb,
            ActionLookup = actionLookup
        }.ScheduleParallel();*/

        new DecisionMakingJob
        {
            Ecb = ecb
        }.ScheduleParallel();
        
    }

    private partial struct DecisionMakingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<ActionChainItem> buffer,
                ref ActorRandomComponent random
            )
        {
            if (buffer.IsEmpty == false)
            {
                return;
            }

            if (MakeDecision(out var newItem, Ecb, entityInQueryIndex, entity, ref random))
            {
                buffer.Add(newItem);
                return;
            }

            return;
        }


        private static bool MakeDecision(out ActionChainItem result, EntityCommandBuffer.ParallelWriter ecb, int sortIndex, Entity actor, ref ActorRandomComponent random)
        {
            var entity = ecb.CreateEntity(sortIndex);
            var actionId = Zoo.Enums.Actions.Search;

            ecb.AddComponent(sortIndex, entity, new Parent
            {
                Value = actor
            });

            ecb.AddComponent(sortIndex, entity, new ActionComponent
            {
                Target = actor,
                Actor = actor,
                ActionId = actionId
            });

            ecb.AddComponent(sortIndex, entity, new SearchActionComponent());
            ecb.AddComponent(sortIndex, entity, new ActionRandomComponent
            {
                Random = new Unity.Mathematics.Random(random.Random.NextUInt())
            });

            result = new ActionChainItem
            {
                Action = entity
            };

            return true;
        }
    }

    private partial struct ActionChainJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ActionComponent> ActionLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute
            (
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<ActionChainItem> buffer
            )
        {
            if (buffer.IsEmpty)
            {
                StatesExtentions.SetState<IdleStateTag>(entity, Ecb, entityInQueryIndex);
                return;
            }

            var firstItem = buffer[0];

            var currentAction = ActionLookup.GetRefRO(firstItem.Action);

            if (currentAction.IsValid)
            {
                var state = currentAction.ValueRO.ActionState;

                if (state == Zoo.Enums.ActionStates.Canceled
                    || state == Zoo.Enums.ActionStates.Failed
                    || state == Zoo.Enums.ActionStates.Succeded)
                {
                    buffer.RemoveAt(0);
                    Ecb.DestroyEntity(entityInQueryIndex, firstItem.Action);

                    if (buffer.IsEmpty)
                    {
                        return;
                    }

                    var newAction = buffer[0];
                    Ecb.SetComponentEnabled<ActionComponent>(entityInQueryIndex, newAction.Action, true);
                }

            }
        }
    }
}
