using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIInventoryManagerCargoDefinitionData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public DocumentedDefinitionId[] validIds = new DocumentedDefinitionId[] { };

        [ProtoMember(3)]
        public string[] validTypes = new string[] { };

        [ProtoMember(4)]
        public DocumentedDefinitionId[] ignoreIds = new DocumentedDefinitionId[] { };

        [ProtoMember(5)]
        public string[] ignoreTypes = new string[] { };

        [ProtoMember(6)]
        public Vector3I position;

    }

}