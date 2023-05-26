using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIIgnoreMapSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public bool enabled;

        [ProtoMember(3)]
        public long[] ignoreCargos = new long[] { };

        [ProtoMember(4)]
        public long[] ignoreFunctionalBlocks = new long[] { };

        [ProtoMember(5)]
        public long[] ignoreConnectors = new long[] { };

    }

}