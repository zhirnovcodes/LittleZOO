using Unity.Entities;

[UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
[UpdateBefore(typeof(BiologicalSystemGroup))]
public partial class InteractionResolveSystemGroup : ComponentSystemGroup
{
}
