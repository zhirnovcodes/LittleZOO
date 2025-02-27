using Unity.Entities;
using Unity.Mathematics;

public struct MoveToTargetOutputComponent : IComponentData
{
    public bool HasArivedToTarget;
    public bool NoTargetSet;
}

public struct MoveToTargetInputComponent : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float TargetScale;
    public float Speed;
}