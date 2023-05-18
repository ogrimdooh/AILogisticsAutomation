namespace AILogisticsAutomation
{
    public static class AIDisplayMonitorBlockTerminal
    {

        public static AIDisplayMonitorBlockTerminalController Controller { get; private set; } = new AIDisplayMonitorBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}