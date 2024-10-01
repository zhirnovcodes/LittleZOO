using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class TestAuthoring : MonoBehaviour
{
    public class TestAuthoringBaker : Baker<TestAuthoring>
    {
        public override void Bake(TestAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);


            AddComponent(entity, new TestComponent
            {
            });
        }
    }
}

public struct TestComponent : IComponentData
{

}

[UpdateInGroup(typeof(ZooPhysicsSystem))]
public partial class TestSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = SystemAPI.Time.DeltaTime;

        Entities.
                WithAll<TestComponent>().
                ForEach(
                (
                    ref PhysicsVelocity velocity
                ) =>
                {
                    velocity.Linear = new Unity.Mathematics.float3(10f, 0, 0);
                }).Run();
    }
}
