using Unity.Entities;
using UnityEngine;

public class ActorNeedAuthoring : MonoBehaviour
{
    public float HungerTuningFactor;
    public float HungerDecayFactor;

    public class ActorNeedBaker : Baker<ActorNeedAuthoring>
    {
        public override void Bake(ActorNeedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ActorNeedsComponent
            {
                Fullness = 100,
                Energy = 100,
                HungerTuningFactor = authoring.HungerTuningFactor,
                EnergyTuningFactor = 1,
                HungerDecayFactor = authoring.HungerDecayFactor,
                EnergyDecayFactor = 1
            });
        }
    }
}
