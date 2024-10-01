using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Zoo.Physics;

//[BurstCompile]
[UpdateInGroup(typeof(ZooPhysicsSystem))]
public partial class MoveStateSystem : SystemBase
{
    //[BurstCompile]
    protected override void OnUpdate()
    {
        var ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        var deltaTime = SystemAPI.Time.DeltaTime;

        Entities.
            WithAll<MovingStateData>().
            ForEach(
            (
                Entity entity,
                int entityInQueryIndex,
                ref MovingStateData movingData,
                ref PhysicsVelocity velocity,
                ref ActorRandomComponent random,
                in LocalTransform transform,
                in GravityComponent gravity
            ) =>
        {
            // TODO to asset
            const float horizontalDrag = 100f;

            if (IsEmptyData(movingData.TargetPosition))
            {
                movingData.TargetPosition = GenerateTargetDistance(ref random, transform.Position, planetCenter, planetScale);
            }

            movingData.HasArivedToTarget = HasArrivedToDestination(transform.Position, movingData.TargetPosition, gravity.GravityDirection, transform.Scale);
            
            if (movingData.HasArivedToTarget)
            {
                movingData.TargetPosition = GenerateTargetDistance(ref random, transform.Position, planetCenter, planetScale);
                return;
            }

            var speed = movingData.Speed;
            var direction = movingData.TargetPosition - transform.Position;
            var up = -gravity.GravityDirection;
            var cross = math.cross(up, direction);
            var forward = math.normalize( math.cross(cross, up ));

            var verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection);
            var horizontalVelocity = velocity.Linear - verticalSpeed * gravity.GravityDirection;
            verticalSpeed = verticalSpeed < 0 ? 0 : verticalSpeed;
            var verticalVelocity = gravity.GravityDirection * verticalSpeed;
            horizontalVelocity = math.lerp(horizontalVelocity, forward * speed, math.clamp(horizontalDrag * deltaTime, 0, 1));

            movingData.Forward = forward;
            movingData.HorizontalSpeed = math.length(horizontalVelocity) / deltaTime;
            movingData.HorizontalVelocity = horizontalVelocity;
            movingData.VerticalSpeed = verticalSpeed;
            movingData.VerticalVelocity = verticalVelocity;

            velocity.Linear = horizontalVelocity + verticalVelocity;
        }).ScheduleParallel();
    }

    private static bool IsEmptyData(float3 targetPositon)
    {
        return math.lengthsq(targetPositon) <= 0;
    }

    private static bool HasArrivedToDestination(float3 positon, float3 targetPositon, float3 gravityVector, float actorScale)
    {
        positon += gravityVector * actorScale / 2f;
        return math.distancesq(targetPositon, positon) <= 0.01f;
    }

    private static bool HasArrivedToDestination(float3 positon, float3 targetPositon, float3 gravityVector)
    {
        const float delta = 0.01f;
        var dot = math.dot(gravityVector, targetPositon - positon);
        return math.abs(dot) >= 1 - delta;
    }

    private static float3 GenerateTargetDistance(ref ActorRandomComponent random, float3 position, float3 planetCenter, float planetScale)
    {
        var randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
        var target = planetCenter + math.normalize(randomTarget) * planetScale / 2f;
        return target;
    }
}
