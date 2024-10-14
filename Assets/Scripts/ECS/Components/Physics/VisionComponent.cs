using Unity.Entities;

public struct VisionComponent : IComponentData, IEnableableComponent
{
    public float Radius;
    public float Interval;
    public float TimeElapsed;
}