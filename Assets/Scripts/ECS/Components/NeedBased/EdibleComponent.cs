using Unity.Entities;
using Unity.Mathematics;

public struct EdibleComponent : IComponentData
{
    public float2 NutritionRange;
    public float Nutrition;
    public float Wholeness;
}

public struct SleepableComponent : IComponentData
{
    public float EnergyIncreaseSpeed;
}
