using Unity.Entities;
using Unity.Transforms;

public static class AnimationExtensions
{
    // Set Sleeping Animation
    public static void SetSleepingAnimation(this EntityCommandBuffer ecb, Entity entity)
    {
        // Reset animation time and set length
        var animComp = new AnimationComponent
        {
            TimeElapsed = 0
        };

        ecb.SetComponent(entity, animComp);
        SetAllDisabled(entity, ecb);

        // Enable sleeping tag
        ecb.SetComponentEnabled<SleepingAnimationTag>(entity, true);
    }

    // Set Waking Up Animation
    public static void SetWakingUpAnimation(this EntityCommandBuffer ecb, Entity entity)
    {
        // Reset animation time and set length
        var animComp = new AnimationComponent
        {
            TimeElapsed = 0
        };

        ecb.SetComponent(entity, animComp);
        SetAllDisabled(entity, ecb);

        // Enable waking up tag
        ecb.SetComponentEnabled<WakingUpAnimationTag>(entity, true);
    }

    // Set Dying Animation
    public static void SetDyingAnimation(this EntityCommandBuffer ecb, Entity entity)
    {
        // Reset animation time and set length
        var animComp = new AnimationComponent
        {
            TimeElapsed = 0
        };

        ecb.SetComponent(entity, animComp);
        SetAllDisabled(entity, ecb);

        // Enable dying tag
        ecb.SetComponentEnabled<DyingAnimationTag>(entity, true);
    }

    // Set Idle Animation (adding this for completeness)
    public static void SetIdleAnimation(this EntityCommandBuffer ecb, Entity entity)
    {
        SetAllDisabled(entity, ecb);

        // Enable idle tag
        ecb.SetComponentEnabled<IdleAnimationTag>(entity, true);
    }

    public static Entity GetView(this BufferLookup<Child> lookup, Entity parent)
    {
        if (lookup.TryGetBuffer(parent, out var buffer) == false)
        {
            return Entity.Null;
        }

        foreach (var child in buffer)
        {
            return child.Value;
        }

        return Entity.Null;
    }

    private static void SetAllDisabled(Entity entity, EntityCommandBuffer ecb)
    {
        ecb.SetComponentEnabled<IdleAnimationTag>(entity, false);
        ecb.SetComponentEnabled<SleepingAnimationTag>(entity, false);
        //ecb.SetComponentEnabled<WakingUpAnimationTag>(entity, false);
        ecb.SetComponentEnabled<DyingAnimationTag>(entity, false);
    }
}