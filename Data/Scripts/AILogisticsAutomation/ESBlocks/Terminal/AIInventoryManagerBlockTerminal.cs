namespace AILogisticsAutomation
{
    public static class AIInventoryManagerBlockTerminal
    {

        public static AIInventoryManagerBlockTerminalController Controller { get; private set; } = new AIInventoryManagerBlockTerminalController();

        public static void InitializeControls()
        {
            Controller.InitializeControls();
        }

    }

}