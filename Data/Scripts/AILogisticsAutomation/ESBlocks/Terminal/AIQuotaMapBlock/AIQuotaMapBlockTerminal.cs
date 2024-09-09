namespace AILogisticsAutomation
{
    public static class AIQuotaMapBlockTerminal
    {

        public static AIQuotaMapBlockTerminalController Controller { get; private set; } = new AIQuotaMapBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}