using Unity.Entities;

[InternalBufferCapacity(16)]
public struct CollisionItem : IBufferElementData
{
    public Entity CollidedEntity;
}