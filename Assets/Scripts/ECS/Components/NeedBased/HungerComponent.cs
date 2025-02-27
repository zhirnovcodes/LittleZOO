using Unity.Entities;

public struct HungerComponent : IComponentData, IEnableableComponent
{
    public Entity Target;
    public float HungerIncrease;
}
