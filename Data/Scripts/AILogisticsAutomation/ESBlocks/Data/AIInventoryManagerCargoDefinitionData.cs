using VRage.ObjectBuilders;
using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIInventoryManagerCargoDefinitionData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public SerializableDefinitionId[] validIds = new SerializableDefinitionId[] { };

        [ProtoMember(3)]
        public string[] validTypes = new string[] { };

        [ProtoMember(4)]
        public SerializableDefinitionId[] ignoreIds = new SerializableDefinitionId[] { };

        [ProtoMember(5)]
        public string[] ignoreTypes = new string[] { };

    }

}