using Unity.Entities;

public struct NeedAdvertiserItem : IBufferElementData
{
    public uint ActionId;
    public float HungerValue;
    public float EnergyValue;
}
