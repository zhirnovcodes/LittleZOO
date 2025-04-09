using Unity.Entities;

public struct HungerComponent : IComponentData, IEnableableComponent
{
    // TODO to needs
    public float FullnessDecaySpeed;
    public float FullnessDecayByDistance;
}

public struct EnergyComponent : IComponentData, IEnableableComponent
{
    // TODO to needs
    public float EnergyDecaySpeed;
    public float EnergyDecayByDistance;
}

public struct SafetyComponent : IComponentData, IEnableableComponent
{
    public float CheckInterval;
    public float TimeElapsed;
}