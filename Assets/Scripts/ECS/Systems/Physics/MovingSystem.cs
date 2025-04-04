using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
[UpdateInGroup(typeof(ZooPhysicsSystemGroup))]
public partial struct MovingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MovingOutputComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var moveToTargetJob = new MoveToTargetJob
        {
            DeltaTime = deltaTime,
        };

        // Schedule parallel job
        state.Dependency = moveToTargetJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct MoveToTargetJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref MovingOutputComponent movingOutputData,
            ref PhysicsVelocity velocity,
            in MovingInputComponent movingInputData,
            in LocalTransform transform,
            in GravityComponent gravity)
        {
            movingOutputData.NoTargetSet = IsEmptyData(movingInputData.TargetPosition);
            movingOutputData.HasArivedToTarget = false;
            movingOutputData.Speed = 0;

            if (movingOutputData.NoTargetSet)
            {
                return;
            }

            movingOutputData.HasArivedToTarget = HasArrivedToDestination(
                transform.Position,
                movingInputData.TargetPosition,
                gravity.GravityDirection,
                transform.Scale,
                movingInputData.TargetScale
            );

            if (movingOutputData.HasArivedToTarget)
            {
                return;
            }

            float distanceToTargetSq = math.lengthsq(movingInputData.TargetPosition - transform.Position);
            float horizontalSpeed = movingInputData.Speed;

            if (distanceToTargetSq < math.square(horizontalSpeed * DeltaTime))
            {
                horizontalSpeed = math.sqrt(distanceToTargetSq) / DeltaTime;
            }

            movingOutputData.Speed = horizontalSpeed;

            float3 direction = movingInputData.TargetPosition - transform.Position;
            float3 up = -gravity.GravityDirection;
            float3 cross = math.cross(up, direction);
            float3 forward = math.normalize(math.cross(cross, up));

            velocity.Linear += forward * horizontalSpeed;
        }
    }

    private static bool IsEmptyData(float3 targetPosition)
    {
        return math.lengthsq(targetPosition) <= 0;
    }

    private static bool HasArrivedToDestination(float3 position, float3 targetPosition, float3 gravityVector, float actorScale, float targetScale)
    {
        float3 actorPositionDown = position + gravityVector * actorScale / 2f;
        float3 targetPositionDown = targetPosition + gravityVector * targetScale / 2f;
        const float delta = 0.01f;
        float minDistanceSquared = (actorScale + targetScale) / 2f + delta;
        minDistanceSquared *= minDistanceSquared;
        return math.distancesq(actorPositionDown, targetPositionDown) <= minDistanceSquared;
    }
}
