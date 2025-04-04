using Unity.Entities;

public struct HungerComponent : IComponentData, IEnableableComponent
{
    public float FullnessDecaySpeed;
    public float FullnessDecayByDistance;
}

public struct EnergyComponent : IComponentData, IEnableableComponent
{
    public float EnergyDecaySpeed;
    public float EnergyDecayByDistance;
}