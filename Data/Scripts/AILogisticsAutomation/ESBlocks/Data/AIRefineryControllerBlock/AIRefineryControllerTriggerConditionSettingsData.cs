using ProtoBuf;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIRefineryControllerTriggerConditionSettingsData
    {
        
        [ProtoMember(1)]
        public int queryType;

        [ProtoMember(2)]
        public SerializableDefinitionId id;

        [ProtoMember(3)]
        public int operationType;

        [ProtoMember(4)]
        public float value;

        [ProtoMember(5)]
        public int index;

    }

}