using Unity.Entities;
using Zoo.Enums;

public struct ActionInputComponent : IComponentData
{
    public float TimeElapsed;
    public Entity Target;
    public ActionTypes Action;
}

public struct SubActionOutputComponent : IComponentData
{
    public ActionStatus Status;
}

public struct SubActionBufferItem : IBufferElementData
{
    public SubActionTypes ActionType;
}
