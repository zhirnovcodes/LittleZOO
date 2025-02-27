using Unity.Entities;
using UnityEngine;

public class CollisionAuthoring : MonoBehaviour
{
    public float IntervalMin;
    public float IntervalMax;
    public SphereCollider Collider;

    public class CollisionBaker : Baker<CollisionAuthoring>
    {
        public override void Bake(CollisionAuthoring authoring)
        {
            authoring.Collider.enabled = false;

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new VisionComponent
            {
                Radius = authoring.Collider.radius,
                Interval = UnityEngine.Random.Range(authoring.IntervalMin, authoring.IntervalMax)
            });

            AddBuffer<VisionItem>(entity);
        }
    }
}
