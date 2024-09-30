using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics.Extensions;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Jobs;
using Unity.Collections;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(ZooPhysicsSystem), OrderFirst = true)]
    [RequireMatchingQueriesForUpdate]
    public partial class PlanetGravitySystem : SystemBase
    {
        // TODO to blob
        private const float GravityForce = 9.8f;

        [BurstCompile]
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
            var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
            var planetCenter = planetTransform.ValueRO.Position;
            var planetScale = planetTransform.ValueRO.Scale;

            var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var collisionWorld = worldSingleton.CollisionWorld;

            Dependency = Entities.
                WithAll<GravityComponent>().
                WithReadOnly(collisionWorld).
                ForEach(
                (
                    ref PhysicsVelocity velocity,
                    ref GravityComponent gravity,
                    in LocalTransform transform,
                    in PhysicsMass mass
                ) =>
            {
                gravity.GravityDirection = math.normalize(planetCenter - transform.Position);
                gravity.IsTouchingPlanet =
                    IsTouchingPlanet(gravity.GravityDirection, transform.Position, transform.Scale, in collisionWorld);

                var gravityImpulse = gravity.GravityDirection * GravityForce * deltaTime;

                var verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection);
                verticalSpeed = verticalSpeed < 0 ? 0 : verticalSpeed;

                //velocity.Linear = gravity.GravityDirection * verticalSpeed;// + forward * speed * deltaTime;

                velocity.ApplyLinearImpulse(in mass, gravityImpulse);
            }).ScheduleParallel(Dependency);
        }

        private static bool IsTouchingPlanet(float3 planetCenter, float planetScale, float3 actorPosition, float actorScale)
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
                    BelongsTo = Layers.ActorDynamic,
                    CollidesWith = Layers.Planet, 
                    GroupIndex = 0
                }
            };

            return world.CastRay(input);
        }
    }
}