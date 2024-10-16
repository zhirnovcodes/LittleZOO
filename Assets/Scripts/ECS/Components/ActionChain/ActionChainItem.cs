using Unity.Entities;

[InternalBufferCapacity(8)]
public struct ActionChainItem : IBufferElementData
{
    public Entity Action;
}
