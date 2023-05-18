using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIDisplayMonitorSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public bool enabled;

    }

}