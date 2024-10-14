using Unity.Entities;
using Unity.Mathematics;

public struct MoveToTargetComponent : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float TargetScale;
    public float Speed;
    public bool HasArivedToTarget;
}
