using Unity.Entities;
using Unity.Mathematics;

public struct GrowingComponent : IComponentData
{
    public float3 MinSize; // Size (scale) = MaxSize when Wholeness = 0
    public float3 MaxSize; // Size (scale) = MaxSize when Wholeness = 100
    public float MaxWholeness;
    public float GrowthSpeed;
}
