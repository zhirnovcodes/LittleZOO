using Unity.Entities;
using Unity.Mathematics;
using Zoo.Enums;

[InternalBufferCapacity(16)]
public struct AdvertisedActionItem : IBufferElementData
{
    public NeedType NeedId;
    public ActionID ActionId;
    public float2 NeedsMatrix; // X: Fullness, Y: Energy
}