using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlanetAuthoring : MonoBehaviour
{
    public class PlanetBaker : Baker<PlanetAuthoring>
    {
        public override void Bake(PlanetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlanetComponent());
        }
    }
}
