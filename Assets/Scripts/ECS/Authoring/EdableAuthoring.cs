using Unity.Entities;
using UnityEngine;

public class EdableAuthoring : MonoBehaviour
{
    public class EdableBaker : Baker<EdableAuthoring>
    {
        public override void Bake(EdableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EdableComponent());
        }
    }
}