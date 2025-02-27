namespace Zoo.Enums
{
    public enum ActionStates : byte
    {
        Created,
        Running,
        Succeded,
        Failed,
        CancellationRequested,
        Canceled
    }
}