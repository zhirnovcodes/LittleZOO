using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct StateTimerSystem : ISystem
{
    [BurstCompile]
    private void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        var job = new Job() { DeltaTime = deltaTime };

        job.Schedule();
    }

    [BurstCompile]
    private partial struct Job : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        private void Execute(
                                ref StateTimeComponent time
                            )
        {
            time.StateTimeElapsed += DeltaTime;
        }

    }
}
