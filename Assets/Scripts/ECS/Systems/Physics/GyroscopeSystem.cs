using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(ZooPhysicsSystemGroup))]
    [UpdateAfter(typeof(PlanetGravitySystem))]
    public partial struct GyroscopeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GravityComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var gyroscopeJob = new GyroscopeJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = gyroscopeJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GyroscopeJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                ref LocalTransform transform,
                in PhysicsVelocity velocity,
                in GravityComponent gravity)
            {
                if (math.lengthsq(velocity.Linear) <= 0.001f)
                {
                    return;
                }

                float3 verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection) * gravity.GravityDirection;
                float3 horizontalSpeed = velocity.Linear - verticalSpeed;

                if (math.lengthsq(horizontalSpeed) <= 0.001f)
                {
                    return;
                }

                float3 up = -gravity.GravityDirection;
                float3 cross = math.cross(up, horizontalSpeed);
                float3 right = math.normalize(cross);
                float3 forward = math.normalize(math.cross(right, up));

                transform.Rotation = new quaternion(new float3x3(right, up, forward));
            }
        }
    }
}

