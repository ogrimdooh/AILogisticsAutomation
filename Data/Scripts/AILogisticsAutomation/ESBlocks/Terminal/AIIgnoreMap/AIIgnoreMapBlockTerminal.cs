namespace AILogisticsAutomation
{
    public static class AIIgnoreMapBlockTerminal
    {

        public static AIIgnoreMapBlockTerminalController Controller { get; private set; } = new AIIgnoreMapBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}