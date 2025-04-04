namespace Zoo.Enums
{
    public enum ActionTypes : byte
    {
        Idle,
        Search,
        Eat,
        Sleep,
        Escape
    }

    public enum SubActionTypes
    {
        Idle,
        Search,
        MoveTo,
        RunFrom,
        Eat,
        Sleep
    }

    public enum ActionStatus
    {
        Running,
        Success, 
        Fail
    }
}