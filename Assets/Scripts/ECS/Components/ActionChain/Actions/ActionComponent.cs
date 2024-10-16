using Unity.Entities;

public struct ActionComponent : IComponentData, IEnableableComponent
{
    public byte ActionId;
    public uint ActionState;
    
    public Entity Actor;
    public Entity Target;
}
