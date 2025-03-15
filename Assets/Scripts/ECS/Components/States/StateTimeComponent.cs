using Unity.Entities;

public struct StateTimeComponent : IComponentData, IEnableableComponent
{
    public float StateTimeElapsed;
}
