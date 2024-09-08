using ProtoBuf;
using VRageMath;

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


        [ProtoMember(6)]
        public Vector3I[] ignoreCargosPos = new Vector3I[] { };

        [ProtoMember(7)]
        public Vector3I[] ignoreFunctionalBlocksPos = new Vector3I[] { };

        [ProtoMember(8)]
        public Vector3I[] ignoreConnectorsPos = new Vector3I[] { };

    }

}