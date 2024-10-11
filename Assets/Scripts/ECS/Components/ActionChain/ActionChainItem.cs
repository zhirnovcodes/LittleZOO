using Unity.Entities;

[InternalBufferCapacity(8)]
public struct ActionChainItem : IBufferElementData
{
    public byte ActionId;
    public Entity Actor;
    public Entity Subject;
}
