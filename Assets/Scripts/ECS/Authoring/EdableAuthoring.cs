using Unity.Entities;
using UnityEngine;

public class EdableAuthoring : MonoBehaviour
{
    public float Nutrition = 10;

    public class EdableBaker : Baker<EdableAuthoring>
    {
        public override void Bake(EdableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EdibleComponent
            {
                Wholeness = 100,
                Nutrition = authoring.Nutrition
            });
        }
    }
}