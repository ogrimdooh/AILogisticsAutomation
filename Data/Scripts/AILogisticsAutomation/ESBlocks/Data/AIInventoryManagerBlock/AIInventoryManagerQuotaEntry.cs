using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIInventoryManagerQuotaEntry
    {

        public MyDefinitionId Id { get; set; }
        public float Value { get; set; }
        public int Index { get; set; }

        public AIInventoryManagerQuotaEntryData GetData()
        {
            return new AIInventoryManagerQuotaEntryData()
            {
                id = new DocumentedDefinitionId(Id),
                value = Value,
                index = Index
            };
        }

    }

}