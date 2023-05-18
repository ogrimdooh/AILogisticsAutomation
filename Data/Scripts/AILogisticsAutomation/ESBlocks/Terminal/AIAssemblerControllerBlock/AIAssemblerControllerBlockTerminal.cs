namespace AILogisticsAutomation
{
    public static class AIAssemblerControllerBlockTerminal
    {

        public static AIAssemblerControllerBlockTerminalController Controller { get; private set; } = new AIAssemblerControllerBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}