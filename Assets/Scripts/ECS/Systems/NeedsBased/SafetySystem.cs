using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(BiologicalSystemGroup))]
public partial struct SafetySystem : ISystem
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

        var deltaTime = SystemAPI.Time.DeltaTime;

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        var actorTypeLookup = SystemAPI.GetComponentLookup<ActorTypeComponent>();

        state.Dependency = new SafetyJob
        {
            DeltaTime = deltaTime,
            TransformLookup = transformLookup,
            TypeLookup = actorTypeLookup
        }.Schedule(state.Dependency);
    }

    public partial struct SafetyJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<ActorTypeComponent> TypeLookup;

        void Execute(
            Entity entity,
            ref ActorNeedsComponent needs,
            ref SafetyComponent safety,
            in DynamicBuffer<VisionItem> vision)
        {
            if (safety.TimeElapsed < safety.CheckInterval)
            {
                safety.TimeElapsed += DeltaTime;
                return;
            }

            safety.TimeElapsed = 0;

            var safetyValue = 100f;

            foreach (var item in vision)
            {
                var visible = item.VisibleEntity;

                if (TransformLookup.HasComponent(visible) == false)
                {
                    continue;
                }

                var dangerousFactor = GetDangerousFactor(entity, visible);

                safetyValue = math.max(safetyValue - dangerousFactor, 0);
            }

            needs.SetSafety(safetyValue);
        }

        private float GetDangerousFactor(Entity actor, Entity target)
        {
            if (TypeLookup.TryGetComponent(target, out var targetType) == false)
            {
                return 0;
            }

            if (TypeLookup[actor].Type == ActorsType.Pig
                && targetType.Type == ActorsType.Wolf)
            {
                return 100;
            }

            return 0;
        }
    }
}
