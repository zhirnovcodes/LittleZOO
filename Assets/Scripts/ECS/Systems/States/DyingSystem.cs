using Unity.Burst;
using Unity.Entities;

public partial struct DyingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<SimulationConfigComponent>();

        var pigDyingTime = config.BlobReference.Value.AnimationData.PigData.DyingTime;

    }

    [BurstCompile]
    public partial struct DyingJob : IJobEntity
    {
        public float DeltaTime;
        public float DyingTime;
        public EntityCommandBuffer Ecb;

        private void Execute
                    (
                        in StateTimeComponent time,
                        in DyingStateTag stateTag
                    )
        {
            if (time.StateTimeElapsed >= DyingTime)
            {
                Die();
            }
        }

        private void Die()
        {

        }
    }
}
