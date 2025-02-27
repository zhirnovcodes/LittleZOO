using Unity.Entities;

[InternalBufferCapacity(16)]
public struct CollidedItem : IBufferElementData
{
    public Entity CollidedEntity;
}
