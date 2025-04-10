using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(InteractionResolveSystemGroup))]
public partial struct BiteResolveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var edible = SystemAPI.GetComponentLookup<EdibleComponent>();

        new BiteResolveJob()
        {
            Ecb = ecb,
            EdibleLookup = edible
        }.Run();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    partial struct BiteResolveJob : IJobEntity
    {
        public EntityCommandBuffer Ecb;

        public ComponentLookup<EdibleComponent> EdibleLookup;

        void Execute
            (
                Entity entity,
                ref DynamicBuffer<BiteItem> buffer,
                ref ActorNeedsComponent needs
            )
        {
            if (buffer.Length <= 0)
            {
                return;
            }

            foreach (var bite in buffer)
            {

                var biteWholeness = bite.Wholeness;
                var target = bite.Target;

                if (EdibleLookup.TryGetComponent(target, out var edible) == false)
                {
                    continue;
                }

                biteWholeness = math.min(edible.Wholeness, biteWholeness);

                if (biteWholeness <= 0)
                {
                    continue;
                }

                var biteNutririon = edible.Nutrition * biteWholeness / 100f;
                needs.AddFullness(biteNutririon);
                var newWholeness = edible.Wholeness - biteWholeness;

                if (newWholeness <= 0)
                {
                    Ecb.DestroyEntity(target);
                    continue;
                }

                var newEdible = new EdibleComponent { 
                    Nutrition = edible.Nutrition, 
                    Wholeness = newWholeness,
                    NutritionRange = edible.NutritionRange
                };

                EdibleLookup[target] = newEdible;
            }

            buffer.Clear();
        }
    }
}
