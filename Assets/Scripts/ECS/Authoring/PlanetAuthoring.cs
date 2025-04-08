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

            AddBuffer<AdvertisedActionItem>(entity);
            AppendToBuffer(entity, new AdvertisedActionItem
            {
                ActionId = Zoo.Enums.ActionTypes.Search,
                NeedId = Zoo.Enums.NeedType.Fullness,
                NeedsMatrix = new float2 { x = 1f, y = 0f}
            }) ;
        }
    }
}
