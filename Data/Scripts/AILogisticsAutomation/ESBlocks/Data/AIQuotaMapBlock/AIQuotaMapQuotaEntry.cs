using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIQuotaMapQuotaEntry
    {

        public MyDefinitionId Id { get; set; }
        public float Value { get; set; }
        public int Index { get; set; }

        public AIQuotaMapQuotaEntryData GetData()
        {
            return new AIQuotaMapQuotaEntryData()
            {
                id = new DocumentedDefinitionId(Id),
                value = Value,
                index = Index
            };
        }

    }

}