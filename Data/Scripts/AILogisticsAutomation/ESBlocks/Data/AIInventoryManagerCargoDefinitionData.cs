using VRage.ObjectBuilders;
using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIInventoryManagerCargoDefinitionData
    {

        [ProtoMember(1)]
        public int flags;

        [ProtoMember(2)]
        public long entityId;

        [ProtoMember(3)]
        public SerializableDefinitionId[] validIds = new SerializableDefinitionId[] { };

        [ProtoMember(4)]
        public string[] validTypes = new string[] { };

        [ProtoMember(5)]
        public SerializableDefinitionId[] ignoreIds = new SerializableDefinitionId[] { };

        [ProtoMember(6)]
        public string[] ignoreTypes = new string[] { };

    }

}