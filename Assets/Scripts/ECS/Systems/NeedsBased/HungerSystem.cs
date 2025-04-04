using Unity.Burst;
using Unity.Entities;


[BurstCompile]
[UpdateInGroup(typeof(BiologicalSystemGroup))]
public partial struct HungerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var deltaTime = SystemAPI.Time.DeltaTime;

        state.Dependency = new HungerJob
        {
            Ecb = ecb,
            DeltaTime = deltaTime
        }.Schedule(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
    }

    public partial struct HungerJob : IJobEntity
    {
        public EntityCommandBuffer Ecb;
        public float DeltaTime;

        void Execute(
            Entity entity,
            ref ActorNeedsComponent needs,
            in MovingOutputComponent movingOutput,
            in HungerComponent hunger)
        {
            var naturalDecrease = hunger.FullnessDecaySpeed * DeltaTime;
            var walkingDecrease = 0;//hunger.FullnessDecayByDistance * movingOutput.DistancePassed;

            var decrease = naturalDecrease + walkingDecrease;

            needs.Fullness -= decrease;

            if (needs.Fullness <= 0)
            {
                ActionsExtentions.SetDying(Ecb, entity);
            }
        }
    }
}
