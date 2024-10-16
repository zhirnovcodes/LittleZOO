using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
[UpdateInGroup(typeof(ZooPhysicsSystemGroup))]
public partial class MoveToTargetSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);
        var planetCenter = planetTransform.ValueRO.Position;
        var planetScale = planetTransform.ValueRO.Scale;

        var deltaTime = SystemAPI.Time.DeltaTime;

        Entities.
            WithAll<MoveToTargetOutputComponent, MoveToTargetInputComponent>().
            ForEach(
            (
                Entity entity,
                int entityInQueryIndex,
                ref MoveToTargetOutputComponent movingOutputData,
                ref PhysicsVelocity velocity,
                ref ActorRandomComponent random,
                in MoveToTargetInputComponent movingInputData,
                in LocalTransform transform,
                in GravityComponent gravity
            ) =>
        {
            // TODO to asset
            const float horizontalDrag = 100f;

            if (IsEmptyData(movingInputData.TargetPosition))
            {
                return;
                //movingData.TargetPosition = GenerateTargetDistance(ref random, transform.Position, planetCenter, planetScale);
            }

            movingOutputData.HasArivedToTarget = HasArrivedToDestination(transform.Position, movingInputData.TargetPosition, gravity.GravityDirection, transform.Scale, movingInputData.TargetScale);
            
            if (movingOutputData.HasArivedToTarget)
            {
                //movingData.TargetPosition = GenerateTargetDistance(ref random, transform.Position, planetCenter, planetScale);
                return;
            }

            var distanceToTargetSq = math.lengthsq(movingInputData.TargetPosition - transform.Position);

            var horizontalSpeed = movingInputData.Speed;

            if (distanceToTargetSq <= math.square(horizontalSpeed * deltaTime))
            {
                horizontalSpeed = math.sqrt(distanceToTargetSq) / deltaTime;
            }

            var direction = movingInputData.TargetPosition - transform.Position;
            var up = -gravity.GravityDirection;
            var cross = math.cross(up, direction);
            var forward = math.normalize( math.cross(cross, up ));

            var verticalSpeed = math.dot(velocity.Linear, gravity.GravityDirection);
            var horizontalVelocity = velocity.Linear - verticalSpeed * gravity.GravityDirection;
            verticalSpeed = verticalSpeed < 0 ? 0 : verticalSpeed;
            var verticalVelocity = gravity.GravityDirection * verticalSpeed;
            horizontalVelocity = math.lerp(horizontalVelocity, forward * horizontalSpeed, math.clamp(horizontalDrag * deltaTime, 0, 1));

            velocity.Linear = horizontalVelocity + verticalVelocity;
        }).ScheduleParallel();
    }

    private static bool IsEmptyData(float3 targetPositon)
    {
        return math.lengthsq(targetPositon) <= 0;
    }

    private static bool HasArrivedToDestination(float3 positon, float3 targetPositon, float3 gravityVector, float actorScale, float targetScale)
    {
        var actorPositionDown = positon + gravityVector * actorScale / 2f;
        var targetPositionDown = targetPositon + gravityVector * targetScale / 2f;
        const float delta = 0.01f;
        var minDistanceSquared = (actorScale + targetScale) / 2f + delta;
        minDistanceSquared *= minDistanceSquared;
        return math.distancesq(actorPositionDown, targetPositionDown) <= minDistanceSquared;
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
