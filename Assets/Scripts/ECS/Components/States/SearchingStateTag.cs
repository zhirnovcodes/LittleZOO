using Unity.Entities;

public struct SearchingStateTag : IComponentData, IEnableableComponent, IStateTag
{
    public Entity Action;
}

