namespace AILogisticsAutomation
{
    public static class AIRefineryControllerBlockTerminal
    {

        public static AIRefineryControllerBlockTerminalController Controller { get; private set; } = new AIRefineryControllerBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}