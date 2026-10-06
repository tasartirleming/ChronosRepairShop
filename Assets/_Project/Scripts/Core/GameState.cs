namespace ChronosRepairShop
{
    public enum GameState { Placement, Running, Won, Failed }

    public enum FailReason
    {
        None,
        TimeLeak,   // ball fell out of the clock ("Zaman Sızıntısı")
        Stuck,      // ball stopped moving before the exit gate opened
        Darkness    // run timer expired, the universe went dark
    }

    public struct LevelResult
    {
        public bool Won;
        public FailReason Reason;
        public int Stars;
        public float TimeLeft;
    }
}
