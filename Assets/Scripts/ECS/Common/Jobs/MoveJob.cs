using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Zoo.Physics;

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;
    public float Speed;
    public float TargetPosition;

    public void Execute(
        ref PhysicsVelocity velocity,
        in LocalTransform transform,
        in GravityComponent gravity)
    {
        if (IsEmptyData(TargetPosition))
        {
            return;
        }

        float distanceToTargetSq = math.lengthsq(TargetPosition - transform.Position);
        float horizontalSpeed = Speed;

        if (distanceToTargetSq <= math.square(horizontalSpeed * DeltaTime))
        {
            horizontalSpeed = math.sqrt(distanceToTargetSq) / DeltaTime;
        }

        float3 direction = TargetPosition - transform.Position;
        float3 up = -gravity.GravityDirection;
        float3 cross = math.cross(up, direction);
        float3 forward = math.normalize(math.cross(cross, up));

        velocity.Linear += forward * horizontalSpeed;
    }

    private static bool IsEmptyData(float3 targetPosition)
    {
        return math.lengthsq(targetPosition) <= 0;
    }
}