using Unity.Entities;

public struct CollisionComponent : IComponentData, IEnableableComponent
{
    public float Radius;
    public float Interval;
    public float TimeElapsed;
}
