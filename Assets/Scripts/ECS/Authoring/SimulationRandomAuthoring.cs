using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SimulationRandomAuthoring : MonoBehaviour
{
    public uint Seed = 1;

    public class Baker : Baker<SimulationRandomAuthoring>
    {
        public override void Bake(SimulationRandomAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new SimulationRandomComponent
            {
                Random = new Unity.Mathematics.Random(authoring.Seed)
            }) ;
        }
    }
}
