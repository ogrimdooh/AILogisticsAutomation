using ProtoBuf;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerAssemblerSettingsData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public SerializableDefinitionId[] priority = new SerializableDefinitionId[] { };
        
    }

}