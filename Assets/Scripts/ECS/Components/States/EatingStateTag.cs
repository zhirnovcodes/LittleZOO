using Unity.Entities;

public struct EatingStateTag : IComponentData, IEnableableComponent, IStateTag
{
    public float BiteTimeElapsed;
    public float BiteWholeness;
    public float BiteInterval;
}

