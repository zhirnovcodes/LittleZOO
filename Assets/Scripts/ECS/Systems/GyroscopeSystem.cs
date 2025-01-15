using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(ZooPhysicsSystemGroup), OrderLast = true)]
    //[UpdateInGroup(typeof(SimulationSystemGroup))]
    //[UpdateAfter(typeof(TransformSystemGroup))]
    public partial class GyroscopeSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
            var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
            var planetCenter = planetTransform.ValueRO.Position;

            Entities.
                WithAll<GravityComponent>().
                ForEach(
                (
                    ref LocalTransform transform,
                    in PhysicsVelocity velocity,
                    in GravityComponent gravity
                ) =>
                {
                    if (math.lengthsq(velocity.Linear) <= 0.001f)
                    {
                        return;
                    }

                    var verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection) * gravity.GravityDirection;
                    var horizontalSpeed = velocity.Linear - verticalSpeed;

                    if (math.lengthsq(horizontalSpeed) <= 0.001f)
                    {
                        return;
                    }

                    var up = -gravity.GravityDirection;
                    var cross = math.cross(up, horizontalSpeed);
                    var right = math.normalize(cross);
                    var forward = math.normalize(math.cross(right, up));

                    transform.Rotation = new quaternion(new float3x3(right, up, forward));
                }).ScheduleParallel();
        }
    }

}

