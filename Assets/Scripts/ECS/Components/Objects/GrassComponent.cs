using Unity.Entities;


public struct GrassComponent : IComponentData
{
    public float AgeElapsed; // Time elapsed in seconds
    public float AgeElapsedLast; // Time elapsed in seconds - previous iteration of system

    public float MaxSize;
    public float MaxWholeness;
    public float MaxNutrition;
    public float MaxHeight;

    public float AgingFunctionSpan; // factor of the aging function. Every frame systems checks if grass should die or not. If it passes check - this check is run second time, and if it passes the other time - grass dies
    public float AgingFunctionHeight; // height of the aging function
}

public struct GrassReproductionComponent : IComponentData
{
    public float FunctionFactor; // Each interval reproduction system of the grass checks - if random value overheads function value at this age - grass tries to reproduce
    public float FunctionHeight; // Reproduction function height
    public float Interval;
}
