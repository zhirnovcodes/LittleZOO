using Unity.Entities;

public struct HungerComponent : IComponentData, IEnableableComponent
{
    public float FullnessDecaySpeed;
}

public struct EnergyComponent : IComponentData, IEnableableComponent
{
    public float EnergyDecayFactor;
}