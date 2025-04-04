using Unity.Entities;

public struct EdibleComponent : IComponentData
{
    public float Wholeness;
    public float Nutrition;
    public float SizeMax;
    public float MaxNutrition;
    public float BitenPart;
}

public struct SleepableComponent : IComponentData
{
    public float EnergyIncreaseSpeed;
}
