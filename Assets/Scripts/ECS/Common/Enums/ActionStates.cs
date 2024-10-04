namespace Zoo.Enums
{
    public static class ActionStates
    {
        public const byte Pending = 0;
        public const byte Running = 1;
        public const byte Succeded = 2;
        public const byte Failed = 3;
        public const byte CancellationRequested = 4;
        public const byte Canceled = 5;
    }
}