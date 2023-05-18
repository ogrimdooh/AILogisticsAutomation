using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public bool enabled;

    }

}