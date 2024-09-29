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
                    in PhysicsVelocity velocity
                ) =>
                {
                    if (math.distancesq(velocity.Linear, new float3()) <= 0.001f)
                    {
                        return;
                    }

                    var up = math.normalize(transform.Position - planetCenter);
                    var cross = math.cross(up, velocity.Linear);

                    if (math.distancesq(cross, new float3()) <= 0.00001f)
                    {
                        return;
                    }

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

