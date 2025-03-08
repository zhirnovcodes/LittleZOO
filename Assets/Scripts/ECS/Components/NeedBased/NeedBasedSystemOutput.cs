using Unity.Entities;
using Zoo.Enums;

public struct NeedBasedSystemOutput : IComponentData
{
    public ActionID Action;
    public Entity Advertiser;
}
