using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIAssemblerControllerTriggerActionSettings
    {

        public MyDefinitionId Id { get; set; }
        public float Value { get; set; }
        public int Index { get; set; }

        public AIAssemblerControllerTriggerActionSettingsData GetData()
        {
            return new AIAssemblerControllerTriggerActionSettingsData()
            {
                id = Id,
                value = Value,
                index = Index
            };
        }

    }

}