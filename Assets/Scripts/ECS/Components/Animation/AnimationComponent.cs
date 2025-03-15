using Unity.Entities;

// Animation Component that tracks time and duration
public struct AnimationComponent : IComponentData
{
    public float TimeElapsed;
}

// Child View component to reference the child entity
public struct ViewComponent : IComponentData
{
    public Entity ViewEntity;
}

// Animation Tags as IEnableableComponents
public struct IdleAnimationTag : IEnableableComponent, IComponentData
{
}
public struct SleepingAnimationTag : IEnableableComponent, IComponentData
{
    public float Length;
}
public struct WakingUpAnimationTag : IEnableableComponent, IComponentData
{
    public float Length;
}
public struct DyingAnimationTag : IEnableableComponent , IComponentData
{
    public float Length;
}