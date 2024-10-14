using Unity.Entities;

public struct CurrentActionComponent : IComponentData, IEnableableComponent
{
    public byte ActionId;
    public uint ActionState;
    public Entity Target;
}
