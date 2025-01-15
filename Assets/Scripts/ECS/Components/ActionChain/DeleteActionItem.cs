using Unity.Entities;

[InternalBufferCapacity(8)]
public struct DeleteActionItem : IBufferElementData, IEnableableComponent
{
    public Entity Action;
    public Entity Actor;
    public int Index;
}
