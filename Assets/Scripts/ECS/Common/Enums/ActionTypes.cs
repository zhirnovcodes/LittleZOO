namespace Zoo.Enums
{
    public enum ActionTypes : byte
    {
        Search,
        Eat,
        Sleep
    }

    public enum SubActionTypes
    {
        Explore,
        MoveTo,
        RunFrom,
        Eat,
        Sleep
    }

    public enum ActionStatus
    {
        Success, 
        Fail, 
        Running
    }
}