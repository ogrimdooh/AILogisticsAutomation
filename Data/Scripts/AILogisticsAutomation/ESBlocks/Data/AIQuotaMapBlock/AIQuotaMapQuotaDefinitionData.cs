using ProtoBuf;
using VRageMath;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIQuotaMapQuotaDefinitionData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public Vector3I position;
        
        [ProtoMember(3)]
        public AIQuotaMapQuotaEntryData[] entries = new AIQuotaMapQuotaEntryData[] { };

    }

}