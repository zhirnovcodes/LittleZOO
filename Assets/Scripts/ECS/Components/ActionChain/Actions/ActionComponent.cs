using Unity.Entities;
using Zoo.Enums;

public struct ActionComponent : IComponentData, IEnableableComponent
{
    public ActionID ActionId;
    public ActionStates ActionState;
    
    public Entity Actor;
    public Entity Target;
}

public struct ActionDiff
{
    public ActionID ActionId;
    public ActionStates ActionState;

    public Entity Actor;
    public Entity Target;

    public int ActionOrder;
}