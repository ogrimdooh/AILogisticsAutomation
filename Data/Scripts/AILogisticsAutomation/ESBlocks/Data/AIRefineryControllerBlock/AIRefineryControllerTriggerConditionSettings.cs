using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIRefineryControllerTriggerConditionSettings
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
                id = new DocumentedDefinitionId(Id),
                operationType = OperationType,
                value = Value,
                index = Index
            };
        }

    }

}