﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(ZooCollisionsSystemGroup))]
public partial class CollisionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = worldSingleton.CollisionWorld;

        var filter = new CollisionFilter()
        {
            BelongsTo = Zoo.Enums.Layers.ActorDynamic,
            CollidesWith = Zoo.Enums.Layers.ActorDynamic |
                Zoo.Enums.Layers.ActorStatic,
            GroupIndex = 0
        };

        new SpawnCircleColliderJob
        {
            World = collisionWorld,
            Filter = filter,
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct SpawnCircleColliderJob : IJobEntity
    {
        [ReadOnly] public CollisionWorld World;
        [ReadOnly] public CollisionFilter Filter;
        public float DeltaTime;

        [BurstCompile]
        private void Execute(
            Entity entity,
            ref VisionComponent vision,
            ref DynamicBuffer<VisionItem> visionItems,
            in LocalTransform transform)
        {
            vision.TimeElapsed += DeltaTime;

            if (vision.TimeElapsed < vision.Interval)
            {
                return;
            }

            vision.TimeElapsed = 0;

            var list = new NativeList<ColliderCastHit>(Allocator.Temp);
            var origin = transform.Position;
            var radius = vision.Radius;

            World.SphereCastAll(origin, radius, new float3(0, 0, 1), 0, ref list, Filter);

            visionItems.Clear();

            const uint capacity = 16;

            foreach (var hit in list)
            {
                if (hit.Entity == entity)
                {
                    continue;
                }

                visionItems.Add(new VisionItem
                {
                    VisibleEntity = hit.Entity
                }); ;

                if (visionItems.Length >= capacity)
                {
                    break;
                }
            }
        }
    }
}
