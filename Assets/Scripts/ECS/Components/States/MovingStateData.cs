using Unity.Entities;
using Unity.Mathematics;

public struct MovingStateData : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float Speed;
}
