using Unity.Entities;

public struct EatingStateTag : IComponentData, IEnableableComponent, IStateTag
{
    public Entity Action;
    public Entity Target;
    public float BiteTimeElapsed;
}

