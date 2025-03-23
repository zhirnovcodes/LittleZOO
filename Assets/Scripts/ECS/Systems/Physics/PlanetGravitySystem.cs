using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(ZooPhysicsSystemGroup), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlanetGravitySystem : ISystem
    {
        // TODO move to blob
        private const float GravityForce = 9.8f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GravityComponent>();
            state.RequireForUpdate<PlanetComponent>();
            state.RequireForUpdate<SimulationConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
            var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
            var config = SystemAPI.GetSingleton<SimulationConfigComponent>();

            float3 planetCenter = planetTransform.ValueRO.Position;
            float gravityForce = config.BlobReference.Value.World.GravityForce;

            var gravityJob = new GravityJob
            {
                DeltaTime = deltaTime,
                PlanetCenter = planetCenter,
                GravityForce = gravityForce
            };

            state.Dependency = gravityJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GravityJob : IJobEntity
        {
            public float DeltaTime;
            public float GravityForce;
            public float3 PlanetCenter;

            public void Execute(
                ref PhysicsVelocity velocity,
                ref GravityComponent gravity,
                in LocalTransform transform,
                in PhysicsMass mass)
            {
                gravity.GravityDirection = math.normalize(PlanetCenter - transform.Position);
                float3 gravityImpulse = gravity.GravityDirection * GravityForce * DeltaTime;

                //float verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection);
                //verticalSpeed = verticalSpeed < 0 ? 0 : verticalSpeed;

                velocity.Linear += gravityImpulse * mass.InverseMass;
            }
        }
    }
}
