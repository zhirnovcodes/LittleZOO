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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
            var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
            float3 planetCenter = planetTransform.ValueRO.Position;

            var gravityJob = new GravityJob
            {
                DeltaTime = deltaTime,
                PlanetCenter = planetCenter
            };

            state.Dependency = gravityJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GravityJob : IJobEntity
        {
            public float DeltaTime;
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


/*
 * 
 *         private static bool IsTouchingPlanet(float3 planetCenter, float planetScale, float3 actorPosition, float actorScale)
        {

            var minDistance = planetScale / 2f + actorScale / 2f;
            var distanceSq = math.distancesq(planetCenter, actorPosition);
            return distanceSq <= (minDistance * minDistance);
        }

        private static bool IsTouchingPlanet(float3 gravityDirection, float3 actorPosition, float actorScale, in CollisionWorld world)
        {
            const float minTouchDistance = 0.01f;

            var startPos = actorPosition + gravityDirection * actorScale / 2f;
            var endPos = startPos + gravityDirection * minTouchDistance;

            var input = new RaycastInput()
            {
                Start = startPos,
                End = endPos,
                Filter = new CollisionFilter()
                {
                    BelongsTo = Enums.Layers.ActorDynamic,
                    CollidesWith = Enums.Layers.Planet, 
                    GroupIndex = 0
                }
            };

            return world.CastRay(input);
        }

*/