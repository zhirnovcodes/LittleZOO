using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TouchingAuthoring : MonoBehaviour
{
    public SphereCollider Collider;

    public class Baker : Baker<TouchingAuthoring>
    {
        public override void Bake(TouchingAuthoring authoring)
        {
            authoring.Collider.enabled = false;

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TouchingComponent
            {
                Radius = authoring.Collider.radius
            });

            AddBuffer<VisionItem>(entity);
        }
    }
}
