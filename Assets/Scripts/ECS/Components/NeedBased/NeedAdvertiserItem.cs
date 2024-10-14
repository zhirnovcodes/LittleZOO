using Unity.Entities;

[InternalBufferCapacity(8)]
public struct NeedAdvertiserItem : IBufferElementData
{
    public uint ActionId;
    public float HungerValue;
    public float EnergyValue;
}
