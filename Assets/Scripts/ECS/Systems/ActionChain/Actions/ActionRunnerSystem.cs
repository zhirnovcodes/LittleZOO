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

        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var sleepableLookup = SystemAPI.GetComponentLookup<SleepableComponent>(true);

        var deltaTime = SystemAPI.Time.DeltaTime;

        var idleHandle = new IdleActionJob
        {
            TransformLookup = transformLookup
        }.Schedule(state.Dependency);

        var eatingHandle = new EatingActionJob
        {
            DeltaTime = deltaTime,
            TransformLookup = transformLookup
        }.Schedule(idleHandle);

        var searchHandle = new SearchingActionJob
        {
            PlanetPosition = planetTransform.ValueRO.Position,
            PlanetScale = planetTransform.ValueRO.Scale,
            TransformLookup = transformLookup
        }.Schedule(eatingHandle);

        var movingToHandle = new MovingToActionJob
        {
            TransformLookup = transformLookup
        }.Schedule(searchHandle);

        var movingIntoHandle = new MovingIntoActionJob
        {
            TransformLookup = transformLookup
        }.Schedule(movingToHandle);

        var sleepingHandle = new SleepingActionJob
        {
            DeltaTime = deltaTime,
            SleepableLookup = sleepableLookup
        }.Schedule(movingIntoHandle);

        var runningHandle = new RunningFromActionJob
        {
            PlanetPosition = planetTransform.ValueRO.Position,
            PlanetScale = planetTransform.ValueRO.Scale,
            TransformLookup = transformLookup
        }.Schedule(sleepingHandle);

        var dependency1 = JobHandle.CombineDependencies
            (idleHandle, eatingHandle, searchHandle);
        var dependency2 = JobHandle.CombineDependencies
            (movingToHandle, sleepingHandle, runningHandle);
        var dependency3 = movingIntoHandle;
        state.Dependency = JobHandle.CombineDependencies( dependency1, dependency2, dependency3);
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


    public partial struct MovingIntoActionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(
            ref SubActionOutputComponent output,
            ref MovingInputComponent movingInput,
            in MovingOutputComponent movingOutput,
            in MovingSpeedComponent movingSpeed,
            in ActionInputComponent actionInput,
            in MovingIntoStateTag tag)
        {
            const float stopTime = 10f;
            if (actionInput.TimeElapsed >= stopTime)
            {
                output.Status = ActionStatus.Fail;
                return;
            }

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
            movingInput.TargetScale = 0;
            movingInput.Speed = (movingSpeed.SpeedRange.x + movingSpeed.SpeedRange.y) / 2f;
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
            const float stopTime = 10f;
            if (actionInput.TimeElapsed >= stopTime)
            {
                output.Status = ActionStatus.Fail;
                return;
            }

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
                movingInput.TargetPosition = GenerateTargetPosition(ref random);
                return;
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
        [ReadOnly] public ComponentLookup<SleepableComponent> SleepableLookup;

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

            needs.AddEnergy(energyIncrease);

            if (needs.Energy() >= 100)
            {
                output.Status = ActionStatus.Success;
            }
        }
    }

    public partial struct EatingActionJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute(
            Entity actor,
            ref SubActionOutputComponent output,
            ref ActorNeedsComponent needs,
            ref EatingStateTag tag,
            ref DynamicBuffer<BiteItem> biteBuffer,
            in ActionInputComponent input)
        {
            if (TransformLookup.HasComponent(input.Target) == false)
            {
                output.Status = ActionStatus.Success;
                return;
            }

            if (tag.BiteTimeElapsed < tag.BiteInterval)
            {
                tag.BiteTimeElapsed += DeltaTime;
                return;
            }

            tag.BiteTimeElapsed = 0;

            if (needs.Fullness() >= 100)
            {
                output.Status = ActionStatus.Success;
                return;
            }

            biteBuffer.Add(new BiteItem { Target = input.Target, Wholeness = tag.BiteWholeness });
        }
    }

    public partial struct RunningFromActionJob : IJobEntity
    {
        public float3 PlanetPosition;
        public float PlanetScale;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        void Execute
            (
            Entity actor,
            ref SubActionOutputComponent output,
            ref MovingInputComponent movingInput,
            ref ActorRandomComponent random,
            in MovingOutputComponent movingOutput,
            in ActionInputComponent input,
            in MovingSpeedComponent movingSpeed,
            in ActorNeedsComponent needs,
            in DynamicBuffer<VisionItem> vision,
            in RunningFromStateTag tag
            )
        {
            if (TransformLookup.TryGetComponent(input.Target, out var targetTransform) == false)
            {
                output.Status = ActionStatus.Success;
                return;
            }

            if (movingOutput.NoTargetSet)
            {
                movingInput.TargetPosition = GenerateTargetPosition(ref random, actor, targetTransform);
                movingInput.TargetScale = 0;
                movingInput.Speed = movingSpeed.SpeedRange.y;
                return;
            }

            if (IsSafe(needs) || (SeeTarget(vision, input.Target) == false))
            {
                output.Status = ActionStatus.Success;
                return;
            }

            if (movingOutput.HasArivedToTarget)
            {
                movingInput.TargetPosition = GenerateTargetPosition(ref random, actor, targetTransform);
                movingInput.TargetScale = 0;
                movingInput.Speed = movingSpeed.SpeedRange.y;
                return;
            }
        }

        private bool IsSafe(ActorNeedsComponent needs)
        {
            return needs.Safety() >= 100;
        }

        private bool SeeTarget(DynamicBuffer<VisionItem> vision, Entity target)
        {
            foreach (var visible in vision)
            {
                if (target == visible.VisibleEntity)
                {
                    return true;
                }
            }

            return false;
        }

        // Helper method for generating target positions
        private float3 GenerateTargetPosition(ref ActorRandomComponent random, Entity actor, LocalTransform targetTransform)
        {
            var actorTransform = TransformLookup[actor];

            var direction = math.normalize(actorTransform.Position - targetTransform.Position);

            // TODO to config
            var distance = random.Random.NextFloat(1f, 5f);
            var destinationOuter = direction * distance + actorTransform.Position;

            var destinationOnPlanet = math.normalize(destinationOuter - PlanetPosition) * PlanetScale / 2f;

            return destinationOnPlanet + PlanetPosition;
        }
    }
}
