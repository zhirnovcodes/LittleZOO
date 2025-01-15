using Unity.Collections;
using Unity.Entities;

// TODO remove
public struct DecisionMakingComponent : IComponentData
{
    public NativeList<ActionComponent> CreatedActions;
}
