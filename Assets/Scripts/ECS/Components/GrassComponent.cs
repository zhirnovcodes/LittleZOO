using Unity.Entities;

public struct GrassComponent : IComponentData
{
    public float Wholeness;
    public float WholenessMax;
    public float RadiusMax;

    public float TimeSpan;
}
