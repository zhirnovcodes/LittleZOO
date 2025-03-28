using Unity.Entities;
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
        SetActionDisabled<SleepingStateTag>(entity, commandBuffer);
        SetActionDisabled<EatingStateTag>(entity, commandBuffer);
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

    public static void SetAction(EntityCommandBuffer ecb, SubActionTypes type, Entity entity)
    {
        switch (type)
        {
            case SubActionTypes.MoveTo:
                SetMoving(ecb, entity);
                break;
            case SubActionTypes.Explore:
                SetExploring();
                break;
            case SubActionTypes.Eat:
                SetEating();
                break;
            case SubActionTypes.Sleep:
                SetSleeping();
                break;
            case SubActionTypes.RunFrom:
                SetDying();
                break;
        }
    }

    public static void SetMoving(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.SetComponentEnabled<VisionComponent>(entity, true);
        ecb.SetComponentEnabled<MoveToTargetInputComponent>(entity, true);


        /*
        VisionComponent 	Enabled
		MovingComponent 	Enabled
		DecisionMakingComponent 	Enabled
		SafetyComponent		Enabled
		HungerComponent 	Enabled
		EnergyComponent 	Enabled
		DyingComponent		Disabled
		
		ExploreActionComponent Disabled
		MoveToActionComponent Enabled
		RunFromActionComponent Disabled
		EatActionComponent Disabled
		SleepActionComponent Disabled
		
		ActionInputComponent Enabled
		
		MoveAnimationComponent Enabled
		RunAnimationComponent Disabled
		EatAnimationComponent Disabled
		SleepAnimationComponent Disabled
		DieAnimationComponent	Disabled
        */


    }

    public static void SetExploring()
    {
        // Enable and disable appropriate components for Exploring state
    }

    public static void SetEating()
    {
        // Enable and disable appropriate components for Eating state
    }

    public static void SetSleeping()
    {
        // Enable and disable appropriate components for Sleeping state
    }

    public static void SetDying()
    {
        // Enable and disable appropriate components for Dying state
    }
}
