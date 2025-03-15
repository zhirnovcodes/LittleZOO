using Unity.Entities;
using UnityEngine;

public class ViewAuthoring : MonoBehaviour
{
    public class Baker : Baker<ViewAuthoring>
    {
        public override void Bake(ViewAuthoring authoring)
        {
            // TODO blob
            var sleepLength = 1f;
            var dieLength = 1f;

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new AnimationComponent());

            AddComponent(entity, new IdleAnimationTag());
            AddComponent(entity, new SleepingAnimationTag { Length = sleepLength });
            AddComponent(entity, new DyingAnimationTag { Length = dieLength });

            SetComponentEnabled<SleepingAnimationTag>(entity, false);
            SetComponentEnabled<DyingAnimationTag>(entity, false);
        }
    }
}
