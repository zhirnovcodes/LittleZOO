using Unity.Entities;
using Unity.Mathematics;

public struct ActorNeedsComponent : IComponentData
{
    public float3 Needs;
}
