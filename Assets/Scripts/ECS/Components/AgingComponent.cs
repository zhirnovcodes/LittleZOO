using Unity.Entities;

public struct AgingComponent : IComponentData
{
    public float AgeElapsed; // Time elapsed in seconds
    public float AgingFunctionSpan; // factor of the aging function. Every frame systems checks if grass should die or not. If it passes check - this check is run second time, and if it passes the other time - grass dies
    public float AgingFunctionHeight; // height of the aging function
}
