using Unity.Entities;
using Unity.Mathematics;
using Zoo.Enums;

[InternalBufferCapacity(16)]
public struct AdvertisedActionItem : IBufferElementData
{
    public NeedType NeedId;
    public ActionTypes ActionId;
    public float3 NeedsMatrix; // X: Fullness, Y: Energy
}