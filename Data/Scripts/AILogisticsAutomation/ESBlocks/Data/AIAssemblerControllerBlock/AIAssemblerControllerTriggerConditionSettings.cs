using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIAssemblerControllerTriggerConditionSettings
    {

        public int QueryType { get; set; }
        public MyDefinitionId Id { get; set; }
        public int OperationType { get; set; }
        public float Value { get; set; }
        public int Index { get; set; }

        public AIAssemblerControllerTriggerConditionSettingsData GetData()
        {
            return new AIAssemblerControllerTriggerConditionSettingsData()
            {
                queryType = QueryType,
                id = Id,
                operationType = OperationType,
                value = Value,
                index = Index
            };
        }

    }

}