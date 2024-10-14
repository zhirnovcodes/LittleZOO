using Unity.Entities;
using UnityEngine;

public class VisionAuthoring : MonoBehaviour
{
    public float IntervalMin;
    public float IntervalMax;
    public SphereCollider Collider;

    public class VisionBaker : Baker<VisionAuthoring>
    {
        public override void Bake(VisionAuthoring authoring)
        {
            authoring.Collider.enabled = false;

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new VisionComponent
            {
                Radius = authoring.Collider.radius,
                Interval = Random.Range(authoring.IntervalMin, authoring.IntervalMax)
            });

            AddBuffer<VisionItem>(entity);
        }
    }
}
