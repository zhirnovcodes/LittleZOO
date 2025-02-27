using Unity.Entities;
using UnityEngine;

public class GrassAuthoring : MonoBehaviour
{
    public class Baker : Baker<GrassAuthoring>
    {
        public override void Bake(GrassAuthoring authoring)
        {
            //var entity = GetEntity(TransformUsageFlags.Dynamic);

            //AddComponent(entity, new )
        }
    }
}
