using Unity.Entities;
using Unity.Mathematics;

public struct MovingOutputComponent : IComponentData
{
    public bool HasArivedToTarget;
    public bool NoTargetSet;
    public float Speed;
}

public struct MovingInputComponent : IComponentData, IEnableableComponent
{
    public float3 TargetPosition;
    public float TargetScale;
    public float Speed;
}

public struct MovingSpeedComponent : IComponentData, IEnableableComponent
{
    public float2 SpeedRange; 
}