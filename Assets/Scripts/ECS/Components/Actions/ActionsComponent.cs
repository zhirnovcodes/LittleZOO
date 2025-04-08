using Unity.Entities;
using Zoo.Enums;

public struct ActionInputComponent : IComponentData, IEnableableComponent
{
    public float TimeElapsed;
    public Entity Target;
    public ActionTypes Action;
    public int CurrentActionIndex;
}

public struct SubActionOutputComponent : IComponentData
{
    public ActionStatus Status;
}

public struct NeedBasedDecisionTag : IComponentData, IEnableableComponent 
{
    public float TimeElapsed;
}