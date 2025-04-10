using Unity.Entities;

[InternalBufferCapacity(32)]
public struct BiteItem : IBufferElementData
{
    public Entity Target;
    public float Wholeness;
}
