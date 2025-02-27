using Unity.Entities;

public struct ActorNeedsComponent : IComponentData, IEnableableComponent
{
    public float Fullness;
    public float Energy;

    // Decay functions
    public float HungerDecayFactor;
    public float EnergyDecayFactor;

    // Tuning functions
    public float HungerTuningFactor;
    public float EnergyTuningFactor;
}
