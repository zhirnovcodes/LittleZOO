using Unity.Entities;
using Unity.Transforms;

public partial class EatingActionSystem : SystemBase
{
    public static void Execute(Entity entity,
        ComponentLookup<LocalTransform> TransformLookup,
        ref CurrentActionComponent currentAction,
        ref MoveToTargetComponent moveToTarget,
        ref ActorNeedsComponent needs)
    {
        var target = currentAction.Target;

        if (currentAction.ActionState == Zoo.Enums.ActionStates.CancellationRequested)
        {
            // Disable move to target
            
            currentAction.ActionState = Zoo.Enums.ActionStates.Canceled;
            return;
        }

        if (TransformLookup.TryGetComponent(target, out var targetTransform) == false)
        {
            // Does not exist anymore
            currentAction.ActionState = Zoo.Enums.ActionStates.Failed;
            return;
        }

        var targetPosition = targetTransform.Position;
        var targetScale = targetTransform.Scale;

        moveToTarget.TargetPosition = targetPosition;
        moveToTarget.TargetScale = targetScale;

        if (moveToTarget.HasArivedToTarget)
        {
            // set eating 
            if (true /*eaten*/)
            {
                currentAction.ActionState = Zoo.Enums.ActionStates.Succeded;
                return;
            }

            if (true /*hunger is fullfilled*/)
            {
                currentAction.ActionState = Zoo.Enums.ActionStates.Succeded;
                return;
            }
        }
        else
        {
            // set moving
        }
    }

    protected override void OnUpdate()
    {
        throw new System.NotImplementedException();
    }
}
