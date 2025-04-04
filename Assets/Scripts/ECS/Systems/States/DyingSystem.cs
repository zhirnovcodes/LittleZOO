using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
[UpdateInGroup(typeof(BiologicalSystemGroup))]
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
        var library = SystemAPI.GetSingleton<PrefabsLibraryComponent>();
        var icosphere = SystemAPI.GetSingleton<IcosphereComponent>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var pigComposingTime = config.BlobReference.Value.AnimationData.PigData.ComposingTime;

        var deltaTime = SystemAPI.Time.DeltaTime;

        var job = new DyingJob()
        {
            DeltaTime = deltaTime,
            ComposingTime = pigComposingTime,
            Ecb = ecb,
            Icosphere = icosphere,
            PrefabsLibrary = library,
            Config = config
        };

        state.Dependency = job.Schedule(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct DyingJob : IJobEntity
    {
        public float DeltaTime;
        public float ComposingTime;
        public EntityCommandBuffer Ecb;
        [ReadOnly] public IcosphereComponent Icosphere;
        [ReadOnly] public PrefabsLibraryComponent PrefabsLibrary;
        [ReadOnly] public SimulationConfigComponent Config;

        private void Execute
                    (
                        Entity entity,
                        ref ActorRandomComponent random,
                        in StateTimeComponent time,
                        in DyingStateTag stateTag,
                        in LocalTransform localTransform
                    )
        {
            if (time.StateTimeElapsed >= ComposingTime)
            {
                Compose(entity, ref random, localTransform);
            }
        }

        private void Compose(Entity entity, ref ActorRandomComponent random, in LocalTransform localTransform)
        {
            var freeTriangle = FindFreeGrassSlot(localTransform.Position);
            if (freeTriangle == -1 == false)
            {
                UnityEngine.Debug.Log("SpawnGrass");
                SpawnGrass(ref random, freeTriangle);
            }
            Ecb.DestroyEntity(entity);
        }

        private int FindFreeGrassSlot(float3 position)
        {
            const int attemptsCount = 3;
            int trieangleId = Icosphere.GetTriangleIndex(position);
            return trieangleId;
        }

        private void SpawnGrass(ref ActorRandomComponent random, int trieangleId)
        {
            
            GrassFactory.CreateRandomGrass(Ecb, trieangleId, ref random.Random, Icosphere, PrefabsLibrary.Grass, Config);
        }
    }
}
