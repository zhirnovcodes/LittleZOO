using Unity.Entities;

public struct CurrentActionComponent : IComponentData
{
    public byte ActionId;
    public Entity Subject;
}
