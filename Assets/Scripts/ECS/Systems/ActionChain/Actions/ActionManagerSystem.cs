using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zoo.Enums;


[BurstCompile]
[UpdateInGroup(typeof(AISystemGroup))]
public partial struct ActionManagerSystem : ISystem
{
    private bool IsNativeMapCreated;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
        state.RequireForUpdate<ActionChainConfigComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) 
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        var deltaTime = SystemAPI.Time.DeltaTime;

        var actionMap = SystemAPI.GetSingleton<ActionChainConfigComponent>();
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();

        //var ecb = new EntityCommandBuffer(Allocator.TempJob);
        /*
        var actionHandle = new ActionChainJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb,
            ActionsMap = actionMap
        }.Schedule(state.Dependency);

        var subActionHandle = new SubActionChainJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb,
            ActionsMap = actionMap
        }.Schedule(actionHandle);
        
        state.Dependency = JobHandle.CombineDependencies(actionHandle, subActionHandle);*/

        new SubActionChainJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb,
            ActionsMap = actionMap,
            Lookup = transformLookup
        }.Schedule();
    }

    [BurstCompile]
    partial struct ActionChainJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer Ecb;
        [ReadOnly] public ActionChainConfigComponent ActionsMap;

        void Execute
            (
                Entity entity,
                ref ActionInputComponent actionInput,
                in NeedBasedSystemOutput needs
            )
        {
            // Replace current need with need from DecisionMakingSystem
            if (needs.Action != actionInput.Action)
            {
                UpdateMainAction(entity, ref actionInput, in needs);
                return;
            }

            if (needs.Advertiser != actionInput.Target)
            {
                UpdateMainAction(entity, ref actionInput, in needs);
                return;
            }
        }

        private void UpdateMainAction(
            Entity entity,
            ref ActionInputComponent actionInput,
            in NeedBasedSystemOutput needs)
        {
            actionInput.Action = needs.Action;
            actionInput.Target = needs.Advertiser;

            actionInput.CurrentActionIndex = 0;

            actionInput.TimeElapsed = 0;

            if (ActionsExtentions.TryGetSubAction(ActionsMap, actionInput.Action, 0, out var subAction))
            {
                ActionsExtentions.SetAction(Ecb, subAction, entity);
                return;
            }

            ActionsExtentions.SetAction(Ecb, SubActionTypes.Idle, entity);
        }
    }

    [BurstCompile]
    partial struct SubActionChainJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer Ecb;
        [ReadOnly] public ActionChainConfigComponent ActionsMap;
        [ReadOnly] public ComponentLookup<LocalTransform> Lookup;

        void Execute(
            Entity entity,
            ref ActionInputComponent actionInput,
            in NeedBasedSystemOutput needs,
            in SubActionOutputComponent output)
        {
            // Same current action
            if (actionInput.CurrentActionIndex < 0)
            {
                return;
            }

            var currentStatus = output.Status;
            actionInput.TimeElapsed += DeltaTime;

            switch (currentStatus)
            {
                case ActionStatus.Success:
                    SetNextSubAction(entity, ref actionInput, needs);
                    break;
                case ActionStatus.Fail:
                    ResetMainAction(entity, ref actionInput, needs);
                    break;
                case ActionStatus.Running:
                    // TODO cancel action
                    if (IsPriorityAction(actionInput, needs))
                    {
                        ResetMainAction(entity, ref actionInput, needs);
                    }
                    return;
            }

        }

        private void ResetMainAction(
            Entity entity,
            ref ActionInputComponent actionInput,
            in NeedBasedSystemOutput needs)
        {
            if (needs.Advertiser == Entity.Null ||
                Lookup.TryGetComponent(needs.Advertiser, out var _) == false)
            {
                SetIdleAction(entity, ref actionInput);
                return;
            }

            actionInput.Action = needs.Action;
            actionInput.Target = needs.Advertiser;

            actionInput.CurrentActionIndex = 0;

            actionInput.TimeElapsed = 0;

            if (ActionsExtentions.TryGetSubAction(ActionsMap, actionInput.Action, 0, out var subAction))
            {
                ActionsExtentions.SetAction(Ecb, subAction, entity);
                return;
            }

            SetIdleAction(entity, ref actionInput);
        }

        private void SetIdleAction(
            Entity entity,
            ref ActionInputComponent actionInput)
        {
            actionInput.Action = ActionTypes.Idle;
            actionInput.Target = Entity.Null;
            actionInput.CurrentActionIndex = 0;
            actionInput.TimeElapsed = 0;

            ActionsExtentions.SetAction(Ecb, SubActionTypes.Idle, entity);
        }

        private void SetNextSubAction(
            Entity entity,
            ref ActionInputComponent actionInput,
            in NeedBasedSystemOutput needs)
        {
            actionInput.CurrentActionIndex++;

            if (ActionsMap.TryGetSubAction(actionInput.Action, actionInput.CurrentActionIndex, out var subAction) == false)
            {
                ResetMainAction(entity, ref actionInput, needs);
                return;
            }

            actionInput.TimeElapsed = 0;

            ActionsExtentions.SetAction(Ecb, subAction, entity);
        }

        private bool IsPriorityAction(
            ActionInputComponent actionInput,
            NeedBasedSystemOutput needs)
        {
            return needs.Action == ActionTypes.Escape && 
                (actionInput.Action != needs.Action ||
                actionInput.Target != needs.Advertiser);
        }
    }

}
