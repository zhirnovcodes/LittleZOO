using Unity.Entities;
using Unity.Mathematics;

public struct MovingStateData : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    // TODO to asset
    public float Speed;
    public bool HasArivedToTarget;

    // TODO remove
    public float3 Forward;
    public float3 HorizontalVelocity;
    public float3 VerticalVelocity;
    public float VerticalSpeed;
    public float HorizontalSpeed;
}
