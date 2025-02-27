using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class HungerSystem : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();
        //var ecb = new EntityCommandBuffer(Allocator.TempJob).AsParallelWriter();
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var edibleItems = SystemAPI.GetComponentLookup<EdibleComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        Entities.
            WithAll<ActorNeedsComponent, HungerComponent>().
            ForEach(
                (
                    ref ActorNeedsComponent needs,
                    ref HungerComponent hunger
                ) =>
                {
                    needs.Fullness -= hunger.HungerIncrease;

                    if (needs.Fullness <= 0)
                    {
                        // DIE
                    }

                    hunger.HungerIncrease = 0;
                }
            ).Run();

        Entities.
            WithAll<ActorNeedsComponent, HungerComponent>().
            ForEach(
            (
                Entity entity,
                //int entityInQueryIndex,
                ref HungerComponent hunger,
                in ActorNeedsComponent needs,
                in DynamicBuffer<VisionItem> vision
            ) =>
            {
                // TODO in blob
                //const FoodPreferences foodPreference = FoodPreferences.Herbivore;

                hunger.HungerIncrease += needs.HungerDecayFactor / 100f * deltaTime;
                hunger.Target = Entity.Null;

                var edibleItemsList = new NativeList<Entity>(vision.Length, Allocator.Temp);

                foreach (var visibleItem in vision)
                {
                    if (edibleItems.TryGetComponent(visibleItem.VisibleEntity, out var edible))
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
                    TransformLookup = transformLookup
                };

                edibleItemsList.Sort(comparer);

                hunger.Target = edibleItemsList[0];

                edibleItemsList.Dispose();
            }).ScheduleParallel();
    }

    [BurstCompile]
    public struct EntityDistanceComparer : IComparer<Entity>
    {
        // The reference entity to measure distance from
        [ReadOnly] public Entity ReferenceEntity;

        // Component lookup to get positions of entities
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        // Cache the reference position to avoid repeated lookups
        private float3 _referencePosition;
        private bool _referencePositionInitialized;

        public int Compare(Entity x, Entity y)
        {
            // Lazily initialize the reference position if not already done
            if (!_referencePositionInitialized)
            {
                if (TransformLookup.HasComponent(ReferenceEntity))
                {
                    _referencePosition = TransformLookup[ReferenceEntity].Position;
                }
                else
                {
                    // Fallback if reference entity doesn't have a transform
                    _referencePosition = float3.zero;
                }
                _referencePositionInitialized = true;
            }

            // Get positions of entities to compare
            float3 positionX = TransformLookup.HasComponent(x)
                ? TransformLookup[x].Position
                : float3.zero;

            float3 positionY = TransformLookup.HasComponent(y)
                ? TransformLookup[y].Position
                : float3.zero;

            // Calculate squared distances (more efficient than calculating actual distances)
            float distanceXSquared = math.distancesq(_referencePosition, positionX);
            float distanceYSquared = math.distancesq(_referencePosition, positionY);

            // Compare the distances
            if (distanceXSquared < distanceYSquared)
                return -1;  // x is closer than y
            if (distanceXSquared > distanceYSquared)
                return 1;   // y is closer than x
            return 0;       // equal distances
        }
    }
}
