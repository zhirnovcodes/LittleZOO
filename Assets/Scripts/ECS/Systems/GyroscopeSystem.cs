using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
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

        private static float3 EstimateAnglesBetween(quaternion from, quaternion to)
        {
            float3 fromImag = new float3(from.value.x, from.value.y, from.value.z);
            float3 toImag = new float3(to.value.x, to.value.y, to.value.z);

            float3 angle = math.cross(fromImag, toImag);
            angle -= to.value.w * fromImag;
            angle += from.value.w * toImag;
            angle += angle;
            return math.dot(toImag, fromImag) < 0 ? -angle : angle;
        }
    }

}

