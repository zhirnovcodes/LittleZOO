using Unity.Entities;
// Updated PlantReproductionComponent with elapsed time field
public struct PlantReproductionComponent : IComponentData
{
    public float FunctionFactor; // Each interval reproduction system of the grass checks - if random value overheads function value at this age - grass tries to reproduce
    public float FunctionHeight; // Reproduction function height
    public float Interval;
    public float ReproductionTimeElapsed; // Time elapsed since last reproduction attempt
}
