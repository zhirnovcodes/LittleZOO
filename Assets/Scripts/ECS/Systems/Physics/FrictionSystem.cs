using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Zoo.Physics
{
    [BurstCompile]
    [UpdateInGroup(typeof(ZooPhysicsSystemGroup))]
    [UpdateAfter(typeof(GyroscopeSystem))]
    public partial struct FrictionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var gravityJob = new GravityJob
            {
                DeltaTime = deltaTime
            };

            state.Dependency = gravityJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GravityJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                ref PhysicsVelocity velocity,
                in LocalTransform transform,
                in GravityComponent gravity)
            {
                const float horizontalDrag = 100f;

                float3 up = math.mul(transform.Rotation, new float3(0, 1, 0));
                float dot = math.dot(velocity.Linear, up);
                float3 verticalVelocity = up * dot;
                float3 horizontalVelocity = velocity.Linear - verticalVelocity;

                // Apply horizontal drag
                horizontalVelocity = math.lerp(horizontalVelocity, float3.zero, math.clamp(horizontalDrag * DeltaTime, 0, 1));
                velocity.Linear = horizontalVelocity + verticalVelocity;
            }
        }
    }
}