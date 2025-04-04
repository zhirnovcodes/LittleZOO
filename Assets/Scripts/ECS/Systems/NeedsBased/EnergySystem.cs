using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(BiologicalSystemGroup))]
public partial struct EnergySystem : ISystem
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

        state.Dependency = new EnergyJob
        {
            Ecb = ecb,
            DeltaTime = deltaTime
        }.Schedule(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
    }

    public partial struct EnergyJob : IJobEntity
    {
        public EntityCommandBuffer Ecb;
        public float DeltaTime;

        void Execute(
            Entity entity,
            ref ActorNeedsComponent needs,
            in MovingOutputComponent movingOutput,
            in EnergyComponent hunger)
        {
            var naturalDecrease = hunger.EnergyDecaySpeed * DeltaTime;
            var walkingDecrease = hunger.EnergyDecayByDistance * movingOutput.Speed * DeltaTime;

            var decrease = naturalDecrease + walkingDecrease;

            needs.Energy -= decrease;

            if (needs.Energy <= 0)
            {
                ActionsExtentions.SetSleeping(Ecb, entity);
                needs.Energy = 0;
            }
        }
    }
}
