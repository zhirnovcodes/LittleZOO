using Unity.Entities;

public struct SleepingStateTag : IComponentData, IEnableableComponent, IStateTag
{
    public bool IsSleeping;
}
