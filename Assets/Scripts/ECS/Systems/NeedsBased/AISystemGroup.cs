using System;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
[UpdateAfter(typeof(BiologicalSystemGroup))]
public partial class AISystemGroup : ComponentSystemGroup
{
}
