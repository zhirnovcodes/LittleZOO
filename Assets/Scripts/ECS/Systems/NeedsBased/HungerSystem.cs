using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct HungerSystem : ISystem
{
    private ComponentLookup<EdibleComponent> EdibleItems;
    private ComponentLookup<LocalTransform> TransformLookup;

    public void OnCreate(ref SystemState state)
    {
        EdibleItems = state.GetComponentLookup<EdibleComponent>(true);
        TransformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        EdibleItems.Update(ref state);
        TransformLookup.Update(ref state);

        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Process hunger and destroy starved entities
        foreach (var (needs, entity) in
                 SystemAPI.Query<RefRW<ActorNeedsComponent>>().WithEntityAccess())
        {
            needs.ValueRW.Fullness -= needs.ValueRW.HungerDecayFactor * deltaTime;

            if (needs.ValueRW.Fullness <= 0)
            {
                // Die from hunger
                ecb.DestroyEntity(entity);
                continue;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        /*
        // Schedule a parallel job for finding food targets

        state.Dependency = new FindFoodTargetsJob
        {
            EdibleItems = EdibleItems,
            TransformLookup = TransformLookup,
            DeltaTime = deltaTime
        }.ScheduleParallel(state.Dependency);*/
    }

    [BurstCompile]
    private partial struct FindFoodTargetsJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<EdibleComponent> EdibleItems;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float DeltaTime;

        void Execute(Entity entity, ref HungerComponent hunger, in ActorNeedsComponent needs, in DynamicBuffer<VisionItem> vision)
        {
            // TODO in blob
            //const FoodPreferences foodPreference = FoodPreferences.Herbivore;

            hunger.Target = Entity.Null;

            var edibleItemsList = new NativeList<Entity>(vision.Length, Allocator.Temp);

            foreach (var visibleItem in vision)
            {
                if (EdibleItems.TryGetComponent(visibleItem.VisibleEntity, out var edible))
                {
                    // TODO to blob
                    //var targetType = FoodTypes.Plant;

                    // TODO pref check
                    edibleItemsList.AddNoResize(visibleItem.VisibleEntity);
                }
            }

            if (edibleItemsList.Length <= 0)
            {
                edibleItemsList.Dispose();
                return;
            }

            var comparer = new EntityDistanceComparer
            {
                ReferenceEntity = entity,
                TransformLookup = TransformLookup
            };

            edibleItemsList.Sort(comparer);

            hunger.Target = edibleItemsList[0];

            edibleItemsList.Dispose();
        }
    }

    [BurstCompile]
    public struct EntityDistanceComparer : IComparer<Entity>
    {
        // The reference entity to measure distance from
        [ReadOnly] public Entity ReferenceEntity;

        // Component lookup to get positions of entities
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        
        public int Compare(Entity x, Entity y)
        {
            var referencePosition = TransformLookup[ReferenceEntity].Position;

            // Get positions of entities to compare
            float3 positionX = TransformLookup.HasComponent(x)
                ? TransformLookup[x].Position
                : float3.zero;

            float3 positionY = TransformLookup.HasComponent(y)
                ? TransformLookup[y].Position
                : float3.zero;

            // Calculate squared distances (more efficient than calculating actual distances)
            float distanceXSquared = math.distancesq(referencePosition, positionX);
            float distanceYSquared = math.distancesq(referencePosition, positionY);

            // Compare the distances
            if (distanceXSquared < distanceYSquared)
                return -1;  // x is closer than y
            if (distanceXSquared > distanceYSquared)
                return 1;   // y is closer than x
            return 0;       // equal distances
        }
    }
}