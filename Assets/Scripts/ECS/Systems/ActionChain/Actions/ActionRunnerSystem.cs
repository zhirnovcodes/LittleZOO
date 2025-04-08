using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Zoo.Enums;

[BurstCompile]
[UpdateInGroup(typeof(BiologicalSystemGroup), OrderFirst = true)]
public partial struct ActionRunnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationConfigComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var ecbSystem = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        //var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var planetEntity = SystemAPI.GetSingletonEntity<PlanetComponent>();
        var planetTransform = SystemAPI.GetComponentRO<LocalTransform>(planetEntity);

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        var edibleLookup = SystemAPI.GetComponentLookup<EdibleComponent>();
        var sleepableLookup = SystemAPI.GetComponentLookup<SleepableComponent>();

        var deltaTime = SystemAPI.Time.DeltaTime;

        var idleHandle = new IdleActionJob
        {
            TransformLookup = transformLookup
        }.Schedule(state.Dependency);

        var eatingHandle = new EatingActionJob
        {
            DeltaTime = deltaTime,
            EdibleLookup = edibleLookup
        }.Schedule(idleHandle);

        var searchHandle = new SearchingActionJob
        {
            PlanetPosition = planetTransform.ValueRO.Position,
            PlanetScale = planetTransform.ValueRO.Scale,
            TransformLookup = transformLookup
        }.Schedule(eatingHandle);

        var movingHandle = new MovingToActionJob
        {
            TransformLookup = transformLookup
        }.Schedule(searchHandle);

        var sleepingHandle = new SleepingActionJob
        {
            DeltaTime = deltaTime,
            SleepableLookup = sleepableLookup
        }.Schedule(movingHandle);

        var dependency1 = JobHandle.CombineDependencies
            (idleHandle, eatingHandle, searchHandle);
        state.Dependency = JobHandle.CombineDependencies(dependency1, movingHandle, sleepingHandle);
    }


    public partial struct IdleActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(
            ref SubActionOutputComponent output,
            in NeedBasedSystemOutput needs,
            in IdleStateTag tag)
        {
            if (needs.Action == ActionTypes.Idle)
            {
                return;
            }

            if (needs.Advertiser != Entity.Null &&
                TransformLookup.TryGetComponent(needs.Advertiser, out var _) == false)
            {
                return;
            }
            
            // Found action
            output.Status = ActionStatus.Success;
        }
    }


    public partial struct MovingToActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(
            ref SubActionOutputComponent output,
            ref MovingInputComponent movingInput,
            in MovingOutputComponent movingOutput,
            in MovingSpeedComponent movingSpeed,
            in ActionInputComponent actionInput,
            in MovingToStateTag tag)
        {
            if (movingOutput.HasArivedToTarget)
            {
                output.Status = ActionStatus.Success;
                return;
            }

            if (TransformLookup.TryGetComponent(actionInput.Target, out var transform) == false)
            {
                output.Status = ActionStatus.Fail;
                return;
            }

            movingInput.TargetPosition = transform.Position;
            movingInput.TargetScale = transform.Scale;
            movingInput.Speed = (movingSpeed.SpeedRange.x + movingSpeed.SpeedRange.y) / 2f;
        }
    }

    public partial struct SearchingActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float3 PlanetPosition;
        public float PlanetScale;

        void Execute(
            ref SubActionOutputComponent output,
            ref MovingInputComponent movingInput,
            ref ActorRandomComponent random,
            in MovingOutputComponent movingOutput,
            in MovingSpeedComponent movingSpeed,
            in NeedBasedSystemOutput needs,
            in SearchingStateTag tag)
        {
            if (movingOutput.NoTargetSet)
            {
                movingInput.TargetPosition = GenerateTargetPosition(ref random);
                movingInput.TargetScale = 0;
                movingInput.Speed = (movingSpeed.SpeedRange.x + movingSpeed.SpeedRange.y) / 2f;
                return;
            }

            if (movingOutput.HasArivedToTarget)
            {
                output.Status = ActionStatus.Success;
            }


            if (needs.Action == ActionTypes.Search)
            {
                return;
            }

            if (needs.Advertiser != Entity.Null &&
                TransformLookup.TryGetComponent(needs.Advertiser, out var _) == false)
            {
                return;
            }

            // Found action
            output.Status = ActionStatus.Success;
        }

        // Helper method for generating target positions
        private float3 GenerateTargetPosition(ref ActorRandomComponent random)
        {
            float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
            return PlanetPosition + math.normalize(randomTarget) * PlanetScale / 2f;
        }
    }

    public partial struct SleepingActionJob : IJobEntity
    {
        public float DeltaTime;
        public ComponentLookup<SleepableComponent> SleepableLookup;

        void Execute(
            ref SubActionOutputComponent output,
            ref ActorNeedsComponent needs,
            in SleepingStateTag tag,
            in ActionInputComponent input)
        {
            if (SleepableLookup.TryGetComponent(input.Target, out var sleepable) == false)
            {
                output.Status = ActionStatus.Fail;
                return;
            }

            var energyIncrease = sleepable.EnergyIncreaseSpeed * DeltaTime;

            needs.SetEnergy(math.min(100, needs.Energy() + energyIncrease));

            if (needs.Energy() >= 100)
            {
                output.Status = ActionStatus.Success;
            }
        }
    }

    public partial struct EatingActionJob : IJobEntity
    {
        public float DeltaTime;
        public ComponentLookup<EdibleComponent> EdibleLookup;

        void Execute(
            ref SubActionOutputComponent output,
            ref ActorNeedsComponent needs,
            ref EatingStateTag tag,
            in ActionInputComponent input)
        {
            if (tag.BiteTimeElapsed < tag.BiteInterval)
            {
                tag.BiteTimeElapsed += DeltaTime;
                return;
            }

            tag.BiteTimeElapsed = 0;

            if (EdibleLookup.TryGetComponent(input.Target, out var _) == false)
            {
                output.Status = ActionStatus.Fail;
                return;
            }


            if (Bite(ref needs, input.Target, tag.BiteWholeness))
            {
                output.Status = ActionStatus.Success;
                return;
            }

            if (needs.Fullness() >= 100)
            {
                output.Status = ActionStatus.Success;
                return;
            }
        }

        private bool Bite(ref ActorNeedsComponent needs, Entity target, float wholeness)
        {
            var edibleComponent = EdibleLookup.GetRefRW(target);

            var oldWholeness = edibleComponent.ValueRO.Wholeness;
            var biteValue = math.min(wholeness, oldWholeness);

            var nutritiousAll = edibleComponent.ValueRO.Nutrition;

            var nutritiousValue = biteValue * nutritiousAll / 100f;

            needs.SetFullness(math.min(100, needs.Fullness() + nutritiousValue));
            edibleComponent.ValueRW.BitenPart += biteValue;

            return oldWholeness - biteValue <= 0;
        }
    }

    public partial struct RunningFromActionJob : IJobEntity
    {
        public float3 PlanetPosition;
        public float PlanetScale;

        void Execute(
            ref SubActionOutputComponent output,
            ref MovingInputComponent movingInput,
            ref ActorRandomComponent random,
            in MovingOutputComponent movingOutput,
            in MovingSpeedComponent movingSpeed,
            in RunningFromStateTag tag)
        {
            if (movingOutput.NoTargetSet)
            {
                movingInput.TargetPosition = GenerateTargetPosition(ref random);
                movingInput.TargetScale = 0;
                movingInput.Speed = (movingSpeed.SpeedRange.x + movingSpeed.SpeedRange.y) / 2f;
                return;
            }

            if (movingOutput.HasArivedToTarget)
            {
                output.Status = ActionStatus.Success;
            }
        }

        // Helper method for generating target positions
        private float3 GenerateTargetPosition(ref ActorRandomComponent random)
        {
            float3 randomTarget = random.Random.NextFloat3(new float3(-1, -1, -1), new float3(1, 1, 1));
            return PlanetPosition + math.normalize(randomTarget) * PlanetScale / 2f;
        }
    }
}
