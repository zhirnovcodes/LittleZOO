using Unity.Entities;

public struct EdibleComponent : IComponentData
{
    public float Wholeness;
    public float Nutrition;
    public float RadiusMax;
    public float MaxNutrition;
}
