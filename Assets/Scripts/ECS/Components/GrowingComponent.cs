using Unity.Entities;
using Unity.Mathematics;

public struct GrowingComponent : IComponentData
{
    public float2 Size; // min and max Size (scale) = MaxSize when Wholeness = 0
    public float GrowthSpeed;
}
