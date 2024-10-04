using Unity.Collections;
using Unity.Entities;

public partial struct ActionChainDecisionSystem : ISystem
{
    private void OnCreate(ref SystemState state)
    {
    }

    private void OnDestroy(ref SystemState state)
    {
    }
    
    private void OnUpdate(ref SystemState state)
    {

    }
}

public partial struct ActionChainBufferJob : IJobEntity
{
    [ReadOnly] public BufferLookup<ActionChainItem> BufferLookup;

    public void Execute()
    {

    }
}
