using Unity.Entities;

public struct TouchingComponent : IComponentData, IEnableableComponent
{
    public float Radius;
    public float Interval;
    public float TimeElapsed;
}