using Unity.Entities;
using Unity.Mathematics;

public class PigsDNAComponent : IComponentData
{
    public float FullnessNaturalDecay;
    public float EnergyNaturalDecay;
    public float FullnessDecayByDistance;
    public float EnergyDecayByDistance;

    public float EatInterval;
    public float BiteWholeness;

    public float2 Speed;
    public float Size;
    public float VisionInterval;
    public float VisionRadius;
}
