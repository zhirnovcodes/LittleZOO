using Unity.Entities;

[InternalBufferCapacity(256)]
public struct EmptyTriangleItem : IBufferElementData
{
    public int Index;
}