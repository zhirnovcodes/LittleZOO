using Unity.Entities;

[InternalBufferCapacity(16)]
public struct VisionItem : IBufferElementData
{
    public Entity VisibleEntity;
}