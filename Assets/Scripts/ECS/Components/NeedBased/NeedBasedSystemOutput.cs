using Unity.Entities;
using Zoo.Enums;

public struct NeedBasedSystemOutput : IComponentData
{
    public ActionTypes Action;
    public Entity Advertiser;
}
